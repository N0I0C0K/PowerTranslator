using Wox.Plugin;
using Wox.Plugin.Logger;
using Microsoft.PowerToys.Settings.UI.Library;
using Translater.Utils;
using ManagedCommon;
using System.Windows.Controls;

namespace Translater
{
    public class ResultItem
    {
        public string Title { get; set; } = default!;
        public string SubTitle { get; set; } = default!;
        public Func<ActionContext, bool>? Action { get; set; }
        public string? CopyTgt { get; set; }
    }

    public class Translater : IPlugin, IDisposable, IDelayedExecutionPlugin, ISettingProvider
    {
        public string Name => "Translater";
        public string Description => "A simple translater plugin, based on Youdao Translation";
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => GetAdditionalOptions();
        public PluginMetadata? queryMetaData = null;
        public IPublicAPI? publicAPI = null;
        public const int delayQueryMillSecond = 500;
        private string iconPath = "Images/translater.dark.png";
        public int queryCount = 0;
        private TranslateHelper? translateHelper = null;
        private Suggest.SuggestHelper? suggestHelper;
        private bool isDebug = false;
        private string queryPre = "";
        private long lastQueryTime = 0;
        private string queryPreReal = "";
        private long lastQueryTimeReal = 0;
        private long lastTranslateTime = 0;
        private object preQueryLock = new Object();
        private bool delayedExecution = false;
        private bool enable_suggest = true;
        private void LogInfo(string info)
        {
            if (!isDebug)
                return;
            Log.Info(info, typeof(Translater));
        }
        public List<Result> Query(Query query)
        {
            if (delayedExecution)
                return new List<Result>();
            if (!translateHelper!.inited)
            {
                Task.Factory.StartNew(() =>
                {
                    translateHelper.initTranslater();
                });
                return new List<Result>(){
                    new Result
                    {
                        Title = "Initializing....",
                        SubTitle = "[Initialize translation components]",
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
            if (!translateHelper!.inited)
            {
                Task.Factory.StartNew(() =>
                {
                    if (translateHelper.initTranslater())
                        this.publicAPI?.ChangeQuery(query.RawQuery, true);
                });
                return new List<Result>(){
                    new Result
                    {
                        Title = "Initializing....",
                        SubTitle = "Initialize translation components, please wait.",
                        IcoPath = iconPath
                    }
                };
            }

            var res = new List<ResultItem>();
            if (querySearch.Length == 0)
            {
                string? clipboardText = Utils.UtilsFun.GetClipboardText();
                if (Utils.UtilsFun.WhetherTranslate(clipboardText))
                {
                    // Translate content from the clipboard
                    res.AddRange(translateHelper!.QueryTranslate(clipboardText!, "clipboard"));
                }
                return res.ToResultList(this.iconPath);
            }

            Task<List<ResultItem>>? suggestTask = null;
            if (enable_suggest)
            {
                suggestTask = Task.Run(() =>
                {
                    return this.suggestHelper!.QuerySuggest(querySearch);
                });
            }

            res.AddRange(this.translateHelper!.QueryTranslate(query.Search));
            ResultItem? suggestItem = null;
            if (suggestTask != null)
            {
                var suggest = suggestTask.GetAwaiter().GetResult();
                res.AddRange(suggest);
                suggestItem = suggest.FirstOrDefault((it) =>
                {
                    return it.Title.ToLower() != querySearch.ToLower();
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
            var query_res = res.ToResultList(this.iconPath);
            if (suggestItem != null && query_res.Count > 0)
            {
                query_res.FirstOrDefault()!.ToolTipData = new ToolTipData(suggestItem!.Title, "test");
            }
            return query_res;
        }

        public void Init(PluginInitContext context)
        {
            Log.Info("translater init", typeof(Translater));
            queryMetaData = context.CurrentPluginMetadata;
            publicAPI = context.API;
            translateHelper = new TranslateHelper(publicAPI);
            suggestHelper = new Suggest.SuggestHelper(publicAPI);
            publicAPI.ThemeChanged += this.UpdateIconPath;
        }

        private void UpdateIconPath(Theme pre, Theme now)
        {
            if (now == Theme.Light || now == Theme.HighContrastWhite)
            {
                iconPath = "Images/translater.light.png";
            }
            else
            {
                iconPath = "Images/translater.dark.png";
            }
        }

        public static List<PluginAdditionalOption> GetAdditionalOptions()
        {
            return new List<PluginAdditionalOption>
            {
                new PluginAdditionalOption{
                    Key = "EnableSuggest",
                    DisplayLabel = "Enable search suggest",
                    Value = true,
                },
            };
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
            var GetSetting = (string key) =>
            {
                var target = settings.AdditionalOptions.FirstOrDefault((set) =>
                {
                    return set.Key == key;
                });
                return target?.Value ?? true;
            };
            this.enable_suggest = GetSetting("EnableSuggest");
        }
    }
}