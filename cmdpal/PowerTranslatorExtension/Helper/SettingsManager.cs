using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension;

public class SettingsManager : JsonSettingsManager
{
    private const string _namespace = "translator";

    public static readonly Dictionary<string, string> Languages = new()
    {
        { "auto", "Auto" },
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

    public static readonly Dictionary<string, string> DictionaryUrlPatterns = new()
    {
        { "Youdao", "https://www.youdao.com/result?word={0}&lang=en" },
        { "Oxford", "https://www.oed.com/search/dictionary/?scope=Entries&q={0}" },
        { "Cambridge", "https://dictionary.cambridge.org/us/dictionary/english/{0}" },
    };

    private static string Namespaced(string property) => $"{_namespace}.{property}";

    private static List<ChoiceSetSetting.Choice> LanguageChoices() =>
        Languages.Select(kv => new ChoiceSetSetting.Choice(kv.Value, kv.Key)).ToList();

    private static List<ChoiceSetSetting.Choice> DictionaryChoices() =>
        DictionaryUrlPatterns.Select(kv => new ChoiceSetSetting.Choice(kv.Key, kv.Key)).ToList();

    private readonly ChoiceSetSetting _defaultTargetLanguage = new(
        Namespaced("DefaultTargetLanguage"),
        "Default target language",
        "Used when the query does not contain `->`",
        LanguageChoices());

    private readonly ToggleSetting _enableSuggest = new(
        Namespaced("EnableSuggest"),
        "Enable search suggestions",
        "Append Baidu autocomplete suggestions after translation results",
        true);

    private readonly ToggleSetting _enableAutoRead = new(
        Namespaced("EnableAutoRead"),
        "Automatically read result",
        "Play TTS for the first result after each query",
        false);

    private readonly ToggleSetting _showOriginalQuery = new(
        Namespaced("ShowOriginalQuery"),
        "Show original query",
        "Append the raw query as the final result row",
        false);

    private readonly ToggleSetting _enableJumpDictionary = new(
        Namespaced("EnableJumpDictionary"),
        "Show jump to dictionary command",
        "Adds a per-result command that opens the selected word in an online dictionary",
        false);

    private readonly ChoiceSetSetting _dictionaryPattern = new(
        Namespaced("DictionaryPattern"),
        "Dictionary",
        "Which dictionary to jump to when the command above is enabled",
        DictionaryChoices());

    private readonly ToggleSetting _enableSecondLanguage = new(
        Namespaced("EnableSecondLanguage"),
        "Enable second target language",
        "Show an additional translation row for the language selected below",
        false);

    private readonly ChoiceSetSetting _secondTargetLanguage = new(
        Namespaced("SecondTargetLanguage"),
        "Second target language",
        "Only used when 'Enable second target language' is on",
        LanguageChoices());

    private readonly ToggleSetting _useSystemProxy = new(
        Namespaced("UseSystemProxy"),
        "Use system proxy",
        "Route HTTP requests through the Windows default proxy",
        true);

    private readonly ToggleSetting _enableCodeMode = new(
        Namespaced("EnableCodeMode"),
        "Translate snake_case / camelCase words",
        "Split snake/camel words into spaces before translating",
        true);

    public string DefaultLanguageKey => _defaultTargetLanguage.Value ?? "auto";
    public string SecondLanguageKey => _secondTargetLanguage.Value ?? "auto";
    public string DictionaryUrlPattern
    {
        get
        {
            var key = _dictionaryPattern.Value ?? "Youdao";
            return DictionaryUrlPatterns.TryGetValue(key, out var v) ? v : DictionaryUrlPatterns["Youdao"];
        }
    }
    public bool EnableSuggest => _enableSuggest.Value;
    public bool EnableAutoRead => _enableAutoRead.Value;
    public bool ShowOriginalQuery => _showOriginalQuery.Value;
    public bool EnableJumpDictionary => _enableJumpDictionary.Value;
    public bool EnableSecondLanguage => _enableSecondLanguage.Value;
    public bool UseSystemProxy => _useSystemProxy.Value;
    public bool EnableCodeMode => _enableCodeMode.Value;

    public event Action? OnSettingsChanged;

    private static SettingsManager? _instance;
    public static SettingsManager Instance => _instance ??= new SettingsManager();

    private bool _lastUseSystemProxy;

    private SettingsManager()
    {
        FilePath = SettingsJsonPath();
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

        LoadSettings();
        _lastUseSystemProxy = UseSystemProxy;
        UtilsFun.ChangeDefaultHttpHandlerProxy(_lastUseSystemProxy, callEvent: false);

        Settings.SettingsChanged += OnSettingsChangedInternal;
    }

    private void OnSettingsChangedInternal(object? sender, Settings args)
    {
        SaveSettings();
        if (_lastUseSystemProxy != UseSystemProxy)
        {
            _lastUseSystemProxy = UseSystemProxy;
            UtilsFun.ChangeDefaultHttpHandlerProxy(_lastUseSystemProxy);
        }
        OnSettingsChanged?.Invoke();
    }

    private static string SettingsJsonPath()
    {
        var dir = Utilities.BaseSettingsPath("PowerTranslator");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }
}
