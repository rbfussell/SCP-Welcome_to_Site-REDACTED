using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>A diagnostic mode of the existing left view. Drafts are local UI state; Apply submits normal paid equipment work.</summary>
public partial class InventoryBodyFitPanel : VBoxContainer
{
    public InventoryPanel Panel { get; set; } = null!;
    private OptionButton _items = null!;
    private Label _status = null!, _pocketStatus = null!;
    private Button _apply = null!;
    private string[] _ids = [];
    private string? _selected;
    private ItemPlacement? _seenPlacement;
    internal PocketCoverage[] Pockets => _selected is null ? [] : (Panel.Simulation.Inventory.Definition(_selected).Regions ?? [])
        .Select(r => Panel.Simulation.InventoryPocketCoverage(_selected, r.Id)).OfType<PocketCoverage>().ToArray();
    internal string? SelectedId => _selected;
    internal BodyPose Draft { get; private set; } = new(0, 0);
    internal BodyFitPreview? Preview { get; private set; }
    internal InventoryBodyFitSurface Surface { get; private set; } = null!;
    internal Button ApplyButton => _apply;
    internal OptionButton ItemChoice => _items;
    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 5);
        _items = new OptionButton { FocusMode = FocusModeEnum.None, CustomMinimumSize = new(234, 0) }; AddChild(_items);
        _items.ItemSelected += index => Choose(_ids[(int)index]);
        InventoryPanel.Label(this, "9-hex shoulders · body-local scale", 12);
        Surface = new InventoryBodyFitSurface { Fit = this, CustomMinimumSize = new(234, 300) }; AddChild(Surface);
        var row = new HBoxContainer(); AddChild(row);
        InventoryPanel.Button(row, "Default", ResetDraft);
        InventoryPanel.Button(row, "Rotate 60°", RotateDraft);
        _apply = InventoryPanel.Button(row, "Fit / equip", Apply);
        _status = InventoryPanel.Label(this, "Choose worn or held clothing.", 12);
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _status.CustomMinimumSize = new(234, 65);
        _pocketStatus = InventoryPanel.Label(this, "", 11);
        InventoryPanel.Label(this, "Click sets anchor · drag clothing to fit\nR: 6 body turns / 2 storage turns\nWhite: contact · red: failed fit", 11);
        Refresh();
    }
    internal void Choose(string id)
    {
        _selected = id; _seenPlacement = null;
        Panel.Select(id); Refresh();
    }
    internal void ResetDraft()
    {
        if (_selected is null) return;
        Draft = InventoryBodyFit.Shape(Panel.Simulation.Inventory, _selected)!.DefaultPose; UpdatePreview();
    }
    internal void RotateDraft() { Draft = Draft with { Rotation = (Draft.Rotation + 1) % 6 }; UpdatePreview(); }
    internal void SetDraftAnchor(BodyHex cell) { Draft = Draft with { Q = cell.Q, R = cell.R }; UpdatePreview(); }
    private void Apply()
    {
        if (Preview is null) return;
        Panel.Simulation.HandleInventory(Preview.Request); Panel.ShowReceipt(); Refresh();
    }
    internal BodyMask Body => Panel.Simulation.Inventory.BodyFits!.Body(
        Panel.Simulation.Inventory.Content!.Roots.Single(r => r.ActorId == Panel.Simulation.Player.Id).BodyFamily!);
    internal void Refresh()
    {
        if (!IsNodeReady() || !Visible) return;
        var sim = Panel.Simulation;
        var items = sim.BodyFitItems;
        var ids = items.Select(i => i.Id).ToArray();
        if (!_ids.SequenceEqual(ids))
        {
            _ids = ids; _items.Clear();
            foreach (var item in items) _items.AddItem(sim.Inventory.EquipmentLabel(item.Id) + (item.Placement.RegionId == "hand" ? " · held" : ""));
            if (_selected is not null && !_ids.Contains(_selected)) _selected = null;
        }
        for (var i = 0; i < items.Count; i++)
            _items.SetItemText(i, sim.Inventory.EquipmentLabel(items[i].Id) + (items[i].Placement.RegionId == "hand" ? " · held" : ""));
        if (Panel.SelectedItem is { } selected && _ids.Contains(selected) && selected != _selected) { _selected = selected; _seenPlacement = null; }
        _selected ??= _ids.FirstOrDefault();
        if (_selected is not null)
        {
            if (Panel.DragItemId is null && (Panel.SelectedItem is null || Panel.Simulation.Inventory.Pending?.Request.ItemId != Panel.SelectedItem)
                && (Panel.SelectedItem is null || !_ids.Contains(Panel.SelectedItem))) Panel.Select(_selected);
            _items.Select(Array.IndexOf(_ids, _selected));
            var item = items.Single(i => i.Id == _selected);
            if (_seenPlacement != item.Placement)
            {
                _seenPlacement = item.Placement;
                Draft = InventoryBodyFit.Pose(sim.Inventory, item)!;
            }
        }
        UpdatePreview();
    }
    private void UpdatePreview()
    {
        if (!IsNodeReady()) return;
        if (Surface.IsDropTarget)
        {
            _apply.Disabled = true;
            _status.Text = Surface.DropPreview is { } drop ? $"Drop: {Panel.Simulation.Inventory.Definition(drop.Request.ItemId).Name}\n"
                + $"Anchor {drop.Request.Destination!.BodyPose!.Q},{drop.Request.Destination.BodyPose.R} · {Panel.BodyRotation * 60}°\n"
                + (drop.Handling.Allowed ? $"{drop.Handling.DurationMs / 1000.0:0.00}s · {drop.Handling.Ap} AP · release to fit" : Surface.DropReason)
                : Surface.DropReason;
            if (Surface.DropPreview is { Geometry.Fits: true } fittedDrop && InventoryBodyFit.Shape(Panel.Simulation.Inventory, fittedDrop.Request.ItemId)?.LinearSpan is { } spanDrop)
                _status.Text += $"\nSpan {fittedDrop.Geometry.Cells.Length} cells · range {spanDrop.MinCells}–{spanDrop.MaxCells}";
            _pocketStatus.Text = ""; Surface.QueueRedraw(); return;
        }
        Preview = _selected is null ? null : Panel.Simulation.PreviewBodyFit(_selected, Draft);
        _apply.Disabled = Preview is not { Handling.Allowed: true } || Panel.Simulation.Inventory.Pending is not null || Panel.DragItemId is not null;
        _status.Text = Preview is null ? "No known clothing is available."
            : $"Anchor {Draft.Q},{Draft.R} · {Draft.Rotation * 60}°\n" + (!Preview.Geometry.Fits ? Preview.Geometry.Reason
                : Preview.Handling.Allowed ? $"Fits · {Preview.Handling.DurationMs / 1000.0:0.00}s · {Preview.Handling.Ap} AP"
                : "Shape fits · " + Preview.Handling.Reason);
        if (Preview is { Geometry.Fits: true } fitted && _selected is { } id && InventoryBodyFit.Shape(Panel.Simulation.Inventory, id)?.LinearSpan is { } span)
            _status.Text += $"\nSpan {fitted.Geometry.Cells.Length} cells · range {span.MinCells}–{span.MaxCells}";
        var pockets = Pockets;
        _pocketStatus.Text = pockets.Length == 0 ? "" : $"Pockets now: {pockets.Count(p => p.Covered)} covered · {pockets.Count(p => !p.Covered)} clear";
        Surface.QueueRedraw();
    }
}

