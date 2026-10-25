using Godot;
using SiteSimulation;
using System.IO;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task CheckPlayerCutaway()
    {
        // Held authoring fixture: stand where a foreground wall covers the avatar's torso.
        _simulation.ManualPause = true;
        var player = _simulation.Player;
        player.Route.Clear(); player.SegmentMs = 0; player.StoppedStep = null; player.MoveGoal = null;
        var cell = _simulation.Map.Cells.Where(c => _simulation.Map.Walkable(c)
            && !_simulation.Actors.Any(a => a != player && a.Cell == c))
            .OrderBy(c => c.Distance(new(10, 10))).First(c => _simulation.Map.SolidWalls.Any(w =>
            {
                var feet = WorldView.Screen(c.World); var start = WorldView.Screen(w.Start); var end = WorldView.Screen(w.End);
                return !w.Door && WorldView.Screen(w.Center).Y >= feet.Y && Geometry2D.IsPointInPolygon(feet - new Vector2(0, 30),
                    [start, end, end - new Vector2(0, 68), start - new Vector2(0, 68)]);
            }));
        player.Cell = cell; player.Facing = 1;
        _world.FieldView = true; _world.Cutaway = true; _world.DiagnosticSprites = false;
        ResetCamera(); RefreshCamera();
        var baseline = _simulation.Save();
        var known = _simulation.Actors.Select(a => FieldPresentation.ShowActor(_simulation, a, false))
            .Concat(_simulation.Anomalies.Bodies.Select(a => FieldPresentation.ShowAnomaly(_simulation, a, false))).ToArray();
        _hud.Refresh(_simulation, _world, null, new(0, 0), _zoom, "SOFT WALL CUTAWAY · HELD PRESENTATION FIXTURE");
        _world.QueueRedraw();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (_world.FeatheredWallFaces == 0 || _simulation.Save() != baseline)
            throw new InvalidOperationException("Foreground-wall fixture must draw a cosmetic opening without changing world state.");
        var center = _world.GetGlobalTransform() * _world.CutawayCenter;
        var radius = _world.CutawayRadius * _zoom;
        using var opened = GetViewport().GetTexture().GetImage();
        var prefix = Path.Combine(Path.GetDirectoryName(_capturePath!)!, Path.GetFileNameWithoutExtension(_capturePath!));
        if (opened.SavePng(prefix + "-cutaway.png") != Error.Ok) throw new IOException("Cutaway capture failed.");
        SmokeKey(Key.H); Input.FlushBufferedEvents(); _world.QueueRedraw();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (_world.Cutaway || _world.FeatheredWallFaces != 0 || _simulation.Save() != baseline)
            throw new InvalidOperationException("H must disable only the visual opening.");
        using var closed = GetViewport().GetTexture().GetImage();
        if (closed.SavePng(prefix + "-cutaway-off.png") != Error.Ok) throw new IOException("Uncut wall capture failed.");
        var changed = 0;
        for (var y = (int)(center.Y - radius); y < center.Y + radius; y += 2)
        for (var x = (int)(center.X - radius); x < center.X + radius; x += 2)
        {
            if (x < 0 || y < 0 || x >= opened.GetWidth() || y >= opened.GetHeight()) continue;
            var a = opened.GetPixel(x, y); var b = closed.GetPixel(x, y);
            if (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) > 0.04f) changed++;
        }
        if (changed < 50) throw new InvalidOperationException("Opening must visibly clear the foreground wall around the avatar.");
        SmokeKey(Key.H); Input.FlushBufferedEvents(); _world.QueueRedraw();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (!_world.Cutaway || _world.FeatheredWallFaces == 0 || _simulation.Save() != baseline
            || !known.SequenceEqual(_simulation.Actors.Select(a => FieldPresentation.ShowActor(_simulation, a, false))
                .Concat(_simulation.Anomalies.Bodies.Select(a => FieldPresentation.ShowAnomaly(_simulation, a, false)))))
            throw new InvalidOperationException("Restoring cutaway must leave world state and permitted entities unchanged.");
        // Move within an edge without advancing held simulation; the visual center must follow the exact position.
        var before = _world.CutawayCenter;
        var next = Enumerable.Range(0, 6).Select(player.Cell.Neighbor).First(c => _simulation.Map.CanTraverse(player.Cell, c));
        player.Route = [next]; player.SegmentMs = 100; player.KeepFacing = true;
        var expected = WorldView.Screen(player.Position) - WorldView.Screen(player.Cell.World);
        _world.QueueRedraw();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if ((_world.CutawayCenter - before - expected).Length() > 0.01f)
            throw new InvalidOperationException("Cutaway center must follow fractional movement rather than hex centers.");
        GD.Print($"CUTAWAY_SMOKE_PASS: visible foreground opening ({changed} changed samples), H round-trip, unchanged world/sight and fractional following.");
    }
}
