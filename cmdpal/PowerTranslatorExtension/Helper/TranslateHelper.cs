using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension;

public class TranslateFailedException : Exception
{
    public TranslateFailedException()
        : base("translate failed")
    {
    }
}

public sealed class TranslateHelper
{
    public struct TranslateTarget
    {
        public string src;
        public string toLan;
    }

    public const string ToLanSplit = "->";

    private readonly object _initLock = new();
    private readonly List<ITranslator> _translators;
    private readonly Middleware.Alias.CultureAliasMiddleware _cultureAliasHelper = new(
        new Dictionary<string, string>
        {
            { "zhs", "zh-Hans" },
            { "zht", "zh-Hant" },
        });
    private long _lastInitTime;
    private bool _isIniting;

    public TranslateHelper()
    {
        _translators =
        [
            new Service.Youdao.V2.YoudaoTranslator(),
            new Service.Youdao.old.YoudaoTranslator(),
            new Service.DeepL.DeepLTranslator(),
            new Service.Youdao.Backup.BackUpTranslator(),
        ];
        UtilsFun.onHttpDefaultHandlerChange += Reload;
    }

    public bool inited => _translators.Any(t => t.Inited);

    public TranslateTarget ParseRawSrc(string src)
    {
        string rawSrc;
        string rawTargetLanguage;

        if (src.Contains(ToLanSplit, StringComparison.Ordinal))
        {
            var srcArr = src.Split(ToLanSplit, StringSplitOptions.None);
            rawSrc = srcArr.First().Trim();
            rawTargetLanguage = srcArr.Last().Trim();
        }
        else
        {
            rawSrc = src;
            rawTargetLanguage = SettingHelper.Instance.defaultLanguageKey;
        }

        if (SettingHelper.Instance.enableCodeMode)
        {
            rawSrc = UtilsFun.ConvertSnakeCaseOrCamelCaseToNormalSpace(rawSrc);
        }

        return new TranslateTarget
        {
            src = rawSrc,
            toLan = _cultureAliasHelper.GetCultureFromAlias(rawTargetLanguage),
        };
    }

    public List<ResultItem> QueryTranslate(string raw, string translateFrom = "user input", string? toLanguage = null)
    {
        List<ResultItem> res = [];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return res;
        }

        if (!inited)
        {
            if (!_isIniting)
            {
                Task.Factory.StartNew(InitTranslator);
            }

            res.Add(new ResultItem
            {
                Title = "initializing other apis...",
                SubTitle = "please try later",
            });
            return res;
        }

        var target = ParseRawSrc(raw);
        var translateResult = Translate(target.src, toLanguage ?? target.toLan);
        if (translateResult != null)
        {
            var transformed = translateResult.Transform();
            if (transformed != null)
            {
                res.AddRange(transformed);
            }
        }
        else
        {
            res.Add(new ResultItem
            {
                Title = "result is null, some error happen in translate. check out your network!",
                SubTitle = "Press enter to get help",
                Action = () => UtilsFun.OpenInShell("https://github.com/N0I0C0K/PowerTranslator/issues?q="),
            });
        }

        return res;
    }

    public void Read(string? txt)
    {
        UtilsFun.ReadText(txt);
    }

    public void Reload()
    {
        foreach (var translator in _translators)
        {
            Task.Factory.StartNew(translator.Reset);
        }
    }

    private ITranslateResult? Translate(string text, string toLan)
    {
        return _translators.Enumerate().FirstNotNoneCast(data =>
        {
            var (_, translator) = data;
            try
            {
                return translator.Translate(text, toLan, "auto");
            }
            catch (Exception err)
            {
                UtilsFun.LogMessage($"{translator.Name}: {err.Message}");
                return null;
            }
        });
    }

    private bool InitTranslator()
    {
        var now = UtilsFun.GetUtcTimeNow();
        if ((now - _lastInitTime) < (1000 * 30) || inited || _isIniting)
        {
            return false;
        }

        lock (_initLock)
        {
            _isIniting = true;
            _lastInitTime = now;

            var actions = _translators.Select(translator => Task.Factory.StartNew(() =>
            {
                if (translator.Inited)
                {
                    return;
                }

                try
                {
                    translator.Init();
                    UtilsFun.LogMessage($"init {translator.Name} success: {translator.Inited}");
                }
                catch (Exception ex)
                {
                    UtilsFun.LogMessage($"Init {translator.Name} Error occurred: {(ex.InnerException ?? ex).Message}");
                }
            }));

            try
            {
                Task.WhenAll(actions).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                foreach (var innerException in ex.InnerExceptions)
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
                _isIniting = false;
            }

            return true;
        }
    }
}
