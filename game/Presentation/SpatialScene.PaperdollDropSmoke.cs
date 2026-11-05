using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunPaperdollDropSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof; var surface = proof.Surface; var original = sim.Save();
        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException("Sprite drop: " + message); }
        async Task Finish()
        {
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while (sim.Inventory.Pending is not null && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "paid work timed out"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request) { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        Vector2 Screen(Vector2 uv) { var b = proof.Art.Bounds(surface.Size); return surface.GlobalPosition + b.Position + uv * b.Size; }
        Vector2 Source(string id)
        {
            var grid = panel.FindGrid(sim.InventoryGroundRoot!.Id, InventoryGround.RegionId);
            var p = sim.Inventory.DiagnosticItems.Single(i => i.Id == id).Placement;
            return grid.GlobalPosition + new Vector2(1 + p.X * grid.CellPixels + 8, 44 + p.Y * grid.CellPixels + 8);
        }
        Vector2 Mount(string owner, BodyHex cell) => Screen(proof.Art.MountTargets(sim, proof.Side).Single(t => t.Owner == owner && t.Cell == cell).Origin);
        await Act(sim.GroundDropRequest("utility-belt"));
        var pose = sim.Inventory.BodyFits!.Shape("belt").DefaultPose;
        var bodyTarget = Screen(proof.Art.BodyPoint(new(pose.Q, pose.R), "front")); var before = sim.Save();
        await InventoryDrag(Source("utility-belt"), bodyTarget, async () =>
        {
            Check(surface.DropPreview is { Quote.Allowed: true } preview && preview.Request.Destination?.BodyPose == pose, "body drop did not address exact fitting hex");
            for (var i = 0; i < 6; i++) { InventoryKey(Key.R); await InventoryFrames(); }
            Check(panel.BodyRotation == 0 && !panel.Rotated && surface.DropPreview?.Request.Destination?.BodyPose == pose && sim.Save() == before,
                "body rotation changed storage or physical state"); await InventoryCapture("-sprite-drop-body");
        });
        Check(sim.Save() == before && sim.Inventory.Pending is null, "paused fitting granted free work");
        await InventoryDrag(Source("utility-belt"), surface.GlobalPosition + new Vector2(2, 2), async () =>
        { Check(surface.DropPreview is null, "blank margin snapped to body"); await InventoryFrames(); });
        await InventoryDrag(Source("utility-belt"), Screen(proof.Art.BodyPoint(new(-4, 8), "front")), async () =>
        {
            InventoryKey(Key.R); InventoryKey(Key.R); await InventoryFrames();
            Check(surface.DropPreview is { Geometry.Fits: true, Layers.Length: 0 } free
                && free.Request.Destination?.BodyPose == new BodyPose(-4, 8, 2), "free chest anchor was default-snapped or borrowed waist art");
            InventoryKey(Key.Escape); await InventoryFrames();
        });
        Check(!panel.IsOpen && sim.Save() == before && panel.DragItemId is null, "Esc committed or retained free-placement drag");
        InventoryKey(Key.I); await InventoryFrames(); InventoryClick(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        sim.ManualPause = false; await InventoryDrag(Source("utility-belt"), bodyTarget);
        Check(sim.Inventory.Pending?.Request.Destination?.BodyPose == pose && sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-belt").Placement.GroundOrder is not null,
            "body release bypassed paid source retention");
        var pending = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(pending); Check(clone.Save() == pending, "pending sprite fitting lost exact continuation"); await Finish();
        await Act(sim.GroundDropRequest("utility-hook")); var target = Mount("utility-belt", new(2, 0)); before = sim.Save();
        await InventoryDrag(Source("utility-hook"), target, async () =>
        {
            Check(surface.DropPreview is { Quote.Allowed: true } preview && preview.Request.Destination?.AttachmentPose == new BodyPose(2, 0), "hook preview missed exposed belt point");
            for (var i = 0; i < 6; i++) { InventoryKey(Key.R); await InventoryFrames(); }
            Check(panel.AttachmentRotation == 0 && panel.BodyRotation == 0 && sim.Save() == before, "six mount rotations changed body/storage or state");
            await InventoryCapture("-sprite-drop-hook"); sim.ManualPause = false;
        });
        Check(sim.Inventory.Pending?.Request.Destination?.ParentId == "utility-belt", "mount release lost paid provider"); await Finish();
        await Act(sim.GroundDropRequest("utility-pouch")); target = Mount("utility-hook", new(-1, 2));
        await InventoryDrag(Source("utility-pouch"), target, async () =>
        {
            Check(surface.DropPreview is { Quote.Allowed: true } preview && preview.Request.Destination?.ParentId == "utility-hook", "nested offered point addressed belt instead of hook");
            await InventoryCapture("-sprite-drop-pouch"); sim.ManualPause = false;
        }); await Finish();
        await Act(sim.GroundDropRequest("utility-pouch-2")); before = sim.Save();
        await InventoryDrag(Source("utility-pouch-2"), target, async () =>
        { Check(surface.DropPreview is { Quote.Allowed: false }, "occupied hook did not show refusal"); await InventoryFrames(); });
        Check(sim.Save() == before, "blocked mount changed inventory");
        var loaded = proof.Art.AttachmentDrop(sim, "front", surface.Size, Mount("utility-belt", new(0, 0)) - surface.GlobalPosition, "utility-hook", 1);
        Check(loaded is { Layers.Length: 2 } && loaded.Layers.Select(l => l.ItemId).SequenceEqual(new[] { "utility-hook", "utility-pouch" })
            && loaded.Layers.All(l => l.Mounted!.Rotation == 1) && sim.Save() == before, "loaded mount draft lost composed exterior children or changed state");
        var back = proof.Art.BodyDrop(sim, "back", surface.Size, Screen(proof.Art.BodyPoint(new(-4, 8), "back")) - surface.GlobalPosition, "utility-belt", 2);
        Check(back?.Request.Destination?.BodyPose == new BodyPose(-4, 8, 2), "back-view body hex projection failed to mirror");
        // Sprite itself supplies a drag source; no engineering slot controls are needed.
        var b = proof.Art.Bounds(surface.Size); Vector2? source = null;
        for (var y = .5f; y < .75f && source is null; y += .004f)
        for (var x = .2f; x < .9f; x += .004f)
        { var at = b.Position + new Vector2(x, y) * b.Size; if (surface.Pick(at) == "utility-hook") { source = surface.GlobalPosition + at; break; } }
        Check(source is not null, "missing loaded hook alpha source");
        await InventoryDrag(source!.Value, panel.FindGrid(sim.InventoryGroundRoot!.Id, InventoryGround.RegionId).GlobalPosition + new Vector2(150, 180), async () =>
        { Check(panel.DragItemId == "utility-hook", "tiny pressed hook lost its drag identity"); sim.ManualPause = false; await InventoryFrames(); }); await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-hook").Placement.ParentId.StartsWith("ground:", StringComparison.Ordinal)
            && sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement.ParentId == "utility-hook", "sprite removal lost loaded child");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Inspect("avatar-shirt"); await InventoryFrames();
        GD.Print("PAPERDOLL_DROP_SMOKE_PASS: free body hex and six turns, blank/paused refusal, paid source retention/restore, precise belt and nested hook targets, occupied refusal and loaded sprite drag source.");
    }
}
