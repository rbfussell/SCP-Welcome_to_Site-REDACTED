using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunBodyDropSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory;
        sim.ManualPause = true; var original = sim.Save(); sim.Player.Cell = new(13, 6);
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Body drop UI: " + reason); }
        async Task Finish()
        {
            for (var i = 0; i < 1000 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "paid fitting did not resolve"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task OpenSource()
        {
            panel.Inspect("avatar-backpack"); await InventoryFrames();
            var gear = panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
            InventoryClick(gear.HotspotPosition("main"), true); await InventoryFrames();
            InventoryClick(panel.BodyFitToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        }
        Vector2 Source()
        {
            var grid = panel.FindGrid("avatar-backpack", "main"); var p = sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-belt").Placement;
            return grid.GlobalPosition + new Vector2(1 + p.X * grid.CellPixels + 10, 44 + p.Y * grid.CellPixels + 10);
        }
        sim.ManualPause = false;
        Check(sim.HandleInventory(new(InventoryActionKind.AutoPlace, "utility-belt", new("avatar-backpack", "main"))), sim.LastMessage); await Finish();
        await OpenSource(); var fit = panel.BodyFit; var surface = fit.Surface;
        var before = sim.Save(); var initialDraft = fit.Draft; var pose = sim.Inventory.BodyFits!.Shape("belt").DefaultPose;
        await InventoryDrag(Source(), surface.CellPosition(new(0, 1)), async () =>
        {
            Check(surface.DropPreview is { Geometry.Fits: false, Handling.Allowed: false } && panel.DragItemId == "utility-belt",
                "invalid body drop did not show the physical refusal");
            Check(sim.Save() == before, "invalid hover changed state"); await InventoryCapture("-body-drop-invalid");
        });
        Check(sim.Inventory.Pending is null && sim.Save() == before, "invalid release moved clothing or charged work");
        await InventoryDrag(Source(), surface.GlobalPosition + new Vector2(5, 12), async () =>
        {
            Check(surface.DropPreview is null && !surface._CanDropData(new(5, 12), "utility-belt"), "blank body margin snapped to a hidden nearest hex");
            await InventoryFrames();
        });
        Check(sim.Save() == before, "blank-space release changed inventory");
        await InventoryDrag(Source(), surface.CellPosition(new(pose.Q, pose.R)), async () =>
        {
            Check(surface.DropPreview is { Geometry.Fits: true, Handling.Allowed: true } valid
                && valid.Request.Destination!.BodyPose == pose && valid.Request.ItemId == "utility-belt", "body preview did not snap to its commit anchor");
            for (var step = 0; step < 6; step++) { InventoryKey(Key.R); await InventoryFrames(); }
            Check(panel.BodyRotation == 0 && !panel.Rotated && fit.Draft == initialDraft && surface.DropPreview?.Request.Destination?.BodyPose == pose,
                "six drag rotations changed the ordinary fit draft, packed orientation or final anchor");
            await InventoryDragHover(Source()); InventoryKey(Key.R); await InventoryFrames();
            Check(panel.Rotated && panel.BodyRotation == 0, "R over storage failed to use square rotation while body mode was visible");
            InventoryKey(Key.R); await InventoryFrames();
            await InventoryDragHover(surface.CellPosition(new(pose.Q, pose.R)));
            Check(!panel.Rotated && surface.DropPreview is { Handling.Allowed: true } && sim.Save() == before, "returning to body lost rotation isolation or changed state");
            await InventoryCapture("-body-drop-valid");
        });
        Check(sim.Inventory.Pending is null && sim.Save() == before, "paused body release granted free dressing");
        await InventoryDrag(Source(), surface.CellPosition(new(pose.Q, pose.R)), async () =>
        { InventoryKey(Key.Escape); await InventoryFrames(); });
        Check(!panel.IsOpen && panel.DragItemId is null && sim.Save() == before, "Esc did not cancel the drag/interface without physical work");
        InventoryKey(Key.I); await InventoryFrames(); await OpenSource();
        var sourcePlacement = sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-belt").Placement;
        sim.ManualPause = false;
        await InventoryDrag(Source(), surface.CellPosition(new(pose.Q, pose.R)));
        Check(sim.Inventory.Pending is { Request.ItemId: "utility-belt", Request.Destination.BodyPose: not null }
            && sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-belt").Placement == sourcePlacement,
            "release did not purchase time while retaining the source");
        var pending = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(pending); Check(clone.Save() == pending, "native body-drop paid work failed exact restore");
        await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-belt").Placement.BodyPose == pose && fit.SelectedId == "utility-belt",
            "commit differed from its preview or lost the selected dropped clothing");
        await InventoryCapture("-body-drop-applied");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open();
        if (!panel.EngineeringSlots.ButtonPressed) { InventoryClick(panel.EngineeringSlots.GetGlobalRect().GetCenter()); await InventoryFrames(); }
        sim.ManualPause = false;
        GD.Print("BODY_DROP_SMOKE_PASS: native stored-clothing drag, exact mask/contact preview, blank/invalid rejection, six body versus two storage turns, paused refusal, Esc cancellation, paid source retention and continuation.");
    }
}
