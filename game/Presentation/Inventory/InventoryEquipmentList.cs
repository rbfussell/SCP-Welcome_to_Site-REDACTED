using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Known equipment and current local layers; selecting a covered row never grants physical access.</summary>
public partial class InventoryEquipmentList : ItemList
{
    internal InventoryPanel Panel { get; set; } = null!;
    private readonly List<string?> _ids = [];
    private string _signature = "";
    internal int Row(string id) => _ids.IndexOf(id);
    public override void _Ready()
    {
        CustomMinimumSize = new(234, 180); SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeFontSizeOverride("font_size", 12);
        ItemSelected += index => { if (_ids[(int)index] is { } id) Panel.Select(id); };
        ItemActivated += index => { if (_ids[(int)index] is { } id) Panel.Inspect(id); };
        ItemClicked += (index, at, button) =>
        { if (button == (long)MouseButton.Right && _ids[(int)index] is { } id) Panel.Context(id, null, GlobalPosition + at); };
    }
    internal static string LayerText(IEnumerable<WornLayer> layers)
    {
        var depths = layers.Select(l => l.Layer).Distinct().Order().ToArray();
        return depths.Length == 0 ? "held" : depths.Length == 1 ? depths[0].ToString() : $"{depths[0]}–{depths[^1]}";
    }
    internal void RefreshEntries()
    {
        var entries = Panel.Simulation.InventoryEquipment;
        var signature = Panel.Simulation.Inventory.Revision + string.Join("|", entries.Select(e => e.ItemId + e.Group + LayerText(e.Layers) + e.Visible + e.Access.Reason));
        if (_signature == signature) return;
        _signature = signature; Clear(); _ids.Clear();
        string? group = null;
        foreach (var entry in entries)
        {
            if (entry.Group != group)
            { group = entry.Group; var header = AddItem(group.ToUpperInvariant()); _ids.Add(null); SetItemSelectable(header, false); }
            var status = !entry.Visible ? "covered" : !entry.Access.Allowed ? "restrained" : "exposed";
            var row = AddItem($"  {LayerText(entry.Layers)}  {Panel.Simulation.Inventory.Definition(entry.ItemId).Name} · {status}"); _ids.Add(entry.ItemId);
            SetItemCustomFgColor(row, new Color(entry.Visible ? "d8dfcf" : "8fa598"));
            SetItemTooltip(row, Panel.Describe(entry.ItemId) + "\n" + entry.Access.Reason);
            if (Panel.SelectedItem == entry.ItemId) Select(row);
        }
    }
}
