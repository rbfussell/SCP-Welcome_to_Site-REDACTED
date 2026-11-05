using Godot;
using SiteSimulation;

namespace SitePresentation;

internal sealed record InventoryDropPreview(InventoryGrid Grid, string ItemId, InventoryRequest Request,
    InventoryActionQuote Quote, Rect2 Bounds, bool IntoContainer = false);

/// <summary>A physical compartment view: no transfers, capacity decisions or clock progression in drawing.</summary>
public partial class InventoryGrid : Control
{
    public InventoryPanel Panel { get; set; } = null!;
    public string ParentId { get; set; } = "";
    public string RegionId { get; set; } = "";
    public float CellPixels { get; set; } = 29;
    public bool IsPaperdoll { get; set; }
    private bool IsGround => Panel.Simulation.Inventory.FindRoot(ParentId)?.GroundCell is not null;
    internal IReadOnlyList<InventoryItem> DisplayedItems => IsPaperdoll
        ? Panel.Simulation.InventoryPaperdollContents(RegionId, true)
        : Panel.Simulation.InventoryContents(ParentId, RegionId);
    private readonly Color _grid = new("33443d");
    private StorageRegion Region => Panel.Simulation.Inventory.Region(ParentId, RegionId);
    private Vector2 Origin => new(1, 44);
    public void BindSize()
    { CustomMinimumSize = new(Region.Columns * CellPixels + 2, Region.Rows * CellPixels + 47); MouseFilter = MouseFilterEnum.Stop; TextureFilter = TextureFilterEnum.Nearest; ClipContents = true; }
    public override void _Draw()
    {
        var sim = Panel.Simulation; var state = sim.Inventory; var region = Region;
        DrawString(ThemeDB.FallbackFont, new(0, 14), region.Name.Replace("Backpack ", "").Replace("trouser ", ""), fontSize: 13, modulate: new("d8dfcf"));
        DrawString(ThemeDB.FallbackFont, new(0, 31), region.MountSlot is not null ? "worn / held" : $"{region.CellMm}mm · +{region.AccessAp} AP", fontSize: 11, modulate: new("8fa598"));
        for (var y = 0; y < region.Rows; y++)
        for (var x = 0; x < region.Columns; x++)
        {
            var cell = new Rect2(Origin + new Vector2(x, y) * CellPixels, Vector2.One * CellPixels);
                if (region.Mask is { } mask && !mask.Contains(y * region.Columns + x)) { DrawRect(cell, new("080d0c")); continue; }
            DrawRect(cell, new("111917")); DrawRect(cell, _grid, false, 1);
        }
        var preview = Panel.DropPreview is { } p && p.Grid == this ? p : null;
        if (preview is not null)
        {
            var color = preview.Quote.Allowed ? new Color("69d89a") : new Color("ef8275");
            if (!preview.IntoContainer && preview.Request.Destination is { } destination)
            {
                var width = (int)Math.Round(preview.Bounds.Size.X / CellPixels);
                var height = (int)Math.Round(preview.Bounds.Size.Y / CellPixels);
                for (var y = Math.Max(0, destination.Y); y < Math.Min(region.Rows, destination.Y + height); y++)
                for (var x = Math.Max(0, destination.X); x < Math.Min(region.Columns, destination.X + width); x++)
                {
                    var cell = new Rect2(Origin + new Vector2(x, y) * CellPixels, Vector2.One * CellPixels);
                    DrawRect(cell.Grow(-1), new Color(color, 0.30f)); DrawRect(cell.Grow(-1), color, false, 1);
                }
            }
        }
        var access = sim.InventoryAccess(ParentId, RegionId);
        if (!access.Allowed)
        {
            DrawString(ThemeDB.FallbackFont, Origin + new Vector2(5, 20), state.FindRoot(ParentId) is not null ? "Out of reach" : "Closed / unavailable", fontSize: 12, modulate: new("c99c76"));
            TooltipText = access.Reason;
        }
        foreach (var item in DisplayedItems)
        {
            var (w, h) = InventoryPlacement.Footprint(state.Definition(item.Id), region, item.Placement.Rotated);
            var bounds = new Rect2(Origin + new Vector2(item.Placement.X, item.Placement.Y) * CellPixels, new Vector2(w, h) * CellPixels);
            InventoryArt.Draw(this, item.DefinitionId, bounds.Grow(-3), item.Placement.Rotated,
                Panel.DragItemId == item.Id ? 0.3f : InventoryWear.Visible(state, item) ? 1 : 0.55f);
            if (Panel.SelectedItem == item.Id) DrawRect(bounds.Grow(-1), new("c8b77a"), false, 1);
            if (IsGround && !InventoryGround.Covering(state, item).Any())
            {
                var count = DisplayedItems.Count(other => other.Id != item.Id && InventoryGround.Overlap(state, item, other));
                if (count > 0) DrawString(ThemeDB.FallbackFont, bounds.Position + new Vector2(3, 13), "+" + count, fontSize: 12, modulate: new("f0cf88"));
            }
            if (state.Definition(item.Id).Wear?.OpenExposes is { Length: > 0 })
                DrawString(ThemeDB.FallbackFont, bounds.Position + new Vector2(3, bounds.Size.Y - 4), item.GarmentOpen ? "OPEN" : "FASTENED", fontSize: 11, modulate: new("d8dfcf"));
        }
        if (region.MountSlot is not null && state.Children(ParentId, RegionId).Any(i => !InventoryWear.Visible(state, i)))
            DrawString(ThemeDB.FallbackFont, Origin + new Vector2(5, 20), "Covered", fontSize: 12, modulate: new("c99c76"));
        if (preview is not null)
        {
            if (!preview.IntoContainer) InventoryArt.Draw(this, state.Definition(preview.ItemId).Id, preview.Bounds.Grow(-3), (preview.Request.Destination!).Rotated, 0.85f);
            DrawRect(preview.Bounds.Grow(-1), preview.Quote.Allowed ? new("69d89a") : new("ef8275"), false, 2);
        }
    }
    private (int X, int Y) Cell(Vector2 position) => ((int)Math.Floor((position.X - Origin.X) / CellPixels), (int)Math.Floor((position.Y - Origin.Y) / CellPixels));
    private InventoryItem? Pick(Vector2 position)
    {
        var (x, y) = Cell(position);
        var matching = DisplayedItems.Where(i =>
        {
            var (w, h) = InventoryPlacement.Footprint(Panel.Simulation.Inventory.Definition(i.Id), Region, i.Placement.Rotated);
            return x >= i.Placement.X && x < i.Placement.X + w && y >= i.Placement.Y && y < i.Placement.Y + h;
        });
        return IsGround ? matching.LastOrDefault() : matching.FirstOrDefault();
    }
    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion)
        {
            var item = Pick(motion.Position);
            TooltipText = item is null ? Panel.Simulation.InventoryAccess(ParentId, RegionId).Reason : Panel.Describe(item.Id)
                + (IsGround ? "\n" + Panel.Simulation.InventoryMoveAccess(item.Id).Reason + " · move covering items first" : "");
            if (item is null && Panel.SelectedItem is { } selected) Panel.ShowQuote(Panel.Simulation.QuoteInventory(Request(selected, motion.Position)));
        }
        if (e is not InputEventMouseButton { Pressed: true } mouse) return;
        var picked = Pick(mouse.Position);
        if (mouse.ButtonIndex == MouseButton.Right)
        { if (picked is not null) Panel.Context(picked.Id, null, GetGlobalMousePosition());
          else if (!IsPaperdoll) Panel.ContainerContext(ParentId, RegionId, GetGlobalMousePosition()); AcceptEvent(); return; }
        if (mouse.ButtonIndex == MouseButton.Left && mouse.DoubleClick)
        { if (picked is not null) { Panel.Select(picked.Id); Panel.Inspect(picked.Id); } AcceptEvent(); return; }
        if (mouse.ButtonIndex != MouseButton.Left) return;
        if (picked is not null) Panel.Select(picked.Id);
        else if (Panel.SelectedItem is { } id) Place(id, mouse.Position);
        AcceptEvent();
    }
    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Pick(atPosition) is not { } item) return default;
        var access = Panel.Simulation.InventoryMoveAccess(item.Id);
        if (!access.Allowed) { TooltipText = access.Reason; return default; }
        if (OS.GetCmdlineUserArgs().Contains("--inventory-smoke")) GD.Print("INVENTORY_DRAG_START: " + item.Id);
        Panel.Select(item.Id);
        Panel.BeginDrag(item.Id);
        var (w, h) = InventoryPlacement.Footprint(Panel.Simulation.Inventory.Definition(item.Id), Region, item.Placement.Rotated);
        SetDragPreview(new InventoryDragGhost { Panel = Panel, ItemId = item.Id, MouseFilter = MouseFilterEnum.Ignore, Size = new Vector2(w, h) * CellPixels });
        return item.Id;
    }
    private InventoryRequest Request(string id, Vector2 position)
    {
        var (x, y) = Cell(position);
        return new(InventoryActionKind.Move, id, new(ParentId, RegionId, x, y, Panel.Rotated));
    }
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.String || data.AsString() != Panel.DragItemId) return false;
        Panel.PreviewDrop(this, atPosition);
        return Panel.DropPreview?.Quote.Allowed == true;
    }
    public override void _DropData(Vector2 atPosition, Variant data) { if (OS.GetCmdlineUserArgs().Contains("--inventory-smoke")) GD.Print("INVENTORY_DRAG_DROP: " + data.AsString()); Place(data.AsString(), atPosition); }
    private void Place(string id, Vector2 position)
    { Panel.Simulation.HandleInventory(PreviewAt(id, position)?.Request ?? Request(id, position)); Panel.ShowReceipt(); }

    internal InventoryDropPreview? PreviewAt(string id, Vector2 position)
    {
        if (!IsGround && Pick(position) is { } provider && provider.Id != id && Panel.Simulation.Inventory.Definition(provider.Id).Regions?.Any(r => r.Attachment is not null) == true)
        {
            var region = Region; var p = provider.Placement;
            var size = InventoryPlacement.Footprint(Panel.Simulation.Inventory.Definition(provider.Id), region, p.Rotated);
            var preview = Panel.Simulation.PreviewAttachmentDrop(id, provider.Id, Panel.AttachmentRotation);
            return new(this, id, preview.Request, preview.Handling,
                new(Origin + new Vector2(p.X, p.Y) * CellPixels, new Vector2(size.Width, size.Height) * CellPixels), IntoContainer: true);
        }
        if (!IsGround && Pick(position) is { } container && container.Id != id && Panel.DefaultDropRegion(container.Id) is { } interior)
        {
            var (cw, ch) = InventoryPlacement.Footprint(Panel.Simulation.Inventory.Definition(container.Id), Region, container.Placement.Rotated);
            return AutoPreview(id, container.Id, interior,
                new(Origin + new Vector2(container.Placement.X, container.Placement.Y) * CellPixels, new Vector2(cw, ch) * CellPixels));
        }
        if (!new Rect2(Origin, new Vector2(Region.Columns, Region.Rows) * CellPixels).HasPoint(position)) return null;
        // The cursor cell is the footprint's top-left anchor. Drawing and committing use this same request.
        var request = Request(id, position);
        var (w, h) = InventoryPlacement.Footprint(Panel.Simulation.Inventory.Definition(id), Region, request.Destination!.Rotated);
        var bounds = new Rect2(Origin + new Vector2(request.Destination.X, request.Destination.Y) * CellPixels, new Vector2(w, h) * CellPixels);
        return new(this, id, request, Panel.Simulation.QuoteInventory(request), bounds);
    }
    private InventoryDropPreview AutoPreview(string id, string owner, string region, Rect2 targetBounds)
    {
        var request = new InventoryRequest(InventoryActionKind.AutoPlace, id, new(owner, region));
        var quote = Panel.Simulation.QuoteInventoryPacking(request);
        return new(this, id, request, new(quote.Allowed, quote.Reason, quote.DurationMs, quote.Ap), targetBounds, IntoContainer: true);
    }
}

public partial class InventoryDragGhost : Control
{
    public InventoryPanel Panel { get; set; } = null!;
    public string ItemId { get; set; } = "";
    public override void _Process(double delta) => QueueRedraw();
    public override void _Draw()
    {
        // Within a compartment the grid itself draws the exact snapped object; never draw a second offset sprite.
        if (Panel.DropPreview is null && GetViewport().GuiGetHoveredControl() is not InventoryGearSurface { HasAttachmentSurface: true }
            && GetViewport().GuiGetHoveredControl() is not InventoryBodyFitSurface)
            InventoryArt.Draw(this, Panel.Simulation.Inventory.Definition(ItemId).Id, new Rect2(-Size / 2, Size).Grow(-3), Panel.Rotated, 0.8f);
    }
}
