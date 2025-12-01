using System.Collections.Generic;

namespace PowerTranslatorExtension.Middleware.LanguageCodeHelper;

public class DiffCultureLanguageCodeMiddleware
{
    private Dictionary<string, string> diffPaires;
    public DiffCultureLanguageCodeMiddleware(Dictionary<string, string> diffPaires)
    {
        this.diffPaires = diffPaires;
    }

    public string GetLanguageCode(string curtualName)
    {
        if (diffPaires.ContainsKey(curtualName))
        {
            return diffPaires[curtualName];
        }
        if (curtualName.Contains('-'))
        {
            var curtualCodes = curtualName.Split('-');
            var languageCode = curtualCodes[0];

            return diffPaires.GetValueOrDefault(languageCode, languageCode);
        }
        return curtualName;
    }
}