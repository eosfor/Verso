namespace Verso.Abstractions;

public static class CellViewStateMetadata
{
    public const string ProviderExtensionId = "verso.propertyprovider.display";

    public const string InputCollapsedProperty = "inputCollapsed";
    public const string OutputVisibilityProperty = "outputVisibility";
    public const string InputPreviewLineCountProperty = "inputPreviewLineCount";
    public const string OutputPreviewLineCountProperty = "outputPreviewLineCount";
    public const string PreviewStyleProperty = "previewStyle";

    public const string InputCollapsedKey = "verso:ui.inputCollapsed";
    public const string OutputVisibilityKey = "verso:ui.outputVisibility";
    public const string InputPreviewLineCountKey = "verso:ui.inputPreviewLineCount";
    public const string OutputPreviewLineCountKey = "verso:ui.outputPreviewLineCount";
    public const string PreviewStyleKey = "verso:ui.previewStyle";

    public const string OutputExpanded = "expanded";
    public const string OutputPreview = "preview";
    public const string OutputHidden = "hidden";

    public const string PreviewStyleLines = "lines";

    public const int DefaultInputPreviewLineCount = 2;
    public const int DefaultOutputPreviewLineCount = 5;
}
