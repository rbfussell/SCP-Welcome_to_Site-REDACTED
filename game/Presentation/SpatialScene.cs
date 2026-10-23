using Godot;
using SiteSimulation;
using System.IO;

namespace SitePresentation;

/// <summary>Godot scheduling, input and transport adapter; gameplay rules stay in the C# systems.</summary>
public partial class SpatialScene : Node2D
{
    private SpatialSimulation _simulation = null!;
    private WorldView _world = null!;
    private SpatialHud _hud = null!;
    private double _accumulator;
    private float _zoom = 1.25f;
    private Vector2 _pan;
    private Hex? _previousHover;
    private string? _capturePath;
    private int _smokeFrame, _smokeStage;
    private string? _smokeSave;
    private string? _notice;
    private double _noticeSeconds;

    public override void _Ready()
    {
        _simulation = new(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"));
        var layer = new CanvasLayer(); AddChild(layer);
        _world = new WorldView { Simulation = _simulation };
        _hud = new SpatialHud(); layer.AddChild(_hud); _hud.Initialize(_simulation, _world);
        _hud.SaveRequested = Save; _hud.RestoreRequested = Restore;
        _hud.ResetCameraRequested = () => { _pan = Vector2.Zero; _zoom = 1.25f; };
        _hud.ExportTraceRequested = ExportTrace;
        var args = OS.GetCmdlineUserArgs();
        for (var i = 0; i < args.Length; i++) if (args[i] == "--scene-smoke" && i + 1 < args.Length) _capturePath = args[++i];
        RefreshCamera();
    }
    private void RefreshCamera()
    {
        _hud.Layout();
        _world.Scale = Vector2.One * _zoom;
        _world.Position = _hud.Field.Size / 2 - new Vector2(368, 30) * _zoom + _pan;
    }
    public override void _Process(double delta)
    {
        _accumulator += delta * _simulation.Speed;
        while (_accumulator >= SpatialSimulation.TickMs / 1000.0)
        { _simulation.Tick(); _accumulator -= SpatialSimulation.TickMs / 1000.0; }
        RefreshCamera();
        var local = _world.GetLocalMousePosition();
        var ground = ReferenceProjection.Unproject(new(local.X, local.Y));
        Hex? hover = _hud.Field.GetGlobalRect().HasPoint(GetGlobalMousePosition()) ? Hex.FromWorld(ground) : null;
        _world.Hover = hover;
        if (_previousHover != hover || _simulation.LocalMs % 200 == 0)
        { _world.Preview = hover is { } cell ? _simulation.PreviewPath(cell) : []; _previousHover = hover; }
        _noticeSeconds = Math.Max(0, _noticeSeconds - delta);
        _hud.Refresh(_simulation, _world, hover, ground, _zoom, _noticeSeconds > 0 ? _notice : null);
        _world.QueueRedraw();
        if (_capturePath is not null) SmokeStep();
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion && _hud.Field.GetGlobalRect().HasPoint(motion.Position)
            && Input.IsMouseButtonPressed(MouseButton.Middle)) _pan += motion.Relative;
        if (e is InputEventMouseButton { Pressed: true } mouse && _hud.Field.GetGlobalRect().HasPoint(mouse.Position))
        {
            var local = _world.GetGlobalTransformWithCanvas().AffineInverse() * mouse.Position;
            var target = ReferenceProjection.Unproject(new(local.X, local.Y));
            if (mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                _zoom = Math.Clamp(_zoom * (mouse.ButtonIndex == MouseButton.WheelUp ? 1.125f : 1 / 1.125f), 0.65f, 2.5f);
                RefreshCamera();
                var after = _world.GetGlobalTransformWithCanvas().AffineInverse() * mouse.Position;
                _pan += (after - local) * _zoom;
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
            case Key.O: _simulation.ToggleObserverAttention(); break;
            case Key.K: _simulation.BlinkPlayer(); break;
            case Key.F8: _simulation.ToggleLighting(); break;
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
    private void WriteUserFile(string filename, string json)
    {
        var path = ProjectSettings.GlobalizePath("user://" + filename);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path + ".tmp", json); System.IO.File.Move(path + ".tmp", path, true);
        _notice = "Written: " + filename; _noticeSeconds = 4;
        GD.Print("Written: " + path);
    }
    private void Save()
    {
        try { WriteUserFile("spatial-state.json", _simulation.Save()); }
        catch (IOException ex) { GD.PushError("Save failed: " + ex.Message); }
    }
    private void Restore()
    {
        try
        {
            _simulation.Restore(System.IO.File.ReadAllText(ProjectSettings.GlobalizePath("user://spatial-state.json")));
            _accumulator = 0; _previousHover = null; _notice = null;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or System.Text.Json.JsonException)
        { _notice = "Restore rejected: " + ex.Message; _noticeSeconds = 5; GD.PushWarning(_notice); }
    }
    private void ExportTrace()
    {
        try { WriteUserFile("observation-trace.json", System.Text.Json.JsonSerializer.Serialize(_simulation.Trace.Capture(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true })); }
        catch (IOException ex) { GD.PushError("Trace export failed: " + ex.Message); }
    }
}
