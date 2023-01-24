using Wox.Plugin;
using Wox.Plugin.Logger;
using Microsoft.PowerToys.Settings.UI.Library;

namespace Translater
{
    public class Translater : IPlugin
    {
        public string Name { get; } = "Translater";
        public string Description { get; } = "A simple translater plugin [By N0I0C0K]";
        public PluginMetadata? queryMetaData = null;
        public IPublicAPI? publicAPI = null;
        public int queryCount = 0;
        private string queryPre = "";
        private TranslateHelper? translateHelper = null;
        private int lastQueryTime = 0; public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();
            if (!translateHelper!.inited)
            {
                Task.Factory.StartNew(() =>
                {
                    translateHelper.initTranslater();
                });
                results.Add(new Result()
                {
                    Title = "Initializing....",
                    SubTitle = "[Initialize translation components]"
                });
                return results;
            }

            var queryTime = DateTime.Now;
            if (query.Search.Length == 0)
            {
                string? clipboardText = Utils.UtilsFun.GetClipboardText();
                if (Utils.UtilsFun.WhetherTranslate(clipboardText))
                {
                    // Translate content from the clipboard
                    translateHelper!.TranslateAppendResult(clipboardText!, query, results, "clipboard");
                }
                return results;
            }

            if (query.RawQuery == this.queryPre && queryTime.Millisecond - lastQueryTime > 100)
            {
                string src = query.Search;
                queryCount++;
                translateHelper!.TranslateAppendResult(src, query, results);
            }
            else
            {
                this.queryPre = query.RawQuery;
                this.lastQueryTime = queryTime.Millisecond;
                results.Add(new Result()
                {
                    Title = query.Search,
                    SubTitle = "...."
                });
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(400);
                    Log.Info($"rawquery:{query.RawQuery}, queryPre:{this.queryPre}", typeof(Translater));
                    if (query.RawQuery == this.queryPre)
                        publicAPI!.ChangeQuery(queryPre, true);
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