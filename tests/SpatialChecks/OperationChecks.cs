using SiteSimulation;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class OperationChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string detail) { if (!ok) throw new Exception("Operations: " + detail); passed++; }
        SpatialSimulation Sim(string? definition = null) => new(map, definition ?? config);
        void Ticks(SpatialSimulation sim, int count) { for (var i = 0; i < count; i++) sim.Tick(); }
        var initial = Sim();
        Ticks(initial, 65);
        Check(initial.Operations.PhysicalStatus == "Contained · procedure compliant", "initial scenario must maintain actual three-person coverage");
        Check(!initial.Operations.HadBreach && !initial.Operations.Completed, "initial compliance is not a completed recovery");
        Check(initial.Operations.OrderHistory.Count == 2 && initial.Operations.OrderHistory.All(o => o.Task == PersonnelTask.Observe), "assigned observation duties must displace discretionary patrol");
        var snapshot = initial.Save(); var clone = Sim(); clone.Restore(snapshot);
        Check(clone.Save() == snapshot, "operation state must restore without replaying delivery or rewards");
        initial.LocalHold = true; var held = initial.Operations.StableMs; var world = initial.WorldMs;
        Ticks(initial, 20);
        Check(initial.Operations.StableMs == held && initial.WorldMs == world + 1000, "held operation work stays local while world time continues");
        initial.LocalHold = false;
        Check(!initial.IssueOrder("tech", PersonnelTask.Post, new(8, 4), true), "a remote UI cannot impersonate a usable physical terminal");
        var unknown = initial.Save();
        Check(!initial.IssueOrder("missing", PersonnelTask.Post, new(8, 4)) && initial.Save() == unknown, "unknown personnel reject without state mutation");
        Check(initial.IssueOrder("tech", PersonnelTask.Post, new(8, 4)), "valid voice intent should be queued");
        Check(initial.Operations.OrderHistory.Last().Status == OrderStatus.Pending, "submission is distinct from acknowledgement");
        Ticks(initial, 8);
        Check(initial.Operations.OrderHistory.Last().Status == OrderStatus.Blocked, "closed access must block the post order without teleportation");
        Check(initial.Actors.Single(a => a.Id == "tech").Cell != new Hex(8, 4), "staff must not teleport to a blocked post");
        var before = initial.Save();
        void Reject(Action<JsonNode> tamper)
        {
            var bad = JsonNode.Parse(before)!; tamper(bad);
            try { initial.Restore(bad.ToJsonString()); throw new Exception("Corrupt operation accepted"); }
            catch (ArgumentException) { Check(initial.Save() == before, "invalid operation restore changed live state"); }
        }
        Reject(n => n["Operations"]!["Trolley"]!["Position"]!["X"] = 999);
        Reject(n => n["Operations"]!["Orders"]![0]!["ActorId"] = "missing");
        Reject(n => n["Operations"]!["NextId"] = 1);

        var drill = Sim();
        Check(drill.ToggleNearestDoor(), "director must physically open the receiving access for relief");
        Check(drill.IssueOrder("observer", PersonnelTask.Post, new(4, 6))
            && drill.IssueOrder("tech", PersonnelTask.Post, new(14, 1)), "ordinary relief orders must be deliverable");
        drill.ApproachTerminal();
        Ticks(drill, 140);
        Check(drill.Operations.HadBreach && drill.Anomalies.Bodies.Single().Revision > 0,
            "actual staff travel and avatar attention loss must permit the native module's breach without a mission teleport");
        Check(!drill.Operations.Completed, "a genuine breach must stay unresolved until physical recovery conditions hold");

        var links = Sim();
        links.Player.Cell = new(9, 3); links.Map.OpenDoors.Add("door-4-3-u");
        Check(links.CanUseTerminal, "physical OSCP approach must provide access");
        links.Operations.Radios[1] = links.Operations.Radios[1] with { Powered = false };
        Check(links.IssueOrder("tech", PersonnelTask.Post, new(8, 4), true), "terminal can submit intent while recipient radio is offline");
        Ticks(links, 4);
        Check(links.Operations.OrderHistory.Last().Status == OrderStatus.Pending, "radio outage prevents acknowledgement");
        Check(links.Operations.OrderHistory.First(o => o.ActorId == "tech").Status == OrderStatus.Active, "an undelivered replacement cannot relieve an existing duty");
        links.Operations.Radios[1] = links.Operations.Radios[1] with { Powered = true };
        links.Tick();
        Check(links.Operations.OrderHistory.Last().Status != OrderStatus.Pending, "restoring a link delivers the pending command once");
        var count = links.Operations.ReceivedReports.Count;
        Ticks(links, 3);
        Check(links.Operations.ReceivedReports.Count <= count + 3, "continuous execution must not flood identical acknowledgement reports");
        links.Operations.TerminalPowered = false;
        Check(!links.CanUseTerminal, "terminal power loss closes physical service access");

        // Bounded physical recovery fixture: same native module, open room, no forced move by the mission.
        var definition = JsonSerializer.Deserialize<OperationDefinition>(config, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var recoveryConfig = JsonSerializer.Serialize(definition with
        {
            Home = new(9, -1), Trolley = new(9, -2),
            Starts = [new("avatar", new(10, 0), 5), new("observer", new(5, 0), 1), new("tech", new(8, -2), 1)]
        });
        var recovery = Sim(recoveryConfig);
        Check(recovery.IssueOrder("tech", PersonnelTask.Transport, new(9, -1)), "recovery procedure must be assignable");
        Ticks(recovery, 3);
        Check(recovery.Operations.Trolley.Step is { Stage: "load", ProgressMs: > 0 }, "loading consumes real local work time");
        var loadedLater = recovery.Save(); var restored = Sim(recoveryConfig); restored.Restore(loadedLater);
        Check(restored.Save() == loadedLater, "in-progress loading and pending order state must round-trip");
        var max = 400;
        var phases = new HashSet<string>();
        while (!recovery.Operations.Completed && max-- > 0)
        {
            if (recovery.Operations.Trolley.Step is { } step && phases.Add(step.Stage))
            {
                var continuation = Sim(recoveryConfig); continuation.Restore(recovery.Save());
                Check(continuation.Save() == recovery.Save(), $"{step.Stage} work must preserve its exact continuation");
                if (step.Stage == "push")
                {
                    Check(!recovery.Map.Walkable(step.Next), "pending trolley push reserves its next physical hex");
                    Check(!recovery.RequestMove(step.Next), "player pathfinding cannot steal a reserved trolley destination");
                    var corrupt = JsonNode.Parse(recovery.Save())!;
                    corrupt["Operations"]!["Trolley"]!["Step"]!["Next"]!["Q"] = 999;
                    var safe = continuation.Save();
                    try { continuation.Restore(corrupt.ToJsonString()); throw new Exception("Invalid push continuation accepted"); }
                    catch (ArgumentException) { Check(continuation.Save() == safe, "invalid push restore must leave live work intact"); }
                }
            }
            recovery.Tick(); restored.Tick();
        }
        Check(phases.Contains("push") && phases.Contains("unload"), "recovery must exercise both timed push and unloading");
        if (!recovery.Operations.Completed)
            Console.WriteLine(string.Join("\n", recovery.Trace.Entries.TakeLast(16).Select(e => e.Event + ": " + e.Detail)));
        Check(recovery.Operations.Completed, "observed transport, unloading and staffing must produce actual recovery");
        Check(recovery.Save() == restored.Save(), "save/load during work must match uninterrupted recovery");
        Check(recovery.Anomalies.Bodies.Single().Cell == new Hex(9, -1) && recovery.Anomalies.Bodies.Single().Revision == 0,
            "physical transport preserves the single body and anomalous appearance identity");
        Check(recovery.Operations.Trolley.SubjectId is null && recovery.Operations.Trolley.Position != recovery.Anomalies.Bodies.Single().Position,
            "unloading separates physical cargo from the parked empty trolley");
        Check(recovery.Operations.OrderHistory.Any(o => o.Task == PersonnelTask.Transport && o.Status == OrderStatus.Complete), "transport completion must be distinct from acknowledgement");
        Check(recovery.Trace.Entries.Any(e => e.Event == "pushed") && recovery.Trace.Entries.Any(e => e.Event == "unloaded"), "recovery must use actual physical work, not a success flag");
        recovery.Player.Attending = false; recovery.Tick();
        Check(recovery.Operations.StableMs == 0 && recovery.Operations.Completed, "post-success coverage loss remains a real persistent problem without replaying completion");

        var oneWatcher = Sim(recoveryConfig);
        oneWatcher.IssueOrder("tech", PersonnelTask.Transport, new(9, -1));
        var loadingLimit = 60;
        while (oneWatcher.Operations.Trolley.SubjectId is null && loadingLimit-- > 0) oneWatcher.Tick();
        var stoppedAt = oneWatcher.Operations.Trolley.Position;
        foreach (var actor in oneWatcher.Actors.Where(a => a.Id != "tech")) actor.Attending = false;
        Ticks(oneWatcher, 15);
        Check(oneWatcher.Anomalies.Bodies.Single().Revision == 0 && oneWatcher.Operations.Trolley.Position == stoppedAt,
            "handler sight can suppress relocation while insufficient external coverage stops transport");
        Check(oneWatcher.Operations.OrderHistory.Last(o => o.Task == PersonnelTask.Transport).Status == OrderStatus.Blocked,
            "stopped transport must report its coverage requirement");
        foreach (var actor in oneWatcher.Actors) actor.Attending = true;
        Ticks(oneWatcher, 120);
        Check(oneWatcher.Operations.Completed, "restoring coverage must recover the blocked operation");

        var authority = new SimulationWorld(map, config);
        Check(!PersonnelOrderSystem.Issue(authority, "observer", "tech", PersonnelTask.Post, new(8, 4), "voice", out _),
            "being an actor or using the same command service does not grant director authority");
        var hidden = new SimulationWorld(map, config);
        Check(PersonnelOrderSystem.Issue(hidden, "avatar", "tech", PersonnelTask.Observe, new(8, 4), "voice", out _),
            "observation intent may be submitted without a target location");
        PersonnelOrderSystem.Advance(hidden);
        Check(hidden.Operations.Orders.Last().Detail == "No acquired sighting to investigate",
            "an order cannot acquire an unknown subject location from the authoritative entity");

        var loss = Sim(recoveryConfig);
        Check(loss.IssueOrder("tech", PersonnelTask.Transport, new(9, -1)), "loss fixture needs transport");
        var timeout = 60;
        while (loss.Operations.Trolley.SubjectId is null && timeout-- > 0) loss.Tick();
        Check(loss.Operations.Trolley.SubjectId == "scp-650", "loss fixture must genuinely load first");
        var abandonedCart = loss.Operations.Trolley.Position;
        foreach (var actor in loss.Actors) actor.Attending = false;
        // The handler cannot restore sight in darkness; let the ordinary module choose a valid relocation.
        loss.Map.LightsOn = false; loss.Tick();
        Check(loss.Anomalies.Bodies.Single().Cell.World != abandonedCart && loss.Operations.Trolley.SubjectId is null,
            "legal relocation during transport must detach cargo atomically");
        Check(loss.Operations.Trolley.Position == abandonedCart && loss.Map.DynamicBlockers.Contains(Hex.FromWorld(abandonedCart)),
            "abandoned trolley remains real blocked equipment");
        Check(!loss.Operations.Completed && loss.Operations.HadBreach, "unresolved recovery must persist rather than invent success");
        var lostClone = Sim(recoveryConfig); lostClone.Restore(loss.Save());
        Check(lostClone.Save() == loss.Save(), "unresolved loss state must persist without duplicate cargo");
        Console.WriteLine($"OPERATION_CHECKS_PASS: {passed} assertions; delivery, authority, blockage, physical recovery, coverage loss and persistence.");
        return passed;
    }
}
