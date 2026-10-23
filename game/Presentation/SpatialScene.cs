using Godot;
using SiteSimulation;
using System.IO;

namespace SitePresentation;

/// <summary>Godot scene adapter for the bounded spatial proof, not the complete M0 milestone.</summary>
public partial class SpatialScene : Node2D
{
    private SpatialSimulation _simulation = null!;
    private WorldView _world = null!;
    private Control _field = null!;
    private Label _clock = null!, _mode = null!, _status = null!, _coordinates = null!, _timer = null!;
    private Label _counters = null!;
    private Control _sidebar = null!, _footer = null!;
    private double _accumulator;
    private float _zoom = 1.25f;
    private Vector2 _pan;
    private Hex? _previousHover;
    private string? _capturePath;
    private int _smokeFrame;
    private int _smokeStage;
    private readonly Dictionary<Key, CheckButton> _toggles = [];
    private string? _notice;
    private double _noticeSeconds;
    private string? _smokeSave;
    private static readonly Color Text = new("e4e3d2");
    private static readonly Color Muted = new("97a7a0");
    private static readonly Color Green = new("a0d1af");

    public override void _Ready()
    {
        _simulation = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"));
        var layer = new CanvasLayer(); AddChild(layer);
        var hud = new Control { Name = "HUD", MouseFilter = Control.MouseFilterEnum.Ignore };
        hud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); layer.AddChild(hud);
        var theme = new Theme();
        theme.DefaultFontSize = 14;
        theme.SetColor("font_color", "Label", Text);
        theme.SetColor("font_color", "Button", Text);
        theme.SetStylebox("normal", "Button", Box("1c2927", "3a4d43"));
        theme.SetStylebox("hover", "Button", Box("2c4035", "9ac5a4"));
        theme.SetStylebox("pressed", "Button", Box("374e3d", "9ac5a4"));
        hud.Theme = theme;
        LabelAt(hud, "FOUNDATION  /  SITE [REDACTED]", new(28, 21), 25, Text);
        LabelAt(hud, "SPATIAL QUALIFICATION     /     FLOOR + HEX REGISTRATION     /     M0.1", new(30, 61), 12, Green);
        _clock = LabelAt(hud, "", new(920, 24), 17, Text);
        LabelAt(hud, "DEBUG VIEW · full map visibility", new(30, 101), 11, Muted);
        _field = new Control { Position = new(20, 124), ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        hud.AddChild(_field);
        _world = new WorldView { Simulation = _simulation }; _field.AddChild(_world);
        _sidebar = new PanelContainer { Position = new(1134, 103), CustomMinimumSize = new(280, 0) };
        ((PanelContainer)_sidebar).AddThemeStyleboxOverride("panel", Box("111c1b", "304039"));
        hud.AddChild(_sidebar);
        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 18);
        _sidebar.AddChild(margin);
        var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 6); margin.AddChild(column);
        TextLine(column, "SCENE CONTROLS", 12, Green);
        TextLine(column, "Two rooms · service corridor", 17, Text);
        _mode = TextLine(column, "", 13, Green);
        AddRule(column);
        _toggles[Key.C] = Toggle(column, "White collision boundaries  [C]", true, value => _world.CollisionOverlay = value);
        _toggles[Key.G] = Toggle(column, "Actor hex grid  [G]", false, value => _world.HexGrid = value);
        _toggles[Key.B] = Toggle(column, "Square floor grid  [B]", false, value => _world.FloorGrid = value);
        _toggles[Key.L] = Toggle(column, "Directional sight  [L]", true, value => _world.SightOverlay = value);
        _toggles[Key.H] = Toggle(column, "Wall cutaway  [H]", true, value => _world.Cutaway = value);
        AddRule(column);
        Button(column, "Use nearest door     E", () => _simulation.ToggleNearestDoor());
        Button(column, "Local tactical hold     F6", () => _simulation.LocalHold = !_simulation.LocalHold);
        Button(column, "Global manual pause     Space", () => _simulation.ManualPause = !_simulation.ManualPause);
        var speeds = new HBoxContainer(); column.AddChild(speeds);
        foreach (var speed in new[] { 1, 2, 4 }) Button(speeds, $"{speed}×", () => _simulation.Speed = speed);
        AddRule(column);
        _timer = TextLine(column, "", 13, Text);
        _counters = TextLine(column, "", 12, Muted);
        TextLine(column, "Local hold freezes work and timers.\nThe world / off-site counter continues.", 11, Muted);
        AddRule(column);
        var saveButtons = new HBoxContainer(); column.AddChild(saveButtons);
        Button(saveButtons, "Save  F5", Save); Button(saveButtons, "Restore  F9", Restore);
        Button(column, "Reset camera     Home", () => { _pan = Vector2.Zero; _zoom = 1.25f; });
        TextLine(column, "White: blocked cells + wall footprints\nMint: sight / route   Amber: doors\n650 is a static pose fixture here.", 11, Muted);
        _footer = new Control { MouseFilter = Control.MouseFilterEnum.Ignore }; hud.AddChild(_footer);
        _status = LabelAt(_footer, "", new(28, 0), 14, Text);
        _coordinates = LabelAt(_footer, "", new(28, 26), 11, Green);
        LabelAt(_footer, "LMB move   ·   Ctrl-click / RMB face   ·   Shift-click keep facing   ·   MMB drag   ·   Wheel zoom   ·   Esc cancel", new(28, 48), 12, Muted);
        var args = OS.GetCmdlineUserArgs();
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--scene-smoke" && i + 1 < args.Length) _capturePath = args[++i];
        RefreshLayout();
    }

    private static StyleBoxFlat Box(string background, string border) => new()
    {
        BgColor = new Color(background), BorderColor = new Color(border),
        BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
        ContentMarginLeft = 11, ContentMarginRight = 11, ContentMarginTop = 6, ContentMarginBottom = 6
    };
    private static Label TextLine(Node parent, string text, int size, Color color)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color); parent.AddChild(label); return label;
    }
    private static Label LabelAt(Node parent, string text, Vector2 position, int size, Color color)
    { var l = TextLine(parent, text, size, color); l.Position = position; return l; }
    private static void AddRule(Node parent) => parent.AddChild(new HSeparator());
    private static void Button(Node parent, string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.None, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        button.Pressed += action; parent.AddChild(button);
    }
    private static CheckButton Toggle(Node parent, string text, bool initial, Action<bool> action)
    {
        var button = new CheckButton { Text = text, ButtonPressed = initial, FocusMode = Control.FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 12); button.Toggled += value => action(value); parent.AddChild(button); return button;
    }

    private void RefreshLayout()
    {
        var size = GetViewportRect().Size;
        _field.Size = new(Math.Max(500, size.X - 340), Math.Max(350, size.Y - 235));
        _sidebar.Position = new(size.X - 304, 103);
        _footer.Position = new(0, size.Y - 88);
        _clock.Position = new(size.X - 440, 27);
        _world.Scale = Vector2.One * _zoom;
        _world.Position = _field.Size / 2 - new Vector2(368, 30) * _zoom + _pan;
    }

    public override void _Process(double delta)
    {
        _accumulator += delta * _simulation.Speed;
        while (_accumulator >= SpatialSimulation.TickMs / 1000.0)
        { _simulation.Tick(); _accumulator -= SpatialSimulation.TickMs / 1000.0; }
        RefreshLayout();
        var local = _world.GetLocalMousePosition();
        var point = ReferenceProjection.Unproject(new(local.X, local.Y));
        Hex? hover = _field.GetGlobalRect().HasPoint(GetGlobalMousePosition()) ? Hex.FromWorld(point) : null;
        _world.Hover = hover;
        if (_previousHover != hover)
        {
            _world.Preview = hover is { } h ? _simulation.Map.FindPath(_simulation.Player.Cell, h, _simulation.Actors.Where(a => a != _simulation.Player).Select(a => a.Cell).ToHashSet()) : [];
            _previousHover = hover;
        }
        var worldTime = TimeSpan.FromHours(8) + TimeSpan.FromMilliseconds(_simulation.WorldMs);
        _clock.Text = $"WORLD  {worldTime:hh\\:mm\\:ss}     {_simulation.Speed}×";
        _mode.Text = _simulation.ManualPause ? "MANUAL PAUSE · all areas" : _simulation.LocalHold ? "TACTICAL WAIT FIXTURE · local held" : "REAL TIME · local active";
        _noticeSeconds = Math.Max(0, _noticeSeconds - delta);
        _status.Text = _noticeSeconds > 0 ? _notice : _simulation.LastMessage;
        _toggles[Key.C].SetPressedNoSignal(_world.CollisionOverlay);
        _toggles[Key.G].SetPressedNoSignal(_world.HexGrid);
        _toggles[Key.B].SetPressedNoSignal(_world.FloorGrid);
        _toggles[Key.L].SetPressedNoSignal(_world.SightOverlay);
        _toggles[Key.H].SetPressedNoSignal(_world.Cutaway);
        _coordinates.Text = hover is { } cell ? $"HEX {cell.Q}, {cell.R}     FLOOR {Math.Floor(point.X)}, {Math.Floor(point.Y)}     {(_simulation.Map.Walkable(cell) ? "TRAVERSABLE CELL" : "BLOCKED")}     ZOOM {_zoom:0.00}×" : "FLOOR / HEX / SPRITE anchors remain separate";
        _timer.Text = $"LOCAL TEST TIMER     {_simulation.LocalCountdownMs / 1000.0:0.00}s";
        _counters.Text = $"World elapsed   {_simulation.WorldMs / 1000.0:0.0}s\nLocal progress   {_simulation.LocalMs / 1000.0:0.0}s\nOff-site updates   {_simulation.OffsiteUpdates}";
        _world.QueueRedraw();
        if (_capturePath is not null) SmokeStep();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        var inField = _field.GetGlobalRect().HasPoint(GetGlobalMousePosition());
        if (e is InputEventMouseMotion motion && inField && Input.IsMouseButtonPressed(MouseButton.Middle)) _pan += motion.Relative;
        if (e is InputEventMouseButton { Pressed: true } mouse && _field.GetGlobalRect().HasPoint(mouse.Position))
        {
            var local = _world.GetGlobalTransformWithCanvas().AffineInverse() * mouse.Position;
            var target = ReferenceProjection.Unproject(new(local.X, local.Y));
            if (mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                var before = local;
                _zoom = Math.Clamp(_zoom * (mouse.ButtonIndex == MouseButton.WheelUp ? 1.125f : 1 / 1.125f), 0.65f, 2.5f);
                RefreshLayout();
                var after = _world.GetGlobalTransformWithCanvas().AffineInverse() * mouse.Position;
                _pan += (before - after) * -_zoom;
            }
            else if (mouse.ButtonIndex == MouseButton.Right || mouse.ButtonIndex == MouseButton.Left && mouse.CtrlPressed) _simulation.Face(target);
            else if (mouse.ButtonIndex == MouseButton.Left) _simulation.RequestMove(Hex.FromWorld(target), mouse.ShiftPressed);
        }
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.C: _world.CollisionOverlay = !_world.CollisionOverlay; break;
            case Key.G: _world.HexGrid = !_world.HexGrid; break;
            case Key.B: _world.FloorGrid = !_world.FloorGrid; break;
            case Key.L: _world.SightOverlay = !_world.SightOverlay; break;
            case Key.H: _world.Cutaway = !_world.Cutaway; break;
            case Key.E: _simulation.ToggleNearestDoor(); break;
            case Key.F6: _simulation.LocalHold = !_simulation.LocalHold; break;
            case Key.Space: _simulation.ManualPause = !_simulation.ManualPause; break;
            case Key.Key1: _simulation.Speed = 1; break;
            case Key.Key2: _simulation.Speed = 2; break;
            case Key.Key3: _simulation.Speed = 4; break;
            case Key.F5: Save(); break;
            case Key.F9: Restore(); break;
            case Key.Escape: _simulation.CancelMove(); break;
            case Key.Home: _pan = Vector2.Zero; _zoom = 1.25f; break;
        }
    }

    private string SavePath => ProjectSettings.GlobalizePath("user://spatial-state.json");
    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            System.IO.File.WriteAllText(SavePath + ".tmp", _simulation.Save());
            System.IO.File.Move(SavePath + ".tmp", SavePath, true);
            GD.Print("Prototype state saved: " + SavePath);
            _notice = "Prototype saved. F9 restores movement, geometry and local/world progress."; _noticeSeconds = 4;
        }
        catch (IOException ex) { GD.PushError("Save failed: " + ex.Message); }
    }
    private void Restore()
    {
        try { _simulation.Restore(System.IO.File.ReadAllText(SavePath)); _accumulator = 0; _previousHover = null; }
        catch (Exception ex) when (ex is IOException or ArgumentException or System.Text.Json.JsonException) { GD.PushError("Restore failed: " + ex.Message); }
    }

    private async void SmokeStep()
    {
        if (++_smokeFrame != 12) return;
        try
        {
            if (_smokeStage == 0)
            {
                var destination = new Hex(12, 3);
                var target = _world.GetGlobalTransform() * WorldView.Screen(destination.World);
                Input.ParseInputEvent(new InputEventMouseButton { Position = target, ButtonIndex = MouseButton.Left, Pressed = true });
                Input.ParseInputEvent(new InputEventMouseButton { Position = target, ButtonIndex = MouseButton.Left, Pressed = false });
                _smokeStage = 1; _smokeFrame = 0; return;
            }
            if (_smokeStage == 1)
            {
                if (!_simulation.Player.Moving || _simulation.Player.Route[^1] != new Hex(12, 3))
                    throw new InvalidOperationException("Mouse picking did not commit the intended hex route.");
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.F6, Pressed = true });
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.F6, Pressed = false });
                _smokeSave = _simulation.Save();
                _smokeStage = 2; _smokeFrame = 0; return;
            }
            if (_smokeStage == 2)
            {
                if (!_simulation.LocalHold) throw new InvalidOperationException("F6 did not hold local simulation.");
                var restored = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"));
                restored.Restore(_smokeSave!);
                if (restored.LocalMs != _simulation.LocalMs || restored.Player.Position != _simulation.Player.Position || restored.WorldMs >= _simulation.WorldMs)
                    throw new InvalidOperationException("Held local progress or continuing world clock diverged.");
                _world.HexGrid = true;
                _smokeStage = 3; _smokeFrame = 0; return;
            }
            if (_smokeStage == 3)
            {
                _smokeStage = 4;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var error = GetViewport().GetTexture().GetImage().SavePng(_capturePath!);
                if (error != Error.Ok) throw new IOException("Screenshot failed: " + error);
                GD.Print("SCENE_SMOKE_PASS: click movement, F6 local hold, continuing world, state restore, screenshot.");
                GetTree().Quit();
            }
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
