using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console.Testing;
using Verso.Abstractions;
using Verso.Cli.Repl.Rendering;
using Verso.Cli.Repl.Settings;
using Verso.Execution;

namespace Verso.Cli.Tests.Repl.Rendering;

[TestClass]
public class TerminalRendererTests
{
    [TestMethod]
    public void RenderCell_SuccessInColor_WrapsOutputInGreenCheckPanel()
    {
        // Regression guard: the original cell-frame used a bare "[N]" rule, which
        // Spectre's markup parser rejected with "Unbalanced markup stack". The panel
        // form sidesteps that by avoiding counter interpolation entirely; this test
        // confirms RenderCell stays exception-free and carries the ✅ glyph.
        var console = new TestConsole();
        console.Profile.Width = 80;
        var renderer = new TerminalRenderer(console, useColor: true);
        renderer.BindSettings(new ReplSettings());

        var cell = new CellModel
        {
            Type = "code",
            Language = "csharp",
            Source = "var x = 1;",
            Outputs = { new CellOutput("text/plain", "1") }
        };
        var result = ExecutionResult.Success(cell.Id, executionCount: 1, elapsed: TimeSpan.FromMilliseconds(5));

        renderer.RenderCell(inputCounter: 1, cell, result, TimeSpan.FromMilliseconds(200));

        StringAssert.Contains(console.Output, "✅", "Success cells are framed with the green check glyph.");
        StringAssert.Contains(console.Output, "1", "Cell output body must still be present inside the panel.");
    }

    [TestMethod]
    public void RenderCell_FailureInColor_UsesCrossGlyph()
    {
        var console = new TestConsole();
        console.Profile.Width = 80;
        var renderer = new TerminalRenderer(console, useColor: true);
        renderer.BindSettings(new ReplSettings());

        var cell = new CellModel
        {
            Type = "code",
            Source = "throw;",
            Outputs = { new CellOutput("text/plain", "boom", IsError: true, ErrorName: "InvalidOperationException") }
        };
        var result = ExecutionResult.Success(cell.Id, 1, TimeSpan.Zero);

        renderer.RenderCell(inputCounter: 2, cell, result, TimeSpan.FromMilliseconds(200));

        StringAssert.Contains(console.Output, "❌", "Error outputs should switch the panel header to the red cross glyph.");
        StringAssert.Contains(console.Output, "boom");
    }

    [TestMethod]
    public void RenderCell_InNoColorMode_ProducesPlainOutputOnly()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.ColorSystem = Spectre.Console.ColorSystem.NoColors;
        var renderer = new TerminalRenderer(console, useColor: false);

        var cell = new CellModel
        {
            Type = "code",
            Source = "x",
            Outputs = { new CellOutput("text/plain", "42") }
        };
        var result = ExecutionResult.Success(cell.Id, 1, TimeSpan.Zero);

        renderer.RenderCell(inputCounter: 3, cell, result, TimeSpan.FromMilliseconds(200));

        StringAssert.Contains(console.Output, "42");
    }

    [TestMethod]
    public void RenderCell_OutputHidden_DoesNotRenderOutputContent()
    {
        // Cells loaded from a .verso file with verso:ui.outputVisibility=hidden
        // (set in the notebook UI) should be silently skipped during REPL replay.
        var console = new TestConsole();
        console.Profile.Width = 80;
        var renderer = new TerminalRenderer(console, useColor: true);
        renderer.BindSettings(new ReplSettings());

        var cell = new CellModel
        {
            Type = "code",
            Language = "csharp",
            Source = "secret",
            Outputs = { new CellOutput("text/plain", "should-not-appear") }
        };
        cell.Metadata["verso:ui.outputVisibility"] = "hidden";
        var result = ExecutionResult.Success(cell.Id, 1, TimeSpan.FromMilliseconds(5));

        renderer.RenderCell(inputCounter: 1, cell, result, TimeSpan.FromMilliseconds(200));

        Assert.IsFalse(console.Output.Contains("should-not-appear"),
            "Hidden outputs must not be rendered during REPL replay.");
    }

    [TestMethod]
    public void RenderCell_OutputHiddenButHasError_StillSurfacesError()
    {
        // Hiding outputs must not silently swallow execution failures.
        var console = new TestConsole();
        console.Profile.Width = 80;
        var renderer = new TerminalRenderer(console, useColor: true);
        renderer.BindSettings(new ReplSettings());

        var cell = new CellModel
        {
            Type = "code",
            Source = "throw;",
            Outputs = { new CellOutput("text/plain", "boom", IsError: true, ErrorName: "InvalidOperationException") }
        };
        cell.Metadata["verso:ui.outputVisibility"] = "hidden";
        var result = ExecutionResult.Success(cell.Id, 1, TimeSpan.Zero);

        renderer.RenderCell(inputCounter: 2, cell, result, TimeSpan.FromMilliseconds(200));

        StringAssert.Contains(console.Output, "boom",
            "Errors must surface even when outputVisibility=hidden.");
    }

    [TestMethod]
    public void RenderCell_OutputPreview_LeavesContentExpandedInRepl()
    {
        // Per spec: REPL treats "preview" as expanded — the REPL is already line-oriented,
        // so we do not truncate. Truncation belongs to `verso run`.
        var console = new TestConsole();
        console.Profile.Width = 80;
        var renderer = new TerminalRenderer(console, useColor: true);
        renderer.BindSettings(new ReplSettings());

        var content = string.Join('\n', Enumerable.Range(1, 8).Select(i => $"line{i}"));
        var cell = new CellModel
        {
            Type = "code",
            Source = "_",
            Outputs = { new CellOutput("text/plain", content) }
        };
        cell.Metadata["verso:ui.outputVisibility"] = "preview";
        cell.Metadata["verso:ui.outputPreviewLineCount"] = 2;
        var result = ExecutionResult.Success(cell.Id, 1, TimeSpan.Zero);

        renderer.RenderCell(inputCounter: 3, cell, result, TimeSpan.FromMilliseconds(200));

        StringAssert.Contains(console.Output, "line8",
            "REPL should render the full output in preview mode.");
    }
}
