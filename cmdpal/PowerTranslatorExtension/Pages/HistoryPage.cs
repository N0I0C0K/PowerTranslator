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
        Title = "Translation history";
        Name = "Open";
        PlaceholderText = "Recent translations";
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
                    Title = "No history yet",
                    Subtitle = "Recent translations will appear here",
                    Icon = this.Icon,
                },
            };
        }
        return items.ToResultList(this.Icon, translateHelper).ToArray();
    }
}
