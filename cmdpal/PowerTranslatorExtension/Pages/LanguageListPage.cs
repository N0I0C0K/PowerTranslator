using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace PowerTranslatorExtension;

internal sealed partial class LanguageListPage : ListPage
{
    public LanguageListPage()
    {
        Icon = IconHelpers.FromRelativePaths("Assets/translator.light.png", "Assets/translator.dark.png");
        Title = "Supported languages";
        Name = "Open";
        PlaceholderText = "Use short code as target language";
    }

    public override IListItem[] GetItems()
    {
        return SettingsManager.Languages.Select(kv => new ListItem(new CopyTextCommand(kv.Key))
        {
            Title = kv.Key,
            Subtitle = kv.Value,
            Icon = this.Icon,
        }).ToArray<IListItem>();
    }
}
