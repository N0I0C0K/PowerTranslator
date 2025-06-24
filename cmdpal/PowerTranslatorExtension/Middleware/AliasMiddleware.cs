using System.Collections.Generic;

namespace PowerTranslatorExtension.Middleware.Alias;

public class CultureAliasMiddleware
{
    private Dictionary<string, string> aliasPaires;
    public CultureAliasMiddleware(Dictionary<string, string> aliasPaires)
    {
        this.aliasPaires = aliasPaires;
    }

    public string GetCultureFromAlias(string curtualName)
    {
        if (aliasPaires.ContainsKey(curtualName))
        {
            return aliasPaires[curtualName];
        }
        return curtualName;
    }
}