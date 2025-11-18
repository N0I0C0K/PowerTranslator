using System.Reflection;

namespace Translator
{
    /// <summary>
    /// Centralized version information for the PowerTranslator plugin.
    /// The version is read from the assembly metadata set in Translater.csproj.
    /// </summary>
    public static class VersionInfo
    {
        private static readonly string? _version = Assembly.GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString(3); // Gets major.minor.build (e.g., "0.12.0")

        /// <summary>
        /// Gets the current version of the plugin.
        /// This version is automatically synchronized with the .csproj file.
        /// </summary>
        public static string Version => _version ?? "0.0.0";
    }
}
