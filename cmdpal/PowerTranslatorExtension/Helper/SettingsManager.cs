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
        Languages.Select(kv => new ChoiceSetSetting.Choice(Loc.Language(kv.Key), kv.Key)).ToList();

    private static List<ChoiceSetSetting.Choice> DictionaryChoices() =>
        DictionaryUrlPatterns.Select(kv => new ChoiceSetSetting.Choice(Loc.Dictionary(kv.Key), kv.Key)).ToList();

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
        _defaultTargetLanguage = new(
            Namespaced("DefaultTargetLanguage"),
            Loc.Get("Setting_DefaultTargetLanguage_Label"),
            Loc.Get("Setting_DefaultTargetLanguage_Desc"),
            LanguageChoices());

        _enableSuggest = new(
            Namespaced("EnableSuggest"),
            Loc.Get("Setting_EnableSuggest_Label"),
            Loc.Get("Setting_EnableSuggest_Desc"),
            true);

        _enableAutoRead = new(
            Namespaced("EnableAutoRead"),
            Loc.Get("Setting_EnableAutoRead_Label"),
            Loc.Get("Setting_EnableAutoRead_Desc"),
            false);

        _showOriginalQuery = new(
            Namespaced("ShowOriginalQuery"),
            Loc.Get("Setting_ShowOriginalQuery_Label"),
            Loc.Get("Setting_ShowOriginalQuery_Desc"),
            false);

        _enableJumpDictionary = new(
            Namespaced("EnableJumpDictionary"),
            Loc.Get("Setting_EnableJumpDictionary_Label"),
            Loc.Get("Setting_EnableJumpDictionary_Desc"),
            false);

        _dictionaryPattern = new(
            Namespaced("DictionaryPattern"),
            Loc.Get("Setting_DictionaryPattern_Label"),
            Loc.Get("Setting_DictionaryPattern_Desc"),
            DictionaryChoices());

        _enableSecondLanguage = new(
            Namespaced("EnableSecondLanguage"),
            Loc.Get("Setting_EnableSecondLanguage_Label"),
            Loc.Get("Setting_EnableSecondLanguage_Desc"),
            false);

        _secondTargetLanguage = new(
            Namespaced("SecondTargetLanguage"),
            Loc.Get("Setting_SecondTargetLanguage_Label"),
            Loc.Get("Setting_SecondTargetLanguage_Desc"),
            LanguageChoices());

        _useSystemProxy = new(
            Namespaced("UseSystemProxy"),
            Loc.Get("Setting_UseSystemProxy_Label"),
            Loc.Get("Setting_UseSystemProxy_Desc"),
            true);

        _enableCodeMode = new(
            Namespaced("EnableCodeMode"),
            Loc.Get("Setting_EnableCodeMode_Label"),
            Loc.Get("Setting_EnableCodeMode_Desc"),
            true);

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
