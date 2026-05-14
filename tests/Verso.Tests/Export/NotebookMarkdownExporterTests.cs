using System.Text;
using Verso.Abstractions;
using Verso.Export;

namespace Verso.Tests.Export;

[TestClass]
public sealed class NotebookMarkdownExporterTests
{
    [TestMethod]
    public void Export_WithTitle_TitleAsHeading()
    {
        var md = ExportToString("My Notebook", Array.Empty<CellModel>());

        Assert.IsTrue(md.StartsWith("# My Notebook"));
    }

    [TestMethod]
    public void Export_NullTitle_NoHeading()
    {
        var md = ExportToString(null, Array.Empty<CellModel>());

        Assert.IsFalse(md.StartsWith("#"));
    }

    [TestMethod]
    public void Export_MarkdownCell_SourceEmittedDirectly()
    {
        var cells = new[]
        {
            new CellModel { Type = "markdown", Source = "## Section\n\nSome text here." }
        };

        var md = ExportToString(null, cells);

        Assert.IsTrue(md.Contains("## Section"));
        Assert.IsTrue(md.Contains("Some text here."));
    }

    [TestMethod]
    public void Export_CodeCell_FencedBlockWithLanguage()
    {
        var cells = new[]
        {
            new CellModel { Type = "code", Language = "python", Source = "print('hello')" }
        };

        var md = ExportToString(null, cells);

        Assert.IsTrue(md.Contains("```python"));
        Assert.IsTrue(md.Contains("print('hello')"));
        Assert.IsTrue(md.Contains("```"));
    }

    [TestMethod]
    public void Export_TextOutput_BlockquotedSection()
    {
        var cells = new[]
        {
            new CellModel
            {
                Type = "code",
                Source = "x",
                Outputs = { new CellOutput("text/plain", "42") }
            }
        };

        var md = ExportToString(null, cells);

        Assert.IsTrue(md.Contains("> Output:"));
        Assert.IsTrue(md.Contains("> 42"));
    }

    [TestMethod]
    public void Export_ErrorOutput_BlockquotedWithName()
    {
        var cells = new[]
        {
            new CellModel
            {
                Type = "code",
                Source = "x",
                Outputs = { new CellOutput("text/plain", "fail", IsError: true, ErrorName: "ValueError") }
            }
        };

        var md = ExportToString(null, cells);

        Assert.IsTrue(md.Contains("> **ValueError:**"));
        Assert.IsTrue(md.Contains("> fail"));
    }

    [TestMethod]
    public void Export_HtmlOutput_BlockquotedWithHtmlLabel()
    {
        var cells = new[]
        {
            new CellModel
            {
                Type = "code",
                Source = "x",
                Outputs = { new CellOutput("text/html", "<b>bold</b>") }
            }
        };

        var md = ExportToString(null, cells);

        Assert.IsTrue(md.Contains("> Output (HTML):"));
        Assert.IsTrue(md.Contains("> <b>bold</b>"));
    }

    [TestMethod]
    public void Export_CodeCell_NullLanguage_FencedBlockWithoutTag()
    {
        var cells = new[]
        {
            new CellModel { Type = "code", Language = null, Source = "some code" }
        };

        var md = ExportToString(null, cells);

        Assert.IsTrue(md.Contains("```\n") || md.Contains("```\r\n"));
        Assert.IsTrue(md.Contains("some code"));
    }

    // ── Visibility-aware export tests ────────────────────────────────────

    [TestMethod]
    public void Export_NullOptions_AllCellsPresent()
    {
        var cells = new[]
        {
            new CellModel { Type = "code", Language = "csharp", Source = "cell1" },
            new CellModel { Type = "code", Language = "csharp", Source = "cell2" }
        };

        var md = ExportToString(null, cells, null);

        Assert.IsTrue(md.Contains("cell1"));
        Assert.IsTrue(md.Contains("cell2"));
    }

    [TestMethod]
    public void Export_PresentationLayout_HiddenCellSkipped()
    {
        var cell = new CellModel { Type = "code", Language = "csharp", Source = "secret-setup" };
        cell.Metadata["verso:ui.layoutVisibility"] = new Dictionary<string, object>
        {
            ["presentation"] = "Hidden"
        };

        var options = MakePresentationOptions();
        var md = ExportToString(null, new[] { cell }, options);

        Assert.IsFalse(md.Contains("secret-setup"));
    }

    [TestMethod]
    public void Export_PresentationLayout_OutputOnlyCell_SourceAbsentOutputPresent()
    {
        var cell = new CellModel
        {
            Type = "code", Language = "sql", Source = "SELECT 1",
            Outputs = { new CellOutput("text/plain", "query-result-42") }
        };
        cell.Metadata["verso:ui.layoutVisibility"] = new Dictionary<string, object>
        {
            ["presentation"] = "OutputOnly"
        };

        var options = MakePresentationOptions();
        var md = ExportToString(null, new[] { cell }, options);

        Assert.IsFalse(md.Contains("SELECT 1"));
        Assert.IsTrue(md.Contains("query-result-42"));
    }

