using Wox.Plugin;
using Wox.Plugin.Logger;
using Translater.Utils;
using System.Windows.Media;
using Translater.Youdao;
using Translater.Youdao.V2;
using Translater.Youdao.Backup;
namespace Translater;

public class TranslateFailedException : Exception
{
    public TranslateFailedException() : base("translate failed")
    {

    }
}

public class TranslateHelper
{
    public struct TranslateTarget
    {
        public string src;
        public string toLan;
    }
    public const string toLanSplit = "->";
    public bool inited => this.translaters.Count >= 3 && (this.translaters[0] != null || this.translaters[1] != null);
    private object initLock = new Object();
    private long lastInitTime = 0;
    private IPublicAPI publicAPI;
    private List<Youdao.ITranslater?> translaters;
    private List<Type> translatorTypes;
    private bool isSpeaking = false;
    private bool isIniting = false;
    public string defaultLanguageKey = "auto";
    public TranslateHelper(IPublicAPI publicAPI, string defaultLanguageKey = "auto")
    {
        this.translaters = new List<Youdao.ITranslater?>{
            null, null, null
        };
        translatorTypes = new List<Type>{
            typeof(Youdao.V2.YoudaoTranslater),
            typeof(Youdao.old.YoudaoTranslater),
            typeof(Youdao.Backup.BackUpTranslater)
        };
        this.initTranslater();
        this.publicAPI = publicAPI;
        this.defaultLanguageKey = defaultLanguageKey;

        // backup translater, We don't need to initialize it with the others, because it doesn't have an error
        // this.backUpTranslater = new Youdao.Backup.BackUpTranslater();
    }
    public TranslateTarget ParseRawSrc(string src)
    {
        if (src.Contains(toLanSplit))
        {
            var srcArr = src.Split(toLanSplit);
            return new TranslateTarget
            {
                src = srcArr.First().TrimEnd().TrimStart(),
                toLan = srcArr.Last().TrimEnd().TrimStart()
            };
        }
        return new TranslateTarget
        {
            src = src,
            toLan = this.defaultLanguageKey
        };
    }
    public List<ResultItem> QueryTranslate(string raw, string translateFrom = "user input", string? toLanuage = null)
    {
        var res = new List<ResultItem>();
        if (raw.Length == 0)
            return res;

        var target = ParseRawSrc(raw);
        string src = target.src;
        string toLan = toLanuage ?? target.toLan;
        Youdao.ITranslateResult? translateResult = null;
        int idx = 0;
        translateResult = this.translaters.FirstNotNoneCast((it) =>
        {
            try
            {
                if (it == null)
                {

                    throw new Exception($"{this.translatorTypes[idx].FullName} is null");
                }
                return it?.Translate(src, toLan, "auto");
            }
            catch (Exception err)
            {
                Log.Error(err.Message, typeof(TranslateHelper));
                return null;
            }
            finally
            {
                idx++;
            }
        });
        if (translateResult != null)
        {
            var tres = translateResult!.Transform();
            if (tres != null)
                res.AddRange(tres);
        }
        else
        {
            res.Add(new ResultItem
            {
                Title = "result is null, some error happen in translate. check out your network!",
                SubTitle = "Press enter to get help",
                Action = (ev) =>
                {
                    UtilsFun.SetClipboardText("https://github.com/N0I0C0K/PowerTranslator/issues?q=");
                    this.publicAPI.ShowMsg("Copy!", "The URL has been copied, Go to your browser and visit the website for help.");
                    return true;
                }
            });
        }

        if (!inited)
        {
            if (!this.isIniting)
            {
                Task.Factory.StartNew(() =>
                {
                    if (this.initTranslater())
                        this.publicAPI?.ChangeQuery(raw, true);
                });
            }
            res.Add(new ResultItem
            {
                Title = "initializing other apis...",
                SubTitle = "please try later"
            });
        }

        return res;
    }

    public void Read(string? txt)
    {
        if (isSpeaking || txt == null || txt.Length == 0)
            return;
        Task.Factory.StartNew(() =>
        {
            this.isSpeaking = true;
            try
            {
                var uri = new Uri($"https://dict.youdao.com/dictvoice?audio={Uri.EscapeDataString(txt.removeSpecialCharacter())}&le=zh");
                uri.fixChinese();

                MediaPlayer player = new MediaPlayer();
                player.Stop();
                player.Volume = 1;
                player.Open(uri);
                TimeSpan tt = TimeSpan.Zero;
                player.Play();
                uint waitTime = 0;
                while (tt == player.Position)
                {
                    if (waitTime > 3 * 1000)
                        return;
                    Thread.Sleep(100);
                    waitTime += 100;
                }
                while (tt != player.Position)
                {
                    tt = player.Position;
                    Thread.Sleep(300);
                }
            }
            finally
            {
                this.isSpeaking = false;
            }
        });
    }

    public bool initTranslater()
    {
        var now = UtilsFun.GetUtcTimeNow();
        if (now - this.lastInitTime < 1000 * 30 || this.inited || this.isIniting)
            return false;
        lock (this.initLock)
        {
            this.isIniting = true;
            this.lastInitTime = now;
            Log.Info("custom init", typeof(TranslateHelper));
            // // if one of the api is inited, then return true. because we need. Because we want to minimize the number of initializations
            // if (this.youdaoTranslaterV2 != null || this.youdaoTranslater != null)
            //     return true;

            var actions = translatorTypes.Select((tp, idx) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    Log.Info($"start init {tp.Namespace} {tp.Name} {idx}", tp);
                    if (translaters[idx] != null)
                        return;
                    try
                    {
                        var tran = tp.GetConstructor(Type.EmptyTypes)?.Invoke(null);
                        this.translaters[idx] = tran as ITranslater;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"{idx} Error occurred: {ex.InnerException!.Message}", typeof(Translater));
                    }
                });
            });

            try
            {
                Task.WhenAll(actions).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                // 捕获并处理 Task.WhenAll 返回的聚合异常
                foreach (Exception innerException in ex.InnerExceptions)
                {
                    Log.Error($"Error occurred during task execution: {innerException.Message}", typeof(Translater));
                }
                return false;
            }
            catch (Exception err)
            {
                Log.Warn(err.Message, typeof(Translater));
                return false;
            }
            finally
            {
                this.isIniting = false;
                Log.Info("custom init complete", typeof(TranslateHelper));
            }
            return true;
        }
    }
}
