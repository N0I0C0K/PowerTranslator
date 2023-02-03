using Wox.Plugin;
using Wox.Plugin.Logger;
using Microsoft.PowerToys.Settings.UI.Library;
using Translater.Utils;
using ManagedCommon;

namespace Translater
{
    public class Translater : IPlugin
    {
        public string Name => "Translater";
        public string Description => "A simple translater plugin, based on Youdao Translation";
        public PluginMetadata? queryMetaData = null;
        public IPublicAPI? publicAPI = null;
        private string iconPath = "Images/translater.dark.png";
        public int queryCount = 0;
        private TranslateHelper? translateHelper = null;
        private bool isDebug = false;
        private string queryPre = "";
        private long lastQueryTime = 0;
        private object preQueryLock = new Object();
        private void LogInfo(string info)
        {
            if (!isDebug)
                return;
            Log.Info(info, typeof(Translater));
        }
        public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();
            if (!translateHelper!.inited)
            {
                Task.Factory.StartNew(() =>
                {
                    translateHelper.initTranslater();
                });
                results.Add(new Result
                {
                    Title = "Initializing....",
                    SubTitle = "[Initialize translation components]",
                    IcoPath = iconPath
                });
                return results;
            }

            var queryTime = UtilsFun.GetUtcTimeNow();

            LogInfo($"{query.RawQuery} | {this.queryPre} | {this.lastQueryTime} | {queryTime}");

            if (query.Search.Length == 0)
            {
                string? clipboardText = Utils.UtilsFun.GetClipboardText();
                if (Utils.UtilsFun.WhetherTranslate(clipboardText))
                {
                    // Translate content from the clipboard
                    translateHelper!.TranslateAppendResult(clipboardText!, query, results, "clipboard");
                }
                results.each(tar =>
                {
                    tar.IcoPath = iconPath;
                });
                return results;
            }

            if (query.RawQuery == this.queryPre && queryTime - this.lastQueryTime > 400)
            {
                string src = query.Search;
                queryCount++;
                translateHelper!.TranslateAppendResult(src, query, results);
                results.each(tar =>
                {
                    tar.IcoPath = iconPath;
                });
            }
            else
            {
                lock (preQueryLock)
                {
                    this.queryPre = query.RawQuery;
                    this.lastQueryTime = queryTime;
                }
                results.Add(new Result()
                {
                    Title = query.Search,
                    SubTitle = "....",
                    IcoPath = iconPath
                });
                Task.Delay(600).ContinueWith((task) =>
                {
                    if (query.RawQuery == this.queryPre)
                    {
                        LogInfo($"change query to {query.RawQuery}({this.queryPre})");
                        publicAPI!.ChangeQuery(query.RawQuery, true);
                    }
                });
            }
            if (isDebug)
            {
                results.Add(new Result
                {
                    Title = $"{this.queryMetaData!.QueryCount},{queryCount}",
                    SubTitle = queryPre
                });
                results.Add(new Result
                {
                    Title = query.Search,
                    SubTitle = $"[{query.RawQuery}]"
                });
            }
            return results;
        }
        public void Init(PluginInitContext context)
        {
            Log.Info("translater init", typeof(Translater));
            queryMetaData = context.CurrentPluginMetadata;
            publicAPI = context.API;
            translateHelper = new TranslateHelper();
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

        private List<PluginAdditionalOption> GetAdditionalOptions()
        {
            return new List<PluginAdditionalOption>
            {
                new PluginAdditionalOption{
                    Key = "",
                    DisplayDescription = "指定翻译关键字",
                    Value = false
                },

            };
        }
    }
}