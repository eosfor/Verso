using Verso.Abstractions;
using Verso.Serializers;

namespace Verso.Tests.Serializers;

[TestClass]
public sealed class JupyterPolyglotPostProcessorTests
{
    private readonly JupyterPolyglotPostProcessor _processor = new();

    [TestMethod]
    public void ExtensionMetadata_IsCorrect()
    {
        Assert.AreEqual("verso.serializer.jupyter-polyglot", _processor.ExtensionId);
        Assert.AreEqual(5, _processor.Priority);
    }

    [TestMethod]
    public void CanProcess_JupyterFormat_True()
    {
        Assert.IsTrue(_processor.CanProcess(null, "jupyter"));
        Assert.IsTrue(_processor.CanProcess("path/to/notebook.ipynb", ""));
        Assert.IsFalse(_processor.CanProcess(null, "verso-native"));
        Assert.IsFalse(_processor.CanProcess("notebook.dib", "dib"));
    }

    [TestMethod]
    public async Task LeadingPwsh_ChangesLanguage_NoSplit_PreservesOutputs()
    {
        var notebook = new NotebookModel
        {
            DefaultKernelId = "csharp",
            Cells =
            {
                MakeCodeCell("csharp", "#!pwsh\nGet-Process", outputs: 1),
            },
        };

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        Assert.AreEqual(1, notebook.Cells.Count);
        Assert.AreEqual("code", notebook.Cells[0].Type);
        Assert.AreEqual("powershell", notebook.Cells[0].Language);
        Assert.AreEqual("Get-Process", notebook.Cells[0].Source);
        Assert.AreEqual(1, notebook.Cells[0].Outputs.Count, "leading-directive case must keep original cell's outputs");
    }

    [TestMethod]
    public async Task MidCellDirective_SplitsBelow_OnlyOriginalKeepsOutputs()
    {
        var notebook = new NotebookModel
        {
            DefaultKernelId = "csharp",
            Cells =
            {
                MakeCodeCell("csharp", "Console.WriteLine(\"a\");\n#!pwsh\nGet-Process", outputs: 2),
            },
        };

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        Assert.AreEqual(2, notebook.Cells.Count);

        Assert.AreEqual("csharp", notebook.Cells[0].Language);
        Assert.AreEqual("Console.WriteLine(\"a\");", notebook.Cells[0].Source);
        Assert.AreEqual(2, notebook.Cells[0].Outputs.Count);

        Assert.AreEqual("powershell", notebook.Cells[1].Language);
        Assert.AreEqual("Get-Process", notebook.Cells[1].Source);
        Assert.AreEqual(0, notebook.Cells[1].Outputs.Count, "split cells must not inherit outputs");
    }

    [TestMethod]
    public async Task MultipleDirectives_PreserveExecutionOrder()
    {
        var notebook = new NotebookModel
        {
            DefaultKernelId = "csharp",
            Cells =
            {
                MakeCodeCell("csharp",
                    "Console.WriteLine(\"a\");\n#!pwsh\nGet-Process\n#!python\nprint('hi')",
                    outputs: 1),
            },
        };

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        Assert.AreEqual(3, notebook.Cells.Count);
        Assert.AreEqual("csharp", notebook.Cells[0].Language);
        Assert.AreEqual("powershell", notebook.Cells[1].Language);
        Assert.AreEqual("python", notebook.Cells[2].Language);
        Assert.AreEqual(1, notebook.Cells[0].Outputs.Count);
        Assert.AreEqual(0, notebook.Cells[1].Outputs.Count);
        Assert.AreEqual(0, notebook.Cells[2].Outputs.Count);
    }

    [TestMethod]
    public async Task ConsecutiveLeadingDirectives_LastOneWinsForOriginalCell()
    {
        // Polyglot allows back-to-back directives; the last one before any body
        // determines the language of the resulting cell.
        var notebook = new NotebookModel
        {
            Cells =
            {
                MakeCodeCell("csharp", "#!pwsh\n#!python\nprint('hi')"),
            },
        };

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        Assert.AreEqual(1, notebook.Cells.Count);
        Assert.AreEqual("python", notebook.Cells[0].Language);
        Assert.AreEqual("print('hi')", notebook.Cells[0].Source);
    }

    [TestMethod]
    public async Task UnknownDirective_SoftDegradesToLanguageToken()
    {
        var notebook = new NotebookModel
        {
            Cells =
            {
                MakeCodeCell("csharp", "#!ruby\nputs 'hi'"),
            },
        };

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        Assert.AreEqual(1, notebook.Cells.Count);
        Assert.AreEqual("ruby", notebook.Cells[0].Language);
        Assert.AreEqual("puts 'hi'", notebook.Cells[0].Source);
    }

