using System.Collections.Generic;

namespace PowerTranslatorExtension.Service.Youdao.Utils;

public class YoudaoUtils
{
    Middleware.LanguageCodeHelper.DiffCultureLanguageCodeMiddleware youdaoLanguageCodeHelper = new(
        new Dictionary<string, string>{
            { "zh-Hans", "zh-CHS" },
            { "zh-Hant", "zh-CHT" },
            { "zh-CN", "zh-CHS" },
            { "zh-TW", "zh-CHT" },
            { "zh-SG", "zh-CHS" },
            {"zh", "zh-CHS"}
        }
    );


    public string GetYoudaoLanguageCode(string curtualName)
    {
        return youdaoLanguageCodeHelper.GetLanguageCode(curtualName);
    }

    private static YoudaoUtils? __instance;
    public static YoudaoUtils Instance
    {
        get
        {
            __instance ??= new YoudaoUtils();
            return __instance;
        }
    }
}