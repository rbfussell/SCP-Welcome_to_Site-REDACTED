using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Realtime equipment and compartment view. Only validated timed commands change the item graph.</summary>
public partial class InventoryPanel : Control
{
    public SpatialSimulation Simulation { get; private set; } = null!;
    public string? SelectedItem { get; private set; }
    public bool Rotated { get; private set; }
    internal int AttachmentRotation { get; private set; }
    public bool IsOpen => Visible;
    internal string? DragItemId { get; private set; }
    internal InventoryDropPreview? DropPreview { get; private set; }
    private PanelContainer _window = null!;
    private Label _clock = null!, _receipt = null!, _detail = null!, _progress = null!;
    private InventoryBrowser _browser = null!;
    private PanelContainer? _contextMenu;
    private readonly List<Action> _contextUpdates = [];
    internal InventoryBrowser Browser => _browser;
    private Button _garmentToggle = null!, _takeInHand = null!, _wearDefault = null!;
    private InventoryBodyFitPanel _bodyFit = null!;
    private Button _bodyFitToggle = null!;
    private VBoxContainer _equipmentActions = null!;
    internal InventoryBodyFitPanel BodyFit => _bodyFit;
    internal Button BodyFitToggle => _bodyFitToggle;
    private ScrollContainer _wornScroll = null!;
    private OptionButton _layerView = null!;
    internal bool ShowCoveredLayers => _layerView.Selected != 0;
    internal int? ViewedLayer => _layerView.Selected >= 2 ? _layerView.Selected - 2 : null;
    internal OptionButton LayerView => _layerView;
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
        Label(column, "Double-click opens · right-click for actions · drag to move · R rotates · ↑/× use gear securing preferences · Esc hides interface", 12);
        column.AddChild(new HSeparator());
        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; body.AddThemeConstantOverride("separation", 26); column.AddChild(body);
        var worn = new VBoxContainer(); body.AddChild(worn);
        Label(worn, "PAPERDOLL / WORN & HELD", 16);
        _bodyFitToggle = Button(worn, "Body fit · hex view", ToggleBodyFit);
        _layerView = new OptionButton { FocusMode = FocusModeEnum.None }; worn.AddChild(_layerView);
        _layerView.AddItem("Layers: exposed equipment"); _layerView.AddItem("Layers: all worn equipment");
        for (var layer = 0; layer <= 9; layer++) _layerView.AddItem($"Layer {layer} · inspect");
        _layerView.ItemSelected += _ => { CancelDragPreview(); Redraw(this); };
        _layerView.TooltipText = "Inspect known worn layers. Covered equipment stays inaccessible until physically exposed.";
        _wornScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new(234, 180), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; worn.AddChild(_wornScroll);
        var mounts = new GridContainer { Columns = 2 }; mounts.AddThemeConstantOverride("h_separation", 16); _wornScroll.AddChild(mounts);
        foreach (var slot in new[] { "outerwear", "back", "legs", "hand", "torso", "suit", "socks", "footwear", "front", "belt" }) AddGrid(mounts, "person:avatar", slot, 90);
        _bodyFit = new InventoryBodyFitPanel { Panel = this, Visible = false }; worn.AddChild(_bodyFit);
        _equipmentActions = new VBoxContainer(); worn.AddChild(_equipmentActions);
        _garmentToggle = new Button { Text = "Open / fasten selected garment", FocusMode = FocusModeEnum.None }; _equipmentActions.AddChild(_garmentToggle);
        _garmentToggle.Pressed += ToggleGarment;
        _takeInHand = Button(_equipmentActions, "Take selected in hand", () => MoveSelectedTo("hand"));
        _wearDefault = Button(_equipmentActions, "Wear selected (default mount)", () =>
        {
            if (SelectedItem is { } id && Simulation.Inventory.Definition(id).WearSlot is { } slot) MoveSelectedTo(slot);
        });
        Button(_equipmentActions, "Rotate selected object  R", Rotate);
        Button(_equipmentActions, "Inspect selected container", () => { if (SelectedItem is { } id) Inspect(id); });
        Button(_equipmentActions, "Switch selected power", () =>
        {
            if (SelectedItem is not { } id) return;
            var item = Simulation.Inventory.DiagnosticItems.Single(i => i.Id == id);
            Simulation.HandleInventory(new(item.Powered ? InventoryActionKind.PowerOff : InventoryActionKind.PowerOn, id)); ShowReceipt();
        });
        Button(_equipmentActions, "Cancel handling", () => { Simulation.CancelInventoryHandling(); ShowReceipt(); });
        Button(_equipmentActions, "Nearby supply crate", () => Inspect("fixture:crate-a"));
        var storage = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; body.AddChild(storage);
        var storageHeader = new HBoxContainer(); storage.AddChild(storageHeader);
        Label(storageHeader, "GEAR & CONTENTS / TWO INDEPENDENT TRAILS", 16).SizeFlagsHorizontal = SizeFlags.ExpandFill;
        Label(storage, "Compartments open individually. ↑/× follow each fastening’s preference. Esc leaves physical closures unchanged.", 12);
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; storage.AddChild(scroll);
        _browser = new InventoryBrowser { Panel = this, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _browser.AddThemeConstantOverride("separation", 18); scroll.AddChild(_browser);
        column.AddChild(new HSeparator());
        _detail = Label(column, "", 13); _detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _progress = Label(column, "", 13);
        _receipt = Label(column, "Arrange equipment before trouble. The world continues while this view is open.", 12);
        _receipt.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        Visible = false;
    }
    internal InventoryGrid AddGrid(Node parent, string parentId, string region, float pixels)
    {
        var grid = new InventoryGrid { Panel = this, ParentId = parentId, RegionId = region, CellPixels = pixels,
            IsPaperdoll = Simulation.Inventory.Content!.Roots.Any(r => r.Id == parentId && r.ActorId == Simulation.Player.Id) };
        grid.BindSize(); parent.AddChild(grid); return grid;
    }
    internal static Label Label(Node parent, string text, int size)
    {
        var label = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore }; label.AddThemeFontSizeOverride("font_size", size); parent.AddChild(label); return label;
    }
    internal static Button Button(Node parent, string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None }; button.Pressed += action; parent.AddChild(button);
        return button;
    }
    public bool Open()
    {
        if (!Simulation.Inventory.Enabled) return false;
        Visible = true; _lastReceiptId = -1; _browser.Clear();
        SetBodyFit(false);
        _wornScroll.ScrollVertical = 0;
        _layerView.Select(0);
        SelectedItem = null; Rotated = false;
        _detail.Text = "Your worn equipment is visible. Inspect a garment or bag to reveal its compartments.";
        Refresh(); return true;
    }
    private void SetBodyFit(bool enabled)
    {
        _bodyFit.Visible = enabled; _wornScroll.Visible = _layerView.Visible = _equipmentActions.Visible = !enabled;
        _bodyFitToggle.Text = enabled ? "Equipment slots" : "Body fit · hex view";
        if (enabled) _bodyFit.Refresh();
    }
    private void ToggleBodyFit() { CancelDragPreview(); SetBodyFit(!_bodyFit.Visible); }
    internal InventoryGrid FindGrid(string parentId, string regionId) => Grids(this).First(g => g.ParentId == parentId && g.RegionId == regionId);
    private static IEnumerable<InventoryGrid> Grids(Node parent)
    {
        foreach (var child in parent.GetChildren())
        { if (child is InventoryGrid grid) yield return grid; foreach (var nested in Grids(child)) yield return nested; }
    }
    public void Close()
    {
        HideContext();
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
        var placement = Simulation.Inventory.DiagnosticItems.Single(i => i.Id == id).Placement;
        Rotated = placement.Rotated; AttachmentRotation = placement.AttachmentPose?.Rotation ?? 0;
        _detail.Text = Describe(id);
    }
    internal Button GarmentToggle => _garmentToggle;
    internal Button TakeInHand => _takeInHand;
    internal Button WearDefault => _wearDefault;
    private void ToggleGarment()
    {
        if (SelectedItem is not { } id) return;
        var item = Simulation.Inventory.DiagnosticItems.Single(i => i.Id == id);
        Simulation.HandleInventory(new(item.GarmentOpen ? InventoryActionKind.CloseGarment : InventoryActionKind.OpenGarment, id)); ShowReceipt();
    }
    private void MoveSelectedTo(string slot)
    {
        if (SelectedItem is { } id) { Simulation.HandleInventory(new(InventoryActionKind.Move, id, new("person:avatar", slot))); ShowReceipt(); }
    }
    public string Describe(string id)
    {
        var state = Simulation.Inventory; var item = state.DiagnosticItems.Single(i => i.Id == id); var definition = state.Definition(id);
        return $"{definition.Name} · {definition.WidthMm} × {definition.LengthMm} × {definition.ThicknessMm} mm · {InventoryHierarchy.Mass(state, id)} g including contents\n"
            + InventoryHierarchy.Breadcrumb(state, id) + (definition.Wear is { } wear ? $" · layer {wear.Layer}" + (wear.OpenExposes is null ? "" : item.GarmentOpen ? " · garment open" : " · garment fastened") : "")
            + (definition.Capability is null ? "" : item.Powered ? " · powered" : " · switched off")
            + (definition.Capability is not null && !Simulation.HasAccessibleEquipment(Simulation.Player.Id, definition.Capability) ? " · unavailable until accessible and powered" : "");
    }
    public void Rotate() { if (_bodyFit.Visible) { _bodyFit.RotateDraft(); return; } if (SelectedItem is null) return;
        if (GetViewport().GuiGetHoveredControl() is InventoryGearSurface surface && surface.HasAttachmentSurface)
        { AttachmentRotation = (AttachmentRotation + 1) % 6; surface.QueueRedraw(); return; }
        Rotated = !Rotated; RefreshDragPreview(); _receipt.Text = "Placement preview rotated; drop to begin timed handling."; }
    public void Inspect(string id)
    {
        if (!_browser.Inspect(id, out var reason)) _receipt.Text = reason;
    }
    internal void OpenCompartment(string id, string region)
    { _browser.OpenRegion(id, region, out var reason); _receipt.Text = reason; }
    internal void OpenGarment(string id)
    {
        if (Simulation.Inventory.DiagnosticItems.Single(i => i.Id == id).GarmentOpen) return;
        Simulation.HandleInventory(new(InventoryActionKind.OpenGarment, id)); ShowReceipt();
    }
    internal void ToggleClosure(string id, string? region = null)
    {
        var action = InventoryClosures.Toggle(Simulation.Inventory, id, region);
        if (action.Request.Kind == InventoryActionKind.Open) OpenCompartment(id, region!);
        else if (action.Request.Kind == InventoryActionKind.OpenGarment) OpenGarment(id);
        else { Simulation.HandleInventory(action.Request); ShowReceipt(); }
    }
    internal string? DefaultDropRegion(string id)
    {
        var definition = Simulation.Inventory.Definition(id);
        return definition.DefaultDropRegion ?? (definition.Regions is { Length: 1 } regions && regions[0].Attachment is null ? regions[0].Id : null);
    }
    internal InventoryPackingQuote PreviewContainerDrop(string item, string owner, string region)
    {
        var quote = Simulation.QuoteInventoryPacking(new(InventoryActionKind.AutoPlace, item, new(owner, region)));
        _receipt.Text = quote.Allowed ? $"{(quote.Plan!.Repacked ? "Arrange and store" : "Store in free space")} · {quote.Plan.Moves.Length} items handled · {quote.DurationMs / 1000.0:0.00}s · {quote.Ap} AP in combat" : quote.Reason;
        return quote;
    }
    internal void StoreInContainer(string item, string owner, string region)
    { Simulation.HandleInventory(new(InventoryActionKind.AutoPlace, item, new(owner, region))); ShowReceipt(); }
    internal void Arrange(string owner, string region)
    { Simulation.HandleInventory(new(InventoryActionKind.Arrange, owner, new(owner, region))); ShowReceipt(); }
    private void HideContext()
    { _contextUpdates.Clear(); if (_contextMenu is not null) { RemoveChild(_contextMenu); _contextMenu.QueueFree(); _contextMenu = null; } }
    public override void _Input(InputEvent e)
    {
        if (Visible && _contextMenu is not null && e is InputEventMouseButton { Pressed: true } mouse
            && !_contextMenu.GetGlobalRect().HasPoint(mouse.Position)) HideContext();
    }
    private VBoxContainer BeginContext()
    {
        HideContext();
        _contextMenu = new PanelContainer { ZIndex = 20 };
        _contextMenu.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new("28372e"), ContentMarginLeft = 8,
            ContentMarginRight = 8, ContentMarginTop = 8, ContentMarginBottom = 8 });
        AddChild(_contextMenu); var list = new VBoxContainer(); _contextMenu.AddChild(list); return list;
    }
    private void PositionContext(Vector2 position)
    {
        var size = _contextMenu!.GetCombinedMinimumSize();
        _contextMenu.Position = new(Math.Clamp(position.X + 10, 0, Math.Max(0, Size.X - size.X)), Math.Clamp(position.Y, 0, Math.Max(0, Size.Y - size.Y)));
    }
    internal void ContainerContext(string owner, string region, Vector2 position)
    {
        var list = BeginContext();
        Button(list, "Arrange " + Simulation.Inventory.Region(owner, region).Name, () => { HideContext(); Arrange(owner, region); });
        PositionContext(position);
    }
    internal void Context(string id, string? region, Vector2 position, bool fastening = false)
    {
        Select(id); var list = BeginContext();
        var definition = Simulation.Inventory.Definition(id);
        void Entry(string name, Action action) => Button(list, name, () => { HideContext(); action(); });
        if (!fastening && (region is null || !Simulation.Inventory.Region(id, region).Sealable)) Entry("Open", () => { if (region is not null) OpenCompartment(id, region); else Inspect(id); });
        var closureRegion = region ?? (definition.Wear?.OpenExposes is not { Length: > 0 } && definition.Regions is { Length: 1 } regions && regions[0].Attachment is null ? regions[0].Id : null);
        if (closureRegion is not null && !Simulation.Inventory.Region(id, closureRegion).Sealable) closureRegion = null;
        if (closureRegion is not null || region is null && definition.Wear?.OpenExposes is { Length: > 0 })
        {
            var closure = Button(list, "", () =>
            {
                HideContext(); ToggleClosure(id, closureRegion);
            });
            var automatic = new CheckButton { Text = "Secure on view close", FocusMode = FocusModeEnum.None,
                TooltipText = "Only this fastening. Changing this preference does not physically open or secure it." }; list.AddChild(automatic);
            automatic.Toggled += value =>
            {
                Simulation.SetInventorySecureOnClose(id, closureRegion, value); ShowReceipt();
                automatic.SetPressedNoSignal(InventoryClosures.SecureOnClose(Simulation.Inventory, id, closureRegion));
            };
            void UpdateClosure()
            {
                var action = InventoryClosures.Toggle(Simulation.Inventory, id, closureRegion);
                closure.Text = action.Verb;
                closure.Disabled = Simulation.Inventory.Pending is not null || !Simulation.QuoteInventory(action.Request).Allowed;
                closure.TooltipText = Simulation.InventoryCost(action.Request);
                automatic.SetPressedNoSignal(InventoryClosures.SecureOnClose(Simulation.Inventory, id, closureRegion));
            }
            _contextUpdates.Add(UpdateClosure); UpdateClosure();
        }
        if (region is not null) Entry("Arrange " + Simulation.Inventory.Region(id, region).Name, () => Arrange(id, region));
        else foreach (var interior in (definition.Regions ?? []).Where(r => r.Attachment is null))
            Entry("Arrange " + interior.Name, () => Arrange(id, interior.Id));
        var location = Simulation.Inventory.DiagnosticItems.Single(i => i.Id == id).Placement;
        var enclosing = Simulation.Inventory.Region(location.ParentId, location.RegionId);
        if (enclosing.MountSlot is null && enclosing.Attachment is null) Entry("Arrange " + enclosing.Name, () => Arrange(location.ParentId, location.RegionId));
        Entry("Remove → hand", () => { Select(id); MoveSelectedTo("hand"); });
        PositionContext(position);
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
        _browser.Refresh(); _bodyFit.Refresh();
        _garmentToggle.Disabled = SelectedItem is not { } selected || Simulation.Inventory.Definition(selected).Wear?.OpenExposes is not { Length: > 0 };
        _garmentToggle.Text = !_garmentToggle.Disabled ? InventoryClosures.Toggle(Simulation.Inventory, SelectedItem!).Verb + " selected garment" : "Garment fastening";
        if (SelectedItem is { } current) _detail.Text = Describe(current);
        foreach (var update in _contextUpdates) update();
        _progress.Text = Simulation.Inventory.Pending is { } work ? $"HANDLING · {(work.TotalMs - work.RemainingMs) / 1000.0:0.00} / {work.TotalMs / 1000.0:0.00}s · {work.Request.Kind}"
            : Simulation.Inventory.SecuringRemaining > 0 ? $"SECURING · {Simulation.Inventory.SecuringRemaining} closures waiting for local progress / your turn and sufficient AP · Cancel handling to stop"
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
            else if (key.Keycode == Key.Escape) Close();
            else if (key.Keycode == Key.R) Rotate();
            else if (key.Keycode == Key.Space) Simulation.ManualPause = !Simulation.ManualPause;
            else if (key.Keycode == Key.Enter && Simulation.Combat.Active) Simulation.EndTurn();
        }
        GetViewport().SetInputAsHandled();
    }
}
