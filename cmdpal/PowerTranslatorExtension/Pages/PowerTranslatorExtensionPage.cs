// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslatorExtension.History;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Suggest;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension;

internal sealed partial class PowerTranslatorExtensionPage : DynamicListPage, IDisposable
{
    private readonly TranslateHelper translateHelper;
    private readonly SuggestHelper suggestHelper;
    private readonly HistoryPage historyPage;
    private readonly LanguageListPage languageListPage;
    private CancellationTokenSource? searchCts;

    // Results computed by the latest background query, returned synchronously by
    // GetItems(). GetItems() runs on the CmdPal host's COM thread and must never
    // block, so all network and clipboard work happens off-thread and lands here.
    private volatile IListItem[] cachedItems = Array.Empty<IListItem>();

    public PowerTranslatorExtensionPage(TranslateHelper translateHelper, SuggestHelper suggestHelper)
    {
        Icon = IconHelpers.FromRelativePaths("Assets/translator.light.png", "Assets/translator.dark.png");
        Title = Loc.Get("Cmd_Translator_DisplayName");
        Name = Loc.Get("Verb_Open");
        PlaceholderText = Loc.Get("Page_Translator_Placeholder");
        this.translateHelper = translateHelper;
        this.suggestHelper = suggestHelper;
        this.historyPage = new HistoryPage(translateHelper);
        this.languageListPage = new LanguageListPage();
        this.EmptyContent = new CommandItem
        {
            Title = PlaceholderText,
            Icon = this.Icon,
        };

        // Show the static menu instantly on open, then refresh in the background
        // with any translatable clipboard content (without blocking the host).
        this.cachedItems = BuildStaticEmptyItems();
        PerformSearch(string.Empty);
    }

    // Returns the most recently computed results immediately. PerformSearch does
    // the actual work off-thread; this only ever hands back the cached array.
    public override IListItem[] GetItems() => cachedItems;

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (!string.Equals(oldSearch, newSearch, StringComparison.Ordinal))
            PerformSearch(newSearch);
    }

    private void PerformSearch(string search)
    {
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref searchCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        var cancellationToken = newCts.Token;
        IsLoading = true;

        _ = Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                Action? onPublished = null;
                var items = search.Length == 0
                    ? BuildEmptyQueryItems(cancellationToken)
                    : BuildTranslationItems(search, cancellationToken, out onPublished);

                cancellationToken.ThrowIfCancellationRequested();
                cachedItems = items;
                RaiseItemsChanged(items.Length);
                onPublished?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // A newer search owns the result list now.
            }
            catch (Exception ex)
            {
                UtilsFun.LogMessage($"Translate query failed: {ex.Message}");
            }
            finally
            {
                if (ReferenceEquals(Volatile.Read(ref searchCts), newCts))
                    IsLoading = false;
            }
        }, cancellationToken);
    }

    private IListItem[] BuildTranslationItems(string search, CancellationToken cancellationToken, out Action? onPublished)
    {
        onPublished = null;
        var settings = SettingsManager.Instance;
        var res = new List<ResultItem>();

        Task<List<ResultItem>>? suggestTask = null;
        if (settings.EnableSuggest)
        {
            suggestTask = Task.Run(() => suggestHelper.QuerySuggest(search));
        }

        Task<List<ResultItem>>? secondTask = null;
        if (settings.EnableSecondLanguage)
        {
            var secondLang = settings.SecondLanguageKey;
            secondTask = Task.Run(() => translateHelper.QueryTranslate(search, toLanguage: secondLang));
        }

        res.AddRange(translateHelper.QueryTranslate(search));
        cancellationToken.ThrowIfCancellationRequested();

        if (secondTask != null)
        {
            var secondRes = secondTask.GetAwaiter().GetResult();
            cancellationToken.ThrowIfCancellationRequested();
            if (secondRes.Count > 0)
            {
                var first = secondRes[0];
                first.SubTitle = $"{first.SubTitle} {Loc.Get("Tag_SecondLanguage")}";
                if (res.Count > 1)
                    res.Insert(1, first);
                else
                    res.Add(first);
            }
        }

        if (suggestTask != null)
        {
            res.AddRange(suggestTask.GetAwaiter().GetResult());
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (settings.ShowOriginalQuery)
        {
            res.Add(new ResultItem { Title = search, SubTitle = Loc.Get("Tag_QueryRaw") });
        }

        var firstWithApi = res.FirstOrDefault(r => r.fromApiName != null);
        var autoReadText = settings.EnableAutoRead && res.Count > 0 ? res[0].Title : null;
        if (autoReadText != null || firstWithApi != null)
        {
            onPublished = () =>
            {
                if (autoReadText != null)
                    translateHelper.Read(autoReadText);

                if (firstWithApi != null)
                {
                    HistoryHelper.Instance.Push(new ResultItem
                    {
                        Title = firstWithApi.Title,
                        SubTitle = search,
                    });
                }
            };
        }

        return res.ToResultList(this.Icon, translateHelper).ToArray<IListItem>();
    }

    private IListItem[] BuildEmptyQueryItems(CancellationToken cancellationToken)
    {
        var items = new List<IListItem>();
        var clipboard = UtilsFun.GetClipboardText();
        if (UtilsFun.WhetherTranslate(clipboard))
        {
            var clipRes = translateHelper.QueryTranslate(clipboard!);
            cancellationToken.ThrowIfCancellationRequested();
            items.AddRange(clipRes.ToResultList(this.Icon, translateHelper).Cast<IListItem>());
        }

        items.AddRange(BuildStaticEmptyItems());
        return items.ToArray();
    }

    // The network-free entries shown for an empty query. Used directly as the
    // instant initial view before the clipboard has been read in the background.
    private IListItem[] BuildStaticEmptyItems()
    {
        return new IListItem[]
        {
            new ListItem(historyPage)
            {
                Title = Loc.Get("Empty_History_Title"),
                Subtitle = Loc.Get("Empty_History_Subtitle"),
                Icon = this.Icon,
            },
            new ListItem(languageListPage)
            {
                Title = Loc.Get("Empty_Languages_Title"),
                Subtitle = Loc.Get("Empty_Languages_Subtitle"),
                Icon = this.Icon,
            },
            new ListItem(new OpenUrlCommand("https://github.com/N0I0C0K/PowerTranslator/issues?q="))
            {
                Title = Loc.Get("Empty_Help_Title"),
                Subtitle = Loc.Get("Empty_Help_Subtitle"),
                Icon = this.Icon,
            },
        };
    }

    public void Dispose()
    {
        var cts = Interlocked.Exchange(ref searchCts, null);
        cts?.Cancel();
        cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
