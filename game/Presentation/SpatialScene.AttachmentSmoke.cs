using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunAttachmentSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory;
        sim.ManualPause = true; var original = sim.Save();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Attachment UI: " + reason); }
        async Task Finish()
        {
            for (var i = 0; i < 900 && (sim.Inventory.Pending is not null || sim.Inventory.SecuringRemaining != 0); i++) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null && sim.Inventory.SecuringRemaining == 0, "paid work failed to finish"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request)
        { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        InventoryGearSurface Surface() => panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        async Task<InventoryGrid> Hand()
        {
            var hand = panel.FindGrid("person:avatar", "hand");
            ((ScrollContainer)hand.GetParent().GetParent()).EnsureControlVisible(hand); await InventoryFrames(); return hand;
        }
        sim.Player.Cell = new(13, 6);
        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        await Act(new(InventoryActionKind.Move, "utility-pouch", new("person:avatar", "hand")));
        panel.Inspect("utility-belt"); await InventoryFrames();
        Check(Surface().HasAttachmentSurface && panel.Browser.CurrentContents is null, "support rendered as exterior gear without a packing grid");
        var hand = await Hand();
        await InventoryDrag(hand.GlobalPosition + new Vector2(30, 80), Surface().MountPosition(new(1, 0)), async () =>
        {
            Check(GetViewport().GuiIsDragging() && Surface().MountPreview?.Handling.Allowed == true, "native drag never acquired a compatible surface");
            for (var rotation = 1; rotation <= 6; rotation++)
            {
                InventoryKey(Key.R); await InventoryDragHover(Surface().MountPosition(new(1, 0)));
                Check(Surface().MountPreview?.Request.Destination?.AttachmentPose?.Rotation == rotation % 6, "R did not cycle six mounting directions");
                var pose = new BodyPose(1, 0, rotation % 6);
                var anchor = Surface().MountedImagePoint("utility-pouch", pose, new(.5f, .12f));
                Check(anchor.DistanceTo(Surface().MountPosition(new(1, 0))) < .01f, "sprite loops drifted away from their mounting anchor");
                var hanging = Surface().MountedImagePoint("utility-pouch", pose, new(.5f, .9f)) - anchor;
                var forward = new Vector2(0, 44).Rotated(rotation % 6 * Mathf.Pi / 3);
                Check(Math.Abs(hanging.Cross(forward)) < .05f && hanging.Dot(forward) > 0, "sprite differs from the unrotated source plus exact sixth-turn steps");
                // Compare with the actual physical cells, not just the sprite's own rotation formula.
                // A two-cell (0,0)/(0,1) footprint leans 30 degrees away from this upright source.
                var profile = sim.Inventory.Definition("utility-pouch").Attachment!;
                var lowerCells = profile.Occupancy.Except(profile.Contacts).ToArray();
                var footprintAxis = lowerCells.Select(cell => Surface().MountPosition(BodyFitGeometry.Transform(cell, pose)) - anchor)
                    .Aggregate(Vector2.Zero, (sum, point) => sum + point) / lowerCells.Length;
                Check(Math.Abs(hanging.Normalized().Cross(footprintAxis.Normalized())) < .0001f && hanging.Dot(footprintAxis) > 0,
                    "physical footprint leans away from the mounted illustration");
                await InventoryCapture("-mount-direction-" + rotation % 6);
            }
            Check(sim.Inventory.Pending is null && sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement.RegionId == "hand", "preview performed free physical work");
            await InventoryCapture("-mount-preview");
            await InventoryDragHover(hand.GlobalPosition + new Vector2(15, 55));
            Check(Surface().MountPreview is null, "leaving support left a duplicate stale attachment preview");
            await InventoryDragHover(Surface().MountPosition(new(1, 0))); sim.ManualPause = false;
        });
        Check(sim.Inventory.Pending?.Request.Destination?.AttachmentPose == new BodyPose(1, 0), "drop did not start the exact displayed pose"); await Finish();
        var pouchPlacement = sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement;
        Check(pouchPlacement.ParentId == "utility-belt", "timed attachment did not commit");
        await InventoryCapture("-belt-mounted");
        // Commit each step too: zero is the original upright source, not a half-step offset onto a hex diagonal.
        var directions = new[] { new Vector2(0, 1), new(-.8660254f, .5f), new(-.8660254f, -.5f),
            new(0, -1), new(.8660254f, -.5f), new(.8660254f, .5f) };
        for (var step = 1; step <= 6; step++)
        {
            var previous = new BodyPose(1, 0, (step - 1) % 6); var next = step % 6;
            await InventoryDrag(Surface().MountedImagePoint("utility-pouch", previous, new(.5f, .6f)), Surface().MountPosition(new(1, 0)), async () =>
            {
                InventoryKey(Key.R); await InventoryDragHover(Surface().MountPosition(new(1, 0)));
                Check(Surface().MountPreview is { Handling.Allowed: true } preview && preview.Request.Destination?.AttachmentPose?.Rotation == next,
                    "one R press skipped or repeated a mount orientation");
                sim.ManualPause = false;
            });
            Check(sim.Inventory.Pending?.Request.Destination?.AttachmentPose?.Rotation == next, "release committed a different rotation from its preview");
            await Finish();
            var placed = sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement;
            Check(placed.AttachmentPose == new BodyPose(1, 0, next), "paid rotation lost its exact sixth-turn pose");
            var sourceAnchor = Surface().MountedImagePoint("utility-pouch", placed.AttachmentPose!, new(.5f, .12f));
            var axis = (Surface().MountedImagePoint("utility-pouch", placed.AttachmentPose!, new(.5f, .9f)) - sourceAnchor).Normalized();
            Check(axis.DistanceTo(directions[next]) < .0001f, "committed sprite misses the independent upright/60-degree reference direction");
            await InventoryCapture("-committed-direction-" + next);
        }

        InventoryClick(Surface().MountPosition(new(-1, 0)), button: MouseButton.Right); await InventoryFrames();
        var supportMenu = panel.GetChildren().OfType<PanelContainer>().Last().GetChild<VBoxContainer>(0).GetChildren().OfType<Button>().Select(b => b.Text).ToArray();
        Check(supportMenu.Contains("Open") && !supportMenu.Any(t => t.StartsWith("Arrange") || t is "Zip" or "Close" or "Snap"),
            "exterior context invented a storage/closure action");
        InventoryClick(new Vector2(600, 55)); await InventoryFrames();
        InventoryClick(Surface().MountedImagePoint("utility-pouch", new(1, 0), new(.5f, .6f)), true); await InventoryFrames();
        Check(panel.Browser.GearIds.SequenceEqual(new[] { "utility-belt", "utility-pouch" }) && panel.Browser.CurrentContents is null,
            "mounted pouch did not enter the middle gear trail without revealing contents");
        sim.ManualPause = false; InventoryClick(Surface().HotspotPosition("inside"), true);
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.Open, "pouch flap bypassed paid unsnapping"); await Finish();
        Check(panel.Browser.CurrentContents == ("utility-pouch", "inside"), "pouch interior did not open in right view");
        await Act(new(InventoryActionKind.Move, "spare-dressing", new("person:avatar", "hand")));
        hand = await Hand(); var inside = panel.FindGrid("utility-pouch", "inside");
        sim.ManualPause = false;
        await InventoryDrag(hand.GlobalPosition + new Vector2(30, 80), inside.GlobalPosition + new Vector2(8, 50)); await Finish();
        Check(sim.InventoryContents("utility-pouch", "inside").Single().Id == "spare-dressing", "native square-grid storage in mounted pouch failed");
        await InventoryCapture("-pouch-interior");
        var middle = panel.Browser.GetChild<InventoryContainerView>(0).GetChild<HBoxContainer>(0);
        sim.ManualPause = false; InventoryClick(middle.GetChildren().OfType<Button>().First().GetGlobalRect().GetCenter()); await Finish();
        Check(panel.Browser.CurrentGear == "utility-belt" && panel.Browser.CurrentContents is null && !sim.Inventory.IsOpen("utility-pouch", "inside"),
            "up did not return to belt and secure its departing child through paid work");
        // A strap cannot be fitted across an outermost pack: put the pack aside first.
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("fixture:crate-a", "store", 0, 8)));
        // Move and rotate the parent using the existing paid body-fit controls.
        panel.Select("utility-belt"); InventoryClick(panel.BodyFitToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        panel.BodyFit.Choose("utility-belt"); InventoryKey(Key.R); InventoryKey(Key.R);
        InventoryClick(panel.BodyFit.Surface.CellPosition(new(-4, 10))); await InventoryFrames();
        Check(panel.BodyFit.Preview is { Geometry.Fits: true, Handling.Allowed: true }, "diagonal belt preview rejected");
        sim.ManualPause = false; InventoryClick(panel.BodyFit.ApplyButton.GetGlobalRect().GetCenter()); await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement == pouchPlacement
            && InventoryAttachments.BodyPosition(sim.Inventory, "utility-pouch") == new BodyPose(-5, 11, 2), "parent refit did not carry local attachment pose");
        await InventoryCapture("-belt-diagonal");
        InventoryClick(panel.BodyFitToggle.GetGlobalRect().GetCenter()); panel.Inspect("utility-belt"); await InventoryFrames();
        hand = await Hand(); sim.ManualPause = false;
        await InventoryDrag(Surface().MountedImagePoint("utility-pouch", new(1, 0), new(.5f, .6f)), hand.GlobalPosition + new Vector2(12, 55)); await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement is { ParentId: "person:avatar", RegionId: "hand", AttachmentPose: null }
            && sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing").Placement.ParentId == "utility-pouch", "native detachment lost the loaded pouch");
        await InventoryCapture("-pouch-detached");
        // Whole-provider drop from the equipped/body view uses support fit, never the belt's folded packing envelope.
        var beltGrid = panel.FindGrid("person:avatar", "belt");
        ((ScrollContainer)beltGrid.GetParent().GetParent()).EnsureControlVisible(beltGrid); await InventoryFrames();
        panel.Select("utility-pouch");
        var loosePouch = sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement;
        var providerPreview = beltGrid.PreviewAt("utility-pouch", new(30, 80));
        Check(providerPreview is { IntoContainer: true, Quote.Allowed: true, Request.Kind: InventoryActionKind.Move }
            && providerPreview.Request.Destination?.ParentId == "utility-belt", "equipped belt failed to offer its exterior support");
        // The hand and belt are far apart in this temporary slot list: start the drag at the hand, then scroll while dragging.
        hand = await Hand();
        await InventoryDrag(hand.GlobalPosition + new Vector2(30, 80), hand.GlobalPosition + new Vector2(15, 55), async () =>
        {
            ((ScrollContainer)beltGrid.GetParent().GetParent()).EnsureControlVisible(beltGrid); await InventoryFrames();
            await InventoryDragHover(beltGrid.GlobalPosition + new Vector2(30, 80));
            InventoryKey(Key.R); await InventoryFrames();
            Check(panel.DropPreview is { IntoContainer: true, Quote.Allowed: true } p && p.Request.Destination?.AttachmentPose?.Rotation == 1,
                "equipped support drop did not use sixth-turn rotation");
            Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement == loosePouch && sim.Inventory.Pending is null,
                "equipped provider preview moved or unpacked the loaded pouch");
            await InventoryCapture("-equipped-belt-drop"); sim.ManualPause = false;
        });
        Check(sim.Inventory.Pending?.Request.Destination?.ParentId == "utility-belt", "equipped provider release did not start paid attachment"); await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement.AttachmentPose == new BodyPose(0, 0, 1)
            && sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing").Placement.ParentId == "utility-pouch", "provider drop lost pose or loaded contents");
        var snapshot = sim.Save(); var copy = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        copy.Restore(snapshot); Check(copy.Save() == snapshot, "loaded rotated attachment hierarchy changed on restore");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = false;
        GD.Print("ATTACHMENT_SMOKE_PASS: native drag, six-direction preview, timed mounting, gear trail, paid pouch flap, square storage, auto-securing, parent refit and persistence.");
    }
}
