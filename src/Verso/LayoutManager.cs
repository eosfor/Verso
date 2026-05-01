using Verso.Abstractions;

namespace Verso;

/// <summary>
/// Manages the active layout engine and exposes capability flags.
/// Handles metadata persistence for layout state across save/load cycles.
/// </summary>
public sealed class LayoutManager
{
    private IReadOnlyList<ILayoutEngine> _availableLayouts;
    private volatile ILayoutEngine? _activeLayout;

    public LayoutManager(IReadOnlyList<ILayoutEngine> availableLayouts, string? defaultLayoutId = null)
    {
        _availableLayouts = availableLayouts ?? throw new ArgumentNullException(nameof(availableLayouts));

        if (defaultLayoutId is not null && !TryActivate(defaultLayoutId))
        {
            // Layout referenced by the notebook isn't registered yet — typically because
            // it ships in an extension that the notebook will load via a #!extension or
            // #!nuget cell. Record the missing id so the host can surface it to the UI;
            // leave _activeLayout null so the default capability set takes over.
            MissingLayoutId = defaultLayoutId;
        }
    }

    /// <summary>
    /// The layout id requested at construction that could not be resolved against
    /// the available layouts, or <c>null</c> if everything resolved. Consumers can
    /// inspect this after construction to decide whether to surface a banner.
    /// </summary>
    public string? MissingLayoutId { get; private set; }

    /// <summary>
    /// Gets the currently active layout engine, or <c>null</c> if none is active.
    /// </summary>
    public ILayoutEngine? ActiveLayout => _activeLayout;

    /// <summary>
    /// Gets the list of available layout engines.
    /// </summary>
    public IReadOnlyList<ILayoutEngine> AvailableLayouts => _availableLayouts;

    /// <summary>
    /// Raised when the active layout changes.
    /// </summary>
    public event Action<ILayoutEngine>? OnLayoutChanged;

    /// <summary>
    /// Replaces the available layouts list with an updated snapshot.
    /// Preserves the active layout if it still exists in the new list.
    /// </summary>
    public void Refresh(IReadOnlyList<ILayoutEngine> updatedLayouts)
    {
        var activeId = _activeLayout?.LayoutId;
        _availableLayouts = updatedLayouts ?? throw new ArgumentNullException(nameof(updatedLayouts));

        if (activeId is not null)
        {
            _activeLayout = _availableLayouts.FirstOrDefault(
                l => string.Equals(l.LayoutId, activeId, StringComparison.OrdinalIgnoreCase));
        }

        // If the previously active layout was disabled, fall back to the first
        // non-custom-renderer layout (e.g. "notebook"), or the first available.
        if (_activeLayout is null && _availableLayouts.Count > 0)
        {
            var fallback = _availableLayouts.FirstOrDefault(l => !l.RequiresCustomRenderer)
                ?? _availableLayouts[0];
            _activeLayout = fallback;
            OnLayoutChanged?.Invoke(fallback);
        }
    }

    /// <summary>
    /// Gets whether the active layout requires a custom renderer.
    /// </summary>
    public bool RequiresCustomRenderer => _activeLayout?.RequiresCustomRenderer ?? false;

    /// <summary>
    /// Gets the capabilities supported by the active layout.
    /// When no layout is active, all capabilities are granted so that the notebook is fully functional.
    /// </summary>
    public LayoutCapabilities Capabilities => _activeLayout?.Capabilities ??
        (LayoutCapabilities.CellInsert | LayoutCapabilities.CellDelete |
         LayoutCapabilities.CellReorder | LayoutCapabilities.CellEdit |
         LayoutCapabilities.CellResize | LayoutCapabilities.CellExecute |
         LayoutCapabilities.MultiSelect);

    /// <summary>
    /// Switches the active layout by layout ID. Throws when the id is unknown.
    /// Use <see cref="TryActivate"/> when a missing id should not throw.
    /// </summary>
    public void SetActiveLayout(string layoutId)
    {
        ArgumentNullException.ThrowIfNull(layoutId);
        var layout = _availableLayouts.FirstOrDefault(
            l => string.Equals(l.LayoutId, layoutId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Layout '{layoutId}' not found.");
        _activeLayout = layout;
        MissingLayoutId = null;
        OnLayoutChanged?.Invoke(layout);
    }

    /// <summary>
    /// Attempts to activate the named layout. Returns <c>true</c> on success, or
    /// <c>false</c> if no layout with that id is registered. Used by the constructor
    /// (where a missing layout from the notebook metadata must not abort initialization)
    /// and by the runtime handler when an unknown layout is requested.
    /// </summary>
    public bool TryActivate(string layoutId)
    {
        ArgumentNullException.ThrowIfNull(layoutId);
        var layout = _availableLayouts.FirstOrDefault(
            l => string.Equals(l.LayoutId, layoutId, StringComparison.OrdinalIgnoreCase));
        if (layout is null) return false;

        _activeLayout = layout;
        MissingLayoutId = null;
        OnLayoutChanged?.Invoke(layout);
        return true;
    }

    /// <summary>
    /// Saves layout metadata from all known layouts into the notebook model.
    /// </summary>
    public Task SaveMetadataAsync(NotebookModel notebook)
    {
        ArgumentNullException.ThrowIfNull(notebook);

        foreach (var layout in _availableLayouts)
        {
            var metadata = layout.GetLayoutMetadata();
            if (metadata.Count > 0)
                notebook.Layouts[layout.LayoutId] = metadata;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Restores layout metadata from the notebook model into matching layout engines.
    /// </summary>
    public async Task RestoreMetadataAsync(NotebookModel notebook, IVersoContext context)
    {
        ArgumentNullException.ThrowIfNull(notebook);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var (layoutId, metadataObj) in notebook.Layouts)
        {
            var layout = _availableLayouts.FirstOrDefault(
                l => string.Equals(l.LayoutId, layoutId, StringComparison.OrdinalIgnoreCase));

            if (layout is null) continue;

            if (metadataObj is Dictionary<string, object> metadata)
            {
                await layout.ApplyLayoutMetadata(metadata, context).ConfigureAwait(false);
            }
        }
    }
}
