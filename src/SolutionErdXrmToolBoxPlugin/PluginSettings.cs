using SolutionErd;

namespace SolutionErdXrmToolBoxPlugin
{
    /// <summary>Saved between sessions with XrmToolBox's SettingsManager.</summary>
    public sealed class PluginSettings
    {
        public ErdOptions Options { get; set; } = new ErdOptions();

        public string LastFolder { get; set; }
    }
}
