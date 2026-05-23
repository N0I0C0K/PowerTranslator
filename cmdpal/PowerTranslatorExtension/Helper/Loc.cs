using Microsoft.Windows.ApplicationModel.Resources;

namespace PowerTranslatorExtension;

internal static class Loc
{
    private static readonly ResourceLoader _loader = new();

    public static string Get(string key) => _loader.GetString(key);

    public static string Language(string code)
    {
        var v = _loader.GetString($"Language_{code}");
        return string.IsNullOrEmpty(v) ? code : v;
    }

    public static string Dictionary(string name)
    {
        var v = _loader.GetString($"Dictionary_{name}");
        return string.IsNullOrEmpty(v) ? name : v;
    }
}
