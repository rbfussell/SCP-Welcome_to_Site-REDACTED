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
                    _smokeStage = 6;
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    var error = GetViewport().GetTexture().GetImage().SavePng(_capturePath!);
                    if (error != Error.Ok) throw new IOException("Screenshot failed: " + error);
                    GD.Print("SCENE_SMOKE_PASS: exact click, human observation, O release/relocation, F6 hold, continuing world, module restore, screenshot.");
                    GetTree().Quit();
                    return;
            }
            _smokeStage++; _smokeFrame = 0;
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
