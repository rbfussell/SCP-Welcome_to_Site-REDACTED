using SiteSimulation;
using System.Text.Json.Nodes;

internal static class GroundChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Ground: " + reason); passed++; }
        SpatialSimulation Sim() => new(map, config);
        InventoryItem Item(SpatialSimulation s, string id) => s.Inventory.DiagnosticItems.Single(i => i.Id == id);
        void Finish(SpatialSimulation s)
        { for (var i = 0; i < 1000 && s.Inventory.Pending is not null; i++) s.Tick(); Check(s.Inventory.Pending is null, "handling did not finish"); }
        void Act(SpatialSimulation s, InventoryRequest r) { Check(s.HandleInventory(r), s.LastMessage); Finish(s); }
        InventoryRequest At(SpatialSimulation s, string id, int x = 0, int y = 0, bool rotated = false)
            => new(InventoryActionKind.Move, id, new(s.InventoryGroundRoot!.Id, InventoryGround.RegionId, x, y, rotated));
        InventoryRequest Wear(string id, string slot) => new(InventoryActionKind.Move, id, new("person:avatar", slot));
        var sim = Sim(); var root = sim.InventoryGroundRoot!; var empty = sim.Save();
        Check(root.GroundCell == Hex.FromWorld(sim.Player.Position) && root.ActorId is null && root.PropId is null, "floor must be a world location");
        Check(root.Regions[0] is { Sealable: false, Columns: 10, Rows: 10, CellMm: 50 }, "floor grid uses its declared physical scale");
        for (var i = 0; i < 10; i++) _ = sim.InventoryGroundRoot;
        Check(sim.Save() == empty && sim.Inventory.Content!.Roots.Length == 4, "empty-floor queries must not mutate or preallocate map roots");
        Check(sim.Inventory.FindRoot("ground:0001:2") is null && sim.Inventory.FindRoot("ground:999:999") is null, "ground IDs must be canonical and map bounded");
        var total = InventoryHierarchy.CarriedMass(sim.Inventory, "person:avatar");
        var request = At(sim, "avatar-backpack"); var quote = sim.QuoteInventory(request);
        Check(quote.Allowed && quote.Ap == sim.Inventory.Definition("avatar-backpack").HandleAp + 2 && quote.DurationMs > 0, "floor drop must quote normal handling and floor access AP");
        sim.ManualPause = true; var held = sim.Save();
        Check(!sim.HandleInventory(request) && sim.Save() == held, "pause must not permit free dropping");
        sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage);
        Check(Item(sim, "avatar-backpack").Placement.RegionId == "back" && sim.Inventory.Pending!.Request.Destination!.GroundOrder == 1,
            "paid work must retain the worn source and save its resolved pile order");
        var mid = sim.Save(); var clone = Sim(); clone.Restore(mid); Check(clone.Save() == mid, "pending ground move must restore exactly");
        Finish(sim); Finish(clone); Check(sim.Save() == clone.Save(), "resumed ground move diverged");
        Check(Item(sim, "medical-case").Placement.ParentId == "avatar-backpack", "dropping a full pack must retain its hierarchy");
        Act(sim, At(sim, "avatar-coat")); Act(sim, At(sim, "avatar-shirt"));
        Check(sim.InventoryContents(root.Id, InventoryGround.RegionId).Select(i => i.Id).SequenceEqual(new[] { "avatar-backpack", "avatar-coat", "avatar-shirt" }), "floor contents must draw in physical pile order");
        Check(!sim.InventoryMoveAccess("avatar-coat").Allowed && !sim.InventoryInspectAccess("avatar-backpack").Allowed, "covered items must be inaccessible");
        Check(!sim.InventoryAccess("avatar-backpack", "main").Allowed && sim.InventoryContents("avatar-backpack", "main").Count == 0,
            "a buried bag must not expose its contents");
        held = sim.Save();
        Check(!sim.HandleInventory(Wear("avatar-coat", "outerwear")) && sim.Save() == held, "cannot pull clothes through covering items");
        Check(!sim.QuoteInventory(new(InventoryActionKind.AutoPlace, "medical-case", new("person:avatar", "hand"))).Allowed, "packing must not bypass a buried ancestor");
        Check(!sim.QuoteInventory(new(InventoryActionKind.Arrange, root.Id, new(root.Id, InventoryGround.RegionId))).Allowed, "Arrange must not reshuffle floor piles");
        var floorMass = sim.InventoryContents(root.Id, InventoryGround.RegionId).Sum(i => InventoryHierarchy.Mass(sim.Inventory, i.Id));
        Check(InventoryHierarchy.CarriedMass(sim.Inventory, "person:avatar") + floorMass == total, "dropping loaded gear must conserve mass exactly once");
        var cell = root.GroundCell!.Value;
        var other = sim.Map.Cells.First(c => sim.Map.CanTraverse(cell, c) && sim.Actors.All(a => a.Cell != c));
        sim.Player.Cell = other;
        Check(sim.InventoryGroundRoot!.Id != root.Id && sim.InventoryContents(sim.InventoryGroundRoot.Id, InventoryGround.RegionId).Count == 0, "moving must show a different empty floor");
        Check(!sim.InventoryAccess(root.Id, InventoryGround.RegionId).Allowed && sim.InventoryContents(root.Id, InventoryGround.RegionId).Count == 0, "remote floor items must not be accessible");
        Check(!sim.QuoteInventory(Wear("avatar-shirt", "torso")).Allowed, "cannot retrieve gear from another tile");
        sim.Player.Cell = cell;
        Check(sim.InventoryContents(root.Id, InventoryGround.RegionId).Count == 3, "floor items must stay where dropped");
        Act(sim, Wear("avatar-shirt", "torso")); Check(Item(sim, "avatar-shirt").Placement.GroundOrder is null, "equipping must clear floor metadata");
        Check(sim.InventoryMoveAccess("avatar-coat").Allowed && !sim.InventoryMoveAccess("avatar-backpack").Allowed, "unstacking must expose only the next accessible item");
        Act(sim, Wear("avatar-coat", "outerwear")); Act(sim, Wear("avatar-backpack", "back"));
        Check(InventoryHierarchy.CarriedMass(sim.Inventory, "person:avatar") == total && sim.InventoryContents(root.Id, InventoryGround.RegionId).Count == 0,
            "dress again without a staging hand, lost items or duplicated mass");

        // Real pickup interruption, top-item repositioning and exact stacking validation.
        Act(sim, At(sim, "avatar-backpack")); Act(sim, At(sim, "avatar-coat"));
        Act(sim, At(sim, "avatar-coat", 2, 1, true));
        Check(Item(sim, "avatar-coat").Placement is { X: 2, Y: 1, Rotated: true }, "floor reposition/rotation must retain exact cells");
        var invalid = At(sim, "avatar-coat", 9, 9); held = sim.Save();
        Check(!sim.HandleInventory(invalid) && sim.Save() == held, "out-of-bounds floor placement must not spill or arrange items");
        var safe = sim.Save();
        void Reject(Action<JsonNode> edit)
        {
            var node = JsonNode.Parse(safe)!; edit(node);
            try { sim.Restore(node.ToJsonString()); throw new Exception("Malformed ground save accepted"); }
            catch (ArgumentException) { Check(sim.Save() == safe, "invalid ground restore must be atomic"); }
        }
        JsonNode Place(JsonNode n, string id) => n["Inventory"]!["Items"]!.AsArray().Single(i => i!["Id"]!.GetValue<string>() == id)!["Placement"]!;
        Reject(n => Place(n, "avatar-coat")["GroundOrder"] = null);
        Reject(n => Place(n, "avatar-coat")["GroundOrder"] = -1);
        Reject(n => Place(n, "avatar-coat")["GroundOrder"] = 1_000_000_001);
        Reject(n => Place(n, "avatar-coat")["GroundOrder"] = Item(sim, "avatar-backpack").Placement.GroundOrder);
        Reject(n => Place(n, "avatar-shirt")["GroundOrder"] = 4);
        Reject(n => Place(n, "avatar-coat")["ParentId"] = "ground:999:999");
        Reject(n => Place(n, "avatar-coat")["RegionId"] = "missing");
        Reject(n => n["Version"] = 14);
        var legacy = Sim(); var legacyJson = JsonNode.Parse(legacy.Save())!; legacyJson["Version"] = 14;
        legacy.Restore(legacyJson.ToJsonString());
        Check(JsonNode.Parse(legacy.Save())!["Version"]!.GetValue<int>() == 15 && legacy.InventoryGroundRoot is not null,
            "matching schema-14 saves must migrate to empty ground support");
        var legacySafe = legacy.Save(); var brokenLegacy = JsonNode.Parse(mid)!;
        brokenLegacy["Version"] = 14; brokenLegacy["Inventory"]!["Pending"]!["Request"] = null;
        try { legacy.Restore(brokenLegacy.ToJsonString()); throw new Exception("Null legacy handling request accepted"); }
        catch (ArgumentException) { Check(legacy.Save() == legacySafe, "malformed legacy work must reject atomically"); }
        var continuation = Sim(); continuation.Restore(mid); var malformed = JsonNode.Parse(mid)!;
        malformed["Inventory"]!["Pending"]!["Request"]!["Destination"]!["GroundOrder"] = 2;
        try { continuation.Restore(malformed.ToJsonString()); throw new Exception("Noncanonical pending floor order accepted"); }
        catch (ArgumentException) { Check(continuation.Save() == mid, "invalid paid floor order must reject atomically"); }

        void RejectPile(bool heavy)
        {
            var world = new SimulationWorld(map, config); var state = world.Inventory;
            var floor = InventoryGround.Id(Hex.FromWorld(world.Player.Position)); var ids = new[] { "flashlight", "gum-packet" };
            for (var i = 0; i < ids.Length; i++)
            {
                var item = state.Items[ids[i]]; var index = Array.FindIndex(state.Content!.Definitions, d => d.Id == item.DefinitionId);
                var d = state.Content.Definitions[index];
                state.Content.Definitions[index] = d with { MassGrams = heavy ? 60000 : d.MassGrams, ThicknessMm = heavy ? 25 : 600 };
                state.Items[item.Id] = item with { Placement = new(floor, InventoryGround.RegionId, GroundOrder: i + 1) };
            }
            try { InventoryPlacement.Validate(state); throw new Exception("Unbounded floor pile accepted"); }
            catch (ArgumentException ex) { Check(ex.Message.Contains(heavy ? "load limit" : "too high"), "pile must enforce declared mass/height independently of icon size"); }
        }
        RejectPile(false); RejectPile(true);
        var interruption = Sim(); Check(interruption.HandleInventory(At(interruption, "avatar-backpack")), "start interrupted drop");
        interruption.Player.Cell = interruption.Map.Cells.First(c => interruption.Map.CanTraverse(interruption.Player.Cell, c) && interruption.Actors.All(a => a.Cell != c));
        Finish(interruption); Check(Item(interruption, "avatar-backpack").Placement.RegionId == "back", "moving away mid-action must leave source intact");

        var tactical = Sim(); tactical.Player.Facing = 1; tactical.ArmTraining();
        tactical.Combat.Cursor = tactical.Combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        var action = At(tactical, "avatar-backpack"); var cost = tactical.QuoteInventory(action).Ap;
        tactical.Combat.Roster[tactical.Combat.Cursor] = tactical.Combat.Roster[tactical.Combat.Cursor] with { Ap = cost - 1 };
        held = tactical.Save(); Check(!tactical.HandleInventory(action) && tactical.Save() == held, "insufficient tactical AP must not drop gear");
        tactical.Combat.Roster[tactical.Combat.Cursor] = tactical.Combat.Roster[tactical.Combat.Cursor] with { Ap = cost };
        Check(tactical.HandleInventory(action) && tactical.Combat.For(tactical.Player.Id)!.Ap == 0, "floor drop must spend tactical AP exactly once");
        var combatMid = tactical.Save(); var combatClone = Sim(); combatClone.Restore(combatMid); Check(combatClone.Save() == combatMid, "tactical floor continuation must restore exactly");
        Finish(tactical); Check(Item(tactical, "avatar-backpack").Placement.GroundOrder is not null, "paid combat drop did not commit");
        Console.WriteLine($"GROUND_CHECKS_PASS: {passed} assertions; world floor identity, ordered piles, blocking ancestors, clothes staging, mass, time/AP, interruption, legacy migration and atomic restore.");
        return passed;
    }
}
