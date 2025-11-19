using Wox.Plugin;
using ManagedCommon;
using Translator.Utils;
using Wox.Plugin.Logger;
using Wox.Infrastructure;
using System.Windows.Controls;
using Microsoft.PowerToys.Settings.UI.Library;

namespace Translator
{
    public class Translator : IPlugin, IDisposable, IDelayedExecutionPlugin, ISettingProvider, IContextMenu, IReloadable
    {
        public string Name => "Translator";
        public static string PluginID => "EY1EBAMTNIWIVLYM039DSOS5MWITDJOD";
        public string Description => "A simple translation plugin, based on Youdao Translation";
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => SettingHelper.pluginAdditionalOptions;
        public PluginMetadata queryMetaData;
        public IPublicAPI publicAPI;
        public PluginInitContext pluginContext;
        public const int delayQueryMillSecond = 500;
        private string iconPath = "Images/translator.dark.png";
        public int queryCount = 0;
        private TranslateHelper? translateHelper;
        private Suggest.SuggestHelper? suggestHelper;
        private History.HistoryHelper? historyHelper;
        private bool isDebug = false;

        // settings
        private readonly SettingHelper settingHelper;
        public Translator()
        {
            settingHelper = SettingHelper.Instance;
        }
        private void LogInfo(string info)
        {
            if (!isDebug)
                return;
            Log.Info(info, typeof(Translator));
        }
        public List<Result> Query(Query query)
        {
            return new List<Result>();
        }
        public List<Result> Query(Query query, bool delayedExecution)
        {
            var querySearch = query.Search;
            var res = new List<ResultItem>();
            // query from clipboard
            if (querySearch.Length == 0)
            {
                string? clipboardText = Utils.UtilsFun.GetClipboardText();
                if (Utils.UtilsFun.WhetherTranslate(clipboardText))
                {
                    // Translate content from the clipboard
                    res.AddRange(translateHelper!.QueryTranslate(clipboardText!, "clipboard"));
                }
                res.AddRange(SettingHelper.helpInfoList);
                return res.ToResultList(this.iconPath, this.pluginContext, clipboardText != null && !clipboardText.Contains(';') && !clipboardText.Contains('；'));
            }
            //  Query history
            if (querySearch == "h")
            {
                res.AddRange(historyHelper!.query().Reverse());
                return res.ToResultList(this.iconPath, this.pluginContext);
            }
            else if (querySearch == "l")
            {
                res.AddRange(SettingHelper.languageList);
                return res.ToResultList(this.iconPath, this.pluginContext);
            }

            // get suggest in other thread
            Task<List<ResultItem>>? suggestTask = null;
            if (settingHelper.enableSuggest)
            {
                suggestTask = Task.Run(() =>
                {
                    return this.suggestHelper!.QuerySuggest(querySearch);
                });
            }

            // get second translate result in other thread
            Task<List<ResultItem>>? secondTranslateTask = null;
            if (settingHelper.enableSecondLanguage && settingHelper.secondLanguageKey != null)
            {
                secondTranslateTask = Task.Run(() =>
                {
                    return translateHelper!.QueryTranslate(querySearch, toLanguage: settingHelper.secondLanguageKey);
                });
            }

            res.AddRange(this.translateHelper!.QueryTranslate(querySearch));

            if (secondTranslateTask != null)
            {
                var secondRes = secondTranslateTask.GetAwaiter().GetResult();
                var resItem = secondRes[0];
                resItem.SubTitle = $"{resItem.SubTitle} [second language]";
                res.Insert(1, resItem);
            }

            if (suggestTask != null)
            {
                var suggest = suggestTask.GetAwaiter().GetResult();
                res.AddRange(suggest);
            }

            if (settingHelper.showOriginalQuery)
            {
                res.Add(new ResultItem
                {
                    Title = querySearch,
                    SubTitle = "[query raw]"
                });
            }

            if (isDebug)
            {
                res.Add(new ResultItem
                {
                    Title = querySearch,
                    SubTitle = $"[{query.RawQuery}]"
                });
            }

            if (settingHelper.enableAutoRead)
            {
                this.translateHelper?.Read(res.FirstOrDefault()?.Title);
            }

            //add the result to this history
            var first = res.FirstOrDefault((val) =>
            {
                return val.fromApiName != null;
            });
            if (first != null)
            {
                historyHelper?.Push(new ResultItem
                {
                    Title = first.Title,
                    SubTitle = querySearch
                });
            }

            var query_res = res.ToResultList(this.iconPath, this.pluginContext, !querySearch.Contains(';') && !querySearch.Contains('；'));

            return query_res;
        }
        public void Init(PluginInitContext context)
        {
            Log.Info("translator init", typeof(Translator));
            queryMetaData = context.CurrentPluginMetadata;
            publicAPI = context.API;
            pluginContext = context;
            var translaTask = Task.Factory.StartNew(() =>
            {
                translateHelper = new TranslateHelper(publicAPI, this.settingHelper.defaultLanguageKey);
            });
            suggestHelper = new Suggest.SuggestHelper(publicAPI);
            historyHelper = new History.HistoryHelper();
            publicAPI.ThemeChanged += this.UpdateIconPath;
            UpdateIconPath(Theme.Light, publicAPI.GetCurrentTheme());
            //translaTask.Wait();
        }
        private void UpdateIconPath(Theme pre, Theme now)
        {
            if (now == Theme.Light || now == Theme.HighContrastWhite)
            {
                iconPath = "Images/translator.light.png";
            }
            else
            {
                iconPath = "Images/translator.dark.png";
            }
            this.historyHelper?.UpdateIconPath(now);
        }
        public void Dispose()
        {
            this.publicAPI!.ThemeChanged -= this.UpdateIconPath;
        }
        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }
        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            this.settingHelper.UpdateSettings(settings);

            if (this.translateHelper != null)
                this.translateHelper.defaultLanguageKey = this.settingHelper.defaultLanguageKey;
        }
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            var contextMenu = new List<ContextMenuResult>
            {
                new ContextMenuResult
                {
                    Title = "Copy (Enter), Subtitle(shift+Enter)",
                    Action = context=>{
                        UtilsFun.SetClipboardText(selectedResult.SubTitle);
                        return false;
                    },
                    Glyph = "\xF413",
                    PluginName="PowerTranslator",
                    FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                    AcceleratorKey = System.Windows.Input.Key.Return,
                    AcceleratorModifiers = System.Windows.Input.ModifierKeys.Shift
                },
                new ContextMenuResult
                {
                    Title = "Read (Ctrl+Enter)",
                    Action = context=>{
                        this.translateHelper?.Read(selectedResult.Title);
                        return false;
                    },
                    Glyph = "\xEDB5",
                    PluginName="PowerTranslator",
                    FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                    AcceleratorKey = System.Windows.Input.Key.Return,
                    AcceleratorModifiers = System.Windows.Input.ModifierKeys.Control
                }

            };
            if (settingHelper.enableJumpToDict)
            {
                contextMenu.Add(
                    new ContextMenuResult
                    {
                        Title = "Go to dictionary",
                        Action = context =>
                        {
                            Helper.OpenInShell(string.Format(settingHelper.dictUtlPattern, selectedResult.Title));
                            return false;
                        },
                        Glyph = "\xE721",
                        PluginName = "PowerTranslator",
                        FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                    }
                );
            }
            return contextMenu;
        }
        public void ReloadData()
        {
            translateHelper?.Reload();
        }
    }
}