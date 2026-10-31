using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunPaperdollSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var original = sim.Save();
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
        Check(layers.Select(l => l.ItemId).SequenceEqual(new string?[] { null, "avatar-shirt", "avatar-coat" }), "actual own dressing order/bindings changed or leaked foreign gear");
        Check(layers[^1].Column == 3 && proof.Art.Omitted(sim).Any(n => n.Contains("pack", StringComparison.OrdinalIgnoreCase)), "open jacket or explicit missing-gear report incorrect");
        Vector2 Find(Func<Vector2, bool> predicate)
        {
            for (var y = .04f; y < .96f; y += .008f)
            for (var x = .04f; x < .96f; x += .008f)
                if (predicate(new(x, y))) return new(x, y);
            throw new InvalidOperationException("Paperdoll: required alpha registration region missing");
        }
        string? PickUv(Vector2 uv) { var b = proof.Art.Bounds(surface.Size); return surface.Pick(b.Position + uv * b.Size); }
        var shirtUv = Find(uv => !proof.Art.HeadOpaque("front", uv) && proof.Art.Opaque(layers[1], "front", uv) && !proof.Art.Opaque(layers[2], "front", uv)
            && proof.Art.Opaque(new("avatar-coat", 2), "front", uv));
        var headUv = Find(uv => proof.Art.HeadOpaque("front", uv) && proof.Art.Opaque(layers[2], "front", uv));
        Check(PickUv(headUv) is null, "far collar selected through the foreground head/neck");
        var coatUv = Find(uv => !proof.Art.HeadOpaque("front", uv) && proof.Art.Opaque(layers[1], "front", uv) && proof.Art.Opaque(layers[2], "front", uv));
        Check(PickUv(shirtUv) == "avatar-shirt" && PickUv(coatUv) == "avatar-coat", "open gap/outer sprite alpha picking disagrees with draw order");
        Check(surface.Pick(Vector2.Zero) is null && surface.Pick(new(surface.Size.X + 2, 10)) is null, "transparent margin or outside area selected an item");
        var blank = Find(uv => layers.All(l => !proof.Art.Opaque(l, "front", uv)));
        Check(PickUv(blank) is null, "atlas padding became a hit target");
        Vector2 At(Vector2 uv) { var b = proof.Art.Bounds(surface.Size); return surface.GlobalPosition + b.Position + uv * b.Size; }
        Click(At(shirtUv), true); await InventoryFrames();
        Check(panel.SelectedItem == "avatar-shirt" && panel.Browser.CurrentGear == "avatar-shirt"
            && !sim.InventoryMoveAccess("avatar-shirt").Allowed && sim.Save() == before, "alpha selection bypassed access or changed physical state");
        await InventoryCapture("-paperdoll-open");
        var oldSize = surface.Size; surface.Size = new(300, 270);
        Check(PickUv(shirtUv) == "avatar-shirt" && PickUv(coatUv) == "avatar-coat", "letterbox resize changed source-alpha picking"); surface.Size = oldSize;
        Click(proof.Jacket.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(sim.Save() == before && sim.Inventory.Pending is null && proof.Art.Layers(sim)[^1].Column == 3, "paused art control did free fastening");
        sim.ManualPause = false; Click(proof.Jacket.GetGlobalRect().GetCenter());
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.CloseGarment && proof.Art.Layers(sim)[^1].Column == 3,
            "sprite changed before normal paid fastening committed");
        var pending = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(pending); Check(clone.Save() == pending, "appearance control changed continuation compatibility");
        await Finish();
        Check(proof.Art.Layers(sim)[^1].Column == 2 && PickUv(shirtUv) == "avatar-coat", "committed closure did not occlude the same shirt pixel");
        Click(At(shirtUv)); await InventoryFrames(); Check(panel.SelectedItem == "avatar-coat", "closed artwork click picked through coat");
        await InventoryCapture("-paperdoll-closed");
        before = sim.Save(); Click(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(proof.Side == "back" && sim.Save() == before, "flipping art performed physical work");
        var back = Find(uv => !proof.Art.HeadOpaque("back", uv) && proof.Art.Opaque(new("avatar-coat", 2), "back", uv));
        Check(PickUv(back) == "avatar-coat", "back artwork used front picking"); await InventoryCapture("-paperdoll-back");
        panel.Close(); panel.Open(); Click(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(proof.Side == "front", "inventory reopen retained art inspection side");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = false;
        GD.Print("PAPERDOLL_SMOKE_PASS: source-alpha picking, layered tint/occlusion, open/closed/front/back sprites, own outfit identity, missing-gear disclosure, resize, paid fastening and continuation.");
    }
}
