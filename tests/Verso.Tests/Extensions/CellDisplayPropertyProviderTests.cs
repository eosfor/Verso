using Verso.Extensions;
using Verso.Testing.Stubs;

namespace Verso.Tests.Extensions;

[TestClass]
public sealed class CellDisplayPropertyProviderTests
{
    private readonly CellDisplayPropertyProvider _provider = new();
    private readonly StubCellRenderContext _renderContext = new();

    [TestMethod]
    public void Metadata_IsCorrect()
    {
        Assert.AreEqual(CellViewStateMetadata.ProviderExtensionId, _provider.ExtensionId);
        Assert.AreEqual(10, _provider.Order);
    }

    [TestMethod]
    public void AppliesTo_ReturnsTrue_ForAnyCell()
    {
        Assert.IsTrue(_provider.AppliesTo(new CellModel { Type = "code" }, _renderContext));
        Assert.IsTrue(_provider.AppliesTo(new CellModel { Type = "markdown" }, _renderContext));
    }

    [TestMethod]
    public async Task GetPropertiesSection_ReturnsDisplayFields()
    {
        var section = await _provider.GetPropertiesSectionAsync(new CellModel { Type = "code" }, _renderContext);

        Assert.AreEqual("Display", section.Title);
        CollectionAssert.AreEqual(
            new[]
            {
                CellViewStateMetadata.InputCollapsedProperty,
                CellViewStateMetadata.OutputVisibilityProperty,
                CellViewStateMetadata.InputPreviewLineCountProperty,
                CellViewStateMetadata.OutputPreviewLineCountProperty,
                CellViewStateMetadata.PreviewStyleProperty,
            },
            section.Fields.Select(f => f.Name).ToArray());
    }

    [TestMethod]
    public async Task GetPropertiesSection_OmitsInputFields_ForNonCodeCells()
    {
        var section = await _provider.GetPropertiesSectionAsync(new CellModel { Type = "markdown" }, _renderContext);

        Assert.AreEqual("Display", section.Title);
        CollectionAssert.AreEqual(
            new[]
            {
                CellViewStateMetadata.OutputVisibilityProperty,
                CellViewStateMetadata.OutputPreviewLineCountProperty,
                CellViewStateMetadata.PreviewStyleProperty,
            },
            section.Fields.Select(f => f.Name).ToArray());
    }

    [TestMethod]
    public async Task GetPropertiesSection_UsesDefaults_WhenMetadataIsAbsent()
    {
        var section = await _provider.GetPropertiesSectionAsync(new CellModel { Type = "code" }, _renderContext);

        Assert.AreEqual(false, section.Fields.Single(f => f.Name == CellViewStateMetadata.InputCollapsedProperty).CurrentValue);
        Assert.AreEqual(CellViewStateMetadata.OutputExpanded, section.Fields.Single(f => f.Name == CellViewStateMetadata.OutputVisibilityProperty).CurrentValue);
        Assert.AreEqual(CellViewStateMetadata.DefaultInputPreviewLineCount, section.Fields.Single(f => f.Name == CellViewStateMetadata.InputPreviewLineCountProperty).CurrentValue);
        Assert.AreEqual(CellViewStateMetadata.DefaultOutputPreviewLineCount, section.Fields.Single(f => f.Name == CellViewStateMetadata.OutputPreviewLineCountProperty).CurrentValue);
        Assert.AreEqual(CellViewStateMetadata.PreviewStyleLines, section.Fields.Single(f => f.Name == CellViewStateMetadata.PreviewStyleProperty).CurrentValue);
    }

    [TestMethod]
    public async Task OnPropertyChanged_WritesNonDefaultMetadata()
    {
        var cell = new CellModel { Type = "code" };

        await _provider.OnPropertyChangedAsync(cell, CellViewStateMetadata.InputCollapsedProperty, true, _renderContext);
        await _provider.OnPropertyChangedAsync(cell, CellViewStateMetadata.OutputVisibilityProperty, CellViewStateMetadata.OutputPreview, _renderContext);
        await _provider.OnPropertyChangedAsync(cell, CellViewStateMetadata.InputPreviewLineCountProperty, "3", _renderContext);
        await _provider.OnPropertyChangedAsync(cell, CellViewStateMetadata.OutputPreviewLineCountProperty, "7", _renderContext);

        Assert.AreEqual(true, cell.Metadata[CellViewStateMetadata.InputCollapsedKey]);
        Assert.AreEqual(CellViewStateMetadata.OutputPreview, cell.Metadata[CellViewStateMetadata.OutputVisibilityKey]);
        Assert.AreEqual(3, cell.Metadata[CellViewStateMetadata.InputPreviewLineCountKey]);
        Assert.AreEqual(7, cell.Metadata[CellViewStateMetadata.OutputPreviewLineCountKey]);
    }

    [TestMethod]
    public async Task OnPropertyChanged_RemovesDefaultMetadata()
    {
        var cell = new CellModel { Type = "code" };
        cell.Metadata[CellViewStateMetadata.InputCollapsedKey] = true;
        cell.Metadata[CellViewStateMetadata.OutputVisibilityKey] = CellViewStateMetadata.OutputHidden;
        cell.Metadata[CellViewStateMetadata.InputPreviewLineCountKey] = 3;
        cell.Metadata[CellViewStateMetadata.OutputPreviewLineCountKey] = 7;

        await _provider.OnPropertyChangedAsync(cell, CellViewStateMetadata.InputCollapsedProperty, false, _renderContext);
        await _provider.OnPropertyChangedAsync(cell, CellViewStateMetadata.OutputVisibilityProperty, CellViewStateMetadata.OutputExpanded, _renderContext);
        await _provider.OnPropertyChangedAsync(cell, CellViewStateMetadata.InputPreviewLineCountProperty, CellViewStateMetadata.DefaultInputPreviewLineCount, _renderContext);
        await _provider.OnPropertyChangedAsync(cell, CellViewStateMetadata.OutputPreviewLineCountProperty, CellViewStateMetadata.DefaultOutputPreviewLineCount, _renderContext);

        Assert.IsFalse(cell.Metadata.ContainsKey(CellViewStateMetadata.InputCollapsedKey));
        Assert.IsFalse(cell.Metadata.ContainsKey(CellViewStateMetadata.OutputVisibilityKey));
        Assert.IsFalse(cell.Metadata.ContainsKey(CellViewStateMetadata.InputPreviewLineCountKey));
        Assert.IsFalse(cell.Metadata.ContainsKey(CellViewStateMetadata.OutputPreviewLineCountKey));
    }

    [TestMethod]
    public async Task ProviderDiscoveredByExtensionHost()
    {
        await using var host = new ExtensionHost();
        await host.LoadBuiltInExtensionsAsync();

        Assert.IsTrue(
            host.GetPropertyProviders().Any(p => p.ExtensionId == CellViewStateMetadata.ProviderExtensionId),
            "CellDisplayPropertyProvider should be discovered by LoadBuiltInExtensionsAsync");
    }
}
