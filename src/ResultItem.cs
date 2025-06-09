using Wox.Plugin;

namespace Translator
{
    public class CustomActionContext
    {
        public required ActionContext actionContext { get; set; }
        public required PluginInitContext pluginInitContext { get; set; }

    }
    public class ResultItem
    {
        public string Title { get; set; } = default!;
        public string SubTitle { get; set; } = default!;
        public Func<CustomActionContext, bool>? Action { get; set; }
        public string? CopyTgt { get; set; }
        public string? iconPath { get; set; }
        public string? transType { get; set; }
        public string? fromApiName { get; set; }
        public string? Description { get; set; }
    }
}