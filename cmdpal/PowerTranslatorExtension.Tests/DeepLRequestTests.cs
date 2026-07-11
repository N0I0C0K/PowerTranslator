using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerTranslatorExtension.Service.DeepL;

namespace PowerTranslatorExtension.Tests;

[TestClass]
public class DeepLRequestTests
{
    [TestMethod]
    public void Params_CreatesSinglePlainTextTranslationJob()
    {
        var parameters = new Params("EN", "ZH", "你好");

        Assert.AreEqual("EN", parameters.Lang.TargetLang);
        Assert.AreEqual("ZH", parameters.Lang.SourceLangComputed);
        Assert.HasCount(1, parameters.Jobs);
        Assert.HasCount(1, parameters.Jobs[0].Sentences);
        Assert.AreEqual("你好", parameters.Jobs[0].Sentences[0].Text);
        Assert.AreEqual("plaintext", parameters.commonJobParams.TextType);
    }

    [TestMethod]
    public void RequestBody_DerivesIdAndUsesExpectedJsonRpcShape()
    {
        var parameters = new Params("EN", null, "hello") { Timestamp = 123456789 };
        var request = new DeepLTranslateRequestBody(parameters);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(request));

        Assert.AreEqual(23456789, request.Id);
        Assert.AreEqual("2.0", json.RootElement.GetProperty("jsonrpc").GetString());
        Assert.AreEqual("LMT_handle_jobs", json.RootElement.GetProperty("method").GetString());
        Assert.AreEqual("EN", json.RootElement.GetProperty("params").GetProperty("lang").GetProperty("target_lang").GetString());
    }
}
