using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace PowerTranslatorExtension;

internal sealed partial class LanguageListPage : ListPage
{
    public LanguageListPage()
    {
        Icon = IconHelpers.FromRelativePaths("Assets/translator.light.png", "Assets/translator.dark.png");
        Title = Loc.Get("Cmd_Languages_Title");
        Name = Loc.Get("Verb_Open");
        PlaceholderText = Loc.Get("Page_Languages_Placeholder");
    }

    public override IListItem[] GetItems()
    {
        return SettingsManager.Languages.Select(kv => new ListItem(new CopyTextCommand(kv.Key))
        {
            Title = kv.Key,
            Subtitle = Loc.Language(kv.Key),
            Icon = this.Icon,
        }).ToArray<IListItem>();
    }
}
