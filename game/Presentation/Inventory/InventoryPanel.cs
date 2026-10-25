using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Realtime equipment and compartment view. Only validated timed commands change the item graph.</summary>
public partial class InventoryPanel : Control
{
    public SpatialSimulation Simulation { get; private set; } = null!;
    public string? SelectedItem { get; private set; }
    public bool Rotated { get; private set; }
    public bool IsOpen => Visible;
    internal string? DragItemId { get; private set; }
    internal InventoryDropPreview? DropPreview { get; private set; }
    private PanelContainer _window = null!;
    private Label _clock = null!, _receipt = null!, _detail = null!, _progress = null!, _breadcrumb = null!;
    private VBoxContainer _nested = null!;
    private InventoryGrid? _nestedGrid;
    private string _inspected = "medical-case";
    private OptionButton _regions = null!;
    private string? _selectedRegion;
    private long _lastReceiptId = -1;
    public void Initialize(SpatialSimulation simulation)
    {
        Simulation = simulation;
        if (!simulation.Inventory.Enabled) { Visible = false; return; }
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Stop;
        Theme = new Theme { DefaultFontSize = 13 };
        Theme.SetColor("font_color", "Label", new("dddcca"));
        var dim = new ColorRect { Color = new(0.018f, 0.025f, 0.025f, 0.90f) }; AddChild(dim); dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _window = new PanelContainer(); AddChild(_window);
        _window.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new("101916"), BorderColor = new("4c5c4d"), BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1, ContentMarginLeft = 22, ContentMarginRight = 22, ContentMarginTop = 16, ContentMarginBottom = 16 });
        var column = new VBoxContainer(); _window.AddChild(column); column.AddThemeConstantOverride("separation", 10);
        var header = new HBoxContainer(); column.AddChild(header);
        Label(header, "PERSONAL EQUIPMENT", 23).SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _clock = Label(header, "", 13);
        Button(header, "CLOSE  /  I", Close);
        Label(column, "Drag an object to place it · R rotates · double-click or right-click inspects a container · selecting and clicking an empty cell also places", 12);
        column.AddChild(new HSeparator());
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto }; column.AddChild(scroll);
        var body = new HBoxContainer(); body.AddThemeConstantOverride("separation", 32); scroll.AddChild(body);
        var worn = new VBoxContainer(); body.AddChild(worn);
        Label(worn, "WORN / HELD", 16);
        var mounts = new GridContainer { Columns = 2 }; mounts.AddThemeConstantOverride("h_separation", 16); worn.AddChild(mounts);
        foreach (var slot in new[] { "outerwear", "back", "legs", "hand" }) AddGrid(mounts, "person:avatar", slot, 100);
        Label(worn, "Other mounts: underwear, socks,\nfootwear, torso, belt, face.\nNo items issued there in this slice.", 12);
        Button(worn, "Rotate selected object  R", Rotate);
        Button(worn, "Inspect selected container", () => { if (SelectedItem is { } id) Inspect(id); });
        Button(worn, "Switch selected power", () =>
        {
            if (SelectedItem is not { } id) return;
            var item = Simulation.Inventory.DiagnosticItems.Single(i => i.Id == id);
            Simulation.HandleInventory(new(item.Powered ? InventoryActionKind.PowerOff : InventoryActionKind.PowerOn, id)); ShowReceipt();
        });
        Button(worn, "Cancel handling", () => { Simulation.CancelInventoryHandling(); ShowReceipt(); });
        var pockets = new VBoxContainer(); body.AddChild(pockets);
        Label(pockets, "POCKETS", 16);
        var pair = new HBoxContainer(); pair.AddThemeConstantOverride("separation", 18); pockets.AddChild(pair);
        AddGrid(pair, "avatar-trousers", "left", 30); AddGrid(pair, "avatar-trousers", "right", 30);
        pair = new HBoxContainer(); pair.AddThemeConstantOverride("separation", 18); pockets.AddChild(pair);
        AddGrid(pair, "avatar-coat", "chest", 30); AddGrid(pair, "avatar-backpack", "side", 30);
        var bag = new VBoxContainer(); body.AddChild(bag);
        Label(bag, "PACK", 16);
        AddGrid(bag, "avatar-backpack", "main", 29);
        var bagButtons = new HBoxContainer(); bag.AddChild(bagButtons);
        Button(bagButtons, "Open main", () => ChangeLid("avatar-backpack", "main", true));
        Button(bagButtons, "Close main", () => ChangeLid("avatar-backpack", "main", false));
        _nested = new VBoxContainer { CustomMinimumSize = new(270, 0) }; body.AddChild(_nested);
        Label(_nested, "INSPECT / NESTED STORAGE", 16);
        _breadcrumb = Label(_nested, "", 12); _breadcrumb.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _regions = new OptionButton { FocusMode = FocusModeEnum.None }; _nested.AddChild(_regions);
        _regions.ItemSelected += index => { _selectedRegion = _regions.GetItemMetadata((int)index).AsString(); BuildNested(); };
        var lids = new HBoxContainer(); _nested.AddChild(lids);
        Button(lids, "Open", () => { if (_selectedRegion is { } r) ChangeLid(_inspected, r, true); });
        Button(lids, "Close", () => { if (_selectedRegion is { } r) ChangeLid(_inspected, r, false); });
        Button(_nested, "Nearby supply crate", () => Inspect("fixture:crate-a"));
        column.AddChild(new HSeparator());
        _detail = Label(column, "", 13); _detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _progress = Label(column, "", 13);
        _receipt = Label(column, "Arrange equipment before trouble. The world continues while this view is open.", 12);
        _receipt.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        Visible = false;
    }
    private InventoryGrid AddGrid(Node parent, string parentId, string region, float pixels)
    {
        var grid = new InventoryGrid { Panel = this, ParentId = parentId, RegionId = region, CellPixels = pixels };
        grid.BindSize(); parent.AddChild(grid); return grid;
    }
    private static Label Label(Node parent, string text, int size)
    {
        var label = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore }; label.AddThemeFontSizeOverride("font_size", size); parent.AddChild(label); return label;
    }
    private static void Button(Node parent, string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None }; button.Pressed += action; parent.AddChild(button);
    }
    public bool Open()
    {
        if (!Simulation.Inventory.Enabled) return false;
        Visible = true; _lastReceiptId = -1; Select("field-phone"); Inspect(_inspected); Refresh(); return true;
    }
    internal InventoryGrid FindGrid(string parentId, string regionId) => Grids(this).First(g => g.ParentId == parentId && g.RegionId == regionId);
    private static IEnumerable<InventoryGrid> Grids(Node parent)
    {
        foreach (var child in parent.GetChildren())
        { if (child is InventoryGrid grid) yield return grid; foreach (var nested in Grids(child)) yield return nested; }
    }
    public void Close()
    {
        if (DragItemId is not null) GetViewport().GuiCancelDrag();
        DragItemId = null; DropPreview = null;
        Visible = false; GetViewport().GuiReleaseFocus();
    }
    internal void BeginDrag(string id) { DragItemId = id; DropPreview = null; }
    internal void PreviewDrop(InventoryGrid grid, Vector2 position)
    {
        DropPreview = DragItemId is { } id ? grid.PreviewAt(id, position) : null;
        if (DropPreview is { } preview) ShowQuote(preview.Quote);
    }
    public override void _Process(double delta) => RefreshDragPreview();
    private void RefreshDragPreview()
    {
        if (!Visible || !GetViewport().GuiIsDragging()) { DragItemId = null; DropPreview = null; return; }
        if (GetViewport().GuiGetHoveredControl() is InventoryGrid grid && grid.Panel == this)
            PreviewDrop(grid, grid.GetLocalMousePosition());
        else DropPreview = null;
    }
    public void Toggle() { if (Visible) Close(); else Open(); }
    public void Select(string id)
    {
        SelectedItem = id;
        Rotated = Simulation.Inventory.DiagnosticItems.Single(i => i.Id == id).Placement.Rotated;
        _detail.Text = Describe(id);
    }
    public string Describe(string id)
    {
        var state = Simulation.Inventory; var item = state.DiagnosticItems.Single(i => i.Id == id); var definition = state.Definition(id);
        return $"{definition.Name} · {definition.WidthMm} × {definition.LengthMm} × {definition.ThicknessMm} mm · {InventoryHierarchy.Mass(state, id)} g including contents\n"
            + InventoryHierarchy.Breadcrumb(state, id) + (definition.Capability is null ? "" : item.Powered ? " · powered" : " · switched off")
            + (definition.Capability is not null && !Simulation.HasAccessibleEquipment(Simulation.Player.Id, definition.Capability) ? " · unavailable until accessible and powered" : "");
    }
    public void Rotate() { if (SelectedItem is null) return; Rotated = !Rotated; RefreshDragPreview(); _receipt.Text = "Placement preview rotated; drop to begin timed handling."; }
    public void Inspect(string id)
    {
        var state = Simulation.Inventory;
        var regions = state.Content!.Roots.FirstOrDefault(r => r.Id == id)?.Regions
            ?? state.DiagnosticItems.Where(i => i.Id == id).Select(i => state.Definition(i.Id).Regions).SingleOrDefault();
        if (regions is not { Length: > 0 }) { _receipt.Text = "This object is not a container."; return; }
        if (!Simulation.InventoryAccess(id, regions[0].Id).Allowed && !state.DiagnosticItems.Any(i => i.Id == id
            && Simulation.InventoryAccess(i.Placement.ParentId, i.Placement.RegionId).Allowed))
        { _receipt.Text = "Container is not physically accessible."; return; }
        _inspected = id; _regions.Clear();
        foreach (var region in regions) { _regions.AddItem(region.Name); _regions.SetItemMetadata(_regions.ItemCount - 1, region.Id); }
        _selectedRegion = regions[0].Id; BuildNested();
    }
    private void BuildNested()
    {
        if (_nestedGrid is not null) { _nested.RemoveChild(_nestedGrid); _nestedGrid.QueueFree(); }
        if (_selectedRegion is not { } region) return;
        var r = Simulation.Inventory.Region(_inspected, region);
        _nestedGrid = AddGrid(_nested, _inspected, region, Math.Min(44, 270f / r.Columns));
    }
    private void ChangeLid(string id, string region, bool open)
    { Simulation.HandleInventory(new(open ? InventoryActionKind.Open : InventoryActionKind.Close, id, RegionId: region)); ShowReceipt(); }
    public void ShowQuote(InventoryActionQuote quote) => _receipt.Text = quote.Allowed ? $"Handling: {quote.DurationMs / 1000.0:0.00}s · {quote.Ap} AP in combat" : quote.Reason;
    public void ShowReceipt() => _receipt.Text = Simulation.LastMessage;
    public void Refresh()
    {
        if (!Visible) return;
        var viewport = GetViewportRect().Size; _window.Position = new(24, 28); _window.Size = viewport - new Vector2(48, 56);
        _clock.Text = Simulation.ManualPause ? "MANUAL PAUSE" : Simulation.Combat.Active ? $"TACTICAL · {Simulation.Combat.For(Simulation.Player.Id)?.Ap ?? 0} AP" : $"REAL TIME · {Simulation.LocalMs / 1000.0:0.0}s";
        _breadcrumb.Text = Simulation.Inventory.Content!.Roots.Any(r => r.Id == _inspected) ? "Nearby fixture / " + _inspected : InventoryHierarchy.Breadcrumb(Simulation.Inventory, _inspected);
        if (_selectedRegion is { } regionId) _breadcrumb.Text += Simulation.Inventory.IsOpen(_inspected, regionId) ? "\nOPEN" : "\nCLOSED";
        _progress.Text = Simulation.Inventory.Pending is { } work ? $"HANDLING · {(work.TotalMs - work.RemainingMs) / 1000.0:0.00} / {work.TotalMs / 1000.0:0.00}s · {work.Request.Kind}"
            : $"Carried mass: {InventoryHierarchy.CarriedMass(Simulation.Inventory, "person:avatar") / 1000.0:0.00} kg · no handling in progress";
        var receipt = Simulation.Events.Entries.LastOrDefault(e => e.Text.StartsWith("Inventory: ", StringComparison.Ordinal));
        if (receipt is not null && receipt.Id != _lastReceiptId) { _lastReceiptId = receipt.Id; _receipt.Text = receipt.Text; }
        Redraw(this);
    }
    public override void _ExitTree() => InventoryArt.Release();
    private static void Redraw(Node node) { if (node is CanvasItem item) item.QueueRedraw(); foreach (var child in node.GetChildren()) Redraw(child); }
    public void HandleInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.Keycode is Key.Escape or Key.I) Close();
            else if (key.Keycode == Key.R) Rotate();
            else if (key.Keycode == Key.Space) Simulation.ManualPause = !Simulation.ManualPause;
            else if (key.Keycode == Key.Enter && Simulation.Combat.Active) Simulation.EndTurn();
        }
        GetViewport().SetInputAsHandled();
    }
}
