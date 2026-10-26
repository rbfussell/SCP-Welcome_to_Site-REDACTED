using SiteSimulation;
using System.Text.Json.Nodes;

internal static class InventoryPackingChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Packing: " + reason); passed++; }
        SpatialSimulation Sim() => new(map, config);
        InventoryRequest Store(string id, string owner, string region) => new(InventoryActionKind.AutoPlace, id, new(owner, region));
        InventoryRequest Arrange(string owner, string region) => new(InventoryActionKind.Arrange, owner, new(owner, region));
        void Finish(SpatialSimulation sim)
        {
            for (var i = 0; i < 2000 && sim.Inventory.Pending is not null; i++) sim.Tick();
            Check(sim.Inventory.Pending is null, "paid packing finishes in bounded simulation time");
        }
        var direct = Sim(); var before = direct.Save();
        var request = Store("gum-packet", "medical-case", "inside");
        var quote = direct.QuoteInventoryPacking(request);
        Check(quote.Allowed && quote.Plan is { Repacked: false, Moves.Length: 1 }, "container-target insertion uses existing space first");
        Check(quote.Plan!.Moves[0].To == new ItemPlacement("medical-case", "inside", 2, 0, true), "only available rotated slot is selected");
        Check(direct.Save() == before, "quoting a fit changes no state");
        Check(direct.HandleInventory(request), "start paid insertion");
        Check(direct.Inventory.Items["gum-packet"].Placement.ParentId == "avatar-trousers", "item stays at source during handling");
        direct.Tick(); var mid = direct.Save(); var copy = Sim(); copy.Restore(mid);
        Check(copy.Save() == mid, "exact pending layout survives restore");
        Finish(direct); Finish(copy);
        Check(direct.Save() == copy.Save(), "restored packing has identical clocks, graph and event results");
        Check(direct.Inventory.Items["gum-packet"].Placement == quote.Plan.Moves[0].To, "paid insertion commits quoted orientation and cell");
        Check(direct.Inventory.Items["gum-1"].Placement.ParentId == "gum-packet", "moving a container preserves nested identities");
        Check(direct.Inventory.Items["dressing-a"].Placement == new ItemPlacement("medical-case", "inside"), "direct fit never moves existing contents");
        var same = direct.Save();
        Check(!direct.QuoteInventoryPacking(request).Allowed && direct.Save() == same, "same-compartment AutoPlace cannot silently reorganize");
        Check(!direct.QuoteInventoryPacking(Store("medical-case", "medical-case", "inside")).Allowed, "self-containment rejected");
        Check(!direct.QuoteInventoryPacking(Store("avatar-backpack", "medical-case", "inside")).Allowed, "ancestor into descendant rejected");
        Check(!direct.QuoteInventoryPacking(Store("flashlight", "fixture:crate-a", "store")).Allowed, "distant fixture cannot receive automatic insertion");
        Check(!direct.QuoteInventoryPacking(Store("flashlight", "tech-backpack", "main")).Allowed, "another person's gear is not remotely writable");
        Check(!direct.QuoteInventoryPacking(Arrange("person:avatar", "hand")).Allowed, "body mounts are not square storage grids");
        Check(!direct.QuoteInventoryPacking(new(InventoryActionKind.AutoPlace, "flashlight", new("avatar-backpack", "main", 2))).Allowed, "AutoPlace cannot smuggle a manual anchor");
        var backpackDefs = direct.Inventory.Content!.Definitions;
        var backpackIndex = Array.FindIndex(backpackDefs, d => d.Id == "backpack");
        backpackDefs[backpackIndex] = backpackDefs[backpackIndex] with
        { Regions = backpackDefs[backpackIndex].Regions!.Select(r => r.Id == "main" ? r with { MaxMassGrams = 205 } : r).ToArray() };
        Check(direct.QuoteInventoryPacking(Store("gum-packet", "avatar-backpack", "main")).Allowed,
            "extracting contents into their outer compartment does not count their mass twice");
        var full = Sim().QuoteInventoryPacking(Store("flashlight", "gum-packet", "pieces"));
        Check(!full.Allowed, "fine packet cannot accept a large object");

        // Fragmented 4x4 region: every row/column occupied, so a 1x4 torch needs real rearrangement.
        var world = new SimulationWorld(map, config); var state = world.Inventory;
        var defs = state.Content!.Definitions;
        var index = Array.FindIndex(defs, d => d.Id == "trousers");
        defs[index] = defs[index] with { Regions = defs[index].Regions!.Select(r => r.Id == "left" ? r with { Rows = 4 } : r).ToArray() };
        state.Items["gum-packet"] = state.Items["gum-packet"] with { Placement = new("avatar-backpack", "main", 4, 5) };
        for (var n = 1; n <= 4; n++) state.Items[$"gum-{n}"] = state.Items[$"gum-{n}"] with { Placement = new("avatar-trousers", "left", n - 1, n - 1) };
        InventoryPlacement.Validate(state);
        var originals = state.Items.ToDictionary(p => p.Key, p => p.Value);
        var pack = InventoryPacking.Plan(state, "avatar-trousers", "left", "flashlight");
        Check(pack.Repacked && pack.Moves.Length > 1, "fragmentation triggers bounded repacking");
        var again = InventoryPacking.Plan(state, "avatar-trousers", "left", "flashlight");
        Check(InventoryPackingActions.Same(pack, again), "identical state produces a deterministic plan");
        Check(state.Items.All(p => p.Value == originals[p.Key]), "planning never mutates the live graph");
        Check(pack.Moves.All(m => m.To.ParentId == "avatar-trousers" && m.To.RegionId == "left"), "only destination compartment is repacked");
        Check(!InventoryActions.Validate(world, "avatar", new(InventoryActionKind.Move, "flashlight", new("avatar-trousers", "left")), out _, out _, out _), "failed exact grid drop does not fall back to rearrangement");
        Check(state.Items.All(p => p.Value == originals[p.Key]), "failed exact placement leaves every item and rotation unchanged");
        Check(InventoryActions.Validate(world, "avatar", Store("flashlight", "avatar-trousers", "left"), out _, out var ms, out var ap), "public packing request validates fragmented fixture");
        Check(ms > quote.DurationMs && ap > quote.Ap, "moving more items pays their handling and ancestor access costs");
        Check(InventoryActions.Start(world, Store("flashlight", "avatar-trousers", "left"), out _), "start fragmented multi-item packing");
        var revision = state.Revision;
        while (state.Pending!.RemainingMs > SpatialSimulation.TickMs)
        {
            InventoryActions.Advance(world);
            Check(state.Items.All(p => p.Value == originals[p.Key]), "no intermediate rearrangement leaks during paid work");
        }
        InventoryActions.Advance(world);
        Check(state.Pending is null && state.Revision == revision + 1, "all packing moves commit in one revision");
        Check(pack.Moves.All(m => state.Items[m.ItemId].Placement == m.To), "atomic commit applies the entire quoted layout");
        InventoryPlacement.Validate(state);
        try { InventoryPacking.Plan(state, "avatar-trousers", "left", null); throw new Exception("Repeated Arrange should be a no-op"); }
        catch (ArgumentException e) { Check(e.Message.Contains("Already arranged"), "canonical Arrange is idempotent"); }
        Check(state.Items["gum-5"] == originals["gum-5"], "unrelated nested contents stay untouched");
        // Area and shape are separate checks: a 1x4 bar has enough area in a 2x2 box, but cannot fit.
        state.Items["dressing-a"] = state.Items["dressing-a"] with { Placement = new("avatar-backpack", "main", 4, 0) };
        var caseIndex = Array.FindIndex(defs, d => d.Id == "case");
        defs[caseIndex] = defs[caseIndex] with { Regions = [defs[caseIndex].Regions![0] with { Columns = 2, Rows = 2 }] };
        try { InventoryPacking.Plan(state, "medical-case", "inside", "flashlight"); throw new Exception("Impossible shape accepted"); }
        catch (ArgumentException e) { Check(e.Message.Contains("shapes cannot fit"), "enough total cells does not imply geometric fit"); }
        defs[caseIndex] = defs[caseIndex] with { Regions = [defs[caseIndex].Regions![0] with { Columns = 1, Rows = 2 }] };
        try { InventoryPacking.Plan(state, "medical-case", "inside", "flashlight"); throw new Exception("Area overflow accepted"); }
        catch (ArgumentException e) { Check(e.Message.Contains("insufficient usable cells"), "area overflow rejects before searching"); }
        defs[caseIndex] = defs[caseIndex] with { Regions = [defs[caseIndex].Regions![0] with { Columns = 4, Rows = 4, Mask = [0, 2, 5, 7, 8, 10, 13, 15] }] };
        try { InventoryPacking.Plan(state, "medical-case", "inside", "flashlight"); throw new Exception("Masked shape accepted"); }
        catch (ArgumentException e) { Check(e.Message.Contains("shapes cannot fit"), "packing respects masked cells rather than their bounding rectangle"); }

        var changed = Sim(); Check(changed.HandleInventory(request), "start stale-plan fixture");
        var staleSource = changed.Inventory.Items["gum-packet"].Placement;
        changed.Inventory.Revision++; Finish(changed);
        Check(changed.Inventory.Items["gum-packet"].Placement == staleSource && changed.Inventory.Children("medical-case", "inside").Count == 1,
            "changed revision aborts whole plan without moving incoming or existing contents");
        var limitWorld = new SimulationWorld(map, config);
        var limitedDefs = limitWorld.Inventory.Content!.Definitions;
        var limitedCase = Array.FindIndex(limitedDefs, d => d.Id == "case");
        limitedDefs[limitedCase] = limitedDefs[limitedCase] with { Regions = [limitedDefs[limitedCase].Regions![0] with { MaxMassGrams = 45 }] };
        Check(!InventoryActions.Validate(limitWorld, "avatar", request, out var limitReason, out _, out _) && limitReason.Contains("load limit"), "geometric free space never bypasses recursive load limit");
        var closed = Sim();
        Check(closed.HandleInventory(new(InventoryActionKind.Close, "medical-case", RegionId: "inside")), "close destination fixture"); Finish(closed);
        Check(!closed.QuoteInventoryPacking(request).Allowed, "closed destination does not get opened for free");
        Check(closed.HandleInventory(new(InventoryActionKind.Close, "avatar-trousers", RegionId: "left")), "close source fixture"); Finish(closed);
        Check(!closed.QuoteInventoryPacking(Store("gum-packet", "avatar-backpack", "main")).Allowed, "closed source blocks automatic insertion");
        var held = Sim(); held.ManualPause = true; var heldSave = held.Save();
        Check(!held.HandleInventory(request) && held.Save() == heldSave, "pause does not grant free arranging");
        held.ManualPause = false; Check(held.HandleInventory(request), "start cancel fixture"); held.Tick();
        Check(held.CancelInventoryHandling() && held.Inventory.Items["gum-packet"].Placement.ParentId == "avatar-trousers", "cancel preserves source and entire destination layout");

        var combat = Sim(); combat.Player.Facing = 1; Check(combat.ArmTraining(), "enter tactical fixture");
        combat.Combat.Cursor = combat.Combat.Roster.FindIndex(c => c.ActorId == combat.Player.Id);
        combat.Combat.Roster[combat.Combat.Cursor] = combat.Combat.Roster[combat.Combat.Cursor] with { Ap = 10 };
        var tacticalQuote = combat.QuoteInventoryPacking(Store("flashlight", "avatar-backpack", "main"));
        Check(combat.HandleInventory(Store("flashlight", "avatar-backpack", "main")) && combat.Combat.For(combat.Player.Id)!.Ap == 10 - tacticalQuote.Ap, "insertion charges actual handling AP at commitment");
        Finish(combat); var stopped = combat.LocalMs; for (var i = 0; i < 10; i++) combat.Tick();
        Check(combat.LocalMs == stopped, "tactical input wait resumes after paid packing");
        combat.Combat.Roster[combat.Combat.Cursor] = combat.Combat.Roster[combat.Combat.Cursor] with { Ap = 1 }; var noAp = combat.Save();
        Check(!combat.HandleInventory(request) && combat.Save() == noAp, "insufficient AP rejects without rearranging or partial charging");

        void Reject(Action<JsonNode> mutate)
        {
            var victim = Sim(); var safe = victim.Save(); var bad = JsonNode.Parse(mid)!; mutate(bad);
            try { victim.Restore(bad.ToJsonString()); throw new Exception("Forged packing accepted"); }
            catch (ArgumentException) { Check(victim.Save() == safe, "invalid plan restore is atomic"); }
        }
        Reject(n => n["Inventory"]!["Pending"]!["Packing"]!["Moves"] = null);
        Reject(n => n["Inventory"]!["Pending"]!["Packing"]!["Moves"]![0]!["To"]!["X"] = 0);
        Reject(n => n["Inventory"]!["Pending"]!["Packing"]!["Moves"]![0]!["ItemId"] = "dressing-a");
        Reject(n => n["Inventory"]!["Pending"]!["Packing"]!["Moves"]![0] = null);
        Reject(n => n["Inventory"]!["Pending"]!["Packing"]!["Repacked"] = true);
        Reject(n => n["Inventory"]!["Pending"]!["CostAp"] = 1);
        Reject(n => n["Inventory"]!["Pending"]!["Packing"] = null);
        Console.WriteLine($"INVENTORY_PACKING_CHECKS_PASS: {passed} assertions.");
        return passed;
    }
}
