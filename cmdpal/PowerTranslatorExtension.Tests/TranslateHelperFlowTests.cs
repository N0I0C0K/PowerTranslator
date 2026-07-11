using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerTranslatorExtension.Protocol;

namespace PowerTranslatorExtension.Tests;

[TestClass]
public class TranslateHelperFlowTests
{
    [TestMethod]
    public void ParseRawSrc_ParsesTargetAliasAndNormalizesCodeWhenEnabled()
    {
        var helper = CreateHelper([], defaultLanguage: "en", codeModeEnabled: true);

        var target = helper.ParseRawSrc("camelCase -> zht");

        Assert.AreEqual("camel Case", target.src);
        Assert.AreEqual("zh-Hant", target.toLan);
    }

    [TestMethod]
    public void QueryTranslate_UsesParsedInputAndFirstTranslatorResult()
    {
        var translator = new FakeTranslator
        {
            Result = new FakeTranslateResult(new ResultItem { Title = "hello", SubTitle = "你好" }),
        };
        var helper = CreateHelper([translator], defaultLanguage: "zhs");

        var results = helper.QueryTranslate("你好 -> zht");

        Assert.HasCount(1, results);
        Assert.AreEqual("hello", results[0].Title);
        Assert.HasCount(1, translator.Calls);
        AssertCall(translator.Calls[0], "你好", "zh-Hant", "auto");
    }

    [TestMethod]
    public void QueryTranslate_FallsBackToNextTranslatorWhenFirstReturnsNull()
    {
        var primary = new FakeTranslator();
        var fallback = new FakeTranslator
        {
            Result = new FakeTranslateResult(new ResultItem { Title = "fallback", SubTitle = "source" }),
        };
        var helper = CreateHelper([primary, fallback], defaultLanguage: "en");

        var results = helper.QueryTranslate("source");

        Assert.HasCount(1, results);
        Assert.AreEqual("fallback", results[0].Title);
        Assert.HasCount(1, primary.Calls);
        Assert.HasCount(1, fallback.Calls);
        AssertCall(primary.Calls[0], "source", "en", "auto");
        AssertCall(fallback.Calls[0], "source", "en", "auto");
    }

    [TestMethod]
    public void QueryTranslate_ExplicitTargetLanguageOverridesParsedTarget()
    {
        var translator = new FakeTranslator
        {
            Result = new FakeTranslateResult(new ResultItem { Title = "bonjour", SubTitle = "hello" }),
        };
        var helper = CreateHelper([translator], defaultLanguage: "en");

        _ = helper.QueryTranslate("hello -> zhs", toLanguage: "fr");

        Assert.HasCount(1, translator.Calls);
        AssertCall(translator.Calls[0], "hello", "fr", "auto");
    }

    [TestMethod]
    public void QueryTranslate_EmptyInputSkipsTranslation()
    {
        var translator = new FakeTranslator();
        var helper = CreateHelper([translator], defaultLanguage: "en");

        var results = helper.QueryTranslate(string.Empty);

        Assert.IsEmpty(results);
        Assert.IsEmpty(translator.Calls);
    }

    private static TranslateHelper CreateHelper(
        IEnumerable<ITranslator> translators,
        string defaultLanguage,
        bool codeModeEnabled = false)
    {
        return new TranslateHelper(translators, defaultLanguage, () => codeModeEnabled);
    }

    private static void AssertCall(FakeTranslator.Call call, string text, string targetLanguage, string sourceLanguage)
    {
        Assert.AreEqual(text, call.Text);
        Assert.AreEqual(targetLanguage, call.TargetLanguage);
        Assert.AreEqual(sourceLanguage, call.SourceLanguage);
    }

    private sealed class FakeTranslateResult(params ResultItem[] items) : ITranslateResult
    {
        public IEnumerable<ResultItem> Transform() => items;
    }

    private sealed class FakeTranslator : ITranslator
    {
        public sealed record Call(string Text, string TargetLanguage, string SourceLanguage);

        public List<Call> Calls { get; } = [];
        public ITranslateResult? Result { get; init; }
        public bool Inited => true;
        public string Name => "Fake";

        public void Init()
        {
        }

        public void Reset()
        {
        }

        public ITranslateResult? Translate(string src, string toLan, string fromLan)
        {
            Calls.Add(new Call(src, toLan, fromLan));
            return Result;
        }
    }
}
