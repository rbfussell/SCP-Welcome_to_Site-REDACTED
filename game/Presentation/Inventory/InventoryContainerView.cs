using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>One of the two fixed inspection views. All physical commands go through the panel/service.</summary>
public partial class InventoryContainerView : VBoxContainer
{
    public InventoryPanel Panel { get; set; } = null!;
    public InventoryBrowser Browser { get; set; } = null!;
    public bool GearView { get; set; }
    private string? _id;
    private string? _region;
    private Label? _state;
    private VBoxContainer? _lower;
    private Button? _fastening;
    private string _lowerSignature = "";
    internal void Build(string? id, string? region, string path)
    {
        foreach (var child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        _id = id; _region = region; _lower = null; _state = null; _fastening = null; _lowerSignature = "";
        CustomMinimumSize = new(GearView ? 330 : 280, 0); SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var header = new HBoxContainer(); AddChild(header);
        InventoryPanel.Label(header, GearView ? "GEAR" : "CONTENTS", 16).SizeFlagsHorizontal = SizeFlags.ExpandFill;
        InventoryPanel.Button(header, "↑", () => Browser.Dismiss(GearView, false));
        InventoryPanel.Button(header, "×", () => Browser.Dismiss(GearView, true));
        var breadcrumb = InventoryPanel.Label(this, path, 11); breadcrumb.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        if (id is null)
        { InventoryPanel.Label(this, GearView ? "Double-click worn gear to inspect it." : "Choose a compartment on the gear illustration.", 12); return; }
        var sim = Panel.Simulation; var state = sim.Inventory;
        var root = state.Content!.Roots.FirstOrDefault(r => r.Id == id);
        var definition = root is null ? state.Definition(id) : null;
        InventoryPanel.Label(this, root?.Name ?? definition!.Name, 15);
        _state = InventoryPanel.Label(this, "", 11); _state.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        if (GearView)
        {
            if (definition is not null)
            {
                AddChild(new InventoryGearSurface { Panel = Panel, Browser = Browser, ItemId = id });
                if (definition.Wear?.OpenExposes is { Length: > 0 })
                    _fastening = InventoryPanel.Button(this, "", () => Panel.ToggleClosure(id));
            }
            // Authoring fallback for an unregistered sprite/hotspot; never derive capacity from pixels.
            if (root is not null || !InventoryGearSurface.Registered(definition!.Id))
                foreach (var r in (root?.Regions ?? definition!.Regions ?? []).Where(r => r.Attachment is null))
                    InventoryPanel.Button(this, "Open " + r.Name, () => Panel.OpenCompartment(id, r.Id));
            _lower = new VBoxContainer(); AddChild(_lower);
        }
        else if (region is not null)
        {
            var r = state.Region(id, region);
            var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            AddChild(scroll); Panel.AddGrid(scroll, id, region, r.CellMm < 25 ? 44 : r.Columns >= 8 ? 29 : 30);
            if (definition is not null && r.Sealable)
                InventoryPanel.Button(this, r.SecureVerb, () => Panel.ChangeLid(id, region, false));
        }
        Refresh();
    }
    internal void Refresh()
    {
        if (_id is null || _state is null) return;
        var sim = Panel.Simulation; var state = sim.Inventory;
        if (_fastening is not null)
        {
            var action = InventoryClosures.Toggle(state, _id);
            _fastening.Text = action.Verb;
            _fastening.Disabled = state.Pending is not null || !sim.QuoteInventory(action.Request).Allowed;
        }
        _state.Text = _region is not null && !state.Region(_id, _region).Sealable ? "UNSEALABLE" + (state.Region(_id, _region).Snug ? " · snug fit" : "")
            : _region is not null ? (state.IsOpen(_id, _region) ? "OPEN · contents accessible" : "SECURED")
            : state.DiagnosticItems.SingleOrDefault(i => i.Id == _id) is { } item
                ? state.Definition(_id).Wear?.OpenExposes is { Length: > 0 }
                    ? item.GarmentOpen ? "GARMENT OPEN · removal restrictions still apply" : "GARMENT FASTENED"
                    : state.Definition(_id).Regions?.Any(r => r.Attachment is not null) == true
                        ? "Exterior mounts · drag compatible gear onto the webbing · R rotates"
                        : "Inspect individual compartments; other compartments stay hidden."
                : "Nearby storage · physical reach required";
        if (_lower is null) return;
        var exposed = sim.InventoryUnderlayers(_id);
        var signature = string.Join("|", exposed.Select(i => i.Id));
        if (signature == _lowerSignature) return;
        _lowerSignature = signature;
        foreach (var child in _lower.GetChildren()) { _lower.RemoveChild(child); child.QueueFree(); }
        foreach (var inner in exposed)
            InventoryPanel.Button(_lower, "Under: " + state.Definition(inner.Id).Name, () => Browser.Descend(inner.Id));
    }
}
