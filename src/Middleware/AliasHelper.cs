namespace Translator.Middleware.Alias;

public class CultureAliasHelper
{
    public Dictionary<string, string> aliasPaires;
    public CultureAliasHelper(Dictionary<string, string> aliasPaires)
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