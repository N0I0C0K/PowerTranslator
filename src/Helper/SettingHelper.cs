using Microsoft.PowerToys.Settings.UI.Library;
using Translator.Properties;

namespace Translator
{
    public class SettingHelper
    {
        public static readonly Dictionary<string, string> Languages = new Dictionary<string, string>{
            { "auto", Resources.Lan_auto },
            { "zh-CHS", Resources.Lan_zh_CHS },
            { "zh-CHT", Resources.Lan_zh_CHT },
            { "en", Resources.Lan_en },
            { "ja", Resources.Lan_ja },
            { "ko", Resources.Lan_ko },
            { "ru", Resources.Lan_ru },
            { "fr", Resources.Lan_fr },
            { "es", Resources.Lan_es },
            { "ar", Resources.Lan_ar },
            { "de", Resources.Lan_de },
            { "it", Resources.Lan_it },
            { "he", Resources.Lan_he }
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
        public bool enableSecondLanuage = false;
        public string secondLanuageKey = "auto";
        public bool showOriginalQuery = false;
        public bool enableJumpToDict = false;
        public string dictUtlPattern = dictUrlPatternValues[0];
        public bool useSystemProxy = true;

        public static List<PluginAdditionalOption> GetAdditionalOptions()
        {
            var lanuageItems = languagesOptions.Select((val, idx) =>
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
                    DisplayDescription = Resources.DefaultTargetLanguageDescription,
                    DisplayLabel = Resources.DefaultTargetLanguage,
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                    ComboBoxValue = 0,
                    ComboBoxItems = lanuageItems
                },
                new PluginAdditionalOption{
                    Key = "EnableSuggest",
                    DisplayLabel = Resources.EnableSuggest,
                    Value = true,
                },
                new PluginAdditionalOption{
                    Key = "EnableAutoRead",
                    DisplayLabel = Resources.EnableAutoRead,
                    Value = false,
                },
                new PluginAdditionalOption{
                    Key = "ShowOriginalQuery",
                    DisplayLabel = Resources.ShowOriginalQuery,
                    DisplayDescription= Resources.ShowOriginalQueryDescription,
                    Value = false,
                },
                new PluginAdditionalOption{
                    Key = "EnableJumpDictionary",
                    DisplayLabel = Resources.EnableJumpDictionary,
                    DisplayDescription= Resources.EnableJumpDictionaryDescription,
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.CheckboxAndCombobox,
                    Value = false,
                    ComboBoxValue = 0,
                    ComboBoxItems = dictItems
                },
                new PluginAdditionalOption{
                    Key = "SecondTargetLanuage",
                    DisplayLabel = Resources.SecondTargetLanuage,
                    DisplayDescription = Resources.SecondTargetLanguageDescription,
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.CheckboxAndCombobox,
                    Value = false,
                    ComboBoxValue = 0,
                    ComboBoxItems = lanuageItems
                },
                new PluginAdditionalOption{
                    Key = "UseSystemProxy",
                    DisplayLabel = Resources.UseSystemProxy,
                    DisplayDescription = Resources.UseSystemProxyDescription,
                    Value = true,
                },
            };
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            var GetSetting = (string key) =>
            {
                PluginAdditionalOption target = settings.AdditionalOptions.FirstOrDefault((set) =>
                {
                    return set.Key == key;
                });
                return target!;
            };
            enableSuggest = GetSetting("EnableSuggest").Value;
            enableAutoRead = GetSetting("EnableAutoRead").Value;
            showOriginalQuery = GetSetting("ShowOriginalQuery").Value;
            var _useSystemProxy = GetSetting("UseSystemProxy").Value;
            if (_useSystemProxy != useSystemProxy)
            {
                useSystemProxy = _useSystemProxy;
                Utils.UtilsFun.ChangeDefaultHttpHandlerProxy(_useSystemProxy);
            }


            int defaultLanguageIdx = GetSetting("DefaultTargetLanguage").ComboBoxValue;
            defaultLanguageIdx = defaultLanguageIdx >= languagesKeys.Count ? 0 : defaultLanguageIdx;
            defaultLanguageKey = languagesKeys[defaultLanguageIdx];

            var secondOption = GetSetting("SecondTargetLanuage");
            enableSecondLanuage = secondOption.Value;
            secondLanuageKey = languagesKeys[secondOption.ComboBoxValue >= languagesKeys.Count ? 0 : secondOption.ComboBoxValue];

            var jumpToDict = GetSetting("EnableJumpDictionary");
            enableJumpToDict = jumpToDict.Value;
            dictUtlPattern = dictUrlPatternValues[jumpToDict.ComboBoxValue >= dictUrlPatternValues.Count ? 0 : jumpToDict.ComboBoxValue];
        }
    }
}