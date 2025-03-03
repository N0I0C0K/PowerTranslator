using System.Linq;
using Wox.Plugin;
using Wox.Plugin.Logger;
using Translator.Utils;
using System.Windows.Media;
using Translator.Protocol;
using Translator.Properties;
namespace Translator;

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
    public bool inited => this.translators.Count >= 3 && (this.translators[0] != null || this.translators[1] != null);
    private object initLock = new Object();
    private long lastInitTime = 0;
    private IPublicAPI publicAPI;
    private List<ITranslator?> translators;
    private List<Type> translatorGenerators;
    private bool isSpeaking = false;
    private bool isIniting = false;
    public string defaultLanguageKey = "auto";
    public TranslateHelper(IPublicAPI publicAPI, string defaultLanguageKey = "auto")
    {
        this.translators = new List<ITranslator?>{
            null, null, null
        };
        translatorGenerators = new List<Type>{
            typeof(Service.Youdao.V2.YoudaoTranslator),
            typeof(Service.Youdao.old.YoudaoTranslator),
            typeof(Service.Youdao.Backup.BackUpTranslator)
        };
        this.InitTranslator();
        this.publicAPI = publicAPI;
        this.defaultLanguageKey = defaultLanguageKey;

        UtilsFun.onHttpDefaultHandlerChange += this.Reload;
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
    private ITranslateResult? Translate(string text, string toLan)
    {
        return translators.Enumerate().FirstNotNoneCast((data) =>
        {
            var (idx, it) = data;
            try
            {
                if (it == null)
                {

                    throw new Exception($"{this.translatorGenerators[idx].FullName} is null");
                }
                return it?.Translate(text, toLan, "auto");
            }
            catch (Exception err)
            {
                Log.Error(err.Message, typeof(TranslateHelper));
                return null;
            }
        });
    }
    public List<ResultItem> QueryTranslate(string raw, string translateFrom = "user input", string? toLanuage = null)
    {
        var res = new List<ResultItem>();
        if (raw.Length == 0)
            return res;

        if (!inited)
        {
            if (!this.isIniting)
            {
                Task.Factory.StartNew(() =>
                {
                    if (this.InitTranslator())
                        this.publicAPI?.ChangeQuery(raw, true);
                });
            }
            res.Add(new ResultItem
            {
                Title = Resources.Error_Init_Title,
                SubTitle = Resources.Error_Init_SubTitle,
            });
            return res;
        }

        var target = ParseRawSrc(raw);
        string src = target.src;
        string toLan = toLanuage ?? target.toLan;
        ITranslateResult? translateResult = Translate(src, toLan);

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
                Title = Resources.Error_Null_title,
                SubTitle = Resources.Error_Null_SubTitile,
                Action = (ev) =>
                {
                    UtilsFun.SetClipboardText("https://github.com/N0I0C0K/PowerTranslator/issues?q=");
                    this.publicAPI.ShowMsg(Resources.Error_Null_ModalTitle, Resources.Error_Null_ModalSubTitle);
                    return true;
                }
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

    public bool InitTranslator()
    {
        var now = UtilsFun.GetUtcTimeNow();
        if (now - this.lastInitTime < 1000 * 30 || this.inited || this.isIniting)
            return false;
        lock (this.initLock)
        {
            this.isIniting = true;
            this.lastInitTime = now;
            Log.Info("custom init", typeof(TranslateHelper));

            var actions = translatorGenerators.Select((tp, idx) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    Log.Info($"start init {tp.Namespace} {tp.Name} {idx}", tp);
                    if (translators[idx] != null)
                        return;
                    try
                    {
                        var tran = tp.GetConstructor(Type.EmptyTypes)?.Invoke(null);
                        this.translators[idx] = tran as ITranslator;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"{idx} Error occurred: {ex.InnerException!.Message}", typeof(Translator));
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
                    Log.Error($"Error occurred during task execution: {innerException.Message}", typeof(Translator));
                }
                return false;
            }
            catch (Exception err)
            {
                Log.Warn(err.Message, typeof(Translator));
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

    public void Reload()
    {
        foreach (var translator in translators)
        {
            Task.Factory.StartNew(() =>
            {
                translator?.Reset();
            });
        }
    }
}
