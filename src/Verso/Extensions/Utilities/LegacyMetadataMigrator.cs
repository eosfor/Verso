using Verso.Abstractions;

namespace Verso.Extensions.Utilities;

/// <summary>
/// Renames legacy cell metadata keys to their current names. Runs once at
/// notebook open time so downstream readers do not each have to carry a
/// fallback. Planned for removal in 1.0.22 (see Verso.Projects #7).
/// </summary>
public static class LegacyMetadataMigrator
{
    public static void Migrate(NotebookModel notebook)
    {
        foreach (var cell in notebook.Cells)
            MigrateCell(cell);
    }

    private static void MigrateCell(CellModel cell)
    {
        if (!cell.Metadata.TryGetValue(CellLayoutVisibilityMetadata.LegacyMetadataKey, out var legacyValue))
            return;

        // If the new key is already present, trust it and drop the legacy entry.
        if (!cell.Metadata.ContainsKey(CellLayoutVisibilityMetadata.MetadataKey))
            cell.Metadata[CellLayoutVisibilityMetadata.MetadataKey] = legacyValue;

        cell.Metadata.Remove(CellLayoutVisibilityMetadata.LegacyMetadataKey);
    }
}
