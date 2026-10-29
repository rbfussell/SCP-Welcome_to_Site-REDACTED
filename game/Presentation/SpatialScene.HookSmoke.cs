using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunHookSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory;
        sim.ManualPause = true; var original = sim.Save(); sim.Player.Cell = new(13, 6);
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Hook UI: " + reason); }
        async Task Finish()
        {
            for (var i = 0; i < 1000 && (sim.Inventory.Pending is not null || sim.Inventory.SecuringRemaining != 0); i++) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null && sim.Inventory.SecuringRemaining == 0, "paid work did not finish");
            sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request)
        { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        async Task<InventoryGrid> Hand()
        {
            var grid = panel.FindGrid("person:avatar", "hand");
            ((ScrollContainer)grid.GetParent().GetParent()).EnsureControlVisible(grid); await InventoryFrames(); return grid;
        }
        InventoryGearSurface Surface() => panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        Button Up() => panel.Browser.GetChild<InventoryContainerView>(0).GetChild<HBoxContainer>(0).GetChildren().OfType<Button>().First();
        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        await Act(new(InventoryActionKind.Move, "utility-hook", new("person:avatar", "hand")));
        panel.Inspect("utility-belt"); await InventoryFrames(); var hand = await Hand(); var before = sim.Save();
        await InventoryDrag(hand.GlobalPosition + new Vector2(30, 80), Surface().MountPosition(new(0, 0)), async () =>
        {
            Check(Surface().MountPreview is { Handling.Allowed: true } p && p.Request.Destination?.ParentId == "utility-belt", "hook did not preview on the belt");
            Check(sim.Save() == before, "hook preview moved the source");
            await InventoryCapture("-hook-mount-preview"); sim.ManualPause = false;
        });
        Check(sim.Inventory.Pending?.Request.ItemId == "utility-hook", "hook release skipped paid handling"); await Finish();
        await Act(new(InventoryActionKind.Move, "utility-pouch", new("person:avatar", "hand"))); hand = await Hand(); before = sim.Save();
        await InventoryDrag(hand.GlobalPosition + new Vector2(30, 80), Surface().MountPosition(new(-1, 2)), async () =>
        {
            Check(Surface().MountPreview is { Handling.Allowed: true } p && p.Request.Destination?.ParentId == "utility-hook"
                && p.Request.Destination.AttachmentPose == new BodyPose(-1, 2), "root view did not target the hook's lower mount");
            Check(sim.Save() == before, "nested mount preview changed physical state");
            await InventoryCapture("-hook-pouch-preview"); sim.ManualPause = false;
        });
        Check(sim.Inventory.Pending?.Request.Destination?.ParentId == "utility-hook", "pouch release addressed the belt instead of the extender"); await Finish();
        await InventoryCapture("-hook-assembly");
        InventoryClick(Surface().MountedImagePoint("utility-pouch", new(-1, 2), new(.5f, .6f)), true); await InventoryFrames();
        Check(panel.Browser.GearIds.SequenceEqual(new[] { "utility-belt", "utility-hook", "utility-pouch" }) && panel.Browser.CurrentContents is null,
            "direct descendant inspection did not preserve the provider trail");
        sim.ManualPause = false; InventoryClick(Surface().HotspotPosition("inside"), true);
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.Open, "hooked pouch flap opened without paid work"); await Finish();
        await Act(new(InventoryActionKind.Move, "spare-dressing", new("utility-pouch", "inside")));
        Check(panel.Browser.CurrentContents == ("utility-pouch", "inside"), "pouch contents left the right-hand trail");
        await InventoryCapture("-hook-pouch-contents");
        sim.ManualPause = false; InventoryClick(Up().GetGlobalRect().GetCenter()); await Finish();
        Check(panel.Browser.CurrentGear == "utility-hook" && !sim.Inventory.IsOpen("utility-pouch", "inside"), "up did not return to the hook and secure the pouch");
        await InventoryCapture("-hook-provider-view");
        InventoryClick(Up().GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(panel.Browser.CurrentGear == "utility-belt", "hook up did not return to belt");
        var child = sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement;
        before = sim.Save();
        Check(Surface().MountedAt(Surface().MountedImagePoint("utility-hook", new(0, 0), new(.36f, .3f))) == "utility-hook", "loaded hook source is not alpha-picked at its clip");
        await InventoryDrag(Surface().MountedImagePoint("utility-hook", new(0, 0), new(.36f, .3f)), Surface().MountPosition(new(2, 0)), async () =>
        {
            InventoryKey(Key.R); await InventoryFrames();
            Check(Surface().MountPreview is { Handling.Allowed: true } p && p.Request.Destination?.AttachmentPose == new BodyPose(2, 0, 1),
                "loaded hook did not preview a single sixth-turn with its assembly: drag=" + panel.DragItemId + " angle=" + panel.AttachmentRotation + " preview=" + Surface().MountPreview);
            Check(sim.Save() == before, "loaded provider drag changed child placement");
            await InventoryCapture("-hook-loaded-rotation"); sim.ManualPause = false;
        });
        await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement == child
            && InventoryAttachmentFrames.RelativePose(sim.Inventory, "utility-pouch", "utility-belt") == new BodyPose(0, 1, 1), "paid provider movement lost the nested transform");
        await Act(new(InventoryActionKind.Move, "utility-pouch-2", new("person:avatar", "hand"))); hand = await Hand(); before = sim.Save();
        await InventoryDrag(hand.GlobalPosition + new Vector2(30, 80), Surface().MountPosition(new(0, 1)), async () =>
        {
            Check(Surface().MountPreview is { Handling.Allowed: false }, "occupied lower hook accepted another pouch");
            Check(sim.Save() == before, "refused occupied-hook preview changed the assembly"); await InventoryCapture("-hook-occupied-mount");
        });
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch-2").Placement.RegionId == "hand", "refused mount moved the source");
        await Act(new(InventoryActionKind.Move, "utility-pouch-2", new("fixture:crate-a", "store", 3, 4)));
        hand = await Hand(); sim.ManualPause = false;
        await InventoryDrag(Surface().MountedImagePoint("utility-hook", new(2, 0, 1), new(.36f, .3f)), hand.GlobalPosition + new Vector2(12, 55)); await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-hook").Placement.RegionId == "hand"
            && sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement == child
            && sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing").Placement.ParentId == "utility-pouch", "loaded hook detachment lost children");
        var saved = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(saved); Check(clone.Save() == saved, "loaded hook hierarchy did not restore exactly");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = false;
        GD.Print("HOOK_SMOKE_PASS: native hook/pouch dragging, extended target, recursive preview/rotation, provider trails, closure, blocked mount, loaded detachment and persistence.");
    }
}
