namespace Verso.Abstractions;

public static class CellLayoutVisibilityMetadata
{
    public const string MetadataKey = "verso:ui.layoutVisibility";

    // Renamed from "verso:visibility" to fit under the verso:ui.* prefix alongside
    // the other view-state keys. Recognized at notebook open time so older .verso
    // files keep loading; planned for removal in 1.0.22 (see Verso.Projects #7).
    public const string LegacyMetadataKey = "verso:visibility";
}
