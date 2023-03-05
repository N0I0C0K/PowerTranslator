using Wox.Plugin;
using Wox.Plugin.Logger;
using Translater.Utils;
using System.Windows.Media;

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
    public bool inited => this.youdaoTranslater != null || this.youdaoTranslaterV2 != null;
    private object initLock = new Object();
    private Youdao.YoudaoTranslater? youdaoTranslater;
    private Youdao.V2.YoudaoTranslater? youdaoTranslaterV2;
    private Youdao.Backup.BackUpTranslater? backUpTranslater;
    private long lastInitTime = 0;
    private IPublicAPI publicAPI;
    private List<Youdao.ITranslater?> translaters;
    public TranslateHelper(IPublicAPI publicAPI)
    {
        this.translaters = new List<Youdao.ITranslater?>(3);
        this.initTranslater();
        this.publicAPI = publicAPI;

        // backup translater, We don't need to initialize it with the others, because it doesn't have an error
        this.backUpTranslater = new Youdao.Backup.BackUpTranslater();
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
            toLan = "AUTO"
        };
    }
    public List<ResultItem> QueryTranslate(string raw, string translateFrom = "user input")
    {
        var res = new List<ResultItem>();
        if (raw.Length == 0)
            return res;
        var target = ParseRawSrc(raw);
        string src = target.src;
        string toLan = target.toLan;
        Youdao.ITranslateResult? translateResult = null;
        translateResult = this.translaters.FirstNotNoneCast<Youdao.ITranslater?, Youdao.ITranslateResult>((it) =>
        {
            try
            {
                return it?.Translate(src, toLan, "auto");
            }
            catch (Exception err)
            {
                Log.Error(err.Message, typeof(TranslateHelper));
                return null;
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
                Title = "Some error happen!",
                SubTitle = $"Press enter to get help",
                Action = (ev) =>
                {
                    UtilsFun.SetClipboardText("https://github.com/N0I0C0K/PowerToysRun.Plugin.Translater/issues?q=");
                    this.publicAPI.ShowMsg("Copy!", "The URL has been copied, Go to your browser and visit the website for help.");
                    return true;
                }
            });
        }
        return res;
    }

    public void Read(string txt)
    {
        Task.Factory.StartNew(() =>
        {
            MediaPlayer player = new MediaPlayer();
            player.Stop();
            player.Volume = 1;
            player.Open(new Uri($"https://dict.youdao.com/dictvoice?audio={txt}&le=zh"));
            TimeSpan tt = TimeSpan.Zero;
            player.Play();
            uint waitTime = 0;
            while (tt == player.Position)
            {
                if (waitTime > 3 * 100)
                    return;
                Thread.Sleep(100);
                waitTime += 100;
            }
            while (tt != player.Position)
            {
                tt = player.Position;
                Thread.Sleep(300);
            }
        });
    }

    public bool initTranslater()
    {
        var now = UtilsFun.GetUtcTimeNow();
        if (now - this.lastInitTime < 1000 * 30)
            return false;
        lock (this.initLock)
        {
            Log.Info("custom init", typeof(TranslateHelper));
            // if one of the api is inited, then return true. because we need. Because we want to minimize the number of initializations
            if (this.youdaoTranslaterV2 != null || this.youdaoTranslater != null)
                return true;
            this.lastInitTime = now;
            try
            {
                youdaoTranslater ??= new Youdao.YoudaoTranslater();
                youdaoTranslaterV2 ??= new Youdao.V2.YoudaoTranslater();
                return true;
            }
            catch (Exception err)
            {
                Log.Warn(err.Message, typeof(Translater));
                return false;
            }
            finally
            {
                this.translaters.Clear();
                this.translaters.Add(youdaoTranslaterV2);
                this.translaters.Add(youdaoTranslater);
                this.translaters.Add(backUpTranslater);
                Log.Info("custom init complete", typeof(TranslateHelper));
            }
        }
    }
}
