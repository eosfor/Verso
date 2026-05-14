using System.Text.Json;
using Verso.Abstractions;

namespace Verso.Extensions.Utilities;

/// <summary>
/// Reads per-cell view-state metadata written by <c>CellDisplayPropertyProvider</c>
/// (the <c>verso:ui.*</c> keys). Used by exporters that honor a subset of the
/// user-actionable display settings.
/// </summary>
internal static class CellViewStateReader
{
    /// <summary>
    /// Returns the cell's output visibility setting, normalized to one of
    /// <see cref="CellViewStateMetadata.OutputExpanded"/>,
    /// <see cref="CellViewStateMetadata.OutputPreview"/>, or
    /// <see cref="CellViewStateMetadata.OutputHidden"/>.
    /// </summary>
    public static string ReadOutputVisibility(CellModel cell)
    {
        var raw = ReadString(cell, CellViewStateMetadata.OutputVisibilityKey);

        if (string.Equals(raw, CellViewStateMetadata.OutputPreview, StringComparison.OrdinalIgnoreCase))
            return CellViewStateMetadata.OutputPreview;
        if (string.Equals(raw, CellViewStateMetadata.OutputHidden, StringComparison.OrdinalIgnoreCase))
            return CellViewStateMetadata.OutputHidden;
        return CellViewStateMetadata.OutputExpanded;
    }

    /// <summary>
    /// Returns whether the cell's input is marked as collapsed.
    /// </summary>
    public static bool ReadInputCollapsed(CellModel cell)
    {
        if (!cell.Metadata.TryGetValue(CellViewStateMetadata.InputCollapsedKey, out var value))
            return false;

        return value switch
        {
            bool b => b,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            string s when bool.TryParse(s, out var parsed) => parsed,
            _ => false
        };
    }

    /// <summary>
    /// Returns the cell's configured output preview line count, falling back to
    /// <see cref="CellViewStateMetadata.DefaultOutputPreviewLineCount"/> when unset or invalid.
    /// </summary>
    public static int ReadOutputPreviewLineCount(CellModel cell)
        => ReadPositiveInt(
            cell,
            CellViewStateMetadata.OutputPreviewLineCountKey,
            CellViewStateMetadata.DefaultOutputPreviewLineCount);

    private static int ReadPositiveInt(CellModel cell, string key, int defaultValue)
    {
        if (!cell.Metadata.TryGetValue(key, out var value))
            return defaultValue;

        var parsed = value switch
        {
            int i => (int?)i,
            long l when l <= int.MaxValue && l >= int.MinValue => (int?)(int)l,
            double d when d <= int.MaxValue && d >= int.MinValue => (int?)(int)d,
            JsonElement { ValueKind: JsonValueKind.Number } element when element.TryGetInt32(out var i) => i,
            string s when int.TryParse(s, out var i) => i,
            _ => (int?)null
        };

        return parsed is > 0 ? parsed.Value : defaultValue;
    }

    private static string? ReadString(CellModel cell, string key)
    {
        if (!cell.Metadata.TryGetValue(key, out var value) || value is null)
            return null;

        return value switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => value.ToString()
        };
    }
}
