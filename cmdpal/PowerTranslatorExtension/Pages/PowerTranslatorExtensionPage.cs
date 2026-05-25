// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslatorExtension;
using PowerTranslatorExtension.History;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Suggest;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension;

internal sealed partial class PowerTranslatorExtensionPage : DynamicListPage
{
    private readonly object delayLock = new();
    private long lastQueryTick;
    private readonly TranslateHelper translateHelper;
    private readonly SuggestHelper suggestHelper;
    private readonly HistoryPage historyPage;
    private readonly LanguageListPage languageListPage;

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
    }

    public override IListItem[] GetItems()
    {
        var search = this.SearchText ?? string.Empty;

        if (search.Length == 0)
        {
            return BuildEmptyQueryItems();
        }

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

        if (secondTask != null)
        {
            var secondRes = secondTask.GetAwaiter().GetResult();
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
        }

        if (settings.ShowOriginalQuery)
        {
            res.Add(new ResultItem { Title = search, SubTitle = Loc.Get("Tag_QueryRaw") });
        }

        if (settings.EnableAutoRead && res.Count > 0)
        {
            translateHelper.Read(res[0].Title);
        }

        var firstWithApi = res.FirstOrDefault(r => r.fromApiName != null);
        if (firstWithApi != null)
        {
            HistoryHelper.Instance.Push(new ResultItem
            {
                Title = firstWithApi.Title,
                SubTitle = search,
            });
        }

        return res.ToResultList(this.Icon, translateHelper).ToArray<IListItem>();
    }

    private IListItem[] BuildEmptyQueryItems()
    {
        var items = new List<IListItem>();
        var clipboard = UtilsFun.GetClipboardText();
        if (UtilsFun.WhetherTranslate(clipboard))
        {
            var clipRes = translateHelper.QueryTranslate(clipboard!);
            items.AddRange(clipRes.ToResultList(this.Icon, translateHelper).Cast<IListItem>());
        }

        items.Add(new ListItem(historyPage)
        {
            Title = Loc.Get("Empty_History_Title"),
            Subtitle = Loc.Get("Empty_History_Subtitle"),
            Icon = this.Icon,
        });
        items.Add(new ListItem(languageListPage)
        {
            Title = Loc.Get("Empty_Languages_Title"),
            Subtitle = Loc.Get("Empty_Languages_Subtitle"),
            Icon = this.Icon,
        });
        items.Add(new ListItem(new OpenUrlCommand("https://github.com/N0I0C0K/PowerTranslator/issues?q="))
        {
            Title = Loc.Get("Empty_Help_Title"),
            Subtitle = Loc.Get("Empty_Help_Subtitle"),
            Icon = this.Icon,
        });
        return items.ToArray();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        Task.Factory.StartNew(() =>
        {
            long thisTick;
            lock (delayLock)
            {
                thisTick = ++lastQueryTick;
            }
            Task.Delay(500).GetAwaiter().GetResult();
            if (thisTick != lastQueryTick)
                return;
            RaiseItemsChanged(0);
        });
    }
}
