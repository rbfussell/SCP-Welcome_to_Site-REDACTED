using SiteSimulation;
using System.Text.Json.Nodes;

internal static class PathfindingChecks
{
    public static int Run(string json)
    {
        var passed = 0;
        void Check(bool ok, string detail) { if (!ok) throw new Exception("Pathfinding: " + detail); passed++; }
        var map = JsonNode.Parse(json)!;
        map["anomalies"] = new JsonArray(); // Isolate ordinary navigation from relocation/awareness interruptions.
        var fixture = map.ToJsonString();
        SpatialSimulation Sim(bool dark = true)
        {
            var sim = new SpatialSimulation(fixture); sim.Map.LightsOn = !dark; return sim;
        }
        void Ticks(SpatialSimulation sim, int count) { for (var i = 0; i < count; i++) sim.Tick(); }
        var goal = new Hex(12, 3);
        var dark = Sim(); dark.Actors.Single(a => a.Id == "observer").Cell = goal;
        var empty = Sim();
        Check(!dark.VisibleToPlayer(goal.World) && !dark.KnownNavigationBlocked(goal), "dark occupant must not be a planning/cursor fact");
        Check(dark.PreviewPath(goal).SequenceEqual(empty.PreviewPath(goal)), "hidden occupancy cannot change the preview");
        Check(dark.RequestMove(goal), "clicking an unseen occupied destination must commit travel");
        Check(dark.Player.Route[^1] == goal && dark.Player.MoveGoal == goal, "preserve intended destination rather than guessing where the unseen occupant is");
        Ticks(dark, 2);
        var saved = dark.Save(); var restored = Sim(); restored.Restore(saved);
        Check(restored.Save() == saved, "in-progress navigation intent must persist");
        Ticks(dark, 80); Ticks(restored, 80);
        Check(dark.Player.Cell != new Hex(5, 6) && dark.Player.Cell.Distance(goal) == 1 && !dark.Player.Moving,
            "walk up to the occupied target and stop before collision");
        Check(dark.Actors.Single(a => a.Id == "observer").Cell == goal && dark.Player.Cell != goal, "collision must not displace or overlap the occupant");
        Check(dark.LastMessage == "Movement stopped: the way ahead is blocked." && dark.Player.MoveGoal is null,
            "a blocked boundary needs clear feedback and no hidden queued continuation");
        Check(dark.WorldMs == 4100 && dark.LocalMs == 4100, "obstruction does not pause either clock");
        Check(restored.Save() == dark.Save(), "restored approach must stop identically");

        var vacated = Sim(); var occupant = vacated.Actors.Single(a => a.Id == "observer"); occupant.Cell = goal;
        Check(vacated.RequestMove(goal), "vacating fixture needs an accepted click");
        Ticks(vacated, 3); occupant.Cell = new(5, 0); Ticks(vacated, 80);
        Check(vacated.Player.Cell == goal, "an occupant leaving before arrival must not leave a shortened phantom route");

        var visible = Sim(false); visible.Actors.Single(a => a.Id == "observer").Cell = goal; visible.Face(goal.World);
        Check(visible.KnownNavigationBlocked(goal), "directly seen occupant can affect planning");
        Check(visible.RequestMove(goal) && visible.Player.Route[^1].Distance(goal) == 1,
            "even a known occupied destination must produce an approach, not reject a distant click");
        Ticks(visible, 80);
        Check(visible.Player.Cell.Distance(goal) == 1 && !visible.Player.Moving, "visible occupied goal also stops at its boundary");

        var acrossWall = new Hex(10, -2);
        var closed = Sim();
        Check(closed.Map.FindPath(closed.Player.Cell, acrossWall).Count == 0, "fixture requires an actually closed boundary");
        Check(closed.RequestMove(acrossWall), "inaccessible far side still accepts an approach");
        Ticks(closed, 100);
        Check(closed.Player.Cell != new Hex(5, 6) && closed.Player.Position.Y > 3 && closed.Player.Cell != acrossWall,
            "approach the wall without walking through it");
        Check(!closed.Player.Moving && closed.Trace.Entries.Any(e => e.Event == "blocked"), "unreachable goal stops at the nearest accessible boundary");
        var opened = Sim(); opened.Map.OpenDoors.Add("door-4-3-u");
        Check(opened.RequestMove(acrossWall), "a proper route through open access must be used");
        Ticks(opened, 100);
        Check(opened.Player.Cell == acrossWall, "valid route must reach the selected far side");
        var opening = Sim(); opening.RequestMove(acrossWall); Ticks(opening, 2);
        opening.Map.OpenDoors.Add("door-4-3-u"); Ticks(opening, 100);
        Check(opening.Player.Cell == acrossWall, "retain the original destination if access opens during the approach");

        var detour = Sim(); var far = new Hex(15, 1);
        var originalPath = detour.PreviewPath(far);
        var obstructed = originalPath[3]; detour.Actors.Single(a => a.Id == "observer").Cell = obstructed;
        Check(detour.PreviewPath(far).SequenceEqual(originalPath), "unseen intermediate blocker cannot cause a preemptive detour");
        Check(detour.RequestMove(far), "detour fixture needs movement");
        var limit = 80;
        while (!detour.Trace.Entries.Any(e => e.Event == "rerouted") && limit-- > 0) detour.Tick();
        Check(limit > 0 && detour.Player.BumpedCells.Contains(obstructed), "actual contact may establish an identity-free obstruction and alternate route");
        Check(detour.Player.MoveGoal == far && !detour.Player.Route.Contains(obstructed), "reroute preserves destination while avoiding the encountered cell");
        var detourSave = detour.Save(); var detourClone = Sim(); detourClone.Restore(detourSave);
        Check(detourClone.Save() == detourSave, "rerouted intent and encountered cells must round-trip");
        Ticks(detour, 100); Ticks(detourClone, 100);
        Check(detour.Player.Cell == far && detour.Save() == detourClone.Save(), "take an actual alternate route to completion without overlap");
        Check(detour.Player.BumpedCells.Count == 0 && detour.Player.MoveGoal is null, "completion clears temporary navigation continuation");
        var corrupt = JsonNode.Parse(detourSave)!; corrupt["Actors"]![0]!["MoveGoal"]!["Q"] = 999;
        var unchanged = detourClone.Save();
        try { detourClone.Restore(corrupt.ToJsonString()); throw new Exception("Invalid destination accepted"); }
        catch (ArgumentException) { Check(detourClone.Save() == unchanged, "invalid navigation restore leaves live state intact"); }

        var cancelled = Sim(); cancelled.RequestMove(far); Ticks(cancelled, 2); cancelled.CancelMove(); Ticks(cancelled, 80);
        Check(!cancelled.Player.Moving && cancelled.Player.Cell != far && cancelled.Player.MoveGoal is null,
            "cancel must not resume the original destination at a partial endpoint");
        var synthetic = Sim(); synthetic.Map.DynamicBlockers.Add(goal);
        Check(synthetic.PreviewPath(goal).SequenceEqual(empty.PreviewPath(goal)), "anonymous hidden collision cells are not a player sensor");
        Check(synthetic.RequestMove(goal), "anonymous blocker also accepts travel"); Ticks(synthetic, 80);
        Check(synthetic.Player.Cell.Distance(goal) == 1, "real collision remains authoritative for unidentified blockers");
        var beforePreview = empty.Save();
        for (var i = 0; i < 10; i++) { empty.PreviewPath(goal); empty.KnownNavigationBlocked(goal); }
        Check(empty.Save() == beforePreview, "preview/cursor queries cannot mutate knowledge, routes or RNG");
        Check(empty.PreviewPath(new(-100, -100)).Count == 0, "off-map clicks remain invalid");
        Console.WriteLine($"PATHFINDING_CHECKS_PASS: {passed} assertions; hidden occupancy, approach, collision, rerouting, access changes and intent persistence.");
        return passed;
    }
}
