using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Two bounded view trails: worn layers in the middle, physical containment on the right.</summary>
public partial class InventoryBrowser : HBoxContainer
{
    public InventoryPanel Panel { get; set; } = null!;
    private sealed record Gear(string Id, ItemPlacement? Placement);
    private sealed record Interior(string Owner, string Region, ItemPlacement? Placement);
    private readonly List<Gear> _gear = [];
    private readonly List<Interior> _contents = [];
    private readonly List<Interior> _visited = [];
    private InventoryContainerView _gearView = null!, _contentsView = null!;
    private (Interior Target, long Context)? _opening;
    private long _context;
    internal IReadOnlyList<string> GearIds => _gear.Select(g => g.Id).ToArray();
    internal IReadOnlyList<string> ContainerIds => _gear.Select(g => g.Id).Concat(_contents.Select(c => c.Owner)).Distinct().ToArray();
    internal string? CurrentGear => _gear.LastOrDefault()?.Id;
    internal (string Owner, string Region)? CurrentContents => _contents.Count == 0 ? null : (_contents[^1].Owner, _contents[^1].Region);

    public override void _Ready()
    {
        _gearView = new InventoryContainerView { Panel = Panel, Browser = this, GearView = true }; AddChild(_gearView);
        _contentsView = new InventoryContainerView { Panel = Panel, Browser = this }; AddChild(_contentsView);
        Build();
    }
    private ItemPlacement? Placement(string id) => Panel.Simulation.Inventory.DiagnosticItems.SingleOrDefault(i => i.Id == id)?.Placement;
    internal bool Inspect(string id, out string reason)
    {
        var sim = Panel.Simulation; var state = sim.Inventory;
        var root = state.Content!.Roots.FirstOrDefault(r => r.Id == id);
        var item = state.DiagnosticItems.SingleOrDefault(i => i.Id == id);
        reason = "Inspect its containing compartment first.";
        if (root is not null)
        {
            if (!root.Regions.Any(r => sim.InventoryAccess(id, r.Id).Allowed)) { reason = "Approach the container with clear access."; return false; }
            StartGear(id); return true;
        }
        if (item is null) return false;
        var access = sim.InventoryInspectAccess(id);
        if (!access.Allowed) { reason = access.Reason; return false; }
        if (state.Content.Roots.Any(r => r.Id == item.Placement.ParentId && r.ActorId == sim.Player.Id))
        { StartGear(id); return true; }
        if (item.Placement.AttachmentPose is not null && CurrentGear == item.Placement.ParentId)
        { Panel.CancelDragPreview(); _gear.Add(new(id, Placement(id))); _contents.Clear(); Build(); return true; }
        if (_contents.LastOrDefault() is not { } containing || containing.Owner != item.Placement.ParentId || containing.Region != item.Placement.RegionId) return false;
        if (state.Definition(id).Regions is not { Length: > 0 } regions) { reason = "This item has no interior."; return false; }
        return OpenRegion(id, regions[0].Id, out reason);
    }
    private void StartGear(string id)
    {
        Panel.CancelDragPreview(); _context++; _opening = null;
        _gear.Clear(); _contents.Clear(); _visited.Clear(); _gear.Add(new(id, Placement(id))); Build();
    }
    internal bool OpenRegion(string owner, string region, out string reason)
    {
        var sim = Panel.Simulation;
        reason = "Choose a compartment in the current gear or contents.";
        if (sim.Inventory.Region(owner, region).Attachment is not null) { reason = "This is an exterior mounting surface."; return false; }
        var isGear = _gear.Any(g => g.Id == owner);
        var item = sim.Inventory.DiagnosticItems.SingleOrDefault(i => i.Id == owner);
        var nested = _contents.LastOrDefault() is { } parent && item?.Placement.ParentId == parent.Owner && item.Placement.RegionId == parent.Region;
        if (!isGear && !nested) return false;
        var target = new Interior(owner, region, Placement(owner));
        // Validate parent access and coverage even if the lid is closed; the action service owns that check.
        var access = sim.InventoryAccess(owner, region);
        if (!sim.Inventory.IsOpen(owner, region))
        {
            if (!sim.HandleInventory(new(InventoryActionKind.Open, owner, RegionId: region))) { reason = sim.LastMessage; return false; }
            _opening = (target, _context); reason = sim.LastMessage; return true;
        }
        if (!access.Allowed) { reason = access.Reason; return false; }
        Enter(target); reason = "Inspecting " + sim.Inventory.Region(owner, region).Name; return true;
    }
    private void Enter(Interior target)
    {
        Panel.CancelDragPreview();
        if (_gear.Any(g => g.Id == target.Owner)) _contents.Clear();
        var existing = _contents.FindIndex(c => c.Owner == target.Owner && c.Region == target.Region);
        if (existing >= 0) _contents.RemoveRange(existing, _contents.Count - existing);
        _contents.Add(target); Build();
        if (!_visited.Any(c => c.Owner == target.Owner && c.Region == target.Region)) _visited.Add(target);
    }
    internal void Descend(string id)
    {
        var sim = Panel.Simulation;
        if (CurrentGear is not { } current || !sim.InventoryUnderlayers(current).Any(i => i.Id == id)) return;
        Panel.CancelDragPreview(); _context++; _opening = null;
        _gear.Add(new(id, Placement(id))); Build();
    }
    internal bool Dismiss(bool gear, bool entire)
    {
        if (gear ? _gear.Count == 0 : _contents.Count == 0) return false;
        var removedGear = gear ? (entire ? _gear.ToArray() : [_gear[^1]]) : Array.Empty<Gear>();
        var removedContents = !gear || _contents.Any(c => removedGear.Any(g => g.Id == c.Owner))
            ? (entire || gear ? _contents.ToArray() : [_contents[^1]]) : Array.Empty<Interior>();
        // Dismissing views has a physical default. A busy handler leaves the views in place with an explanation.
        var interiors = gear && entire ? _visited.Concat(removedGear.SelectMany(g => g.Placement is null ? Array.Empty<Interior>()
            : (Panel.Simulation.Inventory.Definition(g.Id).Regions ?? []).Where(r => r.Attachment is null).Select(r => new Interior(g.Id, r.Id, g.Placement)))) : removedContents;
        var requests = interiors.Where(c => Placement(c.Owner) == c.Placement && Panel.Simulation.Inventory.IsOpen(c.Owner, c.Region))
            .Select(c => new InventoryRequest(InventoryActionKind.Close, c.Owner, RegionId: c.Region))
            .Concat(removedGear.Where(g => g.Placement is not null && Panel.Simulation.Inventory.Definition(g.Id).Wear?.OpenExposes is { Length: > 0 })
                .Select(g => new InventoryRequest(InventoryActionKind.CloseGarment, g.Id)))
            .Where(r => Placement(r.ItemId) is not null).ToArray();
        var automatic = InventoryClosures.OnDismissal(Panel.Simulation.Inventory, requests);
        if (automatic.Length != 0 && !Panel.Simulation.SecureInventory(automatic, automatic: true)) { Panel.ShowReceipt(); return false; }
        Panel.CancelDragPreview(); _context++; _opening = null;
        if (gear) _gear.RemoveRange(entire ? 0 : _gear.Count - 1, entire ? _gear.Count : 1);
        if (removedContents.Length != 0) _contents.RemoveRange(_contents.Count - removedContents.Length, removedContents.Length);
        Build(); Panel.ShowReceipt(); return true;
    }
    internal void CloseView(string id)
    {
        if (_gear.Any(g => g.Id == id)) Dismiss(true, true);
        else if (_contents.Any(c => c.Owner == id)) Dismiss(false, true);
    }
    internal bool Back() => _contents.Count != 0 ? Dismiss(false, false) : Dismiss(true, false);
    internal void Clear()
    { _context++; _opening = null; _gear.Clear(); _contents.Clear(); _visited.Clear(); if (IsNodeReady()) Build(); }
    private void Build()
    {
        if (_gearView is null) return;
        _gearView.Build(CurrentGear, null, string.Join(" → ", _gear.Select(g => ItemName(g.Id))));
        var interior = _contents.LastOrDefault();
        _contentsView.Build(interior?.Owner, interior?.Region, string.Join(" → ", _contents.Select(c => Panel.Simulation.Inventory.Region(c.Owner, c.Region).Name)));
    }
    private string ItemName(string id) => Panel.Simulation.Inventory.Content!.Roots.SingleOrDefault(r => r.Id == id)?.Name ?? Panel.Simulation.Inventory.Definition(id).Name;
    internal void Refresh()
    {
        var sim = Panel.Simulation;
        var badGear = _gear.FindIndex(g => g.Placement != Placement(g.Id)
            || g.Placement is not null && !sim.InventoryInspectAccess(g.Id).Allowed
            || g.Placement is null && !sim.InventoryRoots.Any(r => r.Id == g.Id));
        if (badGear >= 0)
        {
            var removed = _gear.Skip(badGear).Select(g => g.Id).ToArray();
            _gear.RemoveRange(badGear, _gear.Count - badGear);
            if (_contents.Any(c => removed.Contains(c.Owner))) _contents.Clear();
            _context++; _opening = null; Build();
        }
        var badContents = _contents.FindIndex(c => c.Placement != Placement(c.Owner) || !sim.InventoryAccess(c.Owner, c.Region).Allowed);
        if (badContents >= 0) { _contents.RemoveRange(badContents, _contents.Count - badContents); _context++; _opening = null; Build(); }
        if (_opening is { } opening && sim.Inventory.Pending is null)
        {
            _opening = null;
            if (opening.Context == _context && opening.Target.Placement == Placement(opening.Target.Owner)
                && sim.InventoryAccess(opening.Target.Owner, opening.Target.Region).Allowed) Enter(opening.Target);
        }
        _gearView.Refresh(); _contentsView.Refresh();
    }
}
