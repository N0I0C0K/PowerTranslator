# Version Management

## How to Update the Plugin Version

The PowerTranslator plugin uses a centralized version management system. To update the version:

1. **Update the version in ONE place only**: Edit the `<Version>` property in `Translater.csproj`:
   ```xml
   <PropertyGroup>
       <Version>0.12.0</Version>
       <AssemblyVersion>0.12.0</AssemblyVersion>
       <FileVersion>0.12.0</FileVersion>
   </PropertyGroup>
   ```

2. **Automatic synchronization**: When you build the project:
   - The `plugin.json` file will be automatically updated with the same version
   - The assembly metadata will contain the version (displayed in PowerToys Run)
   - The `VersionInfo.Version` property will return the current version at runtime

## Version Information Locations

- **Source of truth**: `Translater.csproj` - `<Version>` property
- **Auto-synced**: `plugin.json` - updated during build
- **Runtime access**: `VersionInfo.Version` static property in code

## Example: Updating to Version 0.13.0

```xml
<!-- In Translater.csproj -->
<PropertyGroup>
    <Version>0.13.0</Version>
    <AssemblyVersion>0.13.0</AssemblyVersion>
    <FileVersion>0.13.0</FileVersion>
</PropertyGroup>
```

Then build the project, and all version references will be automatically synchronized.

## Note on Youdao API Version

The `appVersion = "1.0.0"` in `src/Service/Youdao/YoudaoTranslatorV2.cs` is a parameter for the Youdao Translation API client, not the plugin version. It should remain as "1.0.0" unless the Youdao API documentation specifies otherwise.
