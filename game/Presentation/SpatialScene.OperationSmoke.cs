using Godot;
using SiteSimulation;
using System.IO;

namespace SitePresentation;

public partial class SpatialScene
{
    private int _operationWait;
    private string? _operationInitialSave;
    private async void OperationSmokeStep()
    {
        if (++_smokeFrame != 12) return;
        try
        {
            switch (_smokeStage)
            {
                case 0:
                    _operationInitialSave = _simulation.Save();
                    if (_simulation.Operations.Definition is null || _simulation.InspectAnomalies().Single().Observation.EligibleIds.Length != 3)
                        throw new InvalidOperationException("Operation must start with actual three-person observation.");
                    SmokeKey(Key.E); // Real door interaction, not a map mutation.
                    break;
                case 1:
                    _simulation.ApproachTerminal();
                    break;
                case 2:
                    if (!_simulation.CanUseTerminal)
                    {
                        if (++_operationWait > 120) throw new InvalidOperationException("Could not physically reach OSCP terminal: " + _simulation.LastMessage);
                        _smokeFrame = 0; return;
                    }
                    SmokeKey(Key.T);
                    break;
                case 3:
                    if (!_hud.Operations.TerminalOpen) throw new InvalidOperationException("T did not open reachable OSCP service.");
                    _smokeStopLocalMs = _simulation.LocalMs;
                    _hud.Operations.SelectedRecipient = 1;
                    _hud.Operations.Send(PersonnelTask.Post);
                    break;
                case 4:
                    if (_simulation.Operations.OrderHistory.Last().Status == OrderStatus.Pending
                        || !_simulation.Operations.ReceivedReports.Any(r => r.Text.Contains("Acknowledged")))
                        throw new InvalidOperationException("OSCP did not receive the staff acknowledgement.");
                    if (_simulation.LocalMs <= _smokeStopLocalMs) throw new InvalidOperationException("Opening OSCP suspended local simulation.");
                    var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"),
                        Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
                    clone.Restore(_simulation.Save());
                    if (clone.Save() != _simulation.Save()) throw new InvalidOperationException("Rendered operation save did not round-trip.");
                    _simulation.ManualPause = true;
                    _hud.Refresh(_simulation, _world, null, new(0, 0), _zoom, "OSCP · received acknowledgement / physical site");
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    using (var texture = GetViewport().GetTexture())
                    using (var image = texture.GetImage())
                        if (image.SavePng(_capturePath!) != Error.Ok) throw new IOException("OSCP screenshot failed.");
                    GD.Print("OPERATION_SMOKE_PASS: initial coverage, E door, physical terminal approach, T access, personnel order acknowledgement and operation save round-trip.");
                    // A separate input fixture keeps Chen watching, with an unseen ordinary occupant in the corridor.
                    _simulation.Restore(_operationInitialSave!);
                    _simulation.Player.Cell = new(5, 6); _simulation.Player.Facing = 4;
                    var hidden = _simulation.Actors.Single(a => a.Id == "tech");
                    hidden.Cell = new(12, 3);
                    if (!_simulation.IssueOrder(hidden.Id, PersonnelTask.Post, hidden.Cell))
                        throw new InvalidOperationException("Navigation fixture could not assign a stationary corridor post.");
                    _feedback.Reset(_simulation); _audio.Reset(); ResetCamera(); RefreshCamera();
                    var click = _world.GetGlobalTransform() * WorldView.Screen(hidden.Cell.World);
                    Input.ParseInputEvent(new InputEventMouseButton { Position = click, ButtonIndex = MouseButton.Left, ShiftPressed = true, Pressed = true });
                    Input.ParseInputEvent(new InputEventMouseButton { Position = click, ButtonIndex = MouseButton.Left, ShiftPressed = true, Pressed = false });
                    _operationWait = 0;
                    break;
                case 5:
                    if (!_simulation.Player.Moving || _simulation.Player.MoveGoal != new Hex(12, 3)
                        || _simulation.VisibleToPlayer(_simulation.Actors.Single(a => a.Id == "tech").Position))
                        throw new InvalidOperationException("Click on an unseen occupied destination did not commit ordinary travel.");
                    break;
                case 6:
                    if (_simulation.Player.Moving)
                    {
                        if (++_operationWait > 120) throw new InvalidOperationException("Occupied-goal travel never stopped.");
                        _smokeFrame = 0; return;
                    }
                    if (_simulation.Player.Cell == new Hex(5, 6) || _simulation.Player.Cell.Distance(new(12, 3)) != 1
                        || _simulation.LastMessage != "Movement stopped: the way ahead is blocked.")
                        throw new InvalidOperationException("Occupied-goal click must approach and stop before collision.");
                    _simulation.ManualPause = true;
                    _audio.Muted = true; _audio.Reset();
                    _hud.Refresh(_simulation, _world, null, new(0, 0), _zoom, _simulation.LastMessage);
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    var navigationPath = Path.Combine(Path.GetDirectoryName(_capturePath!)!, Path.GetFileNameWithoutExtension(_capturePath) + "-navigation.png");
                    using (var texture = GetViewport().GetTexture())
                    using (var image = texture.GetImage())
                        if (image.SavePng(navigationPath) != Error.Ok) throw new IOException("Navigation screenshot failed.");
                    GD.Print("NAVIGATION_SMOKE_PASS: actual Shift-click on unseen occupied hex committed travel, approached the blocker and stopped before collision.");
                    break;
                case 7:
                    // Leave the capture continuation before tearing down Godot's native resources.
                    GetTree().Quit(); return;
            }
            _smokeStage++; _smokeFrame = 0;
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
