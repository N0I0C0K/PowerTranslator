using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Utils;
using Windows.Media.Core;
using Windows.Media.Playback;
namespace PowerTranslatorExtension;

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
    public bool inited => this.translators.Count > 0 && this.translators[0].Inited;
    private object initLock = new Object();
    private readonly object translateLock = new();
    private long lastInitTime;
    private List<ITranslator> translators;
    private readonly Func<bool> isCodeModeEnabled;
    private bool isSpeaking;
    private bool isIniting;
    public string defaultLanguageKey = "auto";
    private Middleware.Alias.CultureAliasMiddleware cultureAliasHelper = new(
        new Dictionary<string, string>{
            { "zhs", "zh-Hans" },
            { "zht", "zh-Hant" },
        }
    );
    public TranslateHelper(string defaultLanguageKey = "auto")
        : this(
            [
                new Service.Youdao.V2.YoudaoTranslator(),
                new Service.Youdao.Old.YoudaoTranslator(),
                new Service.DeepL.DeepLTranslator(),
                new Service.Youdao.Backup.BackUpTranslator(),
            ],
            defaultLanguageKey,
            () => SettingsManager.Instance.EnableCodeMode)
    {
        UtilsFun.onHttpDefaultHandlerChange += this.Reload;
    }

    internal TranslateHelper(
        IEnumerable<ITranslator> translators,
        string defaultLanguageKey,
        Func<bool> isCodeModeEnabled)
    {
        ArgumentNullException.ThrowIfNull(translators);
        ArgumentNullException.ThrowIfNull(isCodeModeEnabled);

        this.translators = translators.ToList();
        this.defaultLanguageKey = defaultLanguageKey;
        this.isCodeModeEnabled = isCodeModeEnabled;
    }

    public TranslateTarget ParseRawSrc(string src)
    {
        string _src, _toLan;

        if (src.Contains(toLanSplit))
        {
            var srcArr = src.Split(toLanSplit);
            _src = srcArr.First().TrimEnd().TrimStart();
            _toLan = srcArr.Last().TrimEnd().TrimStart();
        }
        else
        {
            _src = src;
            _toLan = this.defaultLanguageKey;
        }
        if (isCodeModeEnabled())
        {
            _src = UtilsFun.ConvertSnakeCaseOrCamelCaseToNormalSpace(_src);
        }

        return new TranslateTarget
        {
            src = _src,
            toLan = cultureAliasHelper.GetCultureFromAlias(_toLan)
        };
    }
    private ITranslateResult? Translate(string text, string toLan)
    {
        lock (translateLock)
        {
            return translators.Enumerate().FirstNotNoneCast((data) =>
            {
                var (idx, it) = data;
                try
                {
                    if (it == null)
                    {
                        throw new InvalidOperationException($"Translator at index {idx} is null");
                    }
                    return it?.Translate(text, toLan, "auto");
                }
                catch (Exception err)
                {
                    UtilsFun.LogMessage(err.Message);
                    return null;
                }
            });
        }
    }
    public List<ResultItem> QueryTranslate(string raw, string? toLanguage = null)
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
                    {
                        // this.publicAPI?.ChangeQuery(raw, true);
                    }
                });
            }
            res.Add(new ResultItem
            {
                Title = Loc.Get("Status_Initializing_Title"),
                SubTitle = Loc.Get("Status_Initializing_Subtitle")
            });
            return res;
        }

        var target = ParseRawSrc(raw);
        string src = target.src;
        string toLan = toLanguage ?? target.toLan;
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
                Title = Loc.Get("Status_TranslateError_Title"),
                SubTitle = Loc.Get("Status_TranslateError_Subtitle"),
                Action = () =>
                {
                    UtilsFun.SetClipboardText("https://github.com/N0I0C0K/PowerTranslator/issues?q=");
                    //this.publicAPI.ShowMsg("Copy!", "The URL has been copied, Go to your browser and visit the website for help.");
                    return true;
                }
            });
        }

        return res;
    }
    private bool InitTranslator()
    {
        var now = UtilsFun.GetUtcTimeNow();
        if (now - this.lastInitTime < 1000 * 30 || this.inited || this.isIniting)
            return false;
        lock (this.initLock)
        {
            this.isIniting = true;
            this.lastInitTime = now;
            UtilsFun.LogMessage("custom init");

            var actions = translators.Select((translator, idx) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    if (translator.Inited)
                    {
                        return;
                    }
                    try
                    {
                        UtilsFun.LogMessage($"start init {translator.Name}");
                        translator.Init();
                        UtilsFun.LogMessage($"init {translator.Name} success: {translator.Inited}");
                    }
                    catch (Exception ex)
                    {
                        UtilsFun.LogMessage($"Init {translator.Name} Error occurred: {(ex.InnerException ?? ex).Message}");
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
                    UtilsFun.LogMessage($"Error occurred during task execution: {innerException.Message}");
                }
                return false;
            }
            catch (Exception err)
            {
                UtilsFun.LogMessage($"Error occurred: {err.Message}");
                return false;
            }
            finally
            {
                this.isIniting = false;
                UtilsFun.LogMessage("custom init complete");
            }
            return true;
        }
    }

    public void Reload()
    {
        Task.Factory.StartNew(() =>
        {
            lock (translateLock)
            {
                foreach (var translator in translators)
                {
                    try
                    {
                        translator.Reset();
                    }
                    catch (Exception ex)
                    {
                        UtilsFun.LogMessage($"Reset {translator.Name} failed: {ex.Message}");
                    }
                }
            }
        });
    }

    public void Read(string? txt)
    {
        if (isSpeaking || string.IsNullOrEmpty(txt))
            return;
        Task.Factory.StartNew(() =>
        {
            isSpeaking = true;
            MediaPlayer? player = null;
            try
            {
                var uri = new Uri($"https://dict.youdao.com/dictvoice?audio={Uri.EscapeDataString(txt.removeSpecialCharacter())}&le=zh");
                uri.fixChinese();
                player = new MediaPlayer
                {
                    Source = MediaSource.CreateFromUri(uri),
                    Volume = 1.0,
                };
                var ended = new ManualResetEventSlim(false);
                player.MediaEnded += (s, e) => ended.Set();
                player.MediaFailed += (s, e) =>
                {
                    UtilsFun.LogMessage($"TTS failed: {e.ErrorMessage}");
                    ended.Set();
                };
                player.Play();
                ended.Wait(TimeSpan.FromSeconds(15));
            }
            catch (Exception ex)
            {
                UtilsFun.LogMessage($"Read failed: {ex.Message}");
            }
            finally
            {
                player?.Dispose();
                isSpeaking = false;
            }
        });
    }
}
