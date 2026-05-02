using Spectre.Console;
using Spectre.Console.Rendering;
using Verso.Abstractions;

namespace Verso.Cli.Repl.Rendering.Renderers;

internal static class PlainTextRenderer
{
    public static IRenderable AsRenderable(CellOutput output, TruncationPolicy policy)
    {
        // Strip trailing newlines so a single Console.WriteLine doesn't produce
        // an empty final row inside the bordered output panel. Internal blank
        // lines authored by the cell are preserved.
        var content = (output.Content ?? string.Empty).TrimEnd('\r', '\n');
        return new Text(policy.ClipLines(content));
    }
}
