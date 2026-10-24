using Godot;
using SiteSimulation;
using System.IO;
using System.Text.Json.Nodes;

namespace SitePresentation;

public partial class SpatialScene
{
    private long _combatHeldLocal, _combatHeldWorld;
    private int _combatWait;
    private async void CombatSmokeStep()
    {
        if (++_smokeFrame != 12) return;
        try
        {
            switch (_smokeStage)
            {
                case 0:
                    foreach (var actor in _simulation.Actors)
                    { actor.Route.Clear(); actor.SegmentMs = 0; actor.StoppedStep = null; actor.MoveGoal = null; actor.BumpedCells.Clear(); }
                    _simulation.Player.Cell = new(8, 3); _simulation.Player.Facing = 1;
                    var opponent = _simulation.Actors.Single(a => a.Id == "tech"); opponent.Cell = new(11, 3); opponent.Facing = 4;
                    _simulation.Actors.Single(a => a.Id == "observer").Cell = new(5, 5);
                    _simulation.Awareness.StopOnDirectSight = _simulation.Awareness.StopOnReports = false;
                    _audio.Muted = true; _audio.Reset();
                    if (!_simulation.ArmTraining()) throw new InvalidOperationException("Could not arm drill.");
                    // A saved fixture starts the player input check before the opponent's turn; Chen has already left the drill.
                    var setup = JsonNode.Parse(_simulation.Save())!;
                    var roster = setup["Combat"]!["Participants"]!.AsArray();
                    for (var i = 0; i < roster.Count; i++)
                    {
                        var id = roster[i]!["ActorId"]!.GetValue<string>();
                        roster[i]!["Ap"] = id == _simulation.Player.Id ? 10 : 0;
                        if (id == _simulation.Player.Id) setup["Combat"]!["Cursor"] = i;
                        if (id == "observer") roster[i]!["Points"] = 0;
                    }
                    _simulation.Restore(setup.ToJsonString());
                    break;
                case 1:
                    if (_simulation.Combat.CurrentActorId != _simulation.Player.Id || _simulation.Combat.Pending is not null)
                    {
                        if (++_combatWait > 400) throw new InvalidOperationException("Training AI did not return player input.");
                        _smokeFrame = 0; return;
                    }
                    _combatHeldLocal = _simulation.LocalMs; _combatHeldWorld = _simulation.WorldMs;
                    break;
                case 2:
                    if (_simulation.LocalMs != _combatHeldLocal || _simulation.WorldMs <= _combatHeldWorld)
                        throw new InvalidOperationException("Player input did not hold only local time.");
                    SmokeKey(Key.V);
                    break;
                case 3:
                    if (_simulation.Combat.Pending is not null) { _smokeFrame = 0; return; }
                    if (!_simulation.Combat.For(_simulation.Player.Id)!.WeaponReady) throw new InvalidOperationException("V failed to ready training sidearm.");
                    var victim = _simulation.Actors.Single(a => a.Id == "tech");
                    if (!_simulation.VisibleToPlayer(victim.Position)) throw new InvalidOperationException("Training target not visible.");
                    RefreshCamera();
                    var click = _world.GetGlobalTransform() * (WorldView.Screen(victim.Position) + new Vector2(0, -34));
                    Input.ParseInputEvent(new InputEventMouseButton { Position = click, ButtonIndex = MouseButton.Left, Pressed = true });
                    Input.ParseInputEvent(new InputEventMouseButton { Position = click, ButtonIndex = MouseButton.Left, Pressed = false });
                    break;
                case 4:
                    if (_simulation.Combat.Pending is not null) { _smokeFrame = 0; return; }
                    if (!_simulation.Events.Entries.Any(e => e.Kind == MessageKind.Combat && e.Text.StartsWith("You fire"))
                        || _simulation.Combat.For(_simulation.Player.Id)!.Ap != 4 || _simulation.Combat.For(_simulation.Player.Id)!.Ammo != 5)
                        throw new InvalidOperationException("Click attack did not produce AP/ammunition/result feedback.");
                    var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"));
                    clone.Restore(_simulation.Save());
                    if (clone.Save() != _simulation.Save()) throw new InvalidOperationException("Tactical rendered save did not round-trip.");
                    _hud.Refresh(_simulation, _world, null, new(0, 0), _zoom, "TACTICAL TRAINING · input waits locally; world calendar continues");
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    using (var texture = GetViewport().GetTexture())
                    using (var image = texture.GetImage())
                        if (image.SavePng(_capturePath!) != Error.Ok) throw new IOException("Combat screenshot failed.");
                    SmokeKey(Key.Enter); _combatWait = 0;
                    break;
                case 5:
                    if (!_simulation.Combat.Active) throw new InvalidOperationException("Drill unexpectedly ended before the return turn.");
                    if (_simulation.Combat.CurrentActorId != _simulation.Player.Id || _simulation.Combat.Pending is not null)
                    {
                        if (++_combatWait > 400) throw new InvalidOperationException("Autonomous round did not return input.");
                        _smokeFrame = 0; return;
                    }
                    if (!_simulation.Events.Entries.Any(e => e.Kind == MessageKind.Combat && e.Text.Contains("fires")))
                        throw new InvalidOperationException("Autonomous attack missing from acquired log.");
                    GD.Print("COMBAT_SMOKE_PASS: visible contact, initiative, local input hold with world progress, V ready, actual click shot, AP/ammo/result journal, save round-trip, Enter and autonomous turns.");
                    GetTree().Quit(); return;
            }
            _smokeStage++; _smokeFrame = 0;
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
