using SiteSimulation;
using System.Text.Json.Nodes;

internal static class WornOrderChecks
{
    public static int Run(string map, string config)
    {
        var count = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Worn order: " + reason); count++; }
        SpatialSimulation Sim() { var s = new SpatialSimulation(map, config); s.Player.Cell = new(13, 6); return s; }
        InventoryRequest Move(string id, string region, string parent = "person:avatar", int x = 0, int y = 0)
            => new(InventoryActionKind.Move, id, new(parent, region, x, y));
        void Finish(SpatialSimulation s)
        { for (var i = 0; i < 2000 && s.Inventory.Pending is not null; i++) s.Tick(); Check(s.Inventory.Pending is null, "paid work resolves"); }
        void Act(SpatialSimulation s, InventoryRequest request) { Check(s.HandleInventory(request), s.LastMessage); Finish(s); }
        var sim = Sim(); var inv = sim.Inventory;
        var shirt = inv.Items["avatar-shirt"]; var pack = inv.Items["avatar-backpack"];
        Check(InventoryWornOrder.Layers(inv, shirt.Id).All(l => l.Layer == 0), "shirt is on skin despite authored layer 1");
        Check(InventoryWornOrder.Layers(inv, pack.Id).Max(l => l.Layer) == 2, "pack is third on the chest, independent of its authored 5");
        Check(InventoryWornOrder.Layers(inv, "avatar-shoes").All(l => l.Layer is 1 or 2), "local foot layers include physical trouser overlap while ignoring chest clothing");
        var before = sim.Save();
        var row = sim.InventoryEquipment.Single(e => e.ItemId == shirt.Id && e.Group == "Chest");
        Check(row.Layers.All(l => l.Layer == 0) && !row.Access.Allowed && sim.Save() == before,
            "own known equipment projection gives actual layers without granting manipulation or changing state");
        Check(!sim.InventoryEquipment.Any(e => e.ItemId == "gum-packet"), "equipment list cannot reveal nested contents");
        // Put the pack aside, undress the jacket and re-dress in the opposite valid order through normal paid actions.
        Act(sim, Move(pack.Id, "store", "fixture:crate-a", 0, 8));
        Act(sim, Move("avatar-coat", "hand"));
        Act(sim, new(InventoryActionKind.AutoPlace, shirt.Id, new("fixture:crate-a", "store")));
        Check(inv.Items[shirt.Id].Placement.WornOrder is null, "taking gear off clears its worn-only order");
        Act(sim, Move("avatar-coat", "outerwear"));
        Check(sim.HandleInventory(Move(shirt.Id, "torso")), sim.LastMessage);
        var pending = sim.Save(); var held = inv.Items[shirt.Id].Placement;
        Check(held.ParentId == "fixture:crate-a" && sim.Inventory.Pending!.Request.Destination!.WornOrder is > 0,
            "a paid dress action records its resolved order but leaves the source unchanged");
        var clone = Sim(); clone.Restore(pending); Check(clone.Save() == pending, "pending dressing restores exactly");
        Finish(sim); Finish(clone); Check(sim.Save() == clone.Save(), "restored dressing commits identical order and timing");
        Check(InventoryWornOrder.Order(inv.Items[shirt.Id]) > InventoryWornOrder.Order(inv.Items["avatar-coat"])
            && InventoryWornOrder.Layers(inv, shirt.Id).All(l => l.Layer == 1), "shirt can now be worn above the light jacket");
        Check(!sim.QuoteInventory(Move("avatar-coat", "hand")).Allowed
            && !sim.QuoteInventory(new(InventoryActionKind.CloseGarment, "avatar-coat")).Allowed,
            "the real outer shirt blocks jacket removal and access to its fastening");
        Check(sim.InventoryPocketCoverage("avatar-coat", "chest")?.Covered == true, "pocket cover follows the reversed actual order");
        before = sim.Save();
        var forged = Move(shirt.Id, "torso") with { Destination = inv.Items[shirt.Id].Placement with { WornOrder = 1 } };
        Check(!sim.HandleInventory(forged) && sim.Save() == before, "an explicit request cannot reorder a worn garment for free");
        var coatIndex = Array.FindIndex(inv.Content!.Definitions, d => d.Id == "coat");
        inv.Content.Definitions[coatIndex] = inv.Content.Definitions[coatIndex] with { Wear = inv.Content.Definitions[coatIndex].Wear! with { Layer = 9 } };
        Check(InventoryWornOrder.Layers(inv, shirt.Id).All(l => l.Layer == 1) && !sim.InventoryMoveAccess("avatar-coat").Allowed,
            "editing a legacy authored rank cannot change actual order, display or removal restrictions");
        Act(sim, Move(shirt.Id, "hand"));
        Check(InventoryWornOrder.Layers(inv, "avatar-coat").Where(l => l.Region.StartsWith("torso")).All(l => l.Layer == 0),
            "removing the outer garment leaves no gaps in local layers");
        // A pack over just a shirt has depth 1, and an attempted insertion underneath it is not an equip shortcut.
        sim = Sim(); inv = sim.Inventory;
        Act(sim, Move("avatar-backpack", "store", "fixture:crate-a", 0, 8));
        Act(sim, Move("avatar-coat", "hand"));
        Act(sim, Move("avatar-backpack", "back"));
        Check(InventoryWornOrder.Layers(inv, "avatar-backpack").Max(l => l.Layer) == 1, "outer pack fits at actual depth 1 over one shirt");
        before = sim.Save();
        Check(!sim.HandleInventory(Move("avatar-coat", "outerwear")) && sim.Save() == before,
            "a pack must be removed before dressing another overlapping garment");
        void Reject(string saved, Action<JsonNode> change)
        {
            var victim = Sim(); var safe = victim.Save(); var json = JsonNode.Parse(saved)!; change(json);
            try { victim.Restore(json.ToJsonString()); throw new Exception("Invalid worn save accepted"); }
            catch (ArgumentException) { Check(victim.Save() == safe, "invalid worn save rejects atomically"); }
        }
        JsonNode Item(JsonNode n, string id) => n["Inventory"]!["Items"]!.AsArray().Single(i => i!["Id"]!.GetValue<string>() == id)!;
        Reject(before, n => Item(n, "avatar-shirt")["Placement"]!["WornOrder"] = null);
        Reject(before, n => Item(n, "avatar-backpack")["Placement"]!["WornOrder"] = Item(n, "avatar-shirt")["Placement"]!["WornOrder"]!.DeepClone());
        Reject(before, n => Item(n, "avatar-shirt")["Placement"]!["WornOrder"] = -1);
        Reject(before, n => Item(n, "flashlight")["Placement"]!["WornOrder"] = 7);
        Reject(before, n => n["Version"] = 13);
        Reject(pending, n => n["Inventory"]!["Pending"]!["Request"]!["Destination"]!["WornOrder"] = null);
        Reject(pending, n => n["Inventory"]!["Pending"]!["Request"]!["Destination"]!["WornOrder"] = 1);
        Console.WriteLine($"WORN_ORDER_CHECKS_PASS: {count} assertions; actual dressing, local primary layers, reversed cover, canonical work and atomic persistence.");
        return count;
    }
}