/// <summary>Body mask/edge display. Drawing and picking use the same body-local projection, never the map projection.</summary>
public partial class InventoryBodyFitSurface : Control
{
    public InventoryBodyFitPanel Fit { get; set; } = null!;
    private const float Radius = 9;
    private Vector2 Center(BodyHex cell) => new(Size.X / 2 + (float)(Math.Sqrt(3) * Radius * (cell.Q + cell.R / 2.0)), 16 + Radius * 1.5f * cell.R);
    internal Vector2 CellPosition(BodyHex cell) => GlobalPosition + Center(cell);
    private Vector2[] Polygon(BodyHex cell) => Enumerable.Range(0, 6).Select(i => Center(cell)
        + Vector2.FromAngle(Mathf.Pi / 6 + i * Mathf.Pi / 3) * (Radius - .45f)).ToArray();
    private static (Vector2 A, Vector2 B) Edge(Vector2 center, int direction)
    {
        var normal = (direction - 1) * Mathf.Pi / 3;
        return (center + Vector2.FromAngle(normal - Mathf.Pi / 6) * Radius, center + Vector2.FromAngle(normal + Mathf.Pi / 6) * Radius);
    }
    public override void _Ready() { MouseFilter = MouseFilterEnum.Stop; ClipContents = true; TooltipText = "Select clothing, then click a body cell to preview its anchor. Apply uses real handling time/AP."; }
    public override void _Draw()
    {
        var cells = BodyFitGeometry.Cells(Fit.Body.Rows);
        foreach (var cell in cells)
        {
            var polygon = Polygon(cell);
            DrawColoredPolygon(polygon, new("34433e")); DrawPolyline(polygon.Append(polygon[0]).ToArray(), new("60746a"), 1);
        }
        var straps = Fit.Panel.Simulation.InventoryWornCells.Where(c => c.Cell.Surface == "front"
            && Fit.Panel.Simulation.Inventory.Definition(c.ItemId).Wear!.OverlayLimit is not null).ToArray();
        foreach (var cell in straps.Select(c => c.Cell.Cell).Distinct()) DrawColoredPolygon(Polygon(cell), new Color("d1ad68", .5f));
        var activePreview = Fit.Panel.DragItemId is null ? Fit.Preview : DropPreview;
        if (activePreview is { } preview)
        {
            var outside = preview.Geometry.Outside.ToHashSet();
            foreach (var cell in preview.Geometry.Cells)
                DrawColoredPolygon(Polygon(cell), outside.Contains(cell) || Fit.Panel.DragItemId is not null && !preview.Handling.Allowed
                    ? new Color("d06258", .85f) : new Color("62baa2", .65f));
            if (preview.Request.ItemId is { } selected)
                foreach (var region in Fit.Panel.Simulation.Inventory.Definition(selected).Regions ?? [])
                    if (region.Attachment is not null)
                        foreach (var child in Fit.Panel.Simulation.InventoryAttachmentsOn(selected, region.Id))
                            foreach (var local in InventoryAttachments.Occupancy(Fit.Panel.Simulation.Inventory, child))
                                DrawColoredPolygon(Polygon(BodyFitGeometry.Transform(local, preview.Request.Destination!.BodyPose!)), new Color("d1ad68", .85f));
            foreach (var contact in preview.Geometry.Contacts)
            {
                var edge = Edge(Center(contact.Cell), contact.Direction);
                DrawLine(edge.A, edge.B, preview.Geometry.FailedContacts.Contains(contact) ? new("ff786d") : new("e3e5d3"), 2);
            }
        }
        // These marks show actual worn access, not the uncommitted fit draft.
        foreach (var cross in straps.GroupBy(c => c.Cell.Cell).Where(g => g.Count() > 1))
            DrawString(ThemeDB.FallbackFont, Center(cross.Key) + new Vector2(-3, 3), cross.Max(c => c.SubLayer).ToString(), fontSize: 9, modulate: new("ffdc86"));
        foreach (var pocket in Fit.Pockets)
        foreach (var cell in pocket.Mouth)
        {
            var position = Center(cell.Cell);
            var obstructed = pocket.CoarseFallback || pocket.Blocked.Contains(cell);
            DrawCircle(position, 3.2f, obstructed ? new("ff8979") : new("8ee5ce"));
            if (obstructed) DrawLine(position + new Vector2(-3, 3), position + new Vector2(3, -3), new("242b26"), 1.5f);
        }
        var activePose = activePreview?.Request.Destination?.BodyPose ?? Fit.Draft;
        var anchor = Center(new(activePose.Q, activePose.R));
        DrawCircle(anchor, 3, new("f1ca7a")); DrawArc(anchor, 6, 0, Mathf.Tau, 16, new("f1ca7a"), 1);
    }
    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse) return;
        if (PickCell(mouse.Position) is { } cell) Fit.SetDraftAnchor(cell);
        AcceptEvent();
    }
}
