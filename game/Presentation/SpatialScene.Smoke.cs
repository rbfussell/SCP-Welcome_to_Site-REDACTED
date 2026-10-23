using Godot;
using SiteSimulation;
using System.IO;

namespace SitePresentation;

// Automated presentation checks are kept out of normal input and world rules.
public partial class SpatialScene
{
    private static void SmokeKey(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = false });
    }
    private async void SmokeStep()
    {
        if (++_smokeFrame != 12) return;
        try
        {
            switch (_smokeStage)
            {
                case 0:
                    if (!_simulation.InspectAnomalies().Single().Observation.Observed || _simulation.InspectAnomalies().Single().Relocations != 0)
                        throw new InvalidOperationException("Initial human observation failed to hold SCP-650.");
                    var target = _world.GetGlobalTransform() * WorldView.Screen(new Hex(12, 3).World);
                    Input.ParseInputEvent(new InputEventMouseButton { Position = target, ButtonIndex = MouseButton.Left, Pressed = true });
                    Input.ParseInputEvent(new InputEventMouseButton { Position = target, ButtonIndex = MouseButton.Left, Pressed = false });
                    break;
                case 1:
                    if (!_simulation.Player.Moving || _simulation.Player.Route[^1] != new Hex(12, 3))
                        throw new InvalidOperationException("Mouse picking did not commit the intended hex route.");
                    SmokeKey(Key.O);
                    break;
                case 2:
                    if (_simulation.InspectAnomalies().Single().Relocations != 1)
                        throw new InvalidOperationException("Observer attention input did not release SCP-650.");
                    SmokeKey(Key.F6);
                    break;
                case 3:
                    if (!_simulation.LocalHold) throw new InvalidOperationException("F6 did not hold local simulation.");
                    _smokeSave = _simulation.Save();
                    break;
                case 4:
                    var restored = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"));
                    restored.Restore(_smokeSave!);
                    if (restored.InspectAnomalies().Single().Relocations != _simulation.InspectAnomalies().Single().Relocations
                        || restored.Anomalies.Bodies.Single().Cell != _simulation.Anomalies.Bodies.Single().Cell
                        || restored.Anomalies.Bodies.Single().Pose != _simulation.Anomalies.Bodies.Single().Pose
                        || restored.LocalMs != _simulation.LocalMs || restored.Player.Position != _simulation.Player.Position || restored.WorldMs >= _simulation.WorldMs)
                        throw new InvalidOperationException("Held module/local progress or continuing world clock diverged.");
                    _world.HexGrid = true;
                    break;
                case 5:
                    // Place the avatar for a reproducible look-away/discovery input fixture.
                    // This is test setup only; the module still owns the statue's actual destination.
                    SmokeKey(Key.F6);
                    var statue = _simulation.Anomalies.Bodies.Single();
                    var neighbor = Enumerable.Range(0, 6).Select(statue.Cell.Neighbor).Where(c => _simulation.Map.Walkable(c)
                        && !_simulation.Actors.Any(a => a.Cell == c) && _simulation.Map.ClearSight(c.World, statue.Cell.World))
                        .OrderByDescending(c => _simulation.Actors.Where(a => a.Id != _simulation.Player.Id).Min(a => a.Cell.Distance(c))).First();
                    _simulation.Player.Route.Clear(); _simulation.Player.SegmentMs = 0; _simulation.Player.Cell = neighbor;
                    _simulation.Face(_simulation.Player.Position + (_simulation.Player.Position - statue.Cell.World));
                    _feedback.Reset(_simulation); _audio.Reset();
                    if (FieldPresentation.ShowAnomaly(_simulation, statue, false))
                        throw new InvalidOperationException("Field view exposed a statue behind the avatar.");
                    var sightTarget = _world.GetGlobalTransform() * WorldView.Screen(statue.Cell.World);
                    Input.ParseInputEvent(new InputEventMouseButton { Position = sightTarget, ButtonIndex = MouseButton.Right, Pressed = true });
                    Input.ParseInputEvent(new InputEventMouseButton { Position = sightTarget, ButtonIndex = MouseButton.Right, Pressed = false });
                    break;
                case 6:
                    if (!_world.FieldView || _feedback.Discoveries != 1 || _audio.PlayedDiscoveries != 1
                        || !FieldPresentation.ShowAnomaly(_simulation, _simulation.Anomalies.Bodies.Single(), false))
                        throw new InvalidOperationException("Actual avatar discovery did not reveal the statue with one presentation cue.");
                    _smokeSave = _simulation.Save();
                    SmokeKey(Key.J);
                    SmokeKey(Key.F2); SmokeKey(Key.F2); SmokeKey(Key.M); SmokeKey(Key.R);
                    break;
                case 7:
                    var beforeFlag = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"));
                    beforeFlag.Restore(_smokeSave!);
                    if (!_simulation.InspectAnomalies().Single().FlaggedActors("complacency").Contains(_simulation.Player.Id)
                        || _simulation.Anomalies.Bodies.Single().Pose != beforeFlag.Anomalies.Bodies.Single().Pose
                        || _simulation.Anomalies.Bodies.Single().Cell != beforeFlag.Anomalies.Bodies.Single().Cell)
                        throw new InvalidOperationException("Complacency input must change only module policy while observed.");
                    if (!_world.FieldView || !_audio.Muted || !_encounter.ReducedEffects)
                        throw new InvalidOperationException("Presentation/accessibility inputs failed.");
                    break;
                case 8:
                    _smokeStage = 9;
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    var error = GetViewport().GetTexture().GetImage().SavePng(_capturePath!);
                    if (error != Error.Ok) throw new IOException("Screenshot failed: " + error);
                    _world.FieldView = false; ResetCamera(); RefreshCamera();
                    _hud.Refresh(_simulation, _world, null, new(0, 0), _zoom, "DIAGNOSTIC CAPTURE");
                    _encounter.Visible = false; _world.QueueRedraw();
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    var diagnosticPath = Path.Combine(Path.GetDirectoryName(_capturePath!)!, Path.GetFileNameWithoutExtension(_capturePath) + "-diagnostics.png");
                    if (GetViewport().GetTexture().GetImage().SavePng(diagnosticPath) != Error.Ok) throw new IOException("Diagnostic screenshot failed.");
                    GD.Print("SCENE_SMOKE_PASS: click/move, observation/release, held world/save, actual RMB discovery, one cue, J module flag, F2 diagnostics, mute/reduced effects, field/diagnostic screenshots.");
                    GetTree().Quit();
                    return;
            }
            _smokeStage++; _smokeFrame = 0;
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
