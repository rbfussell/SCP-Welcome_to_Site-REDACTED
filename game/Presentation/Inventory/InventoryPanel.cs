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
    private Label _clock = null!, _receipt = null!, _detail = null!, _progress = null!;
    private InventoryBrowser _browser = null!;
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
        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; body.AddThemeConstantOverride("separation", 26); column.AddChild(body);
        var worn = new VBoxContainer(); body.AddChild(worn);
        Label(worn, "PAPERDOLL / WORN & HELD", 16);
        var mounts = new GridContainer { Columns = 2 }; mounts.AddThemeConstantOverride("h_separation", 16); worn.AddChild(mounts);
        foreach (var slot in new[] { "outerwear", "back", "legs", "hand" }) AddGrid(mounts, "person:avatar", slot, 90);
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
        Button(worn, "Nearby supply crate", () => Inspect("fixture:crate-a"));
        var storage = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; body.AddChild(storage);
        var storageHeader = new HBoxContainer(); storage.AddChild(storageHeader);
        Label(storageHeader, "INVENTORY / INSPECTED CONTAINERS", 16).SizeFlagsHorizontal = SizeFlags.ExpandFill;
        Button(storageHeader, "Back / close view", () => _browser.Back());
        Label(storage, "Right-click worn storage to look inside. Close a view to return up its hierarchy.", 12);
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto }; storage.AddChild(scroll);
        _browser = new InventoryBrowser { Panel = this }; _browser.AddThemeConstantOverride("separation", 18); scroll.AddChild(_browser);
        column.AddChild(new HSeparator());
        _detail = Label(column, "", 13); _detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _progress = Label(column, "", 13);
        _receipt = Label(column, "Arrange equipment before trouble. The world continues while this view is open.", 12);
        _receipt.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        Visible = false;
    }
    internal InventoryGrid AddGrid(Node parent, string parentId, string region, float pixels)
    {
        var grid = new InventoryGrid { Panel = this, ParentId = parentId, RegionId = region, CellPixels = pixels };
        grid.BindSize(); parent.AddChild(grid); return grid;
    }
    internal static Label Label(Node parent, string text, int size)
    {
        var label = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore }; label.AddThemeFontSizeOverride("font_size", size); parent.AddChild(label); return label;
    }
    internal static void Button(Node parent, string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None }; button.Pressed += action; parent.AddChild(button);
    }
    public bool Open()
    {
        if (!Simulation.Inventory.Enabled) return false;
        Visible = true; _lastReceiptId = -1; _browser.Clear();
        SelectedItem = null; Rotated = false;
        _detail.Text = "Your worn equipment is visible. Inspect a garment or bag to reveal its compartments.";
        Refresh(); return true;
    }
    internal InventoryGrid FindGrid(string parentId, string regionId) => Grids(this).First(g => g.ParentId == parentId && g.RegionId == regionId);
    private static IEnumerable<InventoryGrid> Grids(Node parent)
    {
        foreach (var child in parent.GetChildren())
        { if (child is InventoryGrid grid) yield return grid; foreach (var nested in Grids(child)) yield return nested; }
    }
    public void Close()
    {
        CancelDragPreview();
        _browser.Clear(); Visible = false; GetViewport().GuiReleaseFocus();
    }
    internal void CancelDragPreview()
    {
        if (DragItemId is not null) GetViewport().GuiCancelDrag();
        DragItemId = null; DropPreview = null;
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
        if (!_browser.Inspect(id, out var reason)) _receipt.Text = reason;
    }
    internal IReadOnlyList<string> InspectedContainers => _browser.ContainerIds;
    internal void CloseContainerView(string id) => _browser.CloseView(id);
    internal void ChangeLid(string id, string region, bool open)
    { Simulation.HandleInventory(new(open ? InventoryActionKind.Open : InventoryActionKind.Close, id, RegionId: region)); ShowReceipt(); }
    public void ShowQuote(InventoryActionQuote quote) => _receipt.Text = quote.Allowed ? $"Handling: {quote.DurationMs / 1000.0:0.00}s · {quote.Ap} AP in combat" : quote.Reason;
    public void ShowReceipt() => _receipt.Text = Simulation.LastMessage;
    public void Refresh()
    {
        if (!Visible) return;
        var viewport = GetViewportRect().Size; _window.Position = new(24, 28); _window.Size = viewport - new Vector2(48, 56);
        _clock.Text = Simulation.ManualPause ? "MANUAL PAUSE" : Simulation.Combat.Active ? $"TACTICAL · {Simulation.Combat.For(Simulation.Player.Id)?.Ap ?? 0} AP" : $"REAL TIME · {Simulation.LocalMs / 1000.0:0.0}s";
        _browser.Refresh();
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
            if (key.Keycode == Key.I) Close();
            else if (key.Keycode == Key.Escape) { if (DragItemId is not null || !_browser.Back()) Close(); }
            else if (key.Keycode == Key.R) Rotate();
            else if (key.Keycode == Key.Space) Simulation.ManualPause = !Simulation.ManualPause;
            else if (key.Keycode == Key.Enter && Simulation.Combat.Active) Simulation.EndTurn();
        }
        GetViewport().SetInputAsHandled();
    }
}
