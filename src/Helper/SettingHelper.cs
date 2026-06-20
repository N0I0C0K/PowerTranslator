using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Infrastructure;

namespace Translator
{
    public class SettingHelper
    {
        public static readonly Dictionary<string, string> Languages = new Dictionary<string, string>{
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
            { "id", "Indonesian" }
        };

        public static readonly List<ResultItem> languageList = Languages.Select((val) => new ResultItem { Title = val.Key, SubTitle = val.Value }).ToList();
        public static readonly List<ResultItem> helpInfoList = new List<ResultItem>
        {
            new ResultItem{
                Title = "History",
                SubTitle = "Type `h` to view history",
                Action = (ev) =>
                {
                    var key = ev.pluginInitContext.CurrentPluginMetadata.ActionKeyword;
                    ev.pluginInitContext.API.ChangeQuery(key+"h", true);
                    return false;
                }
            },
            new ResultItem{
                Title = "Support languages",
                SubTitle = "Type `l` to view support languages and it's short code",
                Action = (ev) =>
                {
                    var key = ev.pluginInitContext.CurrentPluginMetadata.ActionKeyword;
                    ev.pluginInitContext.API.ChangeQuery(key+"l", true);
                    return false;
                }
            },
            new ResultItem
            {
                Title = "Find help",
                SubTitle = "Go to issue page to find help",
                Action = (ev) =>
                {
                    Helper.OpenInShell("https://github.com/N0I0C0K/PowerTranslator/issues?q=");
                    return true;
                }
            }
        };

        public static readonly Dictionary<string, string> DictionaryUrlPatterns = new Dictionary<string, string>
        {
            { "Youdao", "https://www.youdao.com/result?word={0}&lang=en" },
            { "Oxford", "https://www.oed.com/search/dictionary/?scope=Entries&q={0}" },
            { "Cambridge", "https://dictionary.cambridge.org/us/dictionary/english/{0}" }
        };

        public static readonly List<string> languagesKeys = Languages.Keys.ToList();
        public static readonly List<string> languagesOptions = Languages.Values.ToList();
        public static readonly List<string> dictUrlPatternKeys = DictionaryUrlPatterns.Keys.ToList();
        public static readonly List<string> dictUrlPatternValues = DictionaryUrlPatterns.Values.ToList();
        public static readonly List<PluginAdditionalOption> pluginAdditionalOptions = GetAdditionalOptions();
        public string defaultLanguageKey = "auto";
        public bool enableSuggest = true;
        public bool enableAutoRead = false;
        public bool enableSecondLanguage = false;
        public string secondLanguageKey = "auto";
        public bool showOriginalQuery = false;
        public bool enableJumpToDict = false;
        public string dictUtlPattern = dictUrlPatternValues[0];
        public bool useSystemProxy = true;
        public bool enableCodeMode = true;
        private static SettingHelper? __instance;
        public static SettingHelper Instance
        {
            get
            {
                __instance ??= new SettingHelper();
                return __instance;
            }
        }

        public static List<PluginAdditionalOption> GetAdditionalOptions()
        {
            var languageItems = languagesOptions.Select((val, idx) =>
            {
                return new KeyValuePair<string, string>(val, idx.ToString());
            }).ToList();
            var dictItems = dictUrlPatternKeys.Select((val, idx) =>
            {
                return new KeyValuePair<string, string>(val, idx.ToString());
            }).ToList();
            return new List<PluginAdditionalOption>
            {
                new PluginAdditionalOption{
                    Key = "DefaultTargetLanguage",
                    DisplayDescription = "Default translation target language, Default is auto",
                    DisplayLabel = "Default target language",
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                    ComboBoxValue = 0,
                    ComboBoxItems = languageItems
                },
                new PluginAdditionalOption{
                    Key = "EnableSuggest",
                    DisplayLabel = "Enable search suggest",
                    Value = true,
                },
                new PluginAdditionalOption{
                    Key = "EnableAutoRead",
                    DisplayLabel = "Automatic reading result",
                    Value = false,
                },
                new PluginAdditionalOption{
                    Key = "ShowOriginalQuery",
                    DisplayLabel = "Display original query",
                    DisplayDescription= "Will add a item at the end of the results with \"[query raw]\"",
                    Value = false,
                },
                new PluginAdditionalOption{
                    Key = "EnableJumpDictionary",
                    DisplayLabel = "Show jump to dictionary button",
                    DisplayDescription= "will add a button to quick jump to dictionary, default is youdao.",
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.CheckboxAndCombobox,
                    Value = false,
                    ComboBoxValue = 0,
                    ComboBoxItems = dictItems
                },
                new PluginAdditionalOption{
                    Key = "SecondTargetLanguage",
                    DisplayLabel = "Second target language",
                    DisplayDescription = "Active second target language, will display after main target",
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.CheckboxAndCombobox,
                    Value = false,
                    ComboBoxValue = 0,
                    ComboBoxItems = languageItems
                },
                new PluginAdditionalOption{
                    Key = "UseSystemProxy",
                    DisplayLabel = "Use system default proxy",
                    DisplayDescription = "Use a proxy at request time, default to true",
                    Value = true,
                },
                new PluginAdditionalOption{
                    Key = "EnableCodeMode",
                    DisplayLabel = "Translate snake case or camel case words",
                    DisplayDescription = "Translate snake case or camel case words, default to true",
                    Value = true,
                }
            };
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            PluginAdditionalOption GetSetting(string key)
            {
                PluginAdditionalOption target = settings.AdditionalOptions.First((set) =>
                {
                    return set.Key == key;
                });
                return target;
            }
            enableSuggest = GetSetting("EnableSuggest").Value;
            enableAutoRead = GetSetting("EnableAutoRead").Value;
            showOriginalQuery = GetSetting("ShowOriginalQuery").Value;
            enableCodeMode = GetSetting("EnableCodeMode").Value;
            var _useSystemProxy = GetSetting("UseSystemProxy").Value;
            if (_useSystemProxy != useSystemProxy)
            {
                useSystemProxy = _useSystemProxy;
                Utils.UtilsFun.ChangeDefaultHttpHandlerProxy(_useSystemProxy);
            }


            int defaultLanguageIdx = GetSetting("DefaultTargetLanguage").ComboBoxValue;
            defaultLanguageIdx = defaultLanguageIdx >= languagesKeys.Count ? 0 : defaultLanguageIdx;
            defaultLanguageKey = languagesKeys[defaultLanguageIdx];

            var secondOption = GetSetting("SecondTargetLanguage");
            enableSecondLanguage = secondOption.Value;
            secondLanguageKey = languagesKeys[secondOption.ComboBoxValue >= languagesKeys.Count ? 0 : secondOption.ComboBoxValue];

            var jumpToDict = GetSetting("EnableJumpDictionary");
            enableJumpToDict = jumpToDict.Value;
            dictUtlPattern = dictUrlPatternValues[jumpToDict.ComboBoxValue >= dictUrlPatternValues.Count ? 0 : jumpToDict.ComboBoxValue];
        }
    }
}