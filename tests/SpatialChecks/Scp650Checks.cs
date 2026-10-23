using SiteSimulation;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class Scp650Checks
{
    public static int Run(string json)
    {
        var passed = 0;
        void Check(bool condition, string detail)
        { if (!condition) throw new Exception("SCP650: " + detail); passed++; }
        SpatialSimulation Sim() => new(json);
        var observed = Sim();
        var origin = observed.Anomalies.Bodies.Single().Cell;
        for (var i = 0; i < 400; i++) observed.Tick();
        var report = observed.InspectAnomalies().Single();
        Check(report.Observation.EligibleIds.SequenceEqual(new[] { "observer" }), "one human observer should hold the statue");
        Check(report.Relocations == 0 && observed.Anomalies.Bodies.Single().Cell == origin && report.Pose == 0,
            "understaffing is not observation loss; pose/location must stay fixed");

        var redundant = Sim();
        redundant.Player.Cell = new(9, -2);
        redundant.Face(redundant.Anomalies.Bodies.Single().Cell.World);
        Check(redundant.InspectAnomalies().Single().Observation.EligibleIds.Length == 2, "two real observers must share coverage");
        redundant.BlinkPlayer();
        Check(redundant.InspectAnomalies().Single().Relocations == 0, "a second human must preserve observation through avatar blink");
        for (var i = 0; i < 3; i++) redundant.Tick();
        redundant.ToggleObserverAttention();
        Check(redundant.InspectAnomalies().Single().Relocations == 0, "the avatar alone must preserve observation after guard distraction");
        redundant.BlinkPlayer();
        Check(redundant.InspectAnomalies().Single().Relocations == 1, "last-human blink must release relocation immediately");
        Check(redundant.Anomalies.Bodies.Single().Pose is 1 or 2, "unfamiliar witness should receive a threatening pose");

        var sim = Sim();
        sim.ToggleObserverAttention();
        Check(sim.InspectAnomalies().Single().Relocations == 1, "last-observer loss must resolve at the command boundary");
        var relocated = sim.Anomalies.Bodies.Single();
        Check(relocated.Cell != origin && relocated.Pose is >= 0 and < 6, "relocation must choose valid location and discrete pose");
        Check(sim.Map.DynamicBlockers.Contains(relocated.Cell) && !sim.Map.DynamicBlockers.Contains(origin), "location/collision update must be atomic");
        Check(!sim.InspectAnomalies().Single().Observation.Observed, "destination must not already be observed");
        Check(sim.Actors.All(a => a.Cell != relocated.Cell && (!a.Moving || a.Route[0] != relocated.Cell)), "no body/reservation overlap");
        Check(sim.Trace.Entries.Any(e => e.Event == "observation-lost") && sim.Trace.Entries.Any(e => e.Event == "relocated"), "causal transition must be inspectable");
        Check(!sim.Trace.Entries.Any(e => e.Event is "sound" or "attack"), "relocation must not emit sound or attacks");
        var saved = sim.Save();
        for (var i = 0; i < 20; i++) sim.InspectAnomalies();
        Check(sim.Save() == saved, "inspector cannot mutate RNG, pose, cooldown or trace");
        var clone = Sim(); clone.Restore(saved);
        Check(clone.Save() == saved, "save after relocation must restore all module/host state");
        for (var i = 0; i < 200; i++) { sim.Tick(); clone.Tick(); }
        Check(sim.Save() == clone.Save(), "continued relocation/reaction RNG must match after restore");

        var held = Sim(); held.ToggleObserverAttention(); held.LocalHold = true;
        var stateBeforeHold = JsonNode.Parse(held.Save())!;
        held.ToggleObserverAttention(); held.ToggleLighting(); held.BlinkPlayer();
        for (var i = 0; i < 200; i++) held.Tick();
        var stateAfterHold = JsonNode.Parse(held.Save())!;
        Check(stateBeforeHold["Modules"]!.ToJsonString() == stateAfterHold["Modules"]!.ToJsonString()
            && stateBeforeHold["Bodies"]!.ToJsonString() == stateAfterHold["Bodies"]!.ToJsonString(), "local hold must freeze module state and bodies");
        Check(held.LocalMs == 0 && held.WorldMs == 10000 && held.OffsiteUpdates == 10, "world must continue while local SCP progress waits");
        var heldClone = Sim(); heldClone.Restore(held.Save());
        Check(heldClone.Save() == held.Save(), "save during local hold must not reroll the anomaly");
        held.LocalHold = false; held.Tick();
        Check(held.InspectAnomalies().Single().Relocations == 1, "world time must not instantly expire the local cooldown on release");

        var lighting = Sim(); lighting.ToggleLighting();
        Check(lighting.InspectAnomalies().Single().Relocations == 1
            && lighting.InspectAnomalies().Single().Observation.Observers.All(o => o.Reason == "unlit"), "real lighting changes affect observation");
        var perception = Sim().Map;
        var watcher = new ActorState { Id = "test", Kind = "npc", ObserverKind = "human", Cell = new(9, 2), Facing = 5 };
        var target = new Hex(9, 1).World;
        Check(ObservationService.Inspect(perception, watcher, target, 0).Reason == "occluded", "closed door must block actual perception");
        perception.OpenDoors.Add("door-4-3-u");
        Check(ObservationService.Inspect(perception, watcher, target, 0).Detected, "opening door restores actual sight");
        watcher.EyesClosedUntilMs = 150;
        Check(ObservationService.Inspect(perception, watcher, target, 0).Reason == "eyes-closed", "blink participates in observation");
        Check(ObservationService.Inspect(perception, watcher, target, 150).Detected, "blink ends at its local boundary");
        watcher.ObserverKind = "ai";
        var artificial = ObservationService.Inspect(perception, watcher, target, 150, "human");
        Check(artificial.Detected && !artificial.Eligible, "AI can detect/report without counting as a human observer");
        Check(ObservationService.Inspect(perception, watcher, target, 150, "ai").Eligible
            && ObservationService.Inspect(perception, watcher, target, 150, "any").Eligible, "module-selectable AI/any profiles");

        foreach (var profile in new[] { "human", "ai", "any" })
        {
            var config = JsonNode.Parse(json)!;
            config["actors"]![1]!["observerKind"] = "ai";
            config["anomalies"]![0]!["observerProfile"] = profile;
            var variantJson = config.ToJsonString();
            var variant = new SpatialSimulation(variantJson);
            var status = variant.InspectAnomalies().Single();
            Check(status.Observation.Observed == (profile != "human"), "effective profile must be selected by content/scenario");
            var variantClone = new SpatialSimulation(variantJson); variantClone.Restore(variant.Save());
            Check(variant.Save() == variantClone.Save(), "effective profile must survive save/load");
        }
        var gap = Sim(); gap.ToggleObserverAttention(); gap.ToggleObserverAttention();
        Check(gap.LocalMs == 0 && gap.InspectAnomalies().Single().Relocations == 1,
            "same-time loss/regain commands cannot erase an observation gap before the next tick");

        watcher.ObserverKind = "camera";
        Check(!ObservationService.Inspect(perception, watcher, target, 150, "any").Eligible,
            "an unregistered observer kind cannot turn an unattended camera into an eligible actor");

        // Verify commit validation independently from the module's proposed selection.
        var world = new SimulationWorld(json);
        var body = world.Anomalies.Bodies.Single();
        var host = new AnomalyWorldAccess(world, body);
        var query = new RelocationQuery(1, 3, true, "human");
        var choices = host.Candidates("human", query);
        Check(choices.Count > 0, "fixture needs unobserved candidate cells");
        Check(!host.TryRelocate(body.Revision, choices[0].Cell, 0, "human", query, out var reason) && reason == "origin-observed",
            "host must recheck origin observation at commit");
        world.Actors.Single(a => a.Id == "observer").Attending = false;
        var destination = host.Candidates("human", query).First(c => c.NearActorId == "avatar" && c.Cell.World.Y > 3 && c.Cell.World.Y < 5).Cell;
        world.Player.Facing = ObservationService.FacingToward(world.Player.Position, destination.World, world.Player.Facing);
        Check(!host.TryRelocate(body.Revision, destination, 0, "human", query, out reason) && reason == "destination-observed", "host must recheck destination observation");
        world.Player.Attending = false; world.Player.Facing = 1;
        Check(!world.Map.ClearSight(body.Cell.World, destination.World), "fixture must put a closed ordinary wall/door between endpoints");
        Check(host.TryRelocate(body.Revision, destination, 2, "human", query, out _), "self-relocation may cross a closed door; it is not pathfinding");
        Check(!host.TryRelocate(0, choices[0].Cell, 1, "human", query, out reason) && reason == "stale-body-revision", "stale request must be rejected");
        Check(!host.TryRelocate(body.Revision, choices[0].Cell, 1, "ai", query, out reason) && reason == "observer-profile-mismatch", "module cannot bypass the effective observer profile");
        world.LocalHold = true;
        Check(!host.TryRelocate(body.Revision, choices[0].Cell, 1, "human", query, out reason) && reason == "local-held", "host must enforce execution gate");

        var retryWorld = new SimulationWorld(json);
        retryWorld.Actors.Single(a => a.Id == "observer").Attending = false;
        var occupiedActors = retryWorld.Actors.Select(a => a.Cell).ToHashSet();
        retryWorld.Map.DynamicBlockers.UnionWith(retryWorld.Map.Cells.Where(c => !occupiedActors.Contains(c)));
        retryWorld.Anomalies.Evaluate(retryWorld, "no-candidate-test");
        var retry = retryWorld.Anomalies.Inspect(retryWorld).Single();
        var retrySave = retryWorld.Anomalies.CaptureModules().Single().Json;
        retryWorld.Anomalies.Evaluate(retryWorld, "same-local-boundary");
        Check(retry.Relocations == 0 && retry.Candidates == 0 && retry.NextEvaluationMs == 500, "no-candidate policy must schedule bounded local retry");
        Check(retryWorld.Anomalies.CaptureModules().Single().Json == retrySave && retryWorld.Trace.Entries.Count == 1,
            "same-time retry must not loop, reroll or spam traces");

        var discovery = Sim(); discovery.ToggleObserverAttention();
        var statue = discovery.Anomalies.Bodies.Single();
        var neighbor = Enumerable.Range(0, 6).Select(statue.Cell.Neighbor).First(c => discovery.Map.Walkable(c)
            && !discovery.Actors.Any(a => a.Cell == c) && discovery.Map.ClearSight(c.World, statue.Cell.World));
        discovery.Player.Cell = neighbor; discovery.Player.Route.Clear(); discovery.Player.SegmentMs = 0;
        discovery.Face(statue.Cell.World);
        var reactionCount = discovery.Trace.Entries.Count(e => e.Event == "startled" && e.Subject == "avatar");
        Check(reactionCount == 1 && discovery.Player.StartledUntilMs == discovery.LocalMs + 400, "first discovery produces a witness-specific local interruption");
        discovery.Face(statue.Cell.World); discovery.Face(statue.Cell.World);
        Check(discovery.Trace.Entries.Count(e => e.Event == "startled" && e.Subject == "avatar") == 1, "same encounter must not repeatedly startle a witness");
        var pose = statue.Pose; var cellBefore = statue.Cell;
        for (var i = 0; i < 80; i++) discovery.Tick();
        Check(statue.Cell == cellBefore && statue.Pose == pose, "establishing observation stabilizes position and pose");

        var baseSave = discovery.Save();
        void Reject(Action<JsonNode> corrupt)
        {
            var bad = JsonNode.Parse(baseSave)!; corrupt(bad);
            try { discovery.Restore(bad.ToJsonString()); throw new Exception("Corrupt module save accepted."); }
            catch (ArgumentException) { Check(discovery.Save() == baseSave, "failed restore must leave every live system intact"); }
        }
        Reject(n => n["Bodies"]![0]!["Pose"] = 99);
        Reject(n => n["Bodies"]![0]!["Cell"] = JsonSerializer.SerializeToNode(discovery.Player.Cell));
        Reject(n =>
        {
            var module = JsonNode.Parse(n["Modules"]![0]!["Json"]!.GetValue<string>())!;
            module["RandomState"] = 0; n["Modules"]![0]!["Json"] = module.ToJsonString();
        });
        Console.WriteLine($"SCP650_CHECKS_PASS: {passed} assertions; observation, host validation, relocation, discovery, holds and module saves.");
        return passed;
    }
}
