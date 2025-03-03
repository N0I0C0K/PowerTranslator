using Wox.Plugin;
using ManagedCommon;
using Translator.Utils;
using Wox.Plugin.Logger;
using Wox.Infrastructure;
using System.Windows.Controls;
using Microsoft.PowerToys.Settings.UI.Library;
using Translator.Properties;

namespace Translator
{
    public class ResultItem
    {
        public string Title { get; set; } = default!;
        public string SubTitle { get; set; } = default!;
        public Func<ActionContext, bool>? Action { get; set; }
        public string? CopyTgt { get; set; }
        public string? iconPath { get; set; }
        public string? transType { get; set; }
        public string? fromApiName { get; set; }
        public string? Description { get; set; }
    }

    public class Translator : IPlugin, IDisposable, IDelayedExecutionPlugin, ISettingProvider, IContextMenu, IReloadable
    {
        public string Name => Resources.PluginName;
        public static string PluginID => "EY1EBAMTNIWIVLYM039DSOS5MWITDJOD";
        public string Description => Resources.PluginDescription;
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => SettingHelper.pluginAdditionalOptions;
        public PluginMetadata? queryMetaData = null;
        public IPublicAPI? publicAPI = null;
        public const int delayQueryMillSecond = 500;
        private string iconPath = "Images/translator.dark.png";
        public int queryCount = 0;
        private TranslateHelper? translateHelper;
        private Suggest.SuggestHelper? suggestHelper;
        private History.HistoryHelper? historyHelper;
        private bool isDebug = false;
        private string queryPre = "";
        private long lastQueryTime = 0;
        private string queryPreReal = "";
        private long lastQueryTimeReal = 0;
        private long lastTranslateTime = 0;
        private object preQueryLock = new Object();

        // settings
        private readonly SettingHelper settingHelper;
        private bool delayedExecution;
        public Translator()
        {
            settingHelper = new SettingHelper();
        }
        private void LogInfo(string info)
        {
            if (!isDebug)
                return;
            Log.Info(info, typeof(Translator));
        }
        public List<Result> Query(Query query)
        {
            if (delayedExecution)
                return new List<Result>();
            if (!translateHelper!.inited)
            {
                Task.Factory.StartNew(() =>
                {
                    translateHelper.InitTranslator();
                });
                return new List<Result>(){
                    new Result
                    {
                        Title = Resources.InitTitle,
                        SubTitle = Resources.InitSubTitle,
                        IcoPath = iconPath
                    }
                };
            }

            var queryTime = UtilsFun.GetNowTicksMilliseconds();
            var querySearch = query.Search;
            var results = new List<ResultItem>();

            //LogInfo($"{query.RawQuery} | {this.queryPre} | now: {queryTime.ToFormateTime()} | pre: {this.lastQueryTime.ToFormateTime()}");

            if (querySearch.Length == 0)
            {
                string? clipboardText = Utils.UtilsFun.GetClipboardText();
                if (Utils.UtilsFun.WhetherTranslate(clipboardText))
                {
                    // Translate content from the clipboard
                    results.AddRange(translateHelper!.QueryTranslate(clipboardText!, "clipboard"));
                }
                return results.ToResultList(this.iconPath);
            }

            if (query.RawQuery == this.queryPre && queryTime - this.lastQueryTime > 300)
            {
                LogInfo($"translate {querySearch}");
                queryCount++;
                this.lastTranslateTime = queryTime;
                this.lastQueryTime = queryTime;

                var task = Task.Run(() =>
                {
                    return this.suggestHelper!.QuerySuggest(querySearch);
                });

                results.AddRange(translateHelper!.QueryTranslate(querySearch));
                //results.AddRange(task.GetAwaiter().GetResult());
            }
            else
            {
                results.Add(new ResultItem
                {
                    Title = querySearch,
                    SubTitle = "....",
                    Action = (e) => { return false; }
                });
                if (true || querySearch != this.queryPreReal)
                {
                    lock (preQueryLock)
                    {
                        this.queryPre = query.RawQuery;
                        this.lastQueryTime = queryTime;
                    }
                    Task.Delay(delayQueryMillSecond).ContinueWith((task) =>
                    {
                        var time_now = UtilsFun.GetNowTicksMilliseconds();
                        if (query.RawQuery == this.queryPre
                            && this.lastTranslateTime < queryTime)
                        {
                            LogInfo($"change query to {query.RawQuery}({this.queryPre}), {queryTime.ToFormateTime()}");
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                publicAPI!.ChangeQuery(query.RawQuery, true);
                            });
                        }
                    });
                }
            }
            if (isDebug)
            {
                results.Add(new ResultItem
                {
                    Title = $"{this.queryMetaData!.QueryCount},{queryCount}",
                    SubTitle = queryPre
                });
                results.Add(new ResultItem
                {
                    Title = querySearch,
                    SubTitle = $"[{query.RawQuery}]"
                });
            }

            this.queryPreReal = querySearch;
            this.lastQueryTimeReal = queryTime;

            return results.ToResultList(this.iconPath);
        }
        public List<Result> Query(Query query, bool delayedExecution)
        {
            this.delayedExecution = delayedExecution;
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
                else
                {
                    // Query history
                    res.AddRange(historyHelper!.query().Reverse());
                }
                return res.ToResultList(this.iconPath);
            }
            //  Query history
            if (querySearch == "h")
            {
                res.AddRange(historyHelper!.query().Reverse());
                return res.ToResultList(this.iconPath);
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
            if (settingHelper.enableSecondLanuage && settingHelper.secondLanuageKey != null)
            {
                secondTranslateTask = Task.Run(() =>
                {
                    return translateHelper!.QueryTranslate(querySearch, toLanuage: settingHelper.secondLanuageKey);
                });
            }

            res.AddRange(this.translateHelper!.QueryTranslate(querySearch));

            if (secondTranslateTask != null)
            {
                var secondRes = secondTranslateTask.GetAwaiter().GetResult();
                var resItem = secondRes[0];
                resItem.SubTitle = $"{resItem.SubTitle} {Resources.Tag_SecondLanguage}";
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
                    SubTitle = Resources.Tag_QueryRaw
                });
            }

            if (isDebug)
            {
                res.Add(new ResultItem
                {
                    Title = $"{this.queryMetaData!.QueryCount},{++queryCount}",
                    SubTitle = queryPre
                });
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

            var query_res = res.ToResultList(this.iconPath);

            return query_res;
        }
        public void Init(PluginInitContext context)
        {
            Log.Info("translator init", typeof(Translator));
            queryMetaData = context.CurrentPluginMetadata;
            publicAPI = context.API;
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
                    Title = Resources.Menu_Copy_Title,
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
                    Title = Resources.Menu_Read_Title,
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
                        Title = Resources.Tag_GoToDictionary_Title,
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