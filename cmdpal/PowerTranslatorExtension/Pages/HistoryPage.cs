using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslatorExtension.History;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension;

internal sealed partial class HistoryPage : ListPage
{
    private readonly TranslateHelper translateHelper;

    public HistoryPage(TranslateHelper translateHelper)
    {
        this.translateHelper = translateHelper;
        Icon = IconHelpers.FromRelativePaths("Assets/translator.light.png", "Assets/translator.dark.png");
        Title = Loc.Get("Cmd_History_Title");
        Name = Loc.Get("Verb_Open");
        PlaceholderText = Loc.Get("Page_History_Placeholder");
    }

    public override IListItem[] GetItems()
    {
        var items = HistoryHelper.Instance.Query().ToList();
        if (items.Count == 0)
        {
            return new IListItem[]
            {
                new ListItem(new NoOpCommand())
                {
                    Title = Loc.Get("Empty_NoHistory_Title"),
                    Subtitle = Loc.Get("Empty_NoHistory_Subtitle"),
                    Icon = this.Icon,
                },
            };
        }
        return items.ToResultList(this.Icon, translateHelper).ToArray();
    }
}
