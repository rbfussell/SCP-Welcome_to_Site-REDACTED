using Godot;
using SiteSimulation;
using System.IO;

namespace SitePresentation;

public partial class SpatialScene
{
    private int _operationWait;
    private async void OperationSmokeStep()
    {
        if (++_smokeFrame != 12) return;
        try
        {
            switch (_smokeStage)
            {
                case 0:
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
                    GetTree().Quit(); return;
            }
            _smokeStage++; _smokeFrame = 0;
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
