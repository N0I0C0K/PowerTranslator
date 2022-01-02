using System;
using System.Collections.Generic;
using Wox.Plugin;
using Wox.Plugin.Logger;
using System.Windows;
using Translater.utils;
using System.Threading;
using System.Threading.Tasks;

namespace Translater
{
    public class Translater : IPlugin
    {
        // Localized name
        public string Name { get; } = "Translater";

        // Localized description
        public string Description { get; } = "Test";

        /// <summary>
        /// save the last query.
        /// </summary>
        public string queryContent = "";
        public PluginMetadata queryMetaData = null;
        public IPublicAPI publicAPI = null;
        /// <summary>
        /// save the truely query time.
        /// </summary>
        public int queryTimes = 0;
        public Action<string, bool> changeQuery;
        public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();
            Result infoResult = new Result()
            {
                Title = queryMetaData.QueryCount.ToString(),
                SubTitle = string.Format("{0} {1}", queryTimes.ToString(), this.queryContent)
            };
            results.Add(infoResult);

            //if two are equal, it's the true query.
            if (query.RawQuery == queryContent)
            {
                queryTimes++;
                infoResult.SubTitle = string.Format("{0} {1}", queryTimes.ToString(), this.queryContent);
                Log.Info(string.Format("catch a query {0}", queryContent), typeof(Translater));
                try
                {
                    TranslateResponse translateResponse = Utils.TranslateZhToEn(query.Search);
                    if (translateResponse.error_code != 52000)
                    {
                        throw new Exception(string.Format("Error in translate, error code:{0}", translateResponse.error_code));
                    }
                    results.Add(new Result()
                    {
                        Title = translateResponse.trans_result[0].dst,
                        SubTitle = translateResponse.trans_result[0].src,
                        Action = e =>
                        {
                            return false;
                        }
                    });
                }
                catch (System.Exception ex)
                {
                    results.Add(new Result()
                    {
                        Title = ex.Message,
                        SubTitle = ex.ToString(),
                        Action = e =>
                        {
                            return false;
                        }
                    });
                    Task task = Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(1000);
                        changeQuery(this.queryContent, true);
                    });
                }
            }
            //else the user is continue typing.
            else
            {
                queryContent = query.RawQuery;
                infoResult.SubTitle = string.Format("{0} {1}", queryTimes.ToString(), this.queryContent);
                Task task = Task.Factory.StartNew(
                (queryRaw) =>
                {
                    if (queryRaw != null)
                    {
                        Thread.Sleep(500);
                        Log.Info(string.Format("{0} == {1}", queryRaw.ToString(), this.queryContent), typeof(Translater));
                        if (queryRaw.ToString() == this.queryContent)
                        {
                            changeQuery(this.queryContent, true);
                        }
                    }
                }, state: queryContent);
                results.Add(new Result()
                {
                    Title = task.Id.ToString(),
                    SubTitle = task.Status.ToString(),
                    Action = e =>
                    {
                        Clipboard.SetDataObject(task.Status.ToString());
                        changeQuery(this.queryContent, true);
                        return true;
                    }
                });
            }
            return results;
        }
        public void Init(PluginInitContext context)
        {
            Log.Info("translater init", typeof(Translater));
            queryMetaData = context.CurrentPluginMetadata;
            publicAPI = context.API;
            this.changeQuery = (_query, _force) =>
            {
                Log.Info($"{_query} == {this.queryContent} true, start change query", typeof(Translater));
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            this.publicAPI.ChangeQuery(this.queryContent, true);
                        }
                        catch (System.Exception ex)
                        {
                            Log.Exception("Error in invoke", ex, typeof(Translater));
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.Exception("Error in send mainContext", ex, typeof(Translater));
                }
            };
        }
    }
}