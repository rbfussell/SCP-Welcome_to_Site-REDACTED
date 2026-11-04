using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    /// <summary>Actual waist belt and direct exterior pouch: shared transforms/picking, validated inspection and paid loaded continuation.</summary>
    private async Task RunPaperdollBeltSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof; var surface = proof.Surface;
        var original = sim.Save();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Paperdoll belt: " + reason); }
        void Click(Vector2 at, bool twice = false, MouseButton button = MouseButton.Left) { Input.WarpMouse(at); InventoryClick(at, twice, button); }
        async Task Finish()
        {
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while (sim.Inventory.Pending is not null && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "paid work did not finish"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request)
        { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        Vector2 Target(string id)
        {
            var b = proof.Art.Bounds(surface.Size);
            for (var y = .45f; y < .72f; y += .004f)
            for (var x = .12f; x < .89f; x += .004f)
            {
                var at = b.Position + new Vector2(x, y) * b.Size;
                if (surface.Pick(at) == id) return surface.GlobalPosition + at;
            }
            throw new InvalidOperationException("Paperdoll belt: missing visible target " + id);
        }
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId is "utility-belt" or "utility-pouch"), "stored belt/pouch leaked into the outfit");
        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        Check(proof.Art.Layers(sim).Any(l => l.ItemId == "utility-belt" && l.Column == 10), "actual default waist belt was omitted");
        Click(Target("utility-belt"), true); await InventoryFrames();
        Check(panel.SelectedItem == "utility-belt" && panel.Browser.CurrentGear == "utility-belt", "belt pixels selected a different item");
        await Act(new(InventoryActionKind.Move, "utility-pouch", new("person:avatar", "hand")));
        await Act(new(InventoryActionKind.Move, "utility-pouch", new("utility-belt", "webbing", AttachmentPose: new(-1, 0))));
        var first = proof.Art.Layers(sim).Single(l => l.ItemId == "utility-pouch").Mounted!;
        Check(first.Surface == "front" && first.Rotation == 0, "direct attachment lost its surface/rotation");
        panel.Inspect("avatar-shirt"); await InventoryFrames(); var before = sim.Save();
        Click(Target("utility-pouch"), true); await InventoryFrames();
        Check(panel.SelectedItem == "utility-pouch" && panel.Browser.GearIds.SequenceEqual(new[] { "utility-belt", "utility-pouch" })
            && panel.Browser.CurrentContents is null && sim.Save() == before && !sim.Inventory.IsOpen("utility-pouch", "inside"),
            "direct sprite inspection lost its provider trail, revealed contents or did free work");
        await InventoryCapture("-paperdoll-belt-front");
        Click(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(proof.Side == "back" && !proof.Art.Opaque(proof.Art.Layers(sim).Single(l => l.ItemId == "utility-pouch"), "back", first.Origin),
            "front-mounted pouch was painted through the rear view");
        Click(Target("utility-belt"), true); await InventoryFrames();
        Check(panel.SelectedItem == "utility-belt", "rear belt did not retain its actual item identity");
        await InventoryCapture("-paperdoll-belt-back");
        Click(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames();
        await Act(new(InventoryActionKind.Move, "utility-pouch", new("utility-belt", "webbing", AttachmentPose: new(1, 0))));
        var second = proof.Art.Layers(sim).Single(l => l.ItemId == "utility-pouch").Mounted!;
        Check(Math.Abs((second.Origin.X - first.Origin.X) * 360 - 68) < .01f && second.Origin.Y == first.Origin.Y,
            "pouch position did not follow two real provider-local hex steps");
        var old = surface.Size; surface.Size = new(300, 270);
        Check(surface.Pick(Target("utility-pouch") - surface.GlobalPosition) == "utility-pouch", "resized pouch picking differs from drawing transform"); surface.Size = old;
        for (var rotation = 1; rotation <= 6; rotation++)
        {
            await Act(new(InventoryActionKind.Move, "utility-pouch", new("utility-belt", "webbing", AttachmentPose: new(1, 0, rotation % 6))));
            Check(proof.Art.Layers(sim).Single(l => l.ItemId == "utility-pouch").Mounted!.Rotation == rotation % 6,
                "paperdoll skipped an actual sixth-turn orientation");
            Click(Target("utility-pouch")); await InventoryFrames(); Check(panel.SelectedItem == "utility-pouch", "rotated alpha hit lost the real attachment");
        }
        panel.Inspect("avatar-shirt"); Click(Target("utility-pouch"), true); await InventoryFrames();
        var gear = panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        before = sim.Save(); Click(gear.HotspotPosition("inside"), true); await InventoryFrames();
        Check(sim.Save() == before && sim.Inventory.Pending is null, "paused pouch flap supplied free opening");
        sim.ManualPause = false; Click(gear.HotspotPosition("inside"), true);
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.Open && !sim.Inventory.IsOpen("utility-pouch", "inside"), "pouch flap did not retain paid commitment"); await Finish();
        Check(panel.Browser.CurrentContents == ("utility-pouch", "inside"), "normal paid opening did not show its own contents");
        await Act(new(InventoryActionKind.AutoPlace, "spare-dressing", new("utility-pouch", "inside")));
        await Act(new(InventoryActionKind.Close, "utility-pouch", RegionId: "inside"));
        Check(sim.Inventory.Children("utility-pouch", "inside").Single().Id == "spare-dressing", "pouch lost its loaded item");
        Click(Target("utility-pouch"), button: MouseButton.Right); await InventoryFrames();
        var menu = panel.GetChildren().OfType<PanelContainer>().Last().GetChild<VBoxContainer>(0).GetChildren().OfType<Button>();
        Check(menu.Any(b => b.Text == "Open") && menu.Any(b => b.Text == "Remove → hand"), "pouch sprite bypassed ordinary context actions");
        Click(new(600, 55)); await InventoryFrames();
        before = sim.Save(); Check(!sim.HandleInventory(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "hand"))) && sim.Save() == before,
            "paused loaded belt removal succeeded");
        sim.ManualPause = false; Check(sim.HandleInventory(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "hand"))), sim.LastMessage);
        Check(sim.Inventory.Pending is not null && proof.Art.Layers(sim).Any(l => l.ItemId == "utility-pouch"), "loaded outfit vanished before paid removal completed");
        var pending = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(pending); Check(clone.Save() == pending, "pending loaded assembly failed exact restore");
        Check(proof.Art.Layers(clone).Select(l => l.ItemId).SequenceEqual(proof.Art.Layers(sim).Select(l => l.ItemId)), "restored outfit differs while work is pending");
        await Finish();
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId is "utility-belt" or "utility-pouch")
            && sim.Inventory.Children("utility-belt", "webbing").Single().Id == "utility-pouch"
            && sim.Inventory.Children("utility-pouch", "inside").Single().Id == "spare-dressing", "held provider remained worn or lost its descendants");
        await InventoryCapture("-paperdoll-belt-removed");
        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("fixture:crate-a", "store", 0, 8)));
        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt", BodyPose: new(-4, 10, 2))));
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId is "utility-belt" or "utility-pouch") && proof.Art.Omitted(sim).Contains("Utility belt")
            && proof.Art.Omitted(sim).Contains("Utility pouch"), "unqualified non-waist fit silently borrowed waist artwork");
        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        Click(Target("utility-pouch"), true); await InventoryFrames(); await InventoryCapture("-paperdoll-belt-loaded");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Inspect("avatar-shirt"); await InventoryFrames();
        Check(proof.Side == "front" && sim.Save() == original, "belt fixture failed to restore original physical state");
        GD.Print("PAPERDOLL_BELT_SMOKE_PASS: real waist/front-back belt, direct pouch position/six rotations/alpha picking, own exterior inspection, paid flap, loaded source retention/removal and restore; unqualified poses omitted.");
    }
}
