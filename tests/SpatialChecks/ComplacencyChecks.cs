using SiteSimulation;
using System.Text.Json.Nodes;

internal static class ComplacencyChecks
{
    public static int Run(string json)
    {
        var passed = 0;
        void Check(bool condition, string detail)
        { if (!condition) throw new Exception("Complacency: " + detail); passed++; }
        var sim = new SpatialSimulation(json);
        var body = sim.Anomalies.Bodies.Single();
        var cell = body.Cell; var pose = body.Pose;
        Check(sim.SetModuleFlag(body.Id, "complacency", sim.Player.Id, true), "registered module flag must accept a real human");
        Check(sim.InspectAnomalies().Single().FlaggedActors("complacency").SequenceEqual(new[] { sim.Player.Id }), "flag is scoped to a person");
        Check(body.Cell == cell && body.Pose == pose && body.Revision == 0, "flag cannot move/re-pose an observed statue");
        var saved = sim.Save();
        var restored = new SpatialSimulation(json); restored.Restore(saved);
        Check(restored.Save() == saved, "module flag must persist exactly");
        var beforeReject = sim.Save();
        Check(!sim.SetModuleFlag(body.Id, "made-up", sim.Player.Id, true), "module must reject unknown flags");
        Check(!sim.SetModuleFlag(body.Id, "complacency", "missing", true), "host must reject unknown target");
        Check(!sim.SetModuleFlag("missing", "complacency", sim.Player.Id, true), "host must reject unknown instance");
        Check(sim.Save() == beforeReject, "rejected flags cannot mutate authoritative state");
        sim.LocalHold = true;
        Check(!sim.SetModuleFlag(body.Id, "complacency", sim.Player.Id, false), "held local simulation cannot change the flag");
        Check(sim.InspectAnomalies().Single().FlaggedActors("complacency").Length == 1, "rejected held change retains flag");
        sim.LocalHold = false;
        Check(sim.SetModuleFlag(body.Id, "complacency", sim.Player.Id, false), "flag must be reversible");
        Check(sim.InspectAnomalies().Single().FlaggedActors("complacency").Length == 0, "clearing removes the selected person");

        var nonRear = false;
        for (var seed = 1; seed <= 32; seed++)
        {
            var config = JsonNode.Parse(json)!;
            config["anomalies"]![0]!["seed"] = seed;
            var variant = config.ToJsonString();
            var threat = new SpatialSimulation(variant); threat.ToggleObserverAttention();
            var t = threat.Anomalies.Bodies.Single();
            Check(t.Revision == 1 && t.Pose is 1 or 2, "threat profile must produce a threatening discrete pose");
            Check(threat.Actors.Any(a => a.Cell.Distance(t.Cell) == 1 && Behind(a, t.Cell)
                && threat.Map.ClearSight(a.Position, t.Cell.World)), "threat placement must be immediately behind a person without a wall between them");
            Check(threat.InspectAnomalies().Single().FlaggedActors("complacency").Length == 0, "encounters cannot invent the future complacency metric");

            var relaxed = new SpatialSimulation(variant);
            foreach (var actor in relaxed.Actors) relaxed.SetModuleFlag(t.Id, "complacency", actor.Id, true);
            // Everyone else is occupied: front/side destinations can be genuinely unobserved too.
            foreach (var actor in relaxed.Actors.Where(a => a.Id != "observer")) actor.Attending = false;
            relaxed.ToggleObserverAttention();
            var r = relaxed.Anomalies.Bodies.Single();
            Check(r.Revision == 1 && r.Pose is >= 3 and <= 5, "relaxed profile must select from quieter poses only");
            Check(relaxed.Actors.Any(a => a.Cell.Distance(r.Cell) is >= 2 and <= 3
                && relaxed.Map.ClearSight(a.Position, r.Cell.World)), "relaxed profile must use a comfortable distance in connected space");
            Check(!relaxed.InspectAnomalies().Single().Observation.Observed, "relaxed placement still requires an unobserved destination");
            var selected = relaxed.Trace.Entries.Last(e => e.Event == "relocation-selected").Detail;
            nonRear |= relaxed.Actors.Any(a => selected.Contains($"profile near {a.Id};") && a.Cell.Distance(r.Cell) is >= 2 and <= 3
                && !Behind(a, r.Cell) && relaxed.Map.ClearSight(a.Position, r.Cell.World));
        }
        Check(nonRear, "relaxed placement must genuinely permit other positions rather than just change a pose behind someone");

        var aiConfig = JsonNode.Parse(json)!;
        aiConfig["actors"]![1]!["observerKind"] = "ai";
        var ai = new SpatialSimulation(aiConfig.ToJsonString());
        var aiSave = ai.Save();
        Check(!ai.SetModuleFlag("scp-650", "complacency", "observer", true), "SCP module must reject an ineligible flag target kind");
        Check(ai.Save() == aiSave, "invalid kind cannot mutate the world or module");
        var aiBad = JsonNode.Parse(aiSave)!;
        var aiModule = JsonNode.Parse(aiBad["Modules"]![0]!["Json"]!.GetValue<string>())!;
        aiModule["ComplacentActorIds"] = new JsonArray("observer");
        aiBad["Modules"]![0]!["Json"] = aiModule.ToJsonString();
        try { ai.Restore(aiBad.ToJsonString()); throw new Exception("Ineligible saved flag target accepted."); }
        catch (ArgumentException) { Check(ai.Save() == aiSave, "restore rechecks module target kinds without partial mutation"); }

        var invalid = JsonNode.Parse(saved)!;
        var module = JsonNode.Parse(invalid["Modules"]![0]!["Json"]!.GetValue<string>())!;
        module["ComplacentActorIds"] = new JsonArray("unknown-person");
        invalid["Modules"]![0]!["Json"] = module.ToJsonString();
        var beforeLoad = restored.Save();
        try { restored.Restore(invalid.ToJsonString()); throw new Exception("Invalid flag target accepted."); }
        catch (ArgumentException) { Check(restored.Save() == beforeLoad, "bad flag restore cannot partially alter the world"); }
        invalid = JsonNode.Parse(saved)!; invalid["Modules"]![0]!["Version"] = 1;
        try { restored.Restore(invalid.ToJsonString()); throw new Exception("Old implicit-familiarity module state accepted."); }
        catch (ArgumentException) { Check(restored.Save() == beforeLoad, "older module saves must fail intact rather than invent flags"); }
        Console.WriteLine($"COMPLACENCY_CHECKS_PASS: {passed} assertions; per-person flags, module placement/pose policy, host attachment and persistence.");
        return passed;
    }
    private static bool Behind(ActorState actor, Hex cell)
    {
        var delta = cell.World - actor.Position;
        var facing = actor.Cell.Neighbor(actor.Facing).World - actor.Cell.World;
        return delta.X * facing.X + delta.Y * facing.Y < -0.01;
    }
}
