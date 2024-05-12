using Microsoft.PowerToys.Settings.UI.Library;

namespace Translator
{
    public class SettingHelper
    {
        public static readonly List<string> languagesKeys = new List<string> { "auto", "zh-CHS", "zh-CHT", "en", "ja", "ko", "ru", "fr", "es", "ar", "de", "it", "he" };
        public static readonly List<string> languagesOptions = new List<string> { "auto", "Chinese (Simplified)", "Chinese (Traditional)", "English", "Japanese", "Korean", "Russian", "French", "Spanish", "Arabic", "German", "Italian", "Hebrew" };
        public static readonly List<string> dictUrlPatternKeys = new List<string> { "Youdao", "Oxford", "Cambridge" };
        public static readonly List<string> dictUrlPatternValues = new List<string> { "https://www.youdao.com/result?word={0}&lang=en", "https://www.oed.com/search/dictionary/?scope=Entries&q={0}", "https://dictionary.cambridge.org/us/dictionary/english/{0}" };
        public static readonly List<PluginAdditionalOption> pluginAdditionalOptions = GetAdditionalOptions();
        public string defaultLanguageKey = "auto";
        public bool enableSuggest = true;
        public bool enableAutoRead = false;
        public bool enableSecondLanuage = false;
        public string secondLanuageKey = "auto";
        public bool showOriginalQuery = false;
        public bool enableJumpToDict = false;
        public string dictUtlPattern = dictUrlPatternValues[0];

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
                    DisplayDescription = "Default translation target language, Default is auto",
                    DisplayLabel = "Default target lanuage",
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                    ComboBoxValue = 0,
                    ComboBoxItems = lanuageItems
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
                    Key = "SecondTargetLanuage",
                    DisplayLabel = "Second target lanuage",
                    DisplayDescription = "Active second target lanuage, will display after main target",
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.CheckboxAndCombobox,
                    Value = false,
                    ComboBoxValue = 0,
                    ComboBoxItems = lanuageItems
                }
            };
        }
        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            var GetSetting = (string key) =>
            {
                var target = settings.AdditionalOptions.FirstOrDefault((set) =>
                {
                    return set.Key == key;
                });
                return target!;
            };
            enableSuggest = GetSetting("EnableSuggest").Value;
            enableAutoRead = GetSetting("EnableAutoRead").Value;
            showOriginalQuery = GetSetting("ShowOriginalQuery").Value;

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