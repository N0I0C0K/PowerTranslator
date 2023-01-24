using Wox.Plugin;
using Wox.Plugin.Logger;
using Translater.Utils;

namespace Translater
{
    public class TranslateHelper
    {
        public const string toLanSplit = "->";
        public bool inited => this.youdaoTranslater != null;
        private object initLock = new Object();
        private Youdao.YoudaoTranslater? youdaoTranslater;
        public TranslateHelper()
        {
            this.initTranslater();
        }
        public void TranslateAppendResult(string src, Query query, List<Result> results, string translateFrom = "user input")
        {
            try
            {
                if (src.Contains(toLanSplit))
                {

                }
                var translateRes = youdaoTranslater.translate(src);
                if (translateRes != null && translateRes.errorCode == 0)
                {
                    results.Add(new Result()
                    {
                        Title = translateRes.translateResult[0][0].tgt,
                        SubTitle = $"{src} [{translateRes.type}] [Translate form {translateFrom}]",
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

        public bool initTranslater()
        {
            lock (this.initLock)
            {
                if (this.youdaoTranslater != null)
                    return true;
                try
                {
                    youdaoTranslater = new Youdao.YoudaoTranslater();
                    return true;
                }
                catch (Exception err)
                {
                    Log.Warn(err.Message, typeof(Translater));
                    return false;
                }
            }
        }
    }
}