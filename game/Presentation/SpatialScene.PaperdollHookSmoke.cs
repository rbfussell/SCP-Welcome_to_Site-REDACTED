using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    /// <summary>Nested worn art follows physical frames, distinct alpha targets and ordinary loaded handling.</summary>
    private async Task RunPaperdollHookSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof; var surface = proof.Surface;
        var original = sim.Save();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Paperdoll hook: " + reason); }
        async Task Finish()
        {
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while (sim.Inventory.Pending is not null && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "paid work did not finish"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request)
        { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        PaperdollMountedArt Mounted(string id) => proof.Art.Layers(sim).Single(l => l.ItemId == id).Mounted!;
        Vector2 Target(string id)
        {
            var bounds = proof.Art.Bounds(surface.Size); var origin = Mounted(id).Origin;
            for (var y = origin.Y - .12f; y < origin.Y + .12f; y += .003f)
            for (var x = origin.X - .2f; x < origin.X + .2f; x += .003f)
            {
                var point = bounds.Position + new Vector2(x, y) * bounds.Size;
                if (surface.Pick(point) == id) return surface.GlobalPosition + point;
            }
            throw new InvalidOperationException("Paperdoll hook: missing alpha target " + id);
        }
        void CheckPose(string id)
        {
            var expected = InventoryAttachmentFrames.RelativePose(sim.Inventory, id, "utility-belt")!;
            var drawn = Mounted(id);
            var offset = (drawn.Origin - new Vector2(.5f, .518f)) * new Vector2(360, 800);
            Check(offset.DistanceTo(new Vector2(expected.Q + expected.R * .5f, expected.R * .8660254f) * 34) < .01f
                && drawn.Rotation == expected.Rotation && drawn.Surface == "front", "drawn frame differs from composed physical frame: " + id);
        }
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId == "utility-hook"), "stored hook leaked into outfit");
        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        await Act(new(InventoryActionKind.Move, "utility-hook", new("utility-belt", "webbing", AttachmentPose: new(0, 0))));
        for (var rotation = 1; rotation <= 6; rotation++)
        {
            await Act(new(InventoryActionKind.Move, "utility-hook", new("utility-belt", "webbing", AttachmentPose: new(0, 0, rotation % 6))));
            CheckPose("utility-hook"); InventoryClick(Target("utility-hook")); await InventoryFrames();
            Check(panel.SelectedItem == "utility-hook", "six-turn hook alpha selection lost identity");
        }
        await Act(new(InventoryActionKind.Move, "utility-pouch", new("utility-hook", "hook", AttachmentPose: new(-1, 2))));
        CheckPose("utility-hook"); CheckPose("utility-pouch");
        var child = sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement;
        panel.Inspect("avatar-shirt"); await InventoryFrames(); var before = sim.Save();
        InventoryClick(Target("utility-pouch"), true); await InventoryFrames();
        Check(panel.Browser.GearIds.SequenceEqual(new[] { "utility-belt", "utility-hook", "utility-pouch" })
            && panel.Browser.CurrentContents is null && sim.Save() == before, "pouch inspection lost providers or did physical work");
        InventoryClick(Target("utility-hook"), true); await InventoryFrames();
        Check(panel.SelectedItem == "utility-hook" && panel.Browser.GearIds.SequenceEqual(new[] { "utility-belt", "utility-hook" }),
            "loaded hook is not separately inspectable");
        await InventoryCapture("-paperdoll-hook-chain");
        InventoryClick(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(proof.Side == "back" && new[] { "utility-hook", "utility-pouch" }.All(id =>
            !proof.Art.Opaque(proof.Art.Layers(sim).Single(l => l.ItemId == id), "back", Mounted(id).Origin)), "front chain leaked through rear view");
        InventoryClick(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames();
        await Act(new(InventoryActionKind.Move, "utility-hook", new("utility-belt", "webbing", AttachmentPose: new(2, 0, 1))));
        CheckPose("utility-hook"); CheckPose("utility-pouch");
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement == child && Mounted("utility-pouch").Rotation == 1,
            "moving provider rewrote child placement or lost inherited rotation");
        var oldSize = surface.Size; surface.Size = new(300, 270);
        Check(surface.Pick(Target("utility-hook") - surface.GlobalPosition) == "utility-hook"
            && surface.Pick(Target("utility-pouch") - surface.GlobalPosition) == "utility-pouch", "resized nested alpha selection differs");
        surface.Size = oldSize;
        await Act(new(InventoryActionKind.Open, "utility-pouch", RegionId: "inside"));
        await Act(new(InventoryActionKind.AutoPlace, "spare-dressing", new("utility-pouch", "inside")));
        await Act(new(InventoryActionKind.Close, "utility-pouch", RegionId: "inside"));
        InventoryClick(Target("utility-pouch"), true); await InventoryFrames();
        await InventoryCapture("-paperdoll-hook-rotated");
        before = sim.Save(); Check(!sim.HandleInventory(sim.GroundDropRequest("utility-belt")) && sim.Save() == before, "paused floor drop succeeded");
        sim.ManualPause = false; Check(sim.HandleInventory(sim.GroundDropRequest("utility-belt")), sim.LastMessage);
        Check(sim.Inventory.Pending is not null && proof.Art.Layers(sim).Any(l => l.ItemId == "utility-hook")
            && proof.Art.Layers(sim).Any(l => l.ItemId == "utility-pouch"), "pending loaded drop removed worn chain early");
        var pending = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(pending); Check(clone.Save() == pending && proof.Art.Layers(clone).SequenceEqual(proof.Art.Layers(sim)), "pending chain failed exact restore");
        await Finish();
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId is "utility-belt" or "utility-hook" or "utility-pouch")
            && sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement == child && sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing").Placement.ParentId == "utility-pouch",
            "ground assembly remained worn or lost descendants");
        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        CheckPose("utility-hook"); CheckPose("utility-pouch");
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId == "spare-dressing"), "packed contents were rendered as exterior gear");
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("fixture:crate-a", "store", 0, 8)));
        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt", BodyPose: new(-4, 10, 2))));
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId is "utility-belt" or "utility-hook" or "utility-pouch")
            && proof.Art.Omitted(sim).Contains("Utility hook"), "unsupported worn root produced floating descendants");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Inspect("avatar-shirt"); await InventoryFrames();
        Check(proof.Side == "front" && sim.Save() == original, "fixture did not restore original state");
        GD.Print("PAPERDOLL_HOOK_SMOKE_PASS: composed belt/hook/pouch frames, six hook rotations, inherited child rotation, distinct alpha picking/inspection, rear exclusion, paid loaded ground drop/pickup and exact continuation; unsupported root and packed contents omitted.");
    }
}
