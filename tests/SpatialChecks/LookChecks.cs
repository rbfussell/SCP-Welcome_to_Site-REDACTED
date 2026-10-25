using SiteSimulation;

internal static class LookChecks
{
    public static int Run(string json)
    {
        var passed = 0;
        void Check(bool ok, string detail) { if (!ok) throw new Exception("Look: " + detail); passed++; }
        SpatialSimulation Walking(int ticks)
        {
            var sim = new SpatialSimulation(json);
            // Ordinary look must stop travel even with both anomaly interruption settings disabled.
            sim.Awareness.StopOnDirectSight = sim.Awareness.StopOnReports = false;
            Check(sim.RequestMove(new(12, 3)), "fixture needs a multi-step corridor route");
            for (var i = 0; i < ticks; i++) sim.Tick();
            Check(sim.Player.Moving && sim.Player.SegmentMs == ticks * SpatialSimulation.TickMs, "fixture must still be walking");
            return sim;
        }
        var sim = Walking(2);
        var position = sim.Player.Position;
        var origin = sim.Player.Cell;
        var target = position + (origin.World - sim.Player.Route[0].World) * 3;
        var expectedFacing = ObservationService.FacingToward(position, target, sim.Player.Facing);
        var local = sim.LocalMs; var world = sim.WorldMs;
        sim.Face(target);
        Check(!sim.Player.Moving && sim.Player.Route.Count == 0 && sim.Player.SegmentMs == 0,
            "ordinary look clears the active path immediately");
        Check(sim.Player.Position == position && sim.Player.StoppedStep?.ProgressMs == 100,
            "look must not snap or finish the current step");
        Check(sim.Player.Facing == expectedFacing && sim.Player.KeepFacing, "turn uses the actual stopped position");
        Check(sim.Player.MoveGoal is null && sim.Player.BumpedCells.Count == 0, "look clears any automatic goal continuation");
        Check(!sim.ManualPause && !sim.LocalHold && sim.LocalMs == local && sim.WorldMs == world,
            "look changes neither clocks nor pause settings");
        Check(sim.Trace.Entries.Any(e => e.Event == "look-stop"), "trace records an ordinary look stop");
        var clone = new SpatialSimulation(json); clone.Restore(sim.Save());
        Check(clone.Save() == sim.Save(), "fractional look stop must survive save/restore");
        for (var i = 0; i < 20; i++) { sim.Tick(); clone.Tick(); }
        Check(sim.Player.Position == position && !sim.Player.Moving && sim.LocalMs == local + 1000 && sim.WorldMs == world + 1000,
            "travel stays stopped while the simulation continues");
        Check(sim.Save() == clone.Save(), "stopped replay remains deterministic");
        sim.Face(target);
        Check(sim.Player.Position == position && sim.Player.StoppedStep?.ProgressMs == 100, "repeated look preserves an already stopped fraction");
        Check(sim.RequestMove(origin, true) && sim.Player.Position == position, "new movement can reverse without snapping");
        sim.Tick(); sim.Tick();
        Check(!sim.Player.Moving && sim.Player.Cell == origin && sim.Player.StoppedStep is null, "reverse finishes at original center");
        var center = Walking(0); var centerPoint = center.Player.Position;
        center.Face(target);
        Check(!center.Player.Moving && center.Player.Position == centerPoint && center.Player.StoppedStep is null,
            "look at a hex center does not invent a fractional step");
        var deliberate = new SpatialSimulation(json);
        deliberate.Awareness.StopOnDirectSight = deliberate.Awareness.StopOnReports = false;
        deliberate.Face(new(1, 4)); var facing = deliberate.Player.Facing;
        Check(deliberate.RequestMove(new(12, 3), true), "explicit keep-facing movement remains available");
        deliberate.Tick(); deliberate.Tick();
        Check(deliberate.Player.Moving && deliberate.Player.Facing == facing, "Shift-click semantics still allow movement with independent facing");
        deliberate.LocalHold = true; var held = deliberate.Save();
        deliberate.Face(target);
        Check(deliberate.Save() == held, "look cannot mutate a held local simulation");
        Console.WriteLine($"LOOK_CHECKS_PASS: {passed} assertions; exact stop/turn, continuing time, persistence, reversal and explicit keep-facing movement.");
        return passed;
    }
}
