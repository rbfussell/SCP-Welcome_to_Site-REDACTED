using Godot;
using SiteSimulation;
using System.IO;

namespace SitePresentation;

// Automated presentation checks are kept out of normal input and world rules.
public partial class SpatialScene
{
    private int _smokeShadingBuilds;
    private Point2 _smokeStopPoint;
    private long _smokeStopLocalMs;
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
                    _smokeShadingBuilds = _world.GroundShadingBuilds;
                    break;
                case 4:
                    if (_world.GroundShadingBuilds != _smokeShadingBuilds)
                        throw new InvalidOperationException("Unchanged held sight rebuilt its ground meshes.");
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
                    Input.FlushBufferedEvents();
                    if (_simulation.LocalHold) throw new InvalidOperationException("Fixture must release local hold before authoring movement.");
                    var statue = _simulation.Anomalies.Bodies.Single();
                    var neighbor = Enumerable.Range(0, 6).Select(statue.Cell.Neighbor).Where(c => _simulation.Map.Walkable(c)
                        && !_simulation.Actors.Any(a => a.Cell == c) && _simulation.Map.ClearSight(c.World, statue.Cell.World))
                        .OrderByDescending(c => _simulation.Actors.Where(a => a.Id != _simulation.Player.Id).Min(a => a.Cell.Distance(c))).First();
                    _simulation.Player.Route.Clear(); _simulation.Player.SegmentMs = 0; _simulation.Player.StoppedStep = null; _simulation.Player.Cell = neighbor;
                    if (!_simulation.Awareness.StopOnDirectSight || !_simulation.Awareness.StopOnReports)
                        throw new InvalidOperationException("Both shared sighting settings must default on.");
                    _simulation.Awareness.StopOnReports = false;
                    // A preview may intentionally include unseen occupants. This fixture injects an already
                    // started step, so qualify the physical clearance normal movement checks before starting.
                    var walking = _simulation.Map.Cells.Select(c => _simulation.PreviewPath(c)).First(p => p.Count >= 2
                        && _simulation.Map.CanTraverse(_simulation.Player.Cell, p[0])
                        && !_simulation.Actors.Where(a => a != _simulation.Player).Any(a => a.Cell == p[0]
                            || a.Moving && a.Route[0] == p[0] || a.StoppedStep?.Next == p[0]));
                    _simulation.Face(_simulation.Player.Position + (_simulation.Player.Position - statue.Cell.World));
                    _simulation.Player.Route = walking.ToList(); _simulation.Player.SegmentMs = 100; _simulation.Player.KeepFacing = true;
                    _simulation.Player.MoveGoal = walking[^1]; _simulation.Player.BumpedCells.Clear();
                    _feedback.Reset(_simulation); _audio.Reset();
                    if (FieldPresentation.ShowAnomaly(_simulation, statue, false))
                        throw new InvalidOperationException("Field view exposed a statue behind the avatar.");
                    _smokeStopPoint = _simulation.Player.Position; _smokeStopLocalMs = _simulation.LocalMs;
                    var walkingSave = _simulation.Save();
                    _simulation.Awareness.StopOnDirectSight = false;
                    RefreshCamera();
                    var ordinaryLookTarget = _world.GetGlobalTransform() * WorldView.Screen(
                        _simulation.Player.Position + (_simulation.Player.Position - statue.Cell.World) * 0.25);
                    if (!_hud.Field.GetGlobalRect().HasPoint(ordinaryLookTarget))
                        throw new InvalidOperationException("Ordinary look fixture must click inside the field.");
                    Input.ParseInputEvent(new InputEventMouseButton { Position = ordinaryLookTarget, ButtonIndex = MouseButton.Right, Pressed = true });
                    Input.ParseInputEvent(new InputEventMouseButton { Position = ordinaryLookTarget, ButtonIndex = MouseButton.Right, Pressed = false });
                    Input.FlushBufferedEvents();
                    if (_simulation.Player.Moving || _simulation.Player.Position != _smokeStopPoint
                        || _simulation.Player.MoveGoal is not null || _simulation.Player.StoppedStep?.ProgressMs != 100)
                        throw new InvalidOperationException("Ordinary RMB look must stop exactly even with both anomaly-stop settings disabled.");
                    _simulation.Restore(walkingSave);
                    _feedback.Reset(_simulation); _audio.Reset();
                    var sightTarget = _world.GetGlobalTransform() * WorldView.Screen(statue.Cell.World);
                    Input.ParseInputEvent(new InputEventMouseButton { Position = sightTarget, ButtonIndex = MouseButton.Right, Pressed = true });
                    Input.ParseInputEvent(new InputEventMouseButton { Position = sightTarget, ButtonIndex = MouseButton.Right, Pressed = false });
                    break;
                case 6:
                    if (!_world.FieldView || _feedback.Discoveries != 1 || _audio.PlayedDiscoveries != 1
                        || !FieldPresentation.ShowAnomaly(_simulation, _simulation.Anomalies.Bodies.Single(), false))
                        throw new InvalidOperationException($"Actual avatar discovery did not reveal the statue with one presentation cue: discoveries={_feedback.Discoveries}, audio={_audio.PlayedDiscoveries}, visible={FieldPresentation.ShowAnomaly(_simulation, _simulation.Anomalies.Bodies.Single(), false)}, moving={_simulation.Player.Moving}, facing={_simulation.Player.Facing}, message={_simulation.LastMessage}.");
                    if (_simulation.Player.Moving || _simulation.Player.Route.Count != 0 || _simulation.Player.Position != _smokeStopPoint
                        || _simulation.Player.StoppedStep is null || _simulation.ManualPause || _simulation.LocalHold || _simulation.LocalMs <= _smokeStopLocalMs)
                        throw new InvalidOperationException("RMB look must stop exactly in place, clear the path and keep the simulation running.");
                    _simulation.Awareness.StopOnReports = true;
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
                    _simulation.ManualPause = true;
                    var artBaseline = _simulation.Save();
                    var artShadingBuilds = _world.GroundShadingBuilds;
                    SmokeKey(Key.F4);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    if (!_world.DiagnosticSprites || _simulation.Save() != artBaseline || _world.GroundShadingBuilds <= artShadingBuilds)
                        throw new InvalidOperationException("F4 must switch all art/ground bindings without advancing the held world.");
                    var placeholdersPath = Path.Combine(Path.GetDirectoryName(_capturePath!)!, Path.GetFileNameWithoutExtension(_capturePath) + "-placeholders.png");
                    if (GetViewport().GetTexture().GetImage().SavePng(placeholdersPath) != Error.Ok) throw new IOException("Placeholder screenshot failed.");
                    SmokeKey(Key.F4);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    if (_world.DiagnosticSprites || _simulation.Save() != artBaseline)
                        throw new InvalidOperationException("F4 return to authored art changed simulation state.");
                    _world.FieldView = false; ResetCamera(); RefreshCamera();
                    _hud.Refresh(_simulation, _world, null, new(0, 0), _zoom, "DIAGNOSTIC CAPTURE");
                    _encounter.Visible = false; _world.QueueRedraw();
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    var diagnosticPath = Path.Combine(Path.GetDirectoryName(_capturePath!)!, Path.GetFileNameWithoutExtension(_capturePath) + "-diagnostics.png");
                    if (GetViewport().GetTexture().GetImage().SavePng(diagnosticPath) != Error.Ok) throw new IOException("Diagnostic screenshot failed.");
                    // A wider room makes cross-tile sight shading inspectable without moving the anomaly.
                    _simulation.ManualPause = true;
                    _simulation.Player.Route.Clear(); _simulation.Player.SegmentMs = 0;
                    _simulation.Player.StoppedStep = null;
                    _simulation.Player.Cell = new(10, 8); _simulation.Player.Facing = 1;
                    _feedback.Reset(_simulation); _world.FieldView = true; ResetCamera(); RefreshCamera();
                    _encounter.Caption = ""; _encounter.Visible = true;
                    _hud.Refresh(_simulation, _world, null, new(0, 0), _zoom, "VISION SHADING · PRESENTATION FIXTURE");
                    _world.QueueRedraw();
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    var sightPath = Path.Combine(Path.GetDirectoryName(_capturePath!)!, Path.GetFileNameWithoutExtension(_capturePath) + "-sight.png");
                    if (GetViewport().GetTexture().GetImage().SavePng(sightPath) != Error.Ok) throw new IOException("Sight screenshot failed.");
                    await CheckPlayerCutaway();
                    GD.Print("SCENE_SMOKE_PASS: feathered player cutaway/H toggle/fractional following with unchanged world and sight, click/move, observation/release, held world/save and shading cache, ordinary RMB look stop with anomaly interruption disabled, RMB discovery and exact look stop with running local time, one cue, J module flag, F2 diagnostics, mute/reduced effects, F4 reversible full-art swap with unchanged held state, field/diagnostic/placeholder/sight screenshots.");
                    GetTree().Quit();
                    return;
            }
            _smokeStage++; _smokeFrame = 0;
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
