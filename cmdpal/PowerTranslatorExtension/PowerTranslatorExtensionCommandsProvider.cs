// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace PowerTranslatorExtension;

public partial class PowerTranslatorExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly TranslateHelper _translateHelper;
    private readonly SettingHelper _settingHelper;

    public PowerTranslatorExtensionCommandsProvider()
    {
        DisplayName = "Translator";
        Icon = IconHelpers.FromRelativePaths("Assets/translator.light.png", "Assets/translator.dark.png");
        _translateHelper = new TranslateHelper();
        _settingHelper = SettingHelper.Instance;
        Settings = _settingHelper.Settings;
        _commands =
        [
            new CommandItem(new PowerTranslatorExtensionPage(_translateHelper, _settingHelper))
            {
                Title = DisplayName,
                Subtitle = "Translate any text",
                MoreCommands = [new CommandContextItem(_settingHelper.Settings.SettingsPage)],
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }
}
