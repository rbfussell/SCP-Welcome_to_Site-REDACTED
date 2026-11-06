using SiteSimulation;
using System.Text.Json.Nodes;

internal static class BadgeDoorChecks
{
    public static int Run(string map, string operation)
    {
        var count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("Badge door: " + message); count++; }
        string Policy(int clearance = 1, bool powered = true, string issuer = "site-test")
        {
            var doc = JsonNode.Parse(map)!;
            var door = doc["walls"]!.AsArray().Single(w => w!["id"]!.GetValue<string>() == "door-7-5-u")!;
            door["reader"] = new JsonObject { ["issuer"] = issuer, ["clearance"] = clearance, ["powered"] = powered };
            return doc.ToJsonString();
        }
        SpatialSimulation Sim(string? source = null) => new(source ?? Policy(), operation);
        void Finish(SpatialSimulation sim)
        {
            for (var i = 0; i < 1000 && (sim.Inventory.Pending is not null || sim.Combat.Pending is not null); i++) sim.Tick();
            Check(sim.Inventory.Pending is null && sim.Combat.Pending is null, "paid action finishes");
        }
        void Act(SpatialSimulation sim, InventoryRequest request) { Check(sim.HandleInventory(request), sim.LastMessage); Finish(sim); }
        InventoryRequest Hand() => new(InventoryActionKind.Move, "personnel-badge", new("person:avatar", "hand"));
        InventoryRequest Clip() => new(InventoryActionKind.Move, "personnel-badge", new("avatar-shirt", "badge-clip", AttachmentPose: new(0, 2)));
        void Equip(SpatialSimulation sim) { sim.Player.Cell = new(13, 6); Act(sim, Hand()); sim.Player.Cell = new(15, 2); }
        var sim = Sim(); sim.Player.Cell = new(15, 2);
        var before = sim.Save(); Check(!sim.ToggleNearestDoor() && !sim.Map.OpenDoors.Contains("door-7-5-u") && sim.Save() == before, "badge in spare pack cannot authorize entry");
        Equip(sim); sim.ManualPause = true; before = sim.Save();
        Check(!sim.ToggleNearestDoor() && sim.Save() == before, "paused presentation refuses without cost or opening");
        sim.ManualPause = false; Check(sim.ToggleNearestDoor(), sim.LastMessage);
        Check(sim.Inventory.Pending is { TotalMs: 500, CostAp: 2, Request.Kind: InventoryActionKind.PresentBadge }
            && !sim.Map.OpenDoors.Contains("door-7-5-u"), "held badge purchases real presentation time");
        sim.Tick(); var pending = sim.Save(); var clone = Sim(); clone.Restore(pending); Check(clone.Save() == pending, "pending reader use restores exactly");
        Finish(sim); Finish(clone); Check(sim.Save() == clone.Save() && sim.Map.OpenDoors.Contains("door-7-5-u"), "reader opens once after exact continuation");
        Check(sim.ToggleNearestDoor() && !sim.Map.OpenDoors.Contains("door-7-5-u"), "ordinary close remains possible");
        Act(sim, new(InventoryActionKind.Move, "personnel-badge", new("avatar-trousers", "cargo-left")));
        Check(!sim.ToggleNearestDoor(), "accessible pocket is not an equipped hand");
        Act(sim, Hand()); Act(sim, Clip()); Act(sim, new(InventoryActionKind.CloseGarment, "avatar-coat"));
        Check(!sim.ToggleNearestDoor(), "covered worn badge refuses");
        Act(sim, new(InventoryActionKind.OpenGarment, "avatar-coat"));
        Check(sim.ToggleNearestDoor() && sim.Inventory.Pending is null && sim.Map.OpenDoors.Contains("door-7-5-u"), "exposed worn badge needs no manual presentation");
        sim.ToggleNearestDoor(); Check(sim.RequestMove(new(14, 2)), "begin walking near reader"); sim.Tick();
        Check(sim.Map.OpenDoors.Contains("door-7-5-u"), "moving wearer automatically opens nearby reader");
        sim.ManualPause = false; MovementSystem.StopInPlace(sim.Player);
        Act(sim, Hand()); Check(sim.ToggleNearestDoor(), "close before interrupted presentation");
        Check(sim.ToggleNearestDoor(), "begin manual presentation"); sim.Tick();
        sim.Player.Cell = new(13, 2); Finish(sim);
        Check(!sim.Map.OpenDoors.Contains("door-7-5-u"), "moving during presentation aborts without opening");
        foreach (var source in new[] { Policy(2), Policy(powered: false), Policy(issuer: "other-site") })
        {
            var denied = Sim(source); Equip(denied); before = denied.Save();
            Check(!denied.ToggleNearestDoor() && denied.Save() == before && denied.Inventory.Pending is null, "insufficient clearance, power loss or wrong issuer cannot charge or open");
        }
        var forged = JsonNode.Parse(pending)!; forged["Inventory"]!["Pending"]!["Request"]!["TargetId"] = "door-4-3-u";
        before = clone.Save(); var rejected = false; try { clone.Restore(forged.ToJsonString()); } catch (ArgumentException) { rejected = true; }
        Check(rejected && clone.Save() == before, "invalid reader continuation rejects atomically");
        var tactical = Sim(); Equip(tactical); tactical.Actors.Single(a => a.Id == "tech").Cell = new(15, 1);
        tactical.Player.Facing = ObservationService.FacingToward(tactical.Player.Position, tactical.Actors.Single(a => a.Id == "tech").Position, 0);
        Check(tactical.ArmTraining() && tactical.Combat.Active, "reader fixture enters local turns");
        var combat = tactical.Combat; combat.Cursor = combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        for (var i = 0; i < combat.Roster.Count; i++) combat.Roster[i] = combat.Roster[i] with { Ap = combat.Roster[i].ActorId == tactical.Player.Id ? 10 : 0 };
        Check(tactical.ToggleNearestDoor() && combat.Pending is { Kind: CombatActionKind.Inventory, Cost: 2, TotalMs: 500 } && combat.For(tactical.Player.Id)!.Ap == 8,
            "manual tactical presentation shares paid inventory schedule");
        tactical.Tick(); var tacticalPending = tactical.Save(); var tacticalCopy = Sim(); tacticalCopy.Restore(tacticalPending);
        Finish(tactical); Finish(tacticalCopy); Check(tactical.Save() == tacticalCopy.Save() && tactical.Map.OpenDoors.Contains("door-7-5-u"), "tactical badge continuation stays synchronized");
        var changed = Sim(); Equip(changed); Check(changed.ToggleNearestDoor(), "start presentation before badge damage");
        changed.Inventory.Items["personnel-badge"] = changed.Inventory.Items["personnel-badge"] with { Condition = 0 };
        Finish(changed); Check(!changed.Map.OpenDoors.Contains("door-7-5-u"), "commit rechecks damaged badge");
        var displayedTurn = Sim(); Equip(displayedTurn); Act(displayedTurn, Clip());
        displayedTurn.Actors.Single(a => a.Id == "tech").Cell = new(15, 1);
        displayedTurn.Player.Facing = ObservationService.FacingToward(displayedTurn.Player.Position, displayedTurn.Actors.Single(a => a.Id == "tech").Position, 0);
        Check(displayedTurn.ArmTraining() && displayedTurn.Combat.Active, "displayed badge fixture enters combat");
        var roster = displayedTurn.Combat; roster.Cursor = roster.Roster.FindIndex(c => c.ActorId == displayedTurn.Player.Id);
        for (var i = 0; i < roster.Roster.Count; i++) roster.Roster[i] = roster.Roster[i] with { Ap = roster.Roster[i].ActorId == displayedTurn.Player.Id ? 10 : 0 };
        Check(displayedTurn.ToggleNearestDoor() && roster.Pending is { Kind: CombatActionKind.Door, Cost: 2, TotalMs: 300 }, "worn badge tactical door still buys normal AP/time");
        displayedTurn.Inventory.Items["avatar-coat"] = displayedTurn.Inventory.Items["avatar-coat"] with { GarmentOpen = false };
        Finish(displayedTurn); Check(!displayedTurn.Map.OpenDoors.Contains("door-7-5-u"), "tactical commitment rechecks visible wearing");
        // Shared service applies issuer/identity policy to any actor, independent of player-only handling UI.
        var world = new SimulationWorld(Policy(), operation); var badge = world.Inventory.Items["personnel-badge"];
        var defs = world.Inventory.Content!.Definitions; var index = Array.FindIndex(defs, d => d.Id == badge.DefinitionId);
        var original = defs[index]; defs[index] = original with { Credential = new("site-test", 1, "observer") };
        world.Inventory.Items[badge.Id] = badge with { Placement = new("person:avatar", "hand") }; world.Player.Cell = new(15, 2);
        var reader = world.Map.Definition.Walls.Single(w => w.Id == "door-7-5-u");
        Check(!DoorCredentialService.Authorize(world, world.Player, reader, out _, out _), "identity-bound credential rejects another person");
        defs[index] = original; Check(DoorCredentialService.Authorize(world, world.Player, reader, out var held, out _) && held?.Id == badge.Id, "bearer credential accepts physical holder");
        world.Inventory.Items[badge.Id] = world.Inventory.Items[badge.Id] with { Condition = 0 };
        Check(!DoorCredentialService.Authorize(world, world.Player, reader, out _, out _), "destroyed badge refuses");
        var observer = world.Actors.Single(a => a.Id == "observer"); observer.Cell = new(15, 2);
        defs[index] = original with { Credential = new("site-test", 1, observer.Id) };
        world.Inventory.Items[badge.Id] = badge with { Placement = new("person:observer", "hand") };
        Check(DoorCredentialService.Authorize(world, observer, reader, out var npcBadge, out _) && npcBadge?.Id == badge.Id,
            "the same held credential service accepts an authorized NPC");
        Check(!DoorCredentialService.Authorize(world, world.Player, reader, out _, out _), "NPC-held badge does not authorize player");
        defs[index] = original;
        Console.WriteLine($"BADGE_DOOR_CHECKS_PASS: {count} assertions; hand-only presentation, displayed/covered badges, policies, paid AP/time, interruption and exact continuation.");
        return count;
    }
}
