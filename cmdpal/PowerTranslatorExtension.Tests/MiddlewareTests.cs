using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerTranslatorExtension.Middleware.Alias;
using PowerTranslatorExtension.Middleware.LanguageCodeHelper;
using PowerTranslatorExtension.Service.Youdao.Utils;

namespace PowerTranslatorExtension.Tests;

[TestClass]
public class MiddlewareTests
{
    [TestMethod]
    public void CultureAliasMiddleware_ReturnsAliasOrOriginalValue()
    {
        var middleware = new CultureAliasMiddleware(new Dictionary<string, string>
        {
            ["zhs"] = "zh-Hans",
        });

        Assert.AreEqual("zh-Hans", middleware.GetCultureFromAlias("zhs"));
        Assert.AreEqual("en", middleware.GetCultureFromAlias("en"));
    }

    [DataTestMethod]
    [DataRow("zh-Hans", "zh-CHS")]
    [DataRow("zh-CN", "zh-CHS")]
    [DataRow("zh-TW", "zh-CHT")]
    [DataRow("zh", "zh-CHS")]
    [DataRow("en-US", "en")]
    [DataRow("ja", "ja")]
    public void YoudaoUtils_MapsCultureNamesToApiLanguageCodes(string input, string expected)
    {
        Assert.AreEqual(expected, YoudaoUtils.Instance.GetYoudaoLanguageCode(input));
    }

    [TestMethod]
    public void DiffCultureLanguageCodeMiddleware_AppliesBaseLanguageOverride()
    {
        var middleware = new DiffCultureLanguageCodeMiddleware(new Dictionary<string, string>
        {
            ["pt"] = "pt-PT",
        });

        Assert.AreEqual("pt-PT", middleware.GetLanguageCode("pt-BR"));
        Assert.AreEqual("fr", middleware.GetLanguageCode("fr-CA"));
    }
}
