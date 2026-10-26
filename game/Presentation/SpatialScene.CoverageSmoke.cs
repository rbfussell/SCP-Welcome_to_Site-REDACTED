using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunCoverageSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory;
        sim.ManualPause = true; var original = sim.Save();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Coverage UI: " + reason); }
        async Task Finish()
        {
            for (var i = 0; i < 800 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "work failed to finish"); sim.ManualPause = true; await InventoryFrames();
        }
        InventoryGearSurface Surface() => panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        async Task Hover(InventoryGearSurface surface, string region)
        {
            var position = surface.HotspotPosition(region);
            using var motion = new InputEventMouseMotion { Position = position, GlobalPosition = position };
            GetViewport().PushInput(motion); await InventoryFrames();
        }
        async Task ToggleCoat()
        {
            // Keep the current contents trail while changing the actual garment via its ordinary control.
            var coat = panel.FindGrid("person:avatar", "outerwear");
            ((ScrollContainer)coat.GetParent().GetParent()).EnsureControlVisible(coat); await InventoryFrames();
            InventoryClick(coat.GlobalPosition + new Vector2(30, 80)); await InventoryFrames();
            sim.ManualPause = false; InventoryClick(panel.GarmentToggle.GetGlobalRect().GetCenter());
            Check(sim.Inventory.Pending is not null, "native zipper control failed to start paid work"); await Finish();
        }
        sim.ManualPause = false;
        Check(sim.HandleInventory(new(InventoryActionKind.Move, "flashlight", new("avatar-trousers", "cargo-left"))), "prepare real cargo contents"); await Finish();
        panel.Inspect("avatar-trousers"); await InventoryFrames();
        foreach (var region in new[] { "left", "right", "cargo-left", "cargo-right" })
        {
            await Hover(Surface(), region);
            Check(Surface().HoveredRegion == region && !Surface().HoverCovered, "four sprite segments did not map to separate accessible regions");
        }
        InventoryClick(Surface().HotspotPosition("cargo-left"), true); await InventoryFrames();
        Check(panel.Browser.CurrentContents == ("avatar-trousers", "cargo-left"), "cargo pouch did not open its own grid");
        await ToggleCoat();
        Check(panel.Browser.CurrentContents == ("avatar-trousers", "cargo-left") && sim.InventoryAccess("avatar-trousers", "cargo-left").Allowed,
            "closing coat incorrectly dismissed an accessible lower pocket");
        await Hover(Surface(), "left");
        Check(Surface().HoverCovered && Surface().TooltipText.Contains("Field coat"), "blocked hip segment lacks its actual blocker reason");
        await InventoryCapture("-hip-covered");
        var held = sim.Save(); InventoryClick(Surface().HotspotPosition("left"), true); await InventoryFrames();
        Check(panel.Browser.CurrentContents == ("avatar-trousers", "cargo-left") && sim.Save() == held,
            "clicking a covered hip pocket exposed its contents or spent free work");
        await Hover(Surface(), "cargo-left");
        Check(!Surface().HoverCovered && panel.FindGrid("avatar-trousers", "cargo-left").DisplayedItems.Single().Id == "flashlight",
            "uncovered cargo contents were hidden by the coat");
        await InventoryCapture("-cargo-accessible");
        // Body diagnostics use exactly the same authoritative mouth/cover intersection.
        var legs = panel.FindGrid("person:avatar", "legs");
        ((ScrollContainer)legs.GetParent().GetParent()).EnsureControlVisible(legs); await InventoryFrames();
        InventoryClick(legs.GlobalPosition + new Vector2(30, 80));
        InventoryClick(panel.BodyFitToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(panel.BodyFit.Pockets.Length == 4 && panel.BodyFit.Pockets.Count(p => p.Covered) == 2,
            "body marks disagree with actual hip/cargo access");
        var diagnosticSave = sim.Save(); await InventoryCapture("-pocket-overlap");
        Check(sim.Save() == diagnosticSave, "coverage rendering changed state");
        InventoryClick(panel.BodyFitToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        await ToggleCoat();
        await Hover(Surface(), "left");
        Check(!Surface().HoverCovered, "opening jacket did not refresh an existing gear surface");
        InventoryClick(Surface().HotspotPosition("left"), true); await InventoryFrames();
        Check(panel.Browser.CurrentContents == ("avatar-trousers", "left"), "exposed hip pocket did not become available");
        await InventoryCapture("-hip-exposed");
        await ToggleCoat();
        Check(panel.Browser.CurrentContents is null && panel.Browser.CurrentGear == "avatar-trousers",
            "newly covered hip contents did not dismiss while retaining the accessible parent gear");
        var snapshot = sim.Save(); var copy = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        copy.Restore(snapshot); Check(copy.Save() == snapshot && copy.InventoryPocketCoverage("avatar-trousers", "left")!.Covered,
            "coverage changed through save/restore");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = false;
        GD.Print("COVERAGE_SMOKE_PASS: four native pouch segments, actual closed/open jacket access, retained cargo/invalidated hip trails, body overlap marks and persistence.");
    }
}
