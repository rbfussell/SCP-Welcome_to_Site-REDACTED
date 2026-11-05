using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Permanent floor dock. Rebinding follows physical position and never moves items between tiles.</summary>
public partial class InventoryGroundView : VBoxContainer
{
    public InventoryPanel Panel { get; set; } = null!;
    private Label _location = null!;
    private VBoxContainer _gridHost = null!;
    private string? _root;
    internal InventoryGrid? Grid { get; private set; }
    public override void _Ready()
    {
        CustomMinimumSize = new(202, 0);
        InventoryPanel.Label(this, "GROUND / AT YOUR FEET", 15);
        _location = InventoryPanel.Label(this, "", 12);
        var help = InventoryPanel.Label(this, "Drop or pick up here.\nOverlap to stack · top items first", 11);
        help.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _gridHost = new VBoxContainer(); AddChild(_gridHost);
        InventoryPanel.Button(this, "Put selected down", () =>
        {
            if (Panel.SelectedItem is not { } id) return;
            Panel.Simulation.HandleInventory(Panel.Simulation.GroundDropRequest(id, Panel.Rotated)); Panel.ShowReceipt();
        });
        Refresh();
    }
    internal void Refresh()
    {
        if (!IsNodeReady()) return;
        var root = Panel.Simulation.InventoryGroundRoot;
        _location.Text = root?.Name ?? "No ground at this location";
        if (root?.Id == _root) return;
        Panel.CancelDragPreview(); _root = root?.Id;
        foreach (var child in _gridHost.GetChildren()) { _gridHost.RemoveChild(child); child.QueueFree(); }
        Grid = root is null ? null : Panel.AddGrid(_gridHost, root.Id, InventoryGround.RegionId, 20);
    }
}
