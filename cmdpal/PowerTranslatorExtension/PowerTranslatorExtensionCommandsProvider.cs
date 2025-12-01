// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslatorExtension;

namespace PowerTranslatorExtension;

public partial class PowerTranslatorExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private TranslateHelper translateHelper;

    public PowerTranslatorExtensionCommandsProvider()
    {
        DisplayName = "Translator";
        Icon = IconHelpers.FromRelativePaths("Assets/translator.light.png", "Assets/translator.dark.png");
        this.translateHelper = new TranslateHelper();
        _commands = [
            new CommandItem(new PowerTranslatorExtensionPage(translateHelper)) { Title = DisplayName, Subtitle = "Translate any text" },
        ];
    }
    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }
}
