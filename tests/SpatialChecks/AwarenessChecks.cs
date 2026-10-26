using SiteSimulation;
using System.Text.Json.Nodes;

internal static class AwarenessChecks
{
    public static int Run(string json)
    {
        var passed = 0;
        void Check(bool ok, string detail) { if (!ok) throw new Exception("Awareness: " + detail); passed++; }
        var policy = new PlayerAwareness();
        var unknownModule = new PerceivedContact("fixture:unfamiliar-object", 0, new(1, 2), 0, "direct", "avatar");
        Check(policy.StopOnDirectSight && policy.StopOnReports, "both interruption settings default on");
        Check(policy.Receive(unknownModule), "generic anomalous subject must trigger without an SCP-specific reaction");
        Check(!policy.Receive(unknownModule), "continuous sight must not repeatedly interrupt");
        Check(!policy.Receive(unknownModule with { Source = "voice", ObserverId = "colleague" }), "duplicate report is the same encounter");
        Check(policy.Receive(unknownModule with { Encounter = 1 }), "a new appearance is a fresh encounter");
        Check(!policy.Receive(unknownModule), "stale reports must not replace newer evidence or retrigger it");
        policy.StopOnDirectSight = false;
        Check(!policy.Receive(unknownModule with { SubjectId = "fixture:second-object" }), "self sightings can be disabled independently");
        Check(policy.Receive(unknownModule with { SubjectId = "fixture:second-object", Source = "voice", ObserverId = "colleague" }),
            "communicated sighting can still interrupt with self sightings disabled");
        policy.StopOnReports = false;
        Check(!policy.Receive(unknownModule with { SubjectId = "fixture:third-object", Source = "voice", ObserverId = "colleague" }), "reports can also be disabled");
        Check(!policy.Receive(unknownModule with { SubjectId = "sound:unidentified", Source = "sound", Classification = "unknown", UncertaintyRadius = 2 }),
            "an unidentified sound carries uncertain evidence without identifying an anomaly or forcing combat");
        Check(policy.Evidence.Single(e => e.SubjectId == "sound:unidentified").UncertaintyRadius == 2,
            "direction cues must retain uncertainty rather than looking up a hidden emitter");
        var heard = new SpatialSimulation(json);
        heard.Awareness.Receive(new("sound:unidentified", 0, new(3, 4), heard.LocalMs, "sound", heard.Player.Id, "unknown", 2));
        var heardClone = new SpatialSimulation(json); heardClone.Restore(heard.Save());
        Check(heardClone.Save() == heard.Save(), "an uncertain sound contact must persist without inventing an authoritative source entity");

        void DiscoverWhileWalking(SpatialSimulation sim)
        {
            // Sensor fixture: change attention without issuing the player's stop-and-look command.
            sim.Player.Facing = ObservationService.FacingToward(sim.Player.Position, sim.Anomalies.Bodies.Single().Cell.World, sim.Player.Facing);
            sim.Player.KeepFacing = true;
            sim.Tick();
        }
        SpatialSimulation Setup(int ticks, bool trigger = true)
        {
            var sim = new SpatialSimulation(json);
            sim.Awareness.StopOnReports = false;
            sim.Player.Cell = new(12, -2);
            sim.Face(new(8, 3));
            Check(sim.RequestMove(new(14, -2), true), "fixture needs an ordinary multi-step route away from the anomaly");
            for (var i = 0; i < ticks; i++) sim.Tick();
            Check(sim.Player.Moving && sim.Player.SegmentMs == ticks * SpatialSimulation.TickMs, "walking setup must reach its expected fractional progress");
            if (trigger) DiscoverWhileWalking(sim);
            return sim;
        }
        var sim = Setup(2, false);
        var position = sim.Player.Position;
        DiscoverWhileWalking(sim);
        var local = sim.LocalMs; var world = sim.WorldMs;
        Check(!sim.Player.Moving && sim.Player.Route.Count == 0 && sim.Player.SegmentMs == 0, "sighting must clear path and active destination immediately");
        Check(sim.Player.Position == position && sim.Player.StoppedStep?.ProgressMs == 100, "stop must retain exact fractional position without snapping or finishing a step");
        Check(!sim.ManualPause && !sim.LocalHold, "a movement interruption cannot pause the game or local area");
        Check(sim.Trace.Entries.Any(e => e.Event == "awareness-stop"), "shared movement stop needs causal evidence");
        var saved = sim.Save(); var clone = new SpatialSimulation(json); clone.Restore(saved);
        Check(clone.Save() == saved, "fractional stop, settings and acknowledged evidence must restore exactly");
        for (var i = 0; i < 100; i++) { sim.Tick(); clone.Tick(); }
        Check(sim.Player.Position == position && sim.WorldMs == world + 5000 && sim.LocalMs == local + 5000, "world, NPCs and local timers continue while player stays put");
        Check(sim.Save() == clone.Save(), "restored stopped execution must match without repeating the interruption");
        Check(sim.RequestMove(new(14, -2), true) && sim.Player.Position == position, "a deliberate new click must resume continuously");
        for (var i = 0; i < 20; i++) sim.Tick();
        Check(sim.Player.Cell == new Hex(14, -2) && !sim.Player.Moving, "same observed encounter must allow deliberate movement after stopping");
        var reverse = Setup(2); var reversePosition = reverse.Player.Position;
        Check(reverse.RequestMove(new(12, -2), true) && reverse.Player.Position == reversePosition, "reverse from a stopped edge cannot snap position");
        for (var i = 0; i < 2; i++) reverse.Tick();
        Check(reverse.Player.Cell == new Hex(12, -2) && reverse.Player.StoppedStep is null && !reverse.Player.Moving, "returning to the original center must complete normally");
        var boundary = Setup(0);
        Check(!boundary.Player.Moving && boundary.Player.StoppedStep is null && boundary.Player.Position == boundary.Player.Cell.World,
            "sighting at a hex center stops immediately without fabricating an unfinished step");
        var report = Setup(2, false);
        report.Awareness.StopOnDirectSight = false; report.Awareness.StopOnReports = true;
        var reportPosition = report.Player.Position; report.Tick();
        Check(!report.Player.Moving && report.Player.Position == reportPosition
            && report.Awareness.Evidence.Single().Source == "voice", "a delivered nearby human report must use the same exact movement interruption");
        var blocked = new SpatialSimulation(json); blocked.Awareness.StopOnDirectSight = false;
        Check(blocked.RequestMove(new(12, 3)), "closed-room communication fixture needs a clear corridor route");
        blocked.Tick(); blocked.Tick();
        Check(blocked.Player.Moving && blocked.Awareness.Evidence.Count == 0, "someone observing in a disconnected room cannot telepathically interrupt the player");
        var before = clone.Save();
        void Reject(Action<JsonNode> mutate)
        {
            var bad = JsonNode.Parse(before)!; mutate(bad);
            try { clone.Restore(bad.ToJsonString()); throw new Exception("Corrupt stopped awareness accepted."); }
            catch (ArgumentException) { Check(clone.Save() == before, "invalid restore must leave live position, settings and evidence intact"); }
        }
        Reject(n => n["Actors"]![0]!["StoppedStep"]!["ProgressMs"] = 999);
        Reject(n => n["Awareness"]!["Evidence"]![0]!["SubjectId"] = "missing");
        var legacy = JsonNode.Parse(new SpatialSimulation(json).Save())!;
        legacy["Version"] = 2; legacy.AsObject().Remove("Awareness");
        foreach (var actor in legacy["Actors"]!.AsArray()) actor!.AsObject().Remove("StoppedStep");
        var migrated = new SpatialSimulation(json); migrated.Restore(legacy.ToJsonString());
        Check(JsonNode.Parse(migrated.Save())!["Version"]!.GetValue<int>() == 12 && migrated.Awareness.StopOnDirectSight && migrated.Awareness.StopOnReports,
            "compatible schema-2 prototype saves must upgrade with both safety settings enabled");
        var patrolWorld = new SimulationWorld(json);
        var patrol = patrolWorld.Actors.Single(a => a.Id == "tech");
        patrol.Route = patrolWorld.Map.FindPath(patrol.Cell, new(13, 7), MovementSystem.Occupied(patrolWorld, patrol)).ToList();
        patrol.SegmentMs = 100;
        var patrolPosition = patrol.Position;
        MovementSystem.StopInPlace(patrol); patrolWorld.LocalMs = 1000;
        MovementSystem.Advance(patrolWorld);
        Check(patrol.StoppedStep is null && patrol.Moving && patrol.Position == patrolPosition,
            "an autonomous patrol must also replan from a stopped edge without snapping or retaining a stale stop");
        Console.WriteLine($"AWARENESS_CHECKS_PASS: {passed} assertions; generic contacts, settings, delivered reports, exact stops/resume/reversal and persistence.");
        return passed;
    }
}
