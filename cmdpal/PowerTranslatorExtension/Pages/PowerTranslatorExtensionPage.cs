// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslatorExtension;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension;

internal sealed partial class PowerTranslatorExtensionPage : DynamicListPage
{
    private object delayLock = new object();
    private long lastQueryTick;
    private TranslateHelper translateHelper;

    public PowerTranslatorExtensionPage(TranslateHelper translateHelper)
    {
        Icon = IconHelpers.FromRelativePaths("Assets/translator.light.png", "Assets/translator.dark.png");
        Title = "Translator";
        Name = "Open";
        PlaceholderText = "Enter any text...";
        this.translateHelper = translateHelper;
        this.EmptyContent = new CommandItem
        {
            Title = PlaceholderText,
            Icon = this.Icon
        };
    }

    public override IListItem[] GetItems()
    {
        var res = this.translateHelper.QueryTranslate(this.SearchText);
        return res.ToResultList(this.Icon).ToArray();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        Task.Factory.StartNew(() =>
        {
            long thisTick = 0;
            lock (delayLock)
            {
                thisTick = ++lastQueryTick;
            }
            Task.Delay(500).GetAwaiter().GetResult();
            if (thisTick != lastQueryTick)
                return;
            RaiseItemsChanged(0);
        });
    }
}
