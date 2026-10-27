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
    private EncounterAudio _audio = null!;
    private EncounterOverlay _encounter = null!;
    private readonly DiscoveryFeedback _feedback = new();
    private SimulationRuntime _runtime = null!;
    private float _zoom = 1.75f;
    private Vector2 _pan;
    private Hex? _previousHover;
    private string? _capturePath;
    private int _smokeFrame, _smokeStage;
    private string? _smokeSave;
    private string? _notice;
    private double _noticeSeconds;
    private bool _operationSmoke, _combatSmoke, _inventorySmoke, _inventorySmokeRunning, _inventorySmokeQuit;

    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        _operationSmoke = args.Contains("--operation-smoke");
        _inventorySmoke = args.Contains("--inventory-smoke");
        _combatSmoke = args.Contains("--combat-smoke");
        var legacySmoke = args.Contains("--scene-smoke") && !_operationSmoke && !_inventorySmoke;
        _simulation = new(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"),
            legacySmoke ? null : Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        _runtime = new(_simulation);
        var layer = new CanvasLayer(); AddChild(layer);
        _world = new WorldView { Simulation = _simulation };
        _hud = new SpatialHud(); layer.AddChild(_hud); _hud.Initialize(_simulation, _world);
        _audio = new EncounterAudio(); AddChild(_audio);
        _encounter = new EncounterOverlay(); _hud.Field.AddChild(_encounter);
        _feedback.Reset(_simulation);
        _hud.SaveRequested = Save; _hud.RestoreRequested = Restore;
        _hud.ResetCameraRequested = ResetCamera;
        _hud.ExportTraceRequested = ExportTrace;
        for (var i = 0; i < args.Length; i++) if (args[i] == "--scene-smoke" && i + 1 < args.Length) _capturePath = args[++i];
        RefreshCamera();
    }
    private void RefreshCamera()
    {
        _hud.Layout();
        _world.Scale = Vector2.One * _zoom;
        var focus = _world.FieldView ? WorldView.Screen(_simulation.Player.Position) : new Vector2(368, 30);
        _world.Position = _hud.Field.Size / 2 - focus * _zoom + _pan;
        _encounter.Size = _hud.Field.Size;
    }
    private void ResetCamera() { _pan = Vector2.Zero; _zoom = _world.FieldView ? 1.75f : 1.25f; }
    public override void _Process(double delta)
    {
        if (_inventorySmokeQuit)
        { GC.Collect(); GC.WaitForPendingFinalizers(); GetTree().Quit(); return; }
        _runtime.Advance(delta);
        RefreshCamera();
        UpdateEncounter();
        var local = _world.GetLocalMousePosition();
        var ground = ReferenceProjection.Unproject(new(local.X, local.Y));
        Hex? hover = _hud.Field.GetGlobalRect().HasPoint(GetGlobalMousePosition()) ? Hex.FromWorld(ground) : null;
        _world.Hover = hover;
        if (_previousHover != hover || _simulation.LocalMs % 200 == 0)
        { _world.Preview = !_hud.Operations.ChoosingPost && hover is { } cell ? _simulation.PreviewPath(cell) : []; _previousHover = hover; }
        _noticeSeconds = Math.Max(0, _noticeSeconds - delta);
        var audioState = $"{(_audio.Muted ? "MUTED" : "AUDIO ON")} · {(_encounter.ReducedEffects ? "REDUCED EFFECTS" : "SUBTLE EFFECTS")}";
        _hud.Refresh(_simulation, _world, hover, ground, _zoom,
            _noticeSeconds > 0 ? _notice : _world.FieldView ? audioState + " · " + _simulation.LastMessage : null);
        _world.QueueRedraw();
        if (_capturePath is not null) { if (_inventorySmoke) { if (!_inventorySmokeRunning && ++_smokeFrame == 12) { _inventorySmokeRunning = true; RunInventorySmoke(); } } else if (_combatSmoke) CombatSmokeStep(); else if (_operationSmoke) OperationSmokeStep(); else SmokeStep(); }
    }
    private void UpdateEncounter()
    {
        var discovered = _feedback.Update(_simulation, _world.FieldView);
        _audio.Update(_world.FieldView, _simulation.LocalProgressHeld, discovered);
        _encounter.Visible = _world.FieldView;
        _encounter.Pulse = _feedback.Pulse(_simulation.LocalMs);
        _encounter.Caption = _feedback.Caption(_simulation.LocalMs);
        _encounter.PlayerPoint = _world.GetTransform() * WorldView.Screen(_simulation.Player.Position);
        _encounter.Bearings = _simulation.Awareness.Evidence
            .Where(e => _simulation.LocalMs - e.LocalMs <= 15000 && !_simulation.VisibleToPlayer(e.Position)
                && Point2.Distance(e.Position, _simulation.Player.Position) > Math.Max(0.05, e.UncertaintyRadius))
            .OrderByDescending(e => e.LocalMs).Take(4)
            .Select(e => new ContactBearing(WorldView.Screen(e.Position) - WorldView.Screen(_simulation.Player.Position),
                $"{(e.Source == "sound" ? "sound ~" : e.Source is "voice" or "radio" ? "reported" : "last seen")} {(_simulation.LocalMs - e.LocalMs) / 1000.0:0.0}s",
                e.Classification == "hostile"))
            .Concat(_simulation.Combat.Active ? _simulation.Combat.AcquiredContacts
                .Where(c => c.ObserverId == _simulation.Player.Id && !_simulation.VisibleToPlayer(c.Position)
                    && _simulation.LocalMs - c.LocalMs <= 15000 && Point2.Distance(c.Position, _simulation.Player.Position) > 0.05)
                .Select(c => new ContactBearing(WorldView.Screen(c.Position) - WorldView.Screen(_simulation.Player.Position),
                    $"training last seen {(_simulation.LocalMs - c.LocalMs) / 1000.0:0.0}s", true)) : [])
            .Take(4).ToArray();
        _encounter.QueueRedraw();
    }
    public override void _Input(InputEvent e)
    {
        if (_hud.Inventory.IsOpen && e is InputEventKey { Pressed: true, Keycode: Key.Escape })
        { _hud.Inventory.HandleInput(e); return; }
        if (_hud.Workspace.IsOpen && e is InputEventKey { Pressed: true, Keycode: Key.Escape })
        { _hud.Workspace.Close(); GetViewport().SetInputAsHandled(); }
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if (_hud.Inventory.IsOpen) { _hud.Inventory.HandleInput(e); return; }
        if (_hud.Workspace.IsOpen) { _hud.Workspace.HandleInput(e); return; }
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
            else if (_hud.Operations.ChoosingPost && mouse.ButtonIndex == MouseButton.Left) _hud.Operations.PlacePost(Hex.FromWorld(target));
            else if (_hud.Operations.ChoosingPost && mouse.ButtonIndex == MouseButton.Right) _hud.Operations.CancelPost();
            else if (mouse.ButtonIndex == MouseButton.Right || mouse.ButtonIndex == MouseButton.Left && mouse.CtrlPressed) _simulation.Face(target);
            else if (mouse.ButtonIndex == MouseButton.Left)
            {
                var victim = _simulation.Combat.Active && !mouse.ShiftPressed ? _world.PickTrainingOpponent(local) : null;
                if (victim is not null) _simulation.FireTraining(victim.Id);
                else _simulation.RequestMove(Hex.FromWorld(target), mouse.ShiftPressed);
            }
        }
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.I: _hud.Workspace.Close(); _hud.Inventory.Toggle(); break;
            case Key.Enter: _simulation.EndTurn(); break;
            case Key.V: _simulation.UseTrainingEquipment(CombatActionKind.ReadyWeapon); break;
            case Key.Y: _simulation.UseTrainingEquipment(CombatActionKind.Reload); break;
            case Key.F2: _world.FieldView = !_world.FieldView; ResetCamera(); break;
            case Key.F4: _world.DiagnosticSprites = !_world.DiagnosticSprites; break;
            case Key.M: _audio.Muted = !_audio.Muted; break;
            case Key.R: _encounter.ReducedEffects = !_encounter.ReducedEffects; break;
            case Key.J: SpatialHud.ToggleComplacency(_simulation); break;
            case Key.T: _hud.Operations.ToggleTerminal(); break;
            case Key.P: _hud.Workspace.Toggle(OscpDevice.Phone); break;
            case Key.C: _world.CollisionOverlay = !_world.CollisionOverlay; break;
            case Key.G: _world.HexGrid = !_world.HexGrid; break;
            case Key.B: _world.FloorGrid = !_world.FloorGrid; break;
            case Key.L: _world.SightOverlay = !_world.SightOverlay; break;
            case Key.W: _world.ReducedWalls = !_world.ReducedWalls; break;
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
            case Key.Home: ResetCamera(); break;
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
            _feedback.Reset(_simulation); _audio.Reset();
            _runtime.ResetAccumulator(); _previousHover = null; _notice = null;
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
