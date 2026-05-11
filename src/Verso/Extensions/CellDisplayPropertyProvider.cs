using System.Text.Json;
using Verso.Abstractions;

namespace Verso.Extensions;

[VersoExtension]
public sealed class CellDisplayPropertyProvider : ICellPropertyProvider
{
    public string ExtensionId => CellViewStateMetadata.ProviderExtensionId;
    public string Name => "Cell Display Properties";
    public string Version => "1.0.0";
    public string? Author => "Verso Contributors";
    public string? Description => "Provides per-cell input and output display settings.";

    public int Order => 10;

    public Task OnLoadedAsync(IExtensionHostContext context) => Task.CompletedTask;
    public Task OnUnloadedAsync() => Task.CompletedTask;

    public bool AppliesTo(CellModel cell, ICellRenderContext context) => true;

    public Task<PropertySection> GetPropertiesSectionAsync(CellModel cell, ICellRenderContext context)
    {
        var fields = new List<PropertyField>
        {
            new(
                CellViewStateMetadata.InputCollapsedProperty,
                "Collapse input",
                PropertyFieldType.Toggle,
                ReadBool(cell, CellViewStateMetadata.InputCollapsedKey)),
            new(
                CellViewStateMetadata.OutputVisibilityProperty,
                "Output",
                PropertyFieldType.Select,
                ReadOutputVisibility(cell),
                Options: new[]
                {
                    new PropertyFieldOption(CellViewStateMetadata.OutputExpanded, "Full"),
                    new PropertyFieldOption(CellViewStateMetadata.OutputPreview, "Preview"),
                    new PropertyFieldOption(CellViewStateMetadata.OutputHidden, "Hidden"),
                }),
            new(
                CellViewStateMetadata.InputPreviewLineCountProperty,
                "Input preview lines",
                PropertyFieldType.Number,
                ReadPositiveInt(
                    cell,
                    CellViewStateMetadata.InputPreviewLineCountKey,
                    CellViewStateMetadata.DefaultInputPreviewLineCount)),
            new(
                CellViewStateMetadata.OutputPreviewLineCountProperty,
                "Output preview lines",
                PropertyFieldType.Number,
                ReadPositiveInt(
                    cell,
                    CellViewStateMetadata.OutputPreviewLineCountKey,
                    CellViewStateMetadata.DefaultOutputPreviewLineCount)),
            new(
                CellViewStateMetadata.PreviewStyleProperty,
                "Preview style",
                PropertyFieldType.Select,
                ReadPreviewStyle(cell),
                Options: new[]
                {
                    new PropertyFieldOption(CellViewStateMetadata.PreviewStyleLines, "Lines"),
                }),
        };

        return Task.FromResult(new PropertySection("Display", null, fields));
    }

    public Task OnPropertyChangedAsync(CellModel cell, string propertyName, object? value, ICellRenderContext context)
    {
        switch (propertyName)
        {
            case CellViewStateMetadata.InputCollapsedProperty:
                SetBoolMetadata(cell, CellViewStateMetadata.InputCollapsedKey, ReadBoolValue(value));
                break;

            case CellViewStateMetadata.OutputVisibilityProperty:
                SetOutputVisibility(cell, value?.ToString());
                break;

            case CellViewStateMetadata.InputPreviewLineCountProperty:
                SetPositiveIntMetadata(
                    cell,
                    CellViewStateMetadata.InputPreviewLineCountKey,
                    value,
                    CellViewStateMetadata.DefaultInputPreviewLineCount);
                break;

            case CellViewStateMetadata.OutputPreviewLineCountProperty:
                SetPositiveIntMetadata(
                    cell,
                    CellViewStateMetadata.OutputPreviewLineCountKey,
                    value,
                    CellViewStateMetadata.DefaultOutputPreviewLineCount);
                break;

            case CellViewStateMetadata.PreviewStyleProperty:
                SetPreviewStyle(cell, value?.ToString());
                break;
        }

        return Task.CompletedTask;
    }