    [TestMethod]
    public async Task NonLanguageDirective_LeftInSource_NoSplit()
    {
        // #!time, #!who, #!set, #!share, #!connect, etc. are operations, not kernel
        // switches — they must reach the runtime magic dispatcher unchanged.
        var notebook = new NotebookModel
        {
            Cells =
            {
                MakeCodeCell("csharp", "#!time\nvar x = 1;"),
                MakeCodeCell("csharp", "#!who"),
                MakeCodeCell("csharp", "#!share --from pwsh y"),
            },
        };

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        Assert.AreEqual(3, notebook.Cells.Count);
        Assert.AreEqual("csharp", notebook.Cells[0].Language);
        StringAssert.StartsWith(notebook.Cells[0].Source, "#!time");
        Assert.AreEqual("#!who", notebook.Cells[1].Source);
        StringAssert.StartsWith(notebook.Cells[2].Source, "#!share");
    }

    [TestMethod]
    public async Task DirectiveWithArguments_NotSplit()
    {
        // The bare-directive regex must not match `#!set --name X` etc.
        var notebook = new NotebookModel
        {
            Cells =
            {
                MakeCodeCell("csharp", "#!set --name x --value @csharp:1\nx + 1"),
            },
        };

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        Assert.AreEqual(1, notebook.Cells.Count);
        StringAssert.StartsWith(notebook.Cells[0].Source, "#!set");
    }

    [TestMethod]
    public async Task MarkdownCells_NotTouched()
    {
        var notebook = new NotebookModel
        {
            Cells =
            {
                new CellModel { Type = "markdown", Source = "#!pwsh\nGet-Process" },
            },
        };

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        Assert.AreEqual(1, notebook.Cells.Count);
        Assert.AreEqual("markdown", notebook.Cells[0].Type);
        Assert.AreEqual("#!pwsh\nGet-Process", notebook.Cells[0].Source);
    }

    [TestMethod]
    public async Task TrailingDirectiveWithEmptyBody_DoesNotEmitEmptyCell()
    {
        var notebook = new NotebookModel
        {
            Cells =
            {
                MakeCodeCell("csharp", "Console.WriteLine(\"a\");\n#!pwsh\n"),
            },
        };

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        Assert.AreEqual(1, notebook.Cells.Count);
        Assert.AreEqual("csharp", notebook.Cells[0].Language);
    }

    [TestMethod]
    public async Task ProducingFsharpCell_AddsRequiredExtension()
    {
        var notebook = new NotebookModel
        {
            Cells = { MakeCodeCell("csharp", "#!fsharp\nlet x = 1") },
        };

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        CollectionAssert.Contains(notebook.RequiredExtensions, "verso.fsharp");
    }

    [TestMethod]
    public async Task ProducingSqlCell_AddsRequiredExtension()
    {
        var notebook = new NotebookModel
        {
            Cells = { MakeCodeCell("csharp", "#!sql\nSELECT 1") },
        };

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        CollectionAssert.Contains(notebook.RequiredExtensions, "verso.ado");
    }

    [TestMethod]
    public async Task NoDirectives_PreservesCellExactly()
    {
        var notebook = new NotebookModel
        {
            Cells = { MakeCodeCell("csharp", "var x = 1;\nConsole.WriteLine(x);", outputs: 1) },
        };
        var originalId = notebook.Cells[0].Id;

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        Assert.AreEqual(1, notebook.Cells.Count);
        Assert.AreEqual(originalId, notebook.Cells[0].Id);
        Assert.AreEqual("var x = 1;\nConsole.WriteLine(x);", notebook.Cells[0].Source);
        Assert.AreEqual(1, notebook.Cells[0].Outputs.Count);
    }

    [TestMethod]
    public async Task LeadingDirectivePreservesOriginalCellId()
    {
        var notebook = new NotebookModel
        {
            Cells = { MakeCodeCell("csharp", "#!pwsh\nGet-Process") },
        };
        var originalId = notebook.Cells[0].Id;

        await _processor.PostDeserializeAsync(notebook, "n.ipynb");

        Assert.AreEqual(originalId, notebook.Cells[0].Id);
    }

    private static CellModel MakeCodeCell(string language, string source, int outputs = 0)
    {
        var cell = new CellModel
        {
            Type = "code",
            Language = language,
            Source = source,
        };
        for (int i = 0; i < outputs; i++)
            cell.Outputs.Add(new CellOutput("text/plain", $"output {i}"));
        return cell;
    }
}
