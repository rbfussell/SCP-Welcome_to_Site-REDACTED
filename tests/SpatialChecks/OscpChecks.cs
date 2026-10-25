using SiteSimulation;
using System.Text.Json.Nodes;

internal static class OscpChecks
{
    public static int Run(string map, string operation, string applications)
    {
        var passed = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("OSCP: " + message); passed++; }
        var apps = new OscpApplications(applications);
        Check(apps.Find("mail", OscpDevice.Phone)?.Id == "foundation:communications", "aliases resolve registered field apps");
        Check(apps.Find("systems", OscpDevice.Phone) is null && apps.Find("systems", OscpDevice.Workstation) is not null,
            "phone profile cannot inherit workstation-only applications");
        var badApps = JsonNode.Parse(applications)!;
        badApps["applications"]![1]!["aliases"]![0] = "status";
        try { _ = new OscpApplications(badApps.ToJsonString()); throw new Exception("duplicate app alias accepted"); }
        catch (ArgumentException) { Check(true, "ambiguous application aliases reject"); }
        var sim = new SpatialSimulation(map, operation);
        Check(sim.OscpAccess(OscpDevice.Phone).Available && !sim.OscpAccess(OscpDevice.Workstation).Available,
            "issued phone is available while full terminal still requires physical access");
        var before = sim.Save();
        Check(!sim.IssueOscpOrder(OscpDevice.Workstation, "tech", PersonnelTask.Post, new(8, 4)) && sim.Save() == before,
            "selecting a workstation profile remotely grants no access");
        Check(sim.IssueOscpOrder(OscpDevice.Phone, "tech", PersonnelTask.Post, new(8, 4))
            && sim.Operations.OrderHistory.Last().Status == OrderStatus.Pending && sim.Operations.OrderHistory.Last().Channel == "phone",
            "handset submits an ordinary pending intent through its own channel");
        var pending = sim.Save(); var clone = new SpatialSimulation(map, operation); clone.Restore(pending);
        Check(clone.Save() == pending, "handset/cache and pending phone order persist together");
        sim.Operations.NetworkOnline = false; clone.Operations.NetworkOnline = false;
        for (var i = 0; i < 4; i++) { sim.Tick(); clone.Tick(); }
        Check(sim.Operations.OrderHistory.Last().Status == OrderStatus.Pending && sim.Save() == clone.Save(),
            "offline pending order cannot relieve staff and its restored execution stays equivalent");
        sim.Operations.NetworkOnline = true; sim.Tick();
        Check(sim.Operations.OrderHistory.Last().Status != OrderStatus.Pending && sim.OscpRecords.Any(r => r.Text.Contains("Acknowledged")),
            "restoring the site link produces actual acknowledgement and acquired records");
        var cached = sim.OscpRecords.ToArray(); sim.Operations.NetworkOnline = false;
        before = sim.Save();
        Check(sim.OscpAccess(OscpDevice.Phone).Available && !sim.OscpAccess(OscpDevice.Phone).Connected,
            "powered offline handset can open cached views");
        Check(!sim.IssueOscpOrder(OscpDevice.Phone, "tech", PersonnelTask.Follow, sim.Player.Cell)
            && sim.Save() == before && cached.SequenceEqual(sim.OscpRecords), "offline rejection cannot submit or erase cached knowledge");
        sim.Operations.NetworkOnline = true;
        sim.Operations.Phone = sim.Operations.Phone! with { Powered = false };
        before = sim.Save();
        Check(!sim.OscpAccess(OscpDevice.Phone).Available && !sim.IssueOscpOrder(OscpDevice.Phone, "tech", PersonnelTask.Follow, sim.Player.Cell)
            && sim.Save() == before, "unpowered handset provides no control");
        sim.Operations.Phone = sim.Operations.Phone with { Powered = true, Accessible = false };
        Check(!sim.OscpAccess(OscpDevice.Phone).Available, "inaccessible handset provides no control");
        sim.Operations.Phone = sim.Operations.Phone with { Accessible = true };
        sim.LocalHold = true; before = sim.Save();
        Check(!sim.IssueOscpOrder(OscpDevice.Phone, "tech", PersonnelTask.Follow, sim.Player.Cell) && sim.Save() == before,
            "phone cannot bypass local action holds");
        sim.LocalHold = false;
        before = sim.Save();
        for (var i = 0; i < 5; i++) { _ = sim.OscpAccess(OscpDevice.Phone); _ = sim.OscpRecords; }
        Check(sim.Save() == before, "device and acquired-record queries consume no ticks or world mutations");
        var planned = new SpatialSimulation(map, operation); planned.ManualPause = true;
        var points = planned.Actors.Select(a => a.Position).ToArray(); var time = planned.WorldMs;
        Check(planned.SubmitPreparedPhoneOrder("tech", PersonnelTask.Post, new(8, 4))
            && planned.Operations.OrderHistory.Last().Status == OrderStatus.Pending, "prepared phone close can record a pending intent during a pre-existing manual pause");
        planned.Tick();
        Check(planned.WorldMs == time && points.SequenceEqual(planned.Actors.Select(a => a.Position))
            && planned.Operations.OrderHistory.Last().Status == OrderStatus.Pending, "prepared intent advances neither clocks nor staff while paused");
        planned.LocalHold = true; var heldPlan = planned.Save();
        Check(!planned.SubmitPreparedPhoneOrder("tech", PersonnelTask.Follow, planned.Player.Cell) && planned.Save() == heldPlan,
            "prepared intent cannot bypass local tactical/diagnostic holds");
        var invalid = JsonNode.Parse(before)!;
        invalid["Operations"]!["Phone"]!["ActorId"] = "tech";
        try { sim.Restore(invalid.ToJsonString()); throw new Exception("wrong device owner accepted"); }
        catch (ArgumentException) { Check(sim.Save() == before, "invalid device restore is atomic"); }
        invalid = JsonNode.Parse(before)!; invalid["Operations"]!["AcquiredReports"] = new JsonArray(999999);
        try { sim.Restore(invalid.ToJsonString()); throw new Exception("unknown cached record accepted"); }
        catch (ArgumentException) { Check(sim.Save() == before, "invalid acquired-record references reject atomically"); }
        var legacy = JsonNode.Parse(before)!;
        legacy["Operations"]!.AsObject().Remove("Phone"); legacy["Operations"]!.AsObject().Remove("AcquiredReports");
        clone.Restore(legacy.ToJsonString());
        Check(!clone.OscpAccess(OscpDevice.Phone).Available, "matching older saves do not invent an issued handset");
        // A disconnected, unpowered reader cannot acquire a newly delivered server-side report.
        var world = new SimulationWorld(map, operation);
        world.Player.Cell = new(5, 8); world.Operations.Phone = world.Operations.Phone! with { Powered = false };
        world.Operations.Report(world, "RELAY", "Unverified off-facility observation; no SCP designation assigned.", source: "radio");
        var report = world.Operations.ReceivedReports.Last();
        OscpService.Acquire(world);
        Check(!OscpService.Records(world).Any(r => r.Id == report.Id), "radio collector is not automatic player knowledge");
        world.Operations.Phone = world.Operations.Phone with { Powered = true };
        OscpService.Acquire(world);
        Check(OscpService.Records(world).Any(r => r.Id == report.Id), "live phone acquires actual received report");
        world.Operations.NetworkOnline = false;
        Check(OscpService.Records(world).Any(r => r.Id == report.Id), "link loss retains previously acquired report");
        for (var i = 0; i < 50; i++) world.Operations.Report(world, "RELAY", "later message " + i, source: "radio");
        var heldSave = PrototypeSaveCodec.Capture(world, "cache-eviction-fixture");
        var restored = PrototypeSaveCodec.Restore(heldSave, map, "cache-eviction-fixture", operation);
        Check(!restored.Operations.AcquiredReports.Contains(report.Id), "bounded collector evicts cache references without corrupting saves");
        var regionalConfig = JsonNode.Parse(operation)!; regionalConfig["regionalIntakeEnabled"] = true;
        var regional = new SpatialSimulation(map, regionalConfig.ToJsonString());
        regional.Operations.NetworkOnline = false; regional.LocalHold = true;
        for (var i = 0; i < 30; i++) regional.Tick();
        Check(!regional.Operations.ReceivedReports.Any(r => r.Author.Contains("R-017")), "regional fixture waits for an available network route");
        regional.Operations.NetworkOnline = true; regional.Tick();
        Check(regional.LocalMs == 0 && regional.Operations.ReceivedReports.Count(r => r.Author.Contains("R-017")) == 1,
            "remote collector progresses on world time while local actions remain held");
        var regionalClone = new SpatialSimulation(map, regionalConfig.ToJsonString()); regionalClone.Restore(regional.Save());
        regional.LocalHold = regionalClone.LocalHold = false;
        for (var i = 0; i < 20; i++) { regional.Tick(); regionalClone.Tick(); }
        Check(regional.Save() == regionalClone.Save() && regional.OscpRecords.Count(r => r.Author.Contains("R-017")) == 1,
            "regional report persists, becomes acquired over a real link and cannot repeat after restore");
        Check(regional.Anomalies.Bodies.Count == 1, "unverified report cannot spawn an anomaly or field team");
        Console.WriteLine($"OSCP_CHECKS_PASS: {passed} assertions; application/device authority, phone delivery, offline acquisition/cache, holds, persistence and detached restore.");
        return passed;
    }
}
