using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Utils;
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
    private long lastInitTime;
    private List<ITranslator> translators;
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
    {
        this.translators = [
            new Service.Youdao.V2.YoudaoTranslator()
        ];
        // make sure do it before init translator
        // UtilsFun.ChangeDefaultHttpHandlerProxy(SettingHelper.Instance.useSystemProxy, false);

        // this.InitTranslator();
        this.defaultLanguageKey = defaultLanguageKey;
        UtilsFun.LogMessage("custom init 3");

        //UtilsFun.onHttpDefaultHandlerChange += this.Reload;
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
        _src = UtilsFun.ConvertSnakeCaseOrCamelCaseToNormalSpace(_src);
        // if (SettingHelper.Instance.enableCodeMode)
        // {

        // }

        return new TranslateTarget
        {
            src = _src,
            toLan = cultureAliasHelper.GetCultureFromAlias(_toLan)
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
                    throw new Exception($"{it.Name} is null");
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
                Title = "initializing other apis...",
                SubTitle = "please try later"
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
                Title = "result is null, some error happen in translate. check out your network!",
                SubTitle = "Press enter to get help",
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
    public bool InitTranslator()
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
        foreach (var translator in translators)
        {
            Task.Factory.StartNew(() =>
            {
                translator?.Reset();
            });
        }
    }
}
