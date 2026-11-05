using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunPaperdollSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var original = sim.Save(); sim.Player.Cell = new(13, 6);
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Paperdoll: " + reason); }
        void Click(Vector2 at, bool twice = false) { Input.WarpMouse(at); InventoryClick(at, twice); }
        async Task Finish()
        {
            sim.ManualPause = false;
            for (var i = 0; i < 400 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "paid handling did not complete"); sim.ManualPause = true; await InventoryFrames();
        }
        Click(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        var proof = panel.PaperdollProof; var surface = proof.Surface;
        Check(proof.Visible && !panel.BodyFit.Visible && proof.Side == "front", "art view did not remain independent of fit tools");
        sim.ManualPause = true; var before = sim.Save();
        var layers = proof.Art.Layers(sim);
        Check(layers.Select(l => l.ItemId).SequenceEqual(new string?[] { null, "avatar-underwear", "avatar-socks", "avatar-trousers", "avatar-shirt", "avatar-shoes", "avatar-coat", "avatar-backpack" }), "actual own dressing order/bindings changed or leaked foreign gear");
        Check(layers.Single(l => l.ItemId == "avatar-coat").Column == 3 && proof.Art.Omitted(sim).Length == 0,
            "open jacket, backpack binding or omission report incorrect");
        await RunPaperdollBackpackSmoke(); await RunPaperdollMaskSmoke(); await RunPaperdollBeltSmoke(); await RunPaperdollHookSmoke(); await RunPaperdollDropSmoke(); await RunWornStorageSmoke(); before = sim.Save();
        Vector2 Find(Func<Vector2, bool> predicate)
        {
            for (var y = .04f; y < .96f; y += .008f)
            for (var x = .04f; x < .96f; x += .008f)
                if (predicate(new(x, y))) return new(x, y);
            throw new InvalidOperationException("Paperdoll: required alpha registration region missing");
        }
        string? PickUv(Vector2 uv) { var b = proof.Art.Bounds(surface.Size); return surface.Pick(b.Position + uv * b.Size); }
        Check(new[] { "front", "back" }.All(side => !proof.Art.MosaicVisible(sim, side, new(.5f, .55f))),
            "privacy mosaic painted over committed clothing");
        var pack = layers.Single(l => l.ItemId == "avatar-backpack");
        var shirt = layers.Single(l => l.ItemId == "avatar-shirt"); var coat = layers.Single(l => l.ItemId == "avatar-coat");
        var trousers = layers.Single(l => l.ItemId == "avatar-trousers"); var socks = layers.Single(l => l.ItemId == "avatar-socks");
        var shoes = layers.Single(l => l.ItemId == "avatar-shoes"); var underwear = layers.Single(l => l.ItemId == "avatar-underwear");
        var shirtUv = Find(uv => !proof.Art.Opaque(pack, "front", uv) && !proof.Art.HeadOpaque("front", uv) && proof.Art.Opaque(shirt, "front", uv) && !proof.Art.Opaque(coat, "front", uv)
            && proof.Art.Opaque(new("avatar-coat", 2), "front", uv));
        var headUv = Find(uv => proof.Art.HeadOpaque("front", uv));
        Check(PickUv(headUv) is null, "far collar selected through the foreground head/neck");
        var coatUv = Find(uv => !proof.Art.Opaque(pack, "front", uv) && !proof.Art.HeadOpaque("front", uv) && proof.Art.Opaque(shirt, "front", uv) && proof.Art.Opaque(coat, "front", uv));
        Check(PickUv(shirtUv) == "avatar-shirt" && PickUv(coatUv) == "avatar-coat", "open gap/outer sprite alpha picking disagrees with draw order");
        Check(surface.Pick(Vector2.Zero) is null && surface.Pick(new(surface.Size.X + 2, 10)) is null, "transparent margin or outside area selected an item");
        var blank = Find(uv => layers.All(l => !proof.Art.Opaque(l, "front", uv)));
        Check(PickUv(blank) is null, "atlas padding became a hit target");
        Vector2 At(Vector2 uv) { var b = proof.Art.Bounds(surface.Size); return surface.GlobalPosition + b.Position + uv * b.Size; }
        Click(At(shirtUv), true); await InventoryFrames();
        Check(panel.SelectedItem == "avatar-shirt" && panel.Browser.CurrentGear == "avatar-shirt"
            && !sim.InventoryMoveAccess("avatar-shirt").Allowed && sim.Save() == before, "alpha selection bypassed access or changed physical state");
        var concealed = Find(uv => proof.Art.Opaque(underwear, "front", uv) && proof.Art.Opaque(trousers, "front", uv));
        Check(PickUv(concealed) != "avatar-underwear" && !sim.InventoryMoveAccess("avatar-underwear").Allowed, "trousers did not cover the real underwear");
        var feet = new[] { true, false }.Select(left => Find(uv => (uv.X < .5f) == left && proof.Art.Opaque(socks, "front", uv)
            && proof.Art.Opaque(shoes, "front", uv) && !proof.Art.Opaque(trousers, "front", uv))).ToArray();
        Check(feet.All(uv => PickUv(uv) == "avatar-shoes"), "paired footwear parts did not share one real item target over socks");
        await InventoryCapture("-paperdoll-open");
        var oldSize = surface.Size; surface.Size = new(300, 270);
        Check(PickUv(shirtUv) == "avatar-shirt" && PickUv(coatUv) == "avatar-coat", "letterbox resize changed source-alpha picking"); surface.Size = oldSize;
        Click(proof.Jacket.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(sim.Save() == before && sim.Inventory.Pending is null && proof.Art.Layers(sim).Single(l => l.ItemId == "avatar-coat").Column == 3, "paused art control did free fastening");
        sim.ManualPause = false; Click(proof.Jacket.GetGlobalRect().GetCenter());
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.CloseGarment && proof.Art.Layers(sim).Single(l => l.ItemId == "avatar-coat").Column == 3,
            "sprite changed before normal paid fastening committed");
        var pending = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(pending); Check(clone.Save() == pending, "appearance control changed continuation compatibility");
        await Finish();
        Check(proof.Art.Layers(sim).Single(l => l.ItemId == "avatar-coat").Column == 2 && PickUv(shirtUv) == "avatar-coat", "committed closure did not occlude the same shirt pixel");
        Click(At(shirtUv)); await InventoryFrames(); Check(panel.SelectedItem == "avatar-coat", "closed artwork click picked through coat");
        await InventoryCapture("-paperdoll-closed");
        before = sim.Save(); Click(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(proof.Side == "back" && sim.Save() == before, "flipping art performed physical work");
        var back = Find(uv => !proof.Art.Opaque(pack, "back", uv) && !proof.Art.HeadOpaque("back", uv) && proof.Art.Opaque(new("avatar-coat", 2), "back", uv));
        Check(PickUv(back) == "avatar-coat", "back artwork used front picking");
        foreach (var left in new[] { true, false })
        {
            var rearFoot = Find(uv => (uv.X < .5f) == left && proof.Art.Opaque(shoes, "back", uv) && !proof.Art.Opaque(trousers, "back", uv));
            Check(PickUv(rearFoot) == "avatar-shoes", "back paired footwear drifted from its instance");
        }
        await InventoryCapture("-paperdoll-back");
        Click(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames();
        // Context removal is ordinary paid work, not an appearance-only toggle.
        Input.WarpMouse(At(feet[0])); InventoryClick(At(feet[0]), button: MouseButton.Right); await InventoryFrames();
        var menu = panel.GetChildren().OfType<PanelContainer>().Last();
        var removeButton = menu.GetChild<VBoxContainer>(0).GetChildren().OfType<Button>().Single(b => b.Text == "Remove → hand");
        before = sim.Save(); Click(removeButton.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(sim.Save() == before && proof.Art.Layers(sim).Any(l => l.ItemId == "avatar-shoes"), "paused sprite context supplied free footwear removal");
        sim.ManualPause = false;
        Input.WarpMouse(At(feet[0])); InventoryClick(At(feet[0]), button: MouseButton.Right); await InventoryFrames();
        menu = panel.GetChildren().OfType<PanelContainer>().Last();
        removeButton = menu.GetChild<VBoxContainer>(0).GetChildren().OfType<Button>().Single(b => b.Text == "Remove → hand");
        Click(removeButton.GetGlobalRect().GetCenter());
        Check(sim.Inventory.Pending?.Request.ItemId == "avatar-shoes" && PickUv(feet[0]) == "avatar-shoes", "footwear disappeared before paid removal committed");
        await Finish();
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId == "avatar-shoes") && feet.All(uv => PickUv(uv) == "avatar-socks"), "held shoes remained painted on the body or failed to expose both socks");
        await InventoryCapture("-paperdoll-socks");
        sim.ManualPause = false; Check(sim.HandleInventory(new(InventoryActionKind.AutoPlace, "avatar-shoes", new("fixture:crate-a", "store"))), sim.LastMessage); await Finish();
        sim.ManualPause = false; Check(sim.HandleInventory(new(InventoryActionKind.Move, "avatar-socks", new("person:avatar", "hand"))), sim.LastMessage); await Finish();
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId == "avatar-socks") && feet.All(uv => PickUv(uv) is null), "removed socks or base body became false footwear targets");
        await InventoryCapture("-paperdoll-bare-feet");
        await RunPaperdollUnderwearSmoke();
        panel.Close(); panel.Open(); Click(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(proof.Side == "front", "inventory reopen retained art inspection side");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = false;
        GD.Print("PAPERDOLL_SMOKE_PASS: full-body own outfit, covered underwear, paired feet, source-alpha picking/occlusion, front/back, resize, paid fastening/removal, exposed socks/bare feet and continuation.");
    }
}
