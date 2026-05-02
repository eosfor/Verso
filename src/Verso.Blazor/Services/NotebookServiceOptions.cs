namespace Verso.Blazor.Services;

/// <summary>
/// Host-level configuration for <see cref="ServerNotebookService"/>. Populated
/// by the hosting layer (CLI <c>verso serve</c> or the standalone Blazor app)
/// and injected as a singleton.
/// </summary>
public sealed record NotebookServiceOptions
{
    /// <summary>
    /// Optional directory scanned for additional extension assemblies after
    /// built-in extensions are loaded. When null or missing on disk, only the
    /// built-in extensions are loaded.
    /// </summary>
    public string? ExtensionsDirectory { get; init; }
}
