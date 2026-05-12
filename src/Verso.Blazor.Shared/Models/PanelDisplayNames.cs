namespace Verso.Blazor.Shared.Models;

public static class PanelDisplayNames
{
    public static string For(string panelId) => panelId switch
    {
        "metadata" => "METADATA",
        "extensions" => "EXTENSIONS",
        "variables" => "VARIABLES",
        "settings" => "SETTINGS",
        "properties" => "CELL PROPERTIES",
        _ => panelId.ToUpperInvariant()
    };
}
