using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerTranslatorExtension.Service.Aliyun;
using PowerTranslatorExtension.Service.DeepL;
using BackupTranslateResult = PowerTranslatorExtension.Service.Youdao.Backup.TranslateResult;
using OldTranslateResponse = PowerTranslatorExtension.Service.Youdao.Old.TranslateResponse;
using V2TranslateResponse = PowerTranslatorExtension.Service.Youdao.V2.TranslateResponse;

namespace PowerTranslatorExtension.Tests;

[TestClass]
public class TranslateResultTests
{
    [TestMethod]
    public void AliyunTransform_ReturnsNullWhenRequestFailed()
    {
        Assert.IsNull(new AliyunTranslateResult { success = false }.Transform());
    }

    [TestMethod]
    public void AliyunTransform_MapsTranslationAndDetectedLanguage()
    {
        var response = new AliyunTranslateResult
        {
            success = true,
            data = new AliyunTranslateResult.Data
            {
                translateText = "hello",
                detectLanguage = "zh",
            },
        };

        var item = response.Transform()!.Single();

        Assert.AreEqual("hello", item.Title);
        Assert.AreEqual("zh", item.SubTitle);
        Assert.AreEqual("Alibaba", item.fromApiName);
    }

    [TestMethod]
    public void BackupYoudaoTransform_MapsTranslationAndWebResults()
    {
        var response = new BackupTranslateResult
        {
            errorCode = "0",
            tranType = "zh-CHS2en",
            query = "你好",
            translation = ["hello", "hi"],
            basic = new BackupTranslateResult.Basic { phonetic = "ni hao" },
            web =
            [
                new BackupTranslateResult.Web { key = "你好", value = ["hello", "how are you"] },
            ],
        };

        var items = response.Transform()!.ToList();

        Assert.HasCount(2, items);
        Assert.AreEqual("hello,hi", items[0].Title);
        Assert.AreEqual("你好(ni hao) [zh-CHS2en] backup", items[0].SubTitle);
        Assert.AreEqual("hello | how are you", items[1].Title);
        Assert.IsTrue(items.All(item => item.fromApiName == "Backup Youdao Api"));
    }

    [TestMethod]
    public void OldYoudaoTransform_MapsFirstTranslation()
    {
        var response = new OldTranslateResponse
        {
            errorCode = 0,
            type = "ZH_CN2EN",
            translateResult =
            [
                [new OldTranslateResponse.ResStruct { src = "你好", tgt = "hello" }],
            ],
        };

        var item = response.Transform()!.Single();

        Assert.AreEqual("hello", item.Title);
        Assert.AreEqual("你好", item.SubTitle);
        Assert.AreEqual("ZH_CN2EN", item.transType);
        Assert.AreEqual("Youdao Old Web Api", item.fromApiName);
    }

    [TestMethod]
    public void V2YoudaoTransform_MapsPronunciationAndCopyTarget()
    {
        var response = new V2TranslateResponse
        {
            code = 0,
            type = "ZH_CN2EN",
            translateResult =
            [
                [
                    new V2TranslateResponse.TranslateResult
                    {
                        src = "你好",
                        tgt = "hello",
                        srcPronounce = "ni hao",
                        tgtPronounce = "həˈləʊ",
                    },
                ],
            ],
        };

        var item = response.Transform()!.Single();

        Assert.AreEqual("hello", item.Title);
        Assert.AreEqual("你好 (ni hao)", item.SubTitle);
        Assert.AreEqual("hello", item.CopyTgt);
        Assert.AreEqual("ZH_CN2EN", item.transType);
        Assert.AreEqual("Youdao Web Api", item.fromApiName);
        StringAssert.Contains(item.Description, "həˈləʊ");
    }

    [TestMethod]
    public void DeepLTransform_FlattensAllBeamsAndSentences()
    {
        var response = new DeepLTranslateResult
        {
            Result = new DeepLTranslateResult.InnerResult
            {
                SourceLang = "ZH",
                TargetLang = "EN",
                Translations =
                [
                    new DeepLTranslateResult.Translation
                    {
                        Beams =
                        [
                            new DeepLTranslateResult.Beams
                            {
                                Sentences =
                                [
                                    new DeepLTranslateResult.Sentence { Text = "hello" },
                                    new DeepLTranslateResult.Sentence { Text = "world" },
                                ],
                            },
                        ],
                    },
                ],
            },
        };

        var items = response.Transform()!.ToList();

        CollectionAssert.AreEqual(new[] { "hello", "world" }, items.Select(item => item.Title).ToArray());
        Assert.IsTrue(items.All(item => item.SubTitle == "ZH -> EN"));
        Assert.IsTrue(items.All(item => item.CopyTgt == item.Title));
        Assert.IsTrue(items.All(item => item.fromApiName == "DeepL"));
    }
}
