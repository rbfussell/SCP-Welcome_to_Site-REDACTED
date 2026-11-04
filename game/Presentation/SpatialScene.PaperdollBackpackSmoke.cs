using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    /// <summary>Worn pack/strap pixels route to the real segmented container; physical work and contents retain their existing authority.</summary>
    private async Task RunPaperdollBackpackSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof; var surface = proof.Surface;
        var original = sim.Save();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Paperdoll backpack: " + reason); }
        void Click(Vector2 at, bool twice = false) { Input.WarpMouse(at); InventoryClick(at, twice); }
        async Task Finish()
        {
            sim.ManualPause = false;
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while (sim.Inventory.Pending is not null && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "paid pack handling timed out"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request) { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        Vector2 Target(string id)
        {
            var bounds = proof.Art.Bounds(surface.Size);
            for (var y = .19f; y < .52f; y += .008f)
            for (var x = .15f; x < .85f; x += .008f)
            {
                var at = bounds.Position + new Vector2(x, y) * bounds.Size;
                if (surface.Pick(at) == id) return surface.GlobalPosition + at;
            }
            throw new InvalidOperationException("Paperdoll backpack: no visible pack/strap target for " + id + " on " + proof.Side);
        }
        async Task Flip() { Click(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames(); }
        InventoryGearSurface Gear() => panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        Check(proof.Art.Layers(sim).Single(l => l.ItemId == "avatar-backpack").Column == 8, "back mount chose the wrong visual frame");
        var strapAt = Target("avatar-backpack"); var bounds = proof.Art.Bounds(surface.Size);
        var strapUv = (strapAt - surface.GlobalPosition - bounds.Position) / bounds.Size;
        var smaller = proof.Art.Bounds(new(300, 270));
        Check(proof.Art.Pick(sim, "front", new(300, 270), smaller.Position + strapUv * smaller.Size) == "avatar-backpack",
            "resized strap target diverged from the shared art transform");
        Check(surface.Pick(bounds.Position + new Vector2(.5f, .4f) * bounds.Size) == "avatar-shirt",
            "transparent harness gap became a rectangular pack target");
        Click(strapAt, true); await InventoryFrames();
        Check(panel.Browser.CurrentGear == "avatar-backpack" && panel.SelectedItem == "avatar-backpack"
            && panel.Browser.CurrentContents is null && sim.Save() == original, "strap inspection exposed contents or performed work");
        var atMain = Gear().HotspotPosition("main"); Click(atMain, true); await InventoryFrames();
        Check(panel.Browser.CurrentContents == ("avatar-backpack", "main")
            && panel.FindGrid("avatar-backpack", "main").DisplayedItems.Any(i => i.Id == "medical-case"), "existing main segment lost its real contents");
        Click(Gear().HotspotPosition("side"), true); await InventoryFrames();
        Check(panel.Browser.CurrentContents == ("avatar-backpack", "side"), "side segment did not open independently");
        await InventoryCapture("-pack-straps-and-side");
        await Flip();
        foreach (var uv in new[] { new Vector2(.3f, .215f), new Vector2(.7f, .215f) })
        {
            var rearBounds = proof.Art.Bounds(surface.Size);
            Check(surface.Pick(rearBounds.Position + uv * rearBounds.Size) == "avatar-backpack",
                "rear shoulder harness was missing or picked the coat through its material");
        }
        Click(Target("avatar-backpack"), true); await InventoryFrames();
        Check(panel.Browser.CurrentGear == "avatar-backpack" && sim.Save() == original, "rear bag inspection changed ownership or closures");
        var paused = sim.Save(); panel.ChangeLid("avatar-backpack", "main", false); await InventoryFrames();
        Check(sim.Save() == paused && sim.Inventory.IsOpen("avatar-backpack", "main"), "paused pack inspection supplied free zipper work");
        sim.ManualPause = false; panel.ChangeLid("avatar-backpack", "main", false);
        Check(sim.Inventory.Pending is not null && sim.Inventory.IsOpen("avatar-backpack", "main"), "zip state changed before completion"); await Finish();
        Check(!sim.Inventory.IsOpen("avatar-backpack", "main"), "paid zip did not secure the actual compartment");
        Click(Gear().HotspotPosition("main"), true); await InventoryFrames();
        Check(sim.Inventory.Pending is null && panel.Browser.CurrentContents is null, "paused closed main compartment leaked contents");
        sim.ManualPause = false; Click(Gear().HotspotPosition("main"), true);
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.Open && !sim.Inventory.IsOpen("avatar-backpack", "main"), "main segment bypassed paid unzip");
        await Finish(); Check(panel.Browser.CurrentContents == ("avatar-backpack", "main"), "completed unzip failed to reveal main contents");
        await InventoryCapture("-pack-back-and-main");
        var remove = new InventoryRequest(InventoryActionKind.Move, "avatar-backpack", new("person:avatar", "hand"));
        paused = sim.Save(); Check(!sim.HandleInventory(remove) && sim.Save() == paused, "paused body removal supplied free pack handling");
        sim.ManualPause = false; Check(sim.HandleInventory(remove), sim.LastMessage);
        Check(proof.Art.Layers(sim).Any(l => l.ItemId == "avatar-backpack"), "pack vanished before removal committed");
        var saved = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(saved); Check(clone.Save() == saved && proof.Art.Layers(clone).Any(l => l.ItemId == "avatar-backpack"), "paid removal lost exact continuation or worn appearance");
        await Finish();
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId == "avatar-backpack")
            && sim.Inventory.DiagnosticItems.Single(i => i.Id == "medical-case").Placement.ParentId == "avatar-backpack", "held pack stayed worn or detached its contents");
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("person:avatar", "front")));
        Check(proof.Art.Layers(sim).Single(l => l.ItemId == "avatar-backpack").Column == 9, "front mount did not switch bag/strap sides");
        Click(Target("avatar-backpack"), true); await InventoryFrames(); await InventoryCapture("-front-pack-rear-straps");
        await Flip(); Click(Target("avatar-backpack"), true); await InventoryFrames(); await InventoryCapture("-front-pack-bag");
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("person:avatar", "back")));
        await Act(new(InventoryActionKind.Move, "spare-backpack", new("person:avatar", "front")));
        Check(proof.Art.Layers(sim).Where(l => l.Column is 8 or 9).Select(l => l.ItemId)
            .SequenceEqual(new[] { "avatar-backpack", "spare-backpack" }), "two opposite mounts did not retain two distinct physical packs");
        Click(Target("spare-backpack"), true); await InventoryFrames();
        Check(panel.Browser.CurrentGear == "spare-backpack", "front pack selected another bag's inventory");
        await InventoryCapture("-two-packs-front");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Inspect("avatar-shirt"); await InventoryFrames();
        Check(proof.Side == "front" && sim.Save() == original, "pack fixture failed to restore the original outfit/state");
        GD.Print("PAPERDOLL_BACKPACK_SMOKE_PASS: pack/strap sides, own identity, segmented compartments, paid closures/removal, loaded continuation, front mount and two independent packs.");
    }
}
