using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension;

internal sealed partial class PowerTranslatorExtensionPage : DynamicListPage
{
    private readonly object _delayLock = new();
    private readonly TranslateHelper _translateHelper;
    private readonly SettingHelper _settingHelper;
    private readonly SuggestHelper _suggestHelper;
    private readonly HistoryHelper _historyHelper;
    private long _lastQueryTick;

    public PowerTranslatorExtensionPage(TranslateHelper translateHelper, SettingHelper settingHelper)
    {
        Icon = IconHelpers.FromRelativePaths("Assets/translator.light.png", "Assets/translator.dark.png");
        Title = "Translator";
        Name = "Open";
        PlaceholderText = "Enter any text...";
        _translateHelper = translateHelper;
        _settingHelper = settingHelper;
        _suggestHelper = new SuggestHelper();
        _historyHelper = new HistoryHelper();
        EmptyContent = new CommandItem { Title = PlaceholderText, Icon = Icon };
        _settingHelper.Settings.SettingsChanged += (_, _) =>
        {
            _suggestHelper.Reload();
            _translateHelper.Reload();
            RaiseItemsChanged(0);
        };
    }

    public override IListItem[] GetItems()
    {
        var querySearch = SearchText?.Trim() ?? string.Empty;
        List<ResultItem> res = [];

        if (querySearch.Length == 0)
        {
            string? clipboardText = UtilsFun.GetClipboardText();
            if (UtilsFun.WhetherTranslate(clipboardText))
            {
                res.AddRange(_translateHelper.QueryTranslate(clipboardText!, "clipboard"));
            }

            res.AddRange(_settingHelper.GetHelpInfoList());
            return res.ToResultList(Icon, _settingHelper, ShouldCopyOnlyFirstOption(clipboardText)).ToArray();
        }

        if (querySearch == "h")
        {
            return _historyHelper.Query().Reverse().ToResultList(Icon, _settingHelper).ToArray();
        }

        if (querySearch == "l")
        {
            return SettingHelper.languageList.ToResultList(Icon, _settingHelper).ToArray();
        }

        Task<List<ResultItem>>? suggestTask = null;
        if (_settingHelper.enableSuggest)
        {
            suggestTask = Task.Run(() => _suggestHelper.QuerySuggest(querySearch));
        }

        Task<List<ResultItem>>? secondTranslateTask = null;
        if (_settingHelper.enableSecondLanguage && !string.IsNullOrWhiteSpace(_settingHelper.secondLanguageKey))
        {
            secondTranslateTask = Task.Run(() => _translateHelper.QueryTranslate(querySearch, toLanguage: _settingHelper.secondLanguageKey));
        }

        res.AddRange(_translateHelper.QueryTranslate(querySearch));

        if (secondTranslateTask != null)
        {
            var secondRes = secondTranslateTask.GetAwaiter().GetResult();
            if (secondRes.Count > 0)
            {
                var secondItem = secondRes[0];
                secondItem.SubTitle = $"{secondItem.SubTitle} [second language]";
                res.Insert(1, secondItem);
            }
        }

        if (suggestTask != null)
        {
            res.AddRange(suggestTask.GetAwaiter().GetResult());
        }

        if (_settingHelper.showOriginalQuery)
        {
            res.Add(new ResultItem
            {
                Title = querySearch,
                SubTitle = "[query raw]",
                CopyTgt = querySearch,
            });
        }

        if (_settingHelper.enableAutoRead)
        {
            _translateHelper.Read(res.FirstOrDefault()?.Title);
        }

        var first = res.FirstOrDefault(val => val.fromApiName != null);
        if (first != null)
        {
            _historyHelper.Push(new ResultItem
            {
                Title = first.Title,
                SubTitle = querySearch,
            });
        }

        return res.ToResultList(Icon, _settingHelper, ShouldCopyOnlyFirstOption(querySearch)).ToArray();
    }

    private static bool ShouldCopyOnlyFirstOption(string? text)
    {
        return !string.IsNullOrWhiteSpace(text) && !text.Contains(';') && !text.Contains('；');
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        Task.Factory.StartNew(() =>
        {
            long thisTick;
            lock (_delayLock)
            {
                thisTick = ++_lastQueryTick;
            }

            Task.Delay(500).GetAwaiter().GetResult();
            if (thisTick != _lastQueryTick)
            {
                return;
            }

            RaiseItemsChanged(0);
        });
    }
}
