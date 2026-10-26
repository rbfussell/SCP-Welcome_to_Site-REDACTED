using SiteSimulation;
using System.Text.Json.Nodes;

internal static class InventorySecuringChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool value, string reason) { if (!value) throw new Exception("Inventory securing: " + reason); passed++; }
        SpatialSimulation Sim() => new(map, config);
        InventoryRequest Close(string id, string region) => new(InventoryActionKind.Close, id, RegionId: region);
        var parent = Close("avatar-backpack", "main"); var child = Close("medical-case", "inside");
        void Finish(SpatialSimulation s)
        {
            for (var i = 0; i < 500 && (s.Inventory.Pending is not null || s.Inventory.SecuringRemaining != 0); i++) s.Tick();
            Check(s.Inventory.Pending is null && s.Inventory.SecuringRemaining == 0, "bounded realtime sequence did not finish");
        }
        var sim = Sim(); sim.ManualPause = true;
        Check(sim.SecureInventory([parent, child]), "valid child/parent closures should prepare during pause");
        Check(sim.Inventory.IsOpen("avatar-backpack", "main") && sim.Inventory.IsOpen("medical-case", "inside") && sim.Inventory.Pending is null,
            "preparing a close intent must not close anything or purchase work while paused");
        var saved = sim.Save(); var clone = Sim(); clone.Restore(saved);
        Check(clone.Save() == saved, "paused securing queue must round-trip exactly");
        var canceledPause = Sim(); canceledPause.Restore(saved);
        Check(canceledPause.CancelInventoryHandling() && canceledPause.Inventory.SecuringRemaining == 0 && canceledPause.ManualPause
            && canceledPause.Inventory.IsOpen("avatar-backpack", "main"), "unstarted securing intent can be canceled while paused without physical work");
        for (var i = 0; i < 20; i++) sim.Tick(); Check(sim.Save() == saved, "manual pause freezes prepared closures");
        sim.ManualPause = clone.ManualPause = false; sim.Tick(); clone.Tick();
        Check(sim.Inventory.Pending?.Request == child && sim.Inventory.SecuringRemaining == 1, "nested closure must purchase work before its parent regardless of request order");
        var midway = sim.Save(); var midClone = Sim(); midClone.Restore(midway);
        Check(midClone.Save() == midway, "active work plus remaining queue restores as one continuation");
        for (var i = 0; i < 100; i++) { sim.Tick(); clone.Tick(); midClone.Tick(); }
        Check(sim.Save() == clone.Save() && sim.Save() == midClone.Save(), "restored securing must match uninterrupted simulation exactly");
        Check(!sim.Inventory.IsOpen("medical-case", "inside") && !sim.Inventory.IsOpen("avatar-backpack", "main") && sim.Inventory.SecuringRemaining == 0,
            "children and parent must really be secured at completion");
        Check(sim.Events.Entries.Count(e => e.Text == "Inventory: " + sim.Inventory.Definition("medical-case").Name + ": Close completed.") == 1,
            "a persisted sequence must not duplicate completed work");
        var invalid = Sim(); var before = invalid.Save();
        Check(!invalid.SecureInventory([new(InventoryActionKind.Move, "flashlight", new("person:avatar", "hand"))]) && invalid.Save() == before,
            "secure intent cannot inject a move");
        Check(!invalid.SecureInventory([Close("medical-case", "missing")]) && invalid.Save() == before, "unknown closure must reject atomically");
        Check(!invalid.SecureInventory([Close("tech-backpack", "main")]) && invalid.Save() == before, "securing cannot reach another employee's pack");
        var canceled = Sim(); Check(canceled.SecureInventory([parent, child]), "cancel fixture prepares");
        canceled.Tick(); Check(canceled.CancelInventoryHandling(), "explicit cancel stops automatic work");
        Finish(canceled);
        Check(canceled.Inventory.IsOpen("avatar-backpack", "main") && canceled.Inventory.IsOpen("medical-case", "inside"), "canceling realtime work must not manufacture closures");
        var overrideSim = Sim(); overrideSim.SecureInventory([parent]);
        Check(overrideSim.HandleInventory(new(InventoryActionKind.Move, "flashlight", new("person:avatar", "hand"))) && overrideSim.Inventory.SecuringRemaining == 0,
            "a successful explicit action replaces unstarted automatic work");
        var changed = Sim(); changed.SecureInventory([child, parent]);
        var item = changed.Inventory.Items["medical-case"];
        changed.Inventory.Items[item.Id] = item with { Placement = item.Placement with { Y = 4 } };
        changed.Tick();
        Check(changed.Inventory.SecuringRemaining == 0 && changed.Inventory.Pending is null && changed.Inventory.IsOpen("avatar-backpack", "main"),
            "a moved inspected object must abort the remaining sequence safely");
        var covered = Sim(); covered.HandleInventory(new(InventoryActionKind.CloseGarment, "avatar-coat")); Finish(covered);
        var coveredSave = covered.Save();
        Check(!covered.SecureInventory([Close("avatar-trousers", "left")]) && covered.Save() == coveredSave,
            "closing a covered pocket requires physical access just like opening it");
        var tactical = Sim(); tactical.Player.Facing = 1;
        Check(tactical.ArmTraining() && tactical.Combat.Active, "tactical fixture enters combat");
        tactical.Combat.Cursor = tactical.Combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        var cost = tactical.QuoteInventory(child).Ap;
        tactical.Combat.Roster[tactical.Combat.Cursor] = tactical.Combat.Roster[tactical.Combat.Cursor] with { Ap = cost };
        Check(tactical.SecureInventory([parent, child]), "prepare tactical sequence"); tactical.Tick();
        Check(tactical.Combat.For(tactical.Player.Id)!.Ap == 0 && tactical.Inventory.Pending?.Request == child, "first closure charges its full normal AP once");
        for (var i = 0; i < 80; i++) tactical.Tick();
        var local = tactical.LocalMs; var world = tactical.WorldMs;
        var waiting = tactical.Save(); var waitClone = Sim(); waitClone.Restore(waiting);
        Check(waitClone.Save() == waiting, "queue waiting for AP persists without buying unpaid local time");
        Check(waitClone.CancelInventoryHandling() && waitClone.Inventory.SecuringRemaining == 0 && waitClone.Combat.For(waitClone.Player.Id)!.Ap == 0
            && waitClone.Inventory.IsOpen("avatar-backpack", "main"), "waiting close intent can be canceled in combat without refunding AP or closing a lid");
        for (var i = 0; i < 20; i++) tactical.Tick();
        Check(tactical.Inventory.Pending is null && tactical.Inventory.SecuringRemaining == 1 && tactical.LocalMs == local && tactical.WorldMs > world
            && tactical.Inventory.IsOpen("avatar-backpack", "main"), "insufficient AP must wait without auto-ending the turn, advancing local hazards or securing for free");
        Check(tactical.EndTurn(), "player may explicitly end turn while securing waits");
        for (var i = 0; i < 1500 && (tactical.Inventory.SecuringRemaining != 0 || tactical.Inventory.Pending is not null); i++) tactical.Tick();
        Check(!tactical.Inventory.IsOpen("avatar-backpack", "main") && tactical.Inventory.SecuringRemaining == 0, "normal next-turn AP allows the remaining closure");
        var corrupt = JsonNode.Parse(saved)!;
        corrupt["Inventory"]!["SecureQueue"]![0]!["Request"]!["Kind"] = (int)InventoryActionKind.Open;
        var protectedSim = Sim(); var protectedSave = protectedSim.Save();
        try { protectedSim.Restore(corrupt.ToJsonString()); throw new Exception("Injected open intent accepted"); }
        catch (ArgumentException) { Check(protectedSim.Save() == protectedSave, "malformed queue rejects without partially restoring world state"); }
        var legacy = JsonNode.Parse(protectedSave)!; legacy["Version"] = 8; legacy["Inventory"]!.AsObject().Remove("SecureQueue");
        protectedSim.Restore(legacy.ToJsonString()); Check(protectedSim.Inventory.SecuringRemaining == 0, "matching schema-8 inventory upgrades to an empty securing queue");
        Console.WriteLine($"INVENTORY_SECURING_CHECKS_PASS: {passed} assertions; child-first closures, time/AP, pause, interruptions, explicit override and persistence.");
        return passed;
    }
}
