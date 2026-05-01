using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension;

public sealed class SettingHelper
{
    public const string DefaultLanguageKeySetting = "DefaultTargetLanguage";
    public const string EnableSuggestSetting = "EnableSuggest";
    public const string EnableAutoReadSetting = "EnableAutoRead";
    public const string ShowOriginalQuerySetting = "ShowOriginalQuery";
    public const string EnableJumpDictionarySetting = "EnableJumpDictionary";
    public const string SecondTargetLanguageSetting = "SecondTargetLanguage";
    public const string UseSystemProxySetting = "UseSystemProxy";
    public const string EnableCodeModeSetting = "EnableCodeMode";

    public static readonly IReadOnlyDictionary<string, string> Languages = new Dictionary<string, string>
    {
        { "auto", "auto" },
        { "zhs", "Chinese (Simplified)" },
        { "zht", "Chinese (Traditional)" },
        { "en", "English" },
        { "ja", "Japanese" },
        { "ko", "Korean" },
        { "ru", "Russian" },
        { "fr", "French" },
        { "es", "Spanish" },
        { "ar", "Arabic" },
        { "de", "German" },
        { "it", "Italian" },
        { "he", "Hebrew" },
    };

    public static readonly IReadOnlyDictionary<string, string> DictionaryUrlPatterns = new Dictionary<string, string>
    {
        { "Youdao", "https://www.youdao.com/result?word={0}&lang=en" },
        { "Oxford", "https://www.oed.com/search/dictionary/?scope=Entries&q={0}" },
        { "Cambridge", "https://dictionary.cambridge.org/us/dictionary/english/{0}" },
    };

    public static readonly IReadOnlyList<ResultItem> languageList = Languages
        .Select(val => new ResultItem { Title = val.Key, SubTitle = val.Value, CopyTgt = val.Key })
        .ToList();

    private static readonly IReadOnlyList<ChoiceSetSetting.Choice> LanguageChoices = Languages
        .Select(val => new ChoiceSetSetting.Choice(val.Value, val.Key))
        .ToList();

    private static readonly IReadOnlyList<ChoiceSetSetting.Choice> DictionaryChoices = DictionaryUrlPatterns
        .Select(val => new ChoiceSetSetting.Choice(val.Key, val.Value))
        .ToList();

    private readonly ChoiceSetSetting _defaultTargetLanguage;
    private readonly ToggleSetting _enableSuggest;
    private readonly ToggleSetting _enableAutoRead;
    private readonly ToggleSetting _showOriginalQuery;
    private readonly ToggleSetting _enableJumpDictionary;
    private readonly ChoiceSetSetting _dictionaryPattern;
    private readonly ToggleSetting _enableSecondLanguage;
    private readonly ChoiceSetSetting _secondTargetLanguage;
    private readonly ToggleSetting _useSystemProxy;
    private readonly ToggleSetting _enableCodeMode;

    public static SettingHelper Instance { get; } = new();

    public Settings Settings { get; }
    public string defaultLanguageKey { get; private set; } = "auto";
    public bool enableSuggest { get; private set; } = true;
    public bool enableAutoRead { get; private set; }
    public bool enableSecondLanguage { get; private set; }
    public string secondLanguageKey { get; private set; } = "auto";
    public bool showOriginalQuery { get; private set; }
    public bool enableJumpToDict { get; private set; }
    public string dictUtlPattern { get; private set; } = DictionaryUrlPatterns.First().Value;
    public bool useSystemProxy { get; private set; } = true;
    public bool enableCodeMode { get; private set; } = true;

