using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Views of deliberately inspected storage. This history never opens a physical lid or reveals an unseen descendant.</summary>
public partial class InventoryBrowser : HBoxContainer
{
    public InventoryPanel Panel { get; set; } = null!;
    private readonly List<InventoryContainerView> _views = [];
    private Label? _empty;
    private string? _active;
    internal IReadOnlyList<string> ContainerIds => _views.Select(v => v.ContainerId).ToArray();

    public override void _Ready()
    {
        _empty = InventoryPanel.Label(this, "Inspect clothing or a bag from the paperdoll.\nIts pockets stay out of view until you choose it.", 14);
    }

    internal bool Inspect(string id, out string reason)
    {
        reason = "This object is not a container.";
        var state = Panel.Simulation.Inventory;
        var root = state.Content!.Roots.FirstOrDefault(r => r.Id == id);
        var item = state.DiagnosticItems.SingleOrDefault(i => i.Id == id);
        var regions = root?.Regions ?? (item is null ? null : state.Definition(id).Regions);
        if (regions is not { Length: > 0 }) return false;
        string? parentView = null;
        if (item is not null)
        {
            if (!Panel.Simulation.InventoryAccess(item.Placement.ParentId, item.Placement.RegionId).Allowed)
            { reason = "Container is not physically accessible."; return false; }
            var parentRoot = state.Content.Roots.SingleOrDefault(r => r.Id == item.Placement.ParentId);
            if (parentRoot?.ActorId != Panel.Simulation.Player.Id)
            {
                if (!_views.Any(v => v.ContainerId == item.Placement.ParentId))
                { reason = "Inspect its containing storage first."; return false; }
                parentView = item.Placement.ParentId;
            }
        }
        else if (!regions.Any(r => Panel.Simulation.InventoryAccess(id, r.Id).Allowed))
        { reason = "Approach the container with clear access."; return false; }

        _active = id;
        if (_views.Any(v => v.ContainerId == id)) return true;
        var view = new InventoryContainerView { Panel = Panel, ContainerId = id, ParentViewId = parentView,
            PlacementAtInspection = item?.Placement };
        // Keep a nested container immediately to the right of the branch it belongs to.
        var index = parentView is null ? _views.Count : _views.FindIndex(v => v.ContainerId == parentView) + 1;
        while (index < _views.Count && IsDescendant(_views[index], parentView!)) index++;
        _views.Insert(index, view); AddChild(view); MoveChild(view, index);
        view.Build(regions); UpdateEmpty();
        return true;
    }

    private bool IsDescendant(InventoryContainerView view, string ancestor)
    {
        for (var parent = view.ParentViewId; parent is not null; parent = _views.FirstOrDefault(v => v.ContainerId == parent)?.ParentViewId)
            if (parent == ancestor) return true;
        return false;
    }

    internal void CloseView(string id)
    {
        Panel.CancelDragPreview();
        var parent = _views.FirstOrDefault(v => v.ContainerId == id)?.ParentViewId;
        var closing = _views.Where(v => v.ContainerId == id || IsDescendant(v, id)).ToArray();
        if (closing.Any(v => v.ContainerId == _active)) _active = parent;
        foreach (var view in closing)
        { _views.Remove(view); RemoveChild(view); view.QueueFree(); }
        _active ??= _views.LastOrDefault()?.ContainerId;
        UpdateEmpty();
    }
    internal bool Back()
    {
        if (_views.Count == 0) return false;
        CloseView(_active ?? _views[^1].ContainerId); return true;
    }
    internal void Clear()
    {
        foreach (var view in _views) { RemoveChild(view); view.QueueFree(); }
        _views.Clear(); _active = null; UpdateEmpty();
    }
    private void UpdateEmpty() { if (_empty is not null) _empty.Visible = _views.Count == 0; }
    internal void Refresh()
    {
        var state = Panel.Simulation.Inventory;
        foreach (var view in _views.ToArray())
        {
            if (!_views.Contains(view)) continue;
            var item = state.DiagnosticItems.SingleOrDefault(i => i.Id == view.ContainerId);
            // Do not leave a case visible after it is packed elsewhere, its ancestor closes, or a fixture goes out of reach.
            if (item is not null && (item.Placement != view.PlacementAtInspection
                || !Panel.Simulation.InventoryAccess(item.Placement.ParentId, item.Placement.RegionId).Allowed)
                || item is null && !state.Content!.Roots.Single(r => r.Id == view.ContainerId).Regions.Any(r => Panel.Simulation.InventoryAccess(view.ContainerId, r.Id).Allowed))
            { CloseView(view.ContainerId); continue; }
            view.Refresh();
        }
    }
}
