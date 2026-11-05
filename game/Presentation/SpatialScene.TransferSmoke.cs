using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    /// <summary>Transfer directly from retained source storage to exterior mounts without an intermediate hand.</summary>
    private async Task RunTransferSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var original = sim.Save();
        panel.Close(); panel.Open(); sim.ManualPause = true; sim.Player.Cell = new(13, 6);
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Transfer UI: " + reason); }
        async Task Finish()
        {
            for (var i = 0; i < 1000 && (sim.Inventory.Pending is not null || sim.Inventory.SecuringRemaining != 0); i++) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null && sim.Inventory.SecuringRemaining == 0, "paid handling did not finish");
            sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request)
        { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        ItemPlacement Placement(string id) => sim.Inventory.DiagnosticItems.Single(i => i.Id == id).Placement;
        InventoryGearSurface Gear() => panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        void Nav(bool gear, string text) => InventoryClick(panel.Browser.GetChild<InventoryContainerView>(gear ? 0 : 1)
            .GetChild<HBoxContainer>(0).GetChildren().OfType<Button>().Single(b => b.Text == text).GetGlobalRect().GetCenter());
        Vector2 Source(string id)
        {
            var p = Placement(id); var grid = panel.FindGrid(p.ParentId, p.RegionId);
            return grid.GlobalPosition + new Vector2(1 + (p.X + .5f) * grid.CellPixels, 44 + (p.Y + .5f) * grid.CellPixels);
        }
        async Task Inspect(string id) { panel.Inspect(id); await InventoryFrames(); }
        async Task Open(string id, string region)
        { await Inspect(id); InventoryClick(Gear().HotspotPosition(region), true); await InventoryFrames(); }

        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        await Act(new(InventoryActionKind.Move, "utility-hook", new("avatar-backpack", "side", 1, 0)));
        await Act(new(InventoryActionKind.AutoPlace, "utility-pouch", new("avatar-backpack", "main")));
        await Act(new(InventoryActionKind.Move, "spare-dressing", new("person:avatar", "hand")));
        var held = Placement("spare-dressing");
        await Open("avatar-backpack", "side"); var before = sim.Save();
        await Inspect("utility-belt");
        Check(panel.Browser.CurrentContents == ("avatar-backpack", "side") && sim.Save() == before, "choosing belt discarded or changed source storage");
        Check(panel.Browser.GetChild<InventoryContainerView>(1).GetChildren().OfType<Label>().Any(l => l.Text.Contains("Field backpack")), "source breadcrumb omits its actual owner");
        await InventoryDrag(Source("utility-hook"), Gear().MountPosition(new(0, 0)), async () =>
        {
            Check(Gear().MountPreview is { Handling.Allowed: true } p && p.Request.Destination?.ParentId == "utility-belt", "stored hook cannot preview directly on webbing");
            Check(sim.Save() == before, "direct preview performed physical work"); await InventoryCapture("-transfer-hook-preview");
        });
        Check(sim.Inventory.Pending is null && sim.Save() == before, "paused direct drop moved the hook");
        var source = Placement("utility-hook");
        await InventoryDrag(Source("utility-hook"), Gear().MountPosition(new(0, 0)), () => { sim.ManualPause = false; return Task.CompletedTask; });
        Check(sim.Inventory.Pending is { TotalMs: > 0 } && Placement("utility-hook") == source && Placement("spare-dressing") == held,
            "direct mounting bypassed paid source retention or borrowed the occupied hand");
        await Finish(); Check(Placement("utility-hook").ParentId == "utility-belt", "hook never reached the destination");

        await Open("avatar-backpack", "main"); await Inspect("utility-belt"); before = sim.Save();
        await InventoryDrag(Source("utility-pouch"), Gear().MountPosition(new(-1, 2)), async () =>
        {
            Check(Gear().MountPreview is { Handling.Allowed: true } p && p.Request.Destination?.ParentId == "utility-hook", "source pouch missed the lower extender target");
            Check(sim.Save() == before, "nested mounting preview changed state"); await InventoryCapture("-transfer-pouch-preview"); sim.ManualPause = false;
        });
        Check(sim.Inventory.Pending?.Request.Destination?.ParentId == "utility-hook", "direct pouch transfer skipped its normal request");
        var saved = sim.Save(); var copy = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        copy.Restore(saved); Check(copy.Save() == saved, "pending direct transfer did not restore exactly");
        await Finish();
        Check(Placement("utility-pouch").ParentId == "utility-hook" && Placement("spare-dressing") == held, "nested transfer lost the parent or changed the occupied hand");
        InventoryClick(Gear().MountedImagePoint("utility-pouch", new(-1, 2), new(.5f, .6f)), true); await InventoryFrames();
        Check(panel.Browser.GearIds.SequenceEqual(new[] { "utility-belt", "utility-hook", "utility-pouch" })
            && panel.Browser.CurrentContents == ("avatar-backpack", "main"), "provider navigation discarded the independent source");
        before = sim.Save(); Nav(true, "×"); await InventoryFrames();
        Check(panel.Browser.CurrentGear is null && panel.Browser.CurrentContents == ("avatar-backpack", "main") && sim.Save() == before,
            "closing unrelated destination secured or dismissed the backpack source");

        // Nested source breadcrumbs and right-hand Up remain independent of destination navigation.
        await Inspect("medical-case"); await Inspect("utility-belt");
        Check(panel.Browser.CurrentContents == ("medical-case", "inside"), "nested source lost its own ancestry on destination selection");
        Nav(false, "↑"); await InventoryFrames();
        Check(panel.Browser.CurrentContents == ("avatar-backpack", "main") && panel.Browser.CurrentGear == "utility-belt"
            && sim.Inventory.SecuringRemaining == 1 && sim.Inventory.IsOpen("medical-case", "inside"), "source Up did not queue ordinary child closure independently");
        sim.ManualPause = false; await Finish();
        Nav(false, "×"); await InventoryFrames();
        Check(panel.Browser.CurrentContents is null && panel.Browser.CurrentGear == "utility-belt" && sim.Inventory.SecuringRemaining == 1,
            "source close affected the destination or supplied free securing");
        sim.ManualPause = false; await Finish();
        Check(!sim.Inventory.IsOpen("avatar-backpack", "main"), "source zipper was not physically secured");

        // Choosing a destination does not prevent a real parent close from invalidating its source.
        await Act(new(InventoryActionKind.Open, "avatar-backpack", RegionId: "main"));
        await Open("avatar-backpack", "main"); await Inspect("utility-belt");
        await Act(new(InventoryActionKind.Close, "avatar-backpack", RegionId: "main"));
        Check(panel.Browser.CurrentContents is null && panel.Browser.CurrentGear == "utility-belt", "secured independent source remained visible");
        await Inspect("utility-pouch"); sim.ManualPause = false;
        InventoryClick(Gear().HotspotPosition("inside"), true); await Finish();
        Check(panel.Browser.CurrentContents == ("utility-pouch", "inside"), "explicit destination compartment did not replace the old source");
        await Inspect("utility-belt"); Nav(true, "×"); await InventoryFrames();
        Check(panel.Browser.CurrentContents is null && sim.Inventory.SecuringRemaining == 1, "closing real provider failed to dismiss and secure its dependent source");
        sim.ManualPause = false; await Finish();

        // A retained fixture source still requires physical reach.
        panel.Inspect("fixture:crate-a"); panel.OpenCompartment("fixture:crate-a", "store"); await InventoryFrames();
        await Inspect("utility-belt"); Check(panel.Browser.CurrentContents == ("fixture:crate-a", "store"), "fixture source cannot coexist with worn destination");
        sim.Player.Cell = new(5, 3); await InventoryFrames();
        Check(panel.Browser.CurrentContents is null && panel.Browser.CurrentGear == "utility-belt", "out-of-reach source survived independent navigation");

        // Coverage removes a retained hip pocket even when another gear item is displayed.
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = true;
        await Open("avatar-trousers", "left"); await Inspect("avatar-backpack");
        Check(panel.Browser.CurrentContents == ("avatar-trousers", "left"), "hip source was discarded before coverage check");
        await Act(new(InventoryActionKind.CloseGarment, "avatar-coat"));
        Check(panel.Browser.CurrentContents is null && panel.Browser.CurrentGear == "avatar-backpack", "newly covered source pocket remained accessible");
        await InventoryCapture("-transfer-covered-source");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = false;
        GD.Print("TRANSFER_SMOKE_PASS: independent gear/source trails, occupied-hand direct hook and nested pouch transfers, paused refusal, paid source retention/restore, nested source Up/close, provider-dependent securing, physical closure, reach and coverage invalidation.");
    }
}
