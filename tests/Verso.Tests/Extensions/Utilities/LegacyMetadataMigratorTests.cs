using Verso.Extensions.Utilities;

namespace Verso.Tests.Extensions.Utilities;

[TestClass]
public sealed class LegacyMetadataMigratorTests
{
    [TestMethod]
    public void Migrate_RenamesLegacyVisibilityKey()
    {
        var notebook = new NotebookModel();
        var cell = new CellModel { Type = "code" };
        var payload = new Dictionary<string, string> { ["dashboard"] = "hidden" };
        cell.Metadata[CellLayoutVisibilityMetadata.LegacyMetadataKey] = payload;
        notebook.Cells.Add(cell);

        LegacyMetadataMigrator.Migrate(notebook);

        Assert.IsFalse(cell.Metadata.ContainsKey(CellLayoutVisibilityMetadata.LegacyMetadataKey));
        Assert.AreSame(payload, cell.Metadata[CellLayoutVisibilityMetadata.MetadataKey]);
    }

    [TestMethod]
    public void Migrate_PrefersExistingNewKey_AndDropsLegacy()
    {
        var notebook = new NotebookModel();
        var cell = new CellModel { Type = "code" };
        var newPayload = new Dictionary<string, string> { ["dashboard"] = "visible" };
        var legacyPayload = new Dictionary<string, string> { ["dashboard"] = "hidden" };
        cell.Metadata[CellLayoutVisibilityMetadata.MetadataKey] = newPayload;
        cell.Metadata[CellLayoutVisibilityMetadata.LegacyMetadataKey] = legacyPayload;
        notebook.Cells.Add(cell);

        LegacyMetadataMigrator.Migrate(notebook);

        Assert.IsFalse(cell.Metadata.ContainsKey(CellLayoutVisibilityMetadata.LegacyMetadataKey));
        Assert.AreSame(newPayload, cell.Metadata[CellLayoutVisibilityMetadata.MetadataKey]);
    }

    [TestMethod]
    public void Migrate_NoLegacyKey_IsNoOp()
    {
        var notebook = new NotebookModel();
        var cell = new CellModel { Type = "code" };
        var newPayload = new Dictionary<string, string> { ["dashboard"] = "hidden" };
        cell.Metadata[CellLayoutVisibilityMetadata.MetadataKey] = newPayload;
        notebook.Cells.Add(cell);

        LegacyMetadataMigrator.Migrate(notebook);

        Assert.AreSame(newPayload, cell.Metadata[CellLayoutVisibilityMetadata.MetadataKey]);
    }

    [TestMethod]
    public void Migrate_EmptyNotebook_DoesNotThrow()
    {
        var notebook = new NotebookModel();

        LegacyMetadataMigrator.Migrate(notebook);

        Assert.AreEqual(0, notebook.Cells.Count);
    }
}
