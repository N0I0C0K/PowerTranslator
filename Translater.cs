using Wox.Plugin;
using Wox.Plugin.Logger;
using Translater.Youdao;
using System.Text.Json;
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
        /// <summary>
        /// save the truely query time.
        /// </summary>
        private YoudaoTranslater? youdaoTranslater;
        private string queryPre = "";
        private int lastQueryTime = 0;
        private object initLock = new Object();
        public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();
            if (this.youdaoTranslater == null)
            {
                Task.Factory.StartNew(() =>
                {
                    this.initTranslater();
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
                Log.Info($"translate clipbord [{clipboardText}]", typeof(Translater));
                if (Utils.UtilsFun.WhetherTranslate(clipboardText))
                {
                    this.TranslateAppendResult(clipboardText!, query, results);
                }
                return results;
            }


            if (query.RawQuery == this.queryPre && queryTime.Millisecond - lastQueryTime > 100)
            {
                string src = query.Search;
                queryCount++;
                this.TranslateAppendResult(src, query, results);
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

        private void TranslateAppendResult(string src, Query query, List<Result> results, string subTitle = "")
        {
            try
            {
                var translateRes = youdaoTranslater.translate(src);
                if (translateRes != null && translateRes.errorCode == 0)
                {
                    results.Add(new Result()
                    {
                        Title = translateRes.translateResult[0][0].tgt,
                        SubTitle = $"{src} [{translateRes.type}]",
                        Action = e =>
                        {
                            Utils.UtilsFun.SetClipboardText(translateRes.translateResult[0][0].tgt);
                            return true;
                        }
                    });
                    if (translateRes.smartResult != null)
                    {
                        translateRes.smartResult?.entries.each((s) =>
                        {
                            string t = s.Replace("\r\n", " ").TrimStart();
                            if (string.IsNullOrEmpty(t))
                                return;
                            results.Add(new Result()
                            {
                                Title = t,
                                SubTitle = "[smart result]"
                            });
                        });
                    }
                }
                else
                {
                    results.Add(new Result()
                    {
                        Title = query.Search,
                        SubTitle = $"can not translate {src}."
                    });
                }
            }
            catch (Exception err)
            {
                results.Add(new Result()
                {
                    Title = "some error happen!",
                    SubTitle = err.Message
                });
                Log.Error(err.ToString(), typeof(Translater));
            }
        }

        private bool initTranslater()
        {
            lock (this.initLock)
            {
                if (this.youdaoTranslater != null)
                    return true;
                try
                {
                    youdaoTranslater = new YoudaoTranslater();
                    return true;
                }
                catch (Exception err)
                {
                    Log.Warn(err.Message, typeof(Translater));
                    return false;
                }
            }
        }
        public void Init(PluginInitContext context)
        {
            Log.Info("translater init", typeof(Translater));
            queryMetaData = context.CurrentPluginMetadata;
            publicAPI = context.API;
            this.initTranslater();
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