    [TestMethod]
    public void Export_NotebookLayout_AllCellsVisible()
    {
        var cell = new CellModel { Type = "code", Language = "csharp", Source = "visible-cell" };
        cell.Metadata["verso:ui.layoutVisibility"] = new Dictionary<string, object>
        {
            ["notebook"] = "Hidden"
        };

        var notebookOptions = new ExportOptions(
            "notebook",
            new HashSet<CellVisibilityState> { CellVisibilityState.Visible },
            new List<ICellRenderer> { new TestRenderer("code", CellVisibilityHint.Content) });

        var md = ExportToString(null, new[] { cell }, notebookOptions);

        Assert.IsTrue(md.Contains("visible-cell"));
    }

    [TestMethod]
    public void Export_PresentationLayout_InfrastructureHint_CellHidden()
    {
        var cell = new CellModel { Type = "parameters", Source = "param-setup" };

        var options = new ExportOptions(
            "presentation",
            new HashSet<CellVisibilityState> { CellVisibilityState.Visible, CellVisibilityState.Hidden, CellVisibilityState.OutputOnly },
            new List<ICellRenderer> { new TestRenderer("parameters", CellVisibilityHint.Infrastructure) });

        var md = ExportToString(null, new[] { cell }, options);

        Assert.IsFalse(md.Contains("param-setup"));
    }

    [TestMethod]
    public void Export_PresentationLayout_ExplicitVisibleOverride_InfrastructureCellPresent()
    {
        var cell = new CellModel { Type = "parameters", Source = "param-visible" };
        cell.Metadata["verso:ui.layoutVisibility"] = new Dictionary<string, object>
        {
            ["presentation"] = "Visible"
        };

        var options = new ExportOptions(
            "presentation",
            new HashSet<CellVisibilityState> { CellVisibilityState.Visible, CellVisibilityState.Hidden, CellVisibilityState.OutputOnly },
            new List<ICellRenderer> { new TestRenderer("parameters", CellVisibilityHint.Infrastructure) });

        var md = ExportToString(null, new[] { cell }, options);

        Assert.IsTrue(md.Contains("param-visible"));
    }

    // ── View-state honoring tests ────────────────────────────────────────

    [TestMethod]
    public void Export_OutputHidden_OutputsOmitted()
    {
        var cell = new CellModel
        {
            Type = "code", Language = "python", Source = "print(42)",
            Outputs = { new CellOutput("text/plain", "secret-output-value") }
        };
        cell.Metadata[CellViewStateMetadata.OutputVisibilityKey] = CellViewStateMetadata.OutputHidden;

        var md = ExportToString(null, new[] { cell }, null);

        Assert.IsTrue(md.Contains("print(42)"));
        Assert.IsFalse(md.Contains("secret-output-value"));
    }

    [TestMethod]
    public void Export_OutputPreview_TextOutputTruncatedWithFooter()
    {
        var content = string.Join("\n", Enumerable.Range(1, 12).Select(i => $"line-{i}"));
        var cell = new CellModel
        {
            Type = "code", Language = "python", Source = "x",
            Outputs = { new CellOutput("text/plain", content) }
        };
        cell.Metadata[CellViewStateMetadata.OutputVisibilityKey] = CellViewStateMetadata.OutputPreview;

        var md = ExportToString(null, new[] { cell }, null);

        Assert.IsTrue(md.Contains("> line-1"));
        Assert.IsTrue(md.Contains("> line-5"));
        Assert.IsFalse(md.Contains("> line-6"));
        Assert.IsTrue(md.Contains("> ... (7 more lines)"));
    }

    [TestMethod]
    public void Export_OutputPreview_RichOutputUntruncated()
    {
        var content = string.Join("\n", Enumerable.Range(1, 12).Select(i => $"<p>row-{i}</p>"));
        var cell = new CellModel
        {
            Type = "code", Source = "x",
            Outputs = { new CellOutput("text/html", content) }
        };
        cell.Metadata[CellViewStateMetadata.OutputVisibilityKey] = CellViewStateMetadata.OutputPreview;

        var md = ExportToString(null, new[] { cell }, null);

        Assert.IsTrue(md.Contains("<p>row-12</p>"));
        Assert.IsFalse(md.Contains("more lines"));
    }

