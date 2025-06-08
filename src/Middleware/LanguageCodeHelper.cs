namespace Translator.Middleware.LanguageCodeHelper;

public class DiffCultureLanguageCodeHelper
{
    public Dictionary<string, string> diffPaires;
    public DiffCultureLanguageCodeHelper(Dictionary<string, string> diffPaires)
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