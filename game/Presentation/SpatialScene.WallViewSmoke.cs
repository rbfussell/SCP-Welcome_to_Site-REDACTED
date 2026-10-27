using Godot;
using SiteSimulation;
using System.IO;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task CheckReducedWalls()
    {
        _world.ReducedWalls = false; _world.DiagnosticSprites = false; _world.FieldView = true; _world.Cutaway = true;
        var sim = _simulation; var map = sim.Map; var baseline = sim.Save();
        var sight = map.Cells.Select(c => sim.VisibleToPlayer(c.World)).ToArray();
        var walkability = map.Cells.Select(c => map.Walkable(c)).ToArray();
        var prefix = Path.Combine(Path.GetDirectoryName(_capturePath!)!, Path.GetFileNameWithoutExtension(_capturePath!));
        async Task Render(string suffix)
        {
            _world.QueueRedraw(); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage();
            if (image.SavePng(prefix + suffix + ".png") != Error.Ok) throw new IOException("Wall-view capture failed.");
        }
        void Unchanged()
        {
            if (sim.Save() != baseline || !sight.SequenceEqual(map.Cells.Select(c => sim.VisibleToPlayer(c.World)))
                || !walkability.SequenceEqual(map.Cells.Select(c => map.Walkable(c))))
                throw new InvalidOperationException("Wall view must not change sight, collision, actors, clocks, doors or anomaly state.");
        }
        await Render("-walls-full");
        using var full = GetViewport().GetTexture().GetImage();
        SmokeKey(Key.W); Input.FlushBufferedEvents(); await Render("-walls-lowered");
        if (!_world.ReducedWalls || _world.ReducedWallFaces != map.Definition.Walls.Count(w => !w.Door) || _world.FeatheredWallFaces != 0)
            throw new InvalidOperationException("W must lower every solid wall and leave the full-height local opening dormant.");
        if (map.Definition.Walls.Any(w => Math.Abs(_world.RenderedWallHeight(w) - (w.Door ? 68 : 68 * .31f)) > .01f))
            throw new InvalidOperationException("Lowered walls must end at the dark base panel while doors keep their height.");
        Unchanged();
        using var low = GetViewport().GetTexture().GetImage();
        var area = _hud.Field.GetGlobalRect(); var changed = 0;
        for (var y = (int)area.Position.Y; y < Math.Min(low.GetHeight(), area.End.Y); y += 3)
        for (var x = (int)area.Position.X; x < Math.Min(low.GetWidth(), area.End.X); x += 3)
        {
            var a = full.GetPixel(x, y); var b = low.GetPixel(x, y);
            if (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) > .04f) changed++;
        }
        if (changed < 100) throw new InvalidOperationException("Lowering the walls did not visibly clear room interiors.");
        // Use the visible field control to return to full height; the previous local-cutaway preference survives.
        var click = _hud.WallViewButton.GetGlobalRect().GetCenter();
        Input.ParseInputEvent(new InputEventMouseButton { Position = click, ButtonIndex = MouseButton.Left, Pressed = true });
        Input.ParseInputEvent(new InputEventMouseButton { Position = click, ButtonIndex = MouseButton.Left, Pressed = false });
        Input.FlushBufferedEvents(); await Render("-walls-restored");
        if (_world.ReducedWalls || !_world.Cutaway || _world.FeatheredWallFaces == 0)
            throw new InvalidOperationException("Field button must restore full walls and the existing local cutaway.");
        Unchanged();
        SmokeKey(Key.W); SmokeKey(Key.F4); Input.FlushBufferedEvents(); await Render("-walls-lowered-diagnostic");
        if (!_world.DiagnosticSprites || _world.ReducedWallFaces == 0 || map.Definition.Walls.Any(w =>
            Math.Abs(_world.RenderedWallHeight(w) - (w.Door ? 54 : 54 * .31f)) > .01f))
            throw new InvalidOperationException("Diagnostic artwork must use short bases too, with full-height doors.");
        Unchanged();
        SmokeKey(Key.F4); SmokeKey(Key.W); Input.FlushBufferedEvents(); await Render("-walls-final"); Unchanged();
        GD.Print($"WALL_VIEW_SMOKE_PASS: W and native field-button round trip, every wall lowered ({changed} changed samples), cropped base texture, full-height doors, diagnostic parity and unchanged simulation/sight/collision.");
    }
}