    [TestMethod]
    public void Export_OutputPreview_RespectsCustomLineCount()
    {
        var content = string.Join("\n", Enumerable.Range(1, 6).Select(i => $"line-{i}"));
        var cell = new CellModel
        {
            Type = "code", Source = "x",
            Outputs = { new CellOutput("text/plain", content) }
        };
        cell.Metadata[CellViewStateMetadata.OutputVisibilityKey] = CellViewStateMetadata.OutputPreview;
        cell.Metadata[CellViewStateMetadata.OutputPreviewLineCountKey] = 2;

        var md = ExportToString(null, new[] { cell }, null);

        Assert.IsTrue(md.Contains("> line-1"));
        Assert.IsTrue(md.Contains("> line-2"));
        Assert.IsFalse(md.Contains("> line-3"));
        Assert.IsTrue(md.Contains("> ... (4 more lines)"));
    }

    [TestMethod]
    public void Export_InputCollapsed_SourceOmittedOutputPresent()
    {
        var cell = new CellModel
        {
            Type = "code", Language = "python", Source = "secret-source-code",
            Outputs = { new CellOutput("text/plain", "the-output") }
        };
        cell.Metadata[CellViewStateMetadata.InputCollapsedKey] = true;

        var md = ExportToString(null, new[] { cell }, null);

        Assert.IsFalse(md.Contains("secret-source-code"));
        Assert.IsTrue(md.Contains("the-output"));
    }

    [TestMethod]
    public void Export_InputCollapsed_NoOutputs_CellSkipped()
    {
        var cell = new CellModel { Type = "code", Source = "only-source" };
        cell.Metadata[CellViewStateMetadata.InputCollapsedKey] = true;

        var md = ExportToString(null, new[] { cell }, null);

        Assert.IsFalse(md.Contains("only-source"));
    }

    [TestMethod]
    public void Export_RespectCellViewStateFalse_AllContentPresent()
    {
        var cell = new CellModel
        {
            Type = "code", Source = "full-source",
            Outputs = { new CellOutput("text/plain", "full-output") }
        };
        cell.Metadata[CellViewStateMetadata.InputCollapsedKey] = true;
        cell.Metadata[CellViewStateMetadata.OutputVisibilityKey] = CellViewStateMetadata.OutputHidden;

        var md = ExportToString(null, new[] { cell }, new ExportOptions(RespectCellViewState: false));

        Assert.IsTrue(md.Contains("full-source"));
        Assert.IsTrue(md.Contains("full-output"));
    }

    [TestMethod]
    public void Export_OutputHiddenPlusLayoutOutputOnly_CellSkipped()
    {
        var cell = new CellModel
        {
            Type = "code", Language = "sql", Source = "SELECT *",
            Outputs = { new CellOutput("text/plain", "tabular-result") }
        };
        cell.Metadata["verso:ui.layoutVisibility"] = new Dictionary<string, object>
        {
            ["presentation"] = "OutputOnly"
        };
        cell.Metadata[CellViewStateMetadata.OutputVisibilityKey] = CellViewStateMetadata.OutputHidden;

        var md = ExportToString(null, new[] { cell }, MakePresentationOptions());

        Assert.IsFalse(md.Contains("SELECT *"));
        Assert.IsFalse(md.Contains("tabular-result"));
    }

    private static string ExportToString(string? title, IReadOnlyList<CellModel> cells)
    {
        var bytes = NotebookMarkdownExporter.Export(title, cells);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string ExportToString(string? title, IReadOnlyList<CellModel> cells, ExportOptions? options)
    {
        var bytes = NotebookMarkdownExporter.Export(title, cells, options);
        return Encoding.UTF8.GetString(bytes);
    }

    private static ExportOptions MakePresentationOptions(CellVisibilityHint hint = CellVisibilityHint.Content)
        => new(
            "presentation",
            new HashSet<CellVisibilityState> { CellVisibilityState.Visible, CellVisibilityState.Hidden, CellVisibilityState.OutputOnly },
            new List<ICellRenderer> { new TestRenderer("code", hint) });

    private sealed class TestRenderer : ICellRenderer
    {
        private readonly CellVisibilityHint _hint;

        public TestRenderer(string cellTypeId, CellVisibilityHint hint)
        {
            CellTypeId = cellTypeId;
            _hint = hint;
        }

        public string CellTypeId { get; }
        CellVisibilityHint ICellRenderer.DefaultVisibility => _hint;
        public string DisplayName => CellTypeId;
        public string ExtensionId => "test." + CellTypeId;
        public string Name => CellTypeId;
        public string Version => "1.0.0";
        public string? Author => null;
        public string? Description => null;
        public Task OnLoadedAsync(IExtensionHostContext context) => Task.CompletedTask;
        public Task OnUnloadedAsync() => Task.CompletedTask;
        public Task<RenderResult> RenderInputAsync(string source, ICellRenderContext context) => throw new NotImplementedException();
        public Task<RenderResult> RenderOutputAsync(CellOutput output, ICellRenderContext context) => throw new NotImplementedException();
        public string? GetEditorLanguage() => null;
    }
}
