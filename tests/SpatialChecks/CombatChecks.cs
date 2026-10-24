using SiteSimulation;
using System.Text.Json.Nodes;

internal static class CombatChecks
{
    public static int Run(string map, string operation)
    {
        var count = 0;
        void Check(bool ok, string detail) { if (!ok) throw new Exception("Combat: " + detail); count++; }
        SpatialSimulation Fixture(bool playerFirst = true)
        {
            var sim = new SpatialSimulation(map);
            sim.Player.Cell = new(8, 3); sim.Player.Facing = 1;
            var opponent = sim.Actors.Single(a => a.Id == "tech"); opponent.Cell = new(11, 3); opponent.Facing = 4;
            sim.Actors.Single(a => a.Id == "observer").Cell = new(5, 5);
            sim.Awareness.StopOnDirectSight = sim.Awareness.StopOnReports = false;
            Check(sim.Map.Walkable(sim.Player.Cell) && sim.Map.Walkable(opponent.Cell), "fixture positions need physical clearance");
            Check(sim.ArmTraining() && sim.Combat.Active, "visible opposing training contact must enter turns automatically");
            if (playerFirst)
            {
                var s = sim.Combat; s.Cursor = s.Participants.ToList().FindIndex(c => c.ActorId == sim.Player.Id);
                for (var i = 0; i < s.Roster.Count; i++) s.Roster[i] = s.Roster[i] with { Ap = s.Roster[i].ActorId == sim.Player.Id ? 10 : 0, Points = s.Roster[i].ActorId == "observer" ? 0 : 6 };
            }
            return sim;
        }
        void Resolve(SpatialSimulation sim)
        {
            for (var i = 0; sim.Combat.Pending is not null && i < 200; i++) sim.Tick();
            Check(sim.Combat.Pending is null, "accepted action must finish in bounded local ticks");
        }
        var sim = Fixture(); var local = sim.LocalMs; var positions = sim.Actors.Select(a => a.Position).ToArray();
        for (var i = 0; i < 200; i++) sim.Tick();
        Check(sim.WorldMs == 10000 && sim.LocalMs == local && sim.OffsiteUpdates == 10, "player decision holds all local time but world calendar/offsite continue");
        Check(positions.SequenceEqual(sim.Actors.Select(a => a.Position)), "staff cannot move during input wait");
        var before = sim.Save(); var clone = new SpatialSimulation(map); clone.Restore(before);
        Check(before == clone.Save(), "waiting turn must restore without initiative reroll or AP refill");
        sim.ToggleLighting(); sim.ToggleObserverAttention(); sim.BlinkPlayer();
        Check(sim.Save() == before, "ordinary/debug controls cannot mutate a combat turn for free");
        Check(sim.UseTrainingEquipment(CombatActionKind.ReadyWeapon) && sim.Combat.For(sim.Player.Id)!.Ap == 8, "readying charges two AP once");
        Check(!sim.RequestMove(new(9, 3)) && !sim.FireTraining("tech"), "pending equipment action forbids overlapping commands");
        sim.Tick(); sim.Tick();
        sim.LocalHold = true; var heldLocal = sim.LocalMs; var heldRemaining = sim.Combat.Pending!.RemainingMs; var heldWorld = sim.WorldMs;
        for (var i = 0; i < 10; i++) sim.Tick();
        Check(sim.LocalMs == heldLocal && sim.Combat.Pending!.RemainingMs == heldRemaining && sim.WorldMs == heldWorld + 500,
            "an additional hold preserves the pending action while outside time continues");
        sim.LocalHold = false;
        before = sim.Save(); clone.Restore(before);
        for (var i = 0; i < 4; i++) { sim.Tick(); clone.Tick(); }
        Check(sim.Save() == clone.Save() && sim.Combat.For(sim.Player.Id)!.WeaponReady, "mid-ready restore must resolve exactly once");
        Check(sim.FireTraining("tech") && sim.Combat.For(sim.Player.Id)!.Ammo == 5 && sim.Combat.For(sim.Player.Id)!.Ap == 4, "accepted shot reserves round/AP once");
        sim.Tick(); before = sim.Save(); clone.Restore(before);
        for (var i = 0; i < 11; i++) { sim.Tick(); clone.Tick(); }
        Check(sim.Save() == clone.Save(), "mid-shot restore must preserve roll, ammo, damage, clocks and feedback exactly");
        Check(sim.Events.Entries.Count(e => e.Kind == MessageKind.Combat && e.Text.StartsWith("You fire")) == 1, "one attack produces one outcome message");
        Check(sim.Combat.For("tech")!.Points is 4 or 6, "training shot changes only training points");
        var ap = sim.Combat.For(sim.Player.Id)!.Ap;
        Check(!sim.UseTrainingEquipment(CombatActionKind.ReadyWeapon) && sim.Combat.For(sim.Player.Id)!.Ap == ap, "invalid equipment action cannot charge AP");
        Check(sim.UseTrainingEquipment(CombatActionKind.Reload), "part-used magazine can reload"); Resolve(sim);
        Check(sim.Combat.For(sim.Player.Id)!.Ammo == 6 && sim.Combat.For(sim.Player.Id)!.ReserveAmmo == 11, "reload conserves training rounds");
        Check(!sim.FireTraining("tech"), "attack cannot exceed remaining AP");
        Check(sim.EndTurn() && sim.Combat.Pending?.Kind == CombatActionKind.Wait, "end-turn resolves a short local wait so timers can recover"); Resolve(sim);
        Check(sim.Combat.CurrentActorId != sim.Player.Id, "turn order must advance after wait resolves");
        var progressed = sim.LocalMs;
        for (var i = 0; sim.Combat.Active && sim.Combat.CurrentActorId != sim.Player.Id && i < 3000; i++) sim.Tick();
        Check(sim.Combat.Active && sim.Combat.CurrentActorId == sim.Player.Id && sim.Combat.For(sim.Player.Id)!.Ap == 10, $"autonomous participants act and return a fresh player turn; active={sim.Combat.Active}, current={sim.Combat.CurrentActorId}, pending={sim.Combat.Pending}, points={string.Join(",", sim.Combat.Participants.Select(c => c.ActorId + ":" + c.Points))}");
        Check(sim.LocalMs > progressed && sim.Events.Entries.Any(e => e.Kind == MessageKind.Combat && e.Text.Contains("fires")), "visible autonomous attack produces acquired combat feedback");

        var doorUse = Fixture();
        Check(doorUse.ToggleNearestDoor() && doorUse.Combat.For(doorUse.Player.Id)!.Ap == 8, "nearby door use is a paid two-AP action");
        var doorState = doorUse.Map.OpenDoors.Count;
        Resolve(doorUse);
        Check(doorUse.Map.OpenDoors.Count != doorState, "door changes only at its completed action checkpoint");

        var collision = Fixture();
        Check(collision.RequestMove(new(10, 4)), "collision fixture needs an accepted paid route");
        collision.Map.DynamicBlockers.Add(collision.Player.Route[0]);
        collision.Tick();
        Check(collision.Combat.Pending is null && !collision.Player.Moving && collision.Combat.For(collision.Player.Id)!.Ap == 10,
            "new physical obstruction stops the action and refunds all untravelled steps");
        Check(collision.Events.Entries[^1].Text == "Movement stopped: the way ahead is blocked.", "collision feedback cannot identify an unseen obstruction");
        var initiative = Fixture(false);
        Check(initiative.Combat.CurrentActorId != initiative.Player.Id, "initiative can put an autonomous participant first");
        var movement = Fixture();
        Check(movement.RequestMove(new(9, 3), true), "paid hex movement is available");
        var moveAp = movement.Combat.For(movement.Player.Id)!.Ap;
        Check(moveAp == 9, "one hex costs one AP");
        movement.Tick(); movement.Tick(); var fractional = movement.Player.Position;
        var movementClone = new SpatialSimulation(map); movementClone.Restore(movement.Save());
        Check(movementClone.Player.Position == fractional, "tactical save retains interpolation progress");
        for (var i = 0; i < 4; i++) { movement.Tick(); movementClone.Tick(); }
        Check(movement.Save() == movementClone.Save() && movement.Player.Cell == new Hex(9, 3), "restored paid move cannot replay or diverge");
        var oldFacing = movement.Player.Facing;
        movement.Face(new Hex(8, 3).World); Check(movement.Combat.For(movement.Player.Id)!.Ap == 8 && movement.Player.Facing != oldFacing, "facing changes are paid independently"); Resolve(movement);
        Check(movement.EndTraining() && !movement.Combat.Active && !movement.Combat.Armed, "drill can end at a player decision boundary");
        movement.Tick(); Check(movement.LocalMs > sim.LocalMs || !movement.LocalProgressHeld, "ordinary realtime resumes after drill");

        var hidden = new SpatialSimulation(map); hidden.Player.Cell = new(8, 3); hidden.Player.Facing = 4;
        hidden.Actors.Single(a => a.Id == "tech").Cell = new(11, 3);
        hidden.Actors.Single(a => a.Id == "observer").Cell = new(5, 5);
        Check(hidden.ArmTraining() && !hidden.Combat.Active, "hidden nearby opponent cannot force a telltale player hold");
        hidden.Face(new Hex(11, 3).World); hidden.Tick();
        Check(hidden.Combat.Active, "genuine acquired sight starts local combat");

        var orders = new SpatialSimulation(map, operation);
        orders.Operations.Radios.Clear();
        orders.Player.Cell = new(5, 8); orders.Player.Facing = 1;
        Check(orders.IssueOrder("tech", PersonnelTask.Post, new(8, 4)), "out-of-earshot order remains an intent");
        for (var i = 0; i < 60; i++) orders.Tick();
        Check(orders.Events.Entries.Count(e => e.Text.Contains("I don't think they heard me")) == 1, "unacknowledged voice gets one tentative feedback message");
        Check(orders.Operations.OrderHistory.Last().Status == OrderStatus.Pending, "feedback cannot invent delivery or cancel an unheard order");

        var badSource = Fixture(); var good = badSource.Save();
        void Reject(Action<JsonNode> mutate)
        {
            var bad = JsonNode.Parse(good)!; mutate(bad);
            try { badSource.Restore(bad.ToJsonString()); throw new Exception("corrupt tactical save accepted"); }
            catch (ArgumentException) { Check(badSource.Save() == good, "failed restore must be atomic"); }
        }
        Reject(n => n["Combat"]!["Cursor"] = 999);
        Reject(n => n["Combat"]!["Participants"]![0]!["Ap"] = 99);
        Reject(n => n["Combat"]!["Participants"]![0]!["ActorId"] = "missing");
        Reject(n => n["Events"]!["Entries"]![0]!["Text"] = "");
        Reject(n => n["Combat"] = null);
        var legacy = JsonNode.Parse(new SpatialSimulation(map, operation).Save())!;
        legacy["Version"] = 4; legacy.AsObject().Remove("Events"); legacy.AsObject().Remove("Combat");
        var migrated = new SpatialSimulation(map, operation); migrated.Restore(legacy.ToJsonString());
        Check(migrated.Events.Entries.Count == 0 && !migrated.Combat.Armed, "schema-4 migration starts empty journal and inactive combat");
        migrated.Tick();
        Check(!migrated.Events.Entries.Any(e => e.Text.Contains("Observation shift opened")), "migration cannot replay old already-received reports");
        Check(badSource.UseTrainingEquipment(CombatActionKind.ReadyWeapon), "pending corruption fixture needs an action");
        var pendingGood = badSource.Save(); var invalidAction = JsonNode.Parse(pendingGood)!;
        invalidAction["Combat"]!["Pending"]!["Cost"] = 0;
        try { badSource.Restore(invalidAction.ToJsonString()); throw new Exception("invalid AP continuation accepted"); }
        catch (ArgumentException) { Check(badSource.Save() == pendingGood, "invalid pending action cannot mutate the live turn"); }
        Check(badSource.FireTraining("scp-650") == false, "training fixture must never treat nonaggressive SCP-650 as an enemy");
        Console.WriteLine($"COMBAT_CHECKS_PASS: {count} assertions; local turns, AP, equipment, deterministic AI, perception, replay and atomic restores.");
        return count;
    }
}