    private SettingHelper()
    {
        Settings = new Settings();

        _defaultTargetLanguage = new ChoiceSetSetting(DefaultLanguageKeySetting, LanguageChoices.ToList())
        {
            Label = "Default target language",
            Description = "Default translation target language, default is auto",
            Value = defaultLanguageKey,
        };
        _enableSuggest = new ToggleSetting(EnableSuggestSetting, true)
        {
            Label = "Enable search suggest",
            Description = "Show Baidu search suggestions while typing",
        };
        _enableAutoRead = new ToggleSetting(EnableAutoReadSetting, false)
        {
            Label = "Automatic reading result",
            Description = "Automatically read the first translation result",
        };
        _showOriginalQuery = new ToggleSetting(ShowOriginalQuerySetting, false)
        {
            Label = "Display original query",
            Description = "Add the raw query to the result list",
        };
        _enableJumpDictionary = new ToggleSetting(EnableJumpDictionarySetting, false)
        {
            Label = "Show jump to dictionary button",
            Description = "Expose a context command for quickly opening a dictionary",
        };
        _dictionaryPattern = new ChoiceSetSetting("DictionaryUrlPattern", DictionaryChoices.ToList())
        {
            Label = "Dictionary site",
            Description = "Dictionary used by the jump command",
            Value = dictUtlPattern,
        };
        _enableSecondLanguage = new ToggleSetting(SecondTargetLanguageSetting, false)
        {
            Label = "Second target language",
            Description = "Display a second translation result after the main target",
        };
        _secondTargetLanguage = new ChoiceSetSetting(SecondTargetLanguageSetting, LanguageChoices.ToList())
        {
            Label = "Second target language code",
            Description = "Language used by the second translation result",
            Value = secondLanguageKey,
        };
        _useSystemProxy = new ToggleSetting(UseSystemProxySetting, true)
        {
            Label = "Use system default proxy",
            Description = "Use the system proxy for requests",
        };
        _enableCodeMode = new ToggleSetting(EnableCodeModeSetting, true)
        {
            Label = "Translate snake_case or camelCase words",
            Description = "Split identifiers before translating",
        };

        Settings.Add(_defaultTargetLanguage);
        Settings.Add(_enableSuggest);
        Settings.Add(_enableAutoRead);
        Settings.Add(_showOriginalQuery);
        Settings.Add(_enableJumpDictionary);
        Settings.Add(_dictionaryPattern);
        Settings.Add(_enableSecondLanguage);
        Settings.Add(_secondTargetLanguage);
        Settings.Add(_useSystemProxy);
        Settings.Add(_enableCodeMode);

        Settings.SettingsChanged += (_, _) => Apply();
        Apply();
    }

    public IReadOnlyList<ResultItem> GetHelpInfoList()
    {
        return
        [
            new ResultItem
            {
                Title = "History",
                SubTitle = "Type `h` to view history",
                TextToSuggest = "h",
                CopyTgt = "h",
            },
            new ResultItem
            {
                Title = "Support languages",
                SubTitle = "Type `l` to view supported languages and their short codes",
                TextToSuggest = "l",
                CopyTgt = "l",
            },
            new ResultItem
            {
                Title = "Find help",
                SubTitle = "Open the issue page",
                Action = () => UtilsFun.OpenInShell("https://github.com/N0I0C0K/PowerTranslator/issues?q="),
            },
        ];
    }

    private void Apply()
    {
        defaultLanguageKey = CoalesceLanguage(_defaultTargetLanguage.Value);
        enableSuggest = _enableSuggest.Value;
        enableAutoRead = _enableAutoRead.Value;
        showOriginalQuery = _showOriginalQuery.Value;
        enableJumpToDict = _enableJumpDictionary.Value;
        dictUtlPattern = CoalesceDictionaryPattern(_dictionaryPattern.Value);
        enableSecondLanguage = _enableSecondLanguage.Value;
        secondLanguageKey = CoalesceLanguage(_secondTargetLanguage.Value);
        enableCodeMode = _enableCodeMode.Value;

        var nextUseProxy = _useSystemProxy.Value;
        if (nextUseProxy != useSystemProxy)
        {
            UtilsFun.ChangeDefaultHttpHandlerProxy(nextUseProxy);
        }

        useSystemProxy = nextUseProxy;
    }

    private static string CoalesceLanguage(string? key)
    {
        if (!string.IsNullOrWhiteSpace(key) && Languages.ContainsKey(key))
        {
            return key;
        }

        return "auto";
    }

    private static string CoalesceDictionaryPattern(string? key)
    {
        if (!string.IsNullOrWhiteSpace(key) && DictionaryUrlPatterns.Values.Any(value => value == key))
        {
            return key;
        }

        return DictionaryUrlPatterns.First().Value;
    }
}