    private static void SetOutputVisibility(CellModel cell, string? value)
    {
        var normalized = NormalizeOutputVisibility(value);
        if (string.Equals(normalized, CellViewStateMetadata.OutputExpanded, StringComparison.Ordinal))
            cell.Metadata.Remove(CellViewStateMetadata.OutputVisibilityKey);
        else
            cell.Metadata[CellViewStateMetadata.OutputVisibilityKey] = normalized;
    }

    private static void SetPreviewStyle(CellModel cell, string? value)
    {
        var normalized = string.Equals(value, CellViewStateMetadata.PreviewStyleLines, StringComparison.OrdinalIgnoreCase)
            ? CellViewStateMetadata.PreviewStyleLines
            : CellViewStateMetadata.PreviewStyleLines;

        if (string.Equals(normalized, CellViewStateMetadata.PreviewStyleLines, StringComparison.Ordinal))
            cell.Metadata.Remove(CellViewStateMetadata.PreviewStyleKey);
        else
            cell.Metadata[CellViewStateMetadata.PreviewStyleKey] = normalized;
    }

    private static void SetBoolMetadata(CellModel cell, string key, bool value)
    {
        if (value)
            cell.Metadata[key] = true;
        else
            cell.Metadata.Remove(key);
    }

    private static void SetPositiveIntMetadata(CellModel cell, string key, object? value, int defaultValue)
    {
        var parsed = ReadIntValue(value);
        if (parsed is null || parsed <= 0 || parsed == defaultValue)
            cell.Metadata.Remove(key);
        else
            cell.Metadata[key] = parsed.Value;
    }

    private static string ReadOutputVisibility(CellModel cell) =>
        NormalizeOutputVisibility(ReadString(cell, CellViewStateMetadata.OutputVisibilityKey));

    private static string ReadPreviewStyle(CellModel cell)
    {
        var value = ReadString(cell, CellViewStateMetadata.PreviewStyleKey);
        return string.Equals(value, CellViewStateMetadata.PreviewStyleLines, StringComparison.OrdinalIgnoreCase)
            ? CellViewStateMetadata.PreviewStyleLines
            : CellViewStateMetadata.PreviewStyleLines;
    }

    private static string NormalizeOutputVisibility(string? value)
    {
        if (string.Equals(value, CellViewStateMetadata.OutputPreview, StringComparison.OrdinalIgnoreCase))
            return CellViewStateMetadata.OutputPreview;
        if (string.Equals(value, CellViewStateMetadata.OutputHidden, StringComparison.OrdinalIgnoreCase))
            return CellViewStateMetadata.OutputHidden;
        return CellViewStateMetadata.OutputExpanded;
    }

    private static bool ReadBool(CellModel cell, string key)
    {
        if (!cell.Metadata.TryGetValue(key, out var value))
            return false;

        return ReadBoolValue(value);
    }

    private static bool ReadBoolValue(object? value) => value switch
    {
        bool b => b,
        JsonElement { ValueKind: JsonValueKind.True } => true,
        JsonElement { ValueKind: JsonValueKind.False } => false,
        string s when bool.TryParse(s, out var parsed) => parsed,
        _ => false
    };

    private static int ReadPositiveInt(CellModel cell, string key, int defaultValue)
    {
        if (!cell.Metadata.TryGetValue(key, out var value))
            return defaultValue;

        var parsed = ReadIntValue(value);
        return parsed is > 0 ? parsed.Value : defaultValue;
    }

    private static int? ReadIntValue(object? value) => value switch
    {
        int i => i,
        long l when l <= int.MaxValue && l >= int.MinValue => (int)l,
        double d when d <= int.MaxValue && d >= int.MinValue => (int)d,
        JsonElement { ValueKind: JsonValueKind.Number } element when element.TryGetInt32(out var i) => i,
        string s when int.TryParse(s, out var parsed) => parsed,
        _ => null
    };

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
