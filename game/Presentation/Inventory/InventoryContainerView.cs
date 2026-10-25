using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>One inspected container. Closing this view is distinct from shutting its physical compartments.</summary>
public partial class InventoryContainerView : VBoxContainer
{
    public InventoryPanel Panel { get; set; } = null!;
    public string ContainerId { get; set; } = "";
    public string? ParentViewId { get; set; }
    public ItemPlacement? PlacementAtInspection { get; set; }
    private readonly List<(string Id, Label State)> _regions = [];
    private Label _path = null!;

    internal void Build(StorageRegion[] regions)
    {
        var state = Panel.Simulation.Inventory;
        var name = state.Content!.Roots.FirstOrDefault(r => r.Id == ContainerId)?.Name ?? state.Definition(ContainerId).Name;
        var header = new HBoxContainer(); AddChild(header);
        InventoryPanel.Label(header, name.ToUpperInvariant(), 15).SizeFlagsHorizontal = SizeFlags.ExpandFill;
        InventoryPanel.Button(header, "×", () => Panel.CloseContainerView(ContainerId));
        _path = InventoryPanel.Label(this, "", 11);
        _path.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _path.CustomMinimumSize = new(190, 0);
        var compartments = new HBoxContainer(); compartments.AddThemeConstantOverride("separation", 12); AddChild(compartments);
        foreach (var region in regions)
        {
            var column = new VBoxContainer(); compartments.AddChild(column);
            var status = InventoryPanel.Label(column, "", 11); _regions.Add((region.Id, status));
            Panel.AddGrid(column, ContainerId, region.Id, region.CellMm < 25 ? 44 : region.Columns >= 8 ? 29 : 30);
            var lids = new HBoxContainer(); column.AddChild(lids);
            InventoryPanel.Button(lids, "Open", () => Panel.ChangeLid(ContainerId, region.Id, true));
            InventoryPanel.Button(lids, "Shut", () => Panel.ChangeLid(ContainerId, region.Id, false));
        }
        Refresh();
    }
    internal void Refresh()
    {
        var state = Panel.Simulation.Inventory;
        _path.Text = state.Content!.Roots.Any(r => r.Id == ContainerId) ? "Nearby fixture" : InventoryHierarchy.Breadcrumb(state, ContainerId);
        foreach (var (id, label) in _regions)
            label.Text = state.IsOpen(ContainerId, id) ? "OPEN COMPARTMENT" : "CLOSED COMPARTMENT";
    }
}
