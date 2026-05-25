// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslatorExtension.Suggest;

namespace PowerTranslatorExtension;

public partial class PowerTranslatorExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly TranslateHelper translateHelper;
    private readonly SuggestHelper suggestHelper;

    public PowerTranslatorExtensionCommandsProvider()
    {
        DisplayName = Loc.Get("Cmd_Translator_DisplayName");
        Icon = IconHelpers.FromRelativePaths("Assets/translator.light.png", "Assets/translator.dark.png");

        Settings = SettingsManager.Instance.Settings;
        SettingsManager.Instance.OnSettingsChanged += () =>
        {
            this.translateHelper!.defaultLanguageKey = SettingsManager.Instance.DefaultLanguageKey;
        };

        this.translateHelper = new TranslateHelper(SettingsManager.Instance.DefaultLanguageKey);
        this.suggestHelper = new SuggestHelper();
        var mainPage = new PowerTranslatorExtensionPage(translateHelper, suggestHelper);
        var historyPage = new HistoryPage(translateHelper);
        var languagePage = new LanguageListPage();

        _commands =
        [
            new CommandItem(mainPage) { Title = DisplayName, Subtitle = Loc.Get("Cmd_Translator_Subtitle") },
            new CommandItem(historyPage) { Title = Loc.Get("Cmd_History_Title"), Subtitle = Loc.Get("Cmd_History_Subtitle") },
            new CommandItem(languagePage) { Title = Loc.Get("Cmd_Languages_Title"), Subtitle = Loc.Get("Cmd_Languages_Subtitle") },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }
}
