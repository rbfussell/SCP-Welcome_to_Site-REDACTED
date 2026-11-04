using Godot;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    /// <summary>Qualify worn-only shoe holes, unchanged loose source pixels, both feet/sides, picking and paid-state isolation.</summary>
    private async Task RunPaperdollMaskSmoke()
    {
        var sim = _simulation; var proof = _hud.Inventory.PaperdollProof; var surface = proof.Surface;
        var before = sim.Save(); var shoes = proof.Art.Layers(sim).Single(l => l.ItemId == "avatar-shoes");
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Worn mask: " + reason); }
        Check(PaperdollWornMask.Valid(null) && PaperdollWornMask.Valid([]), "optional masks rejected");
        Check(!PaperdollWornMask.Valid([[[float.NaN, 0], [1, 0], [0, 1]]])
            && !PaperdollWornMask.Valid([[[-.1f, 0], [1, 0], [0, 1]]])
            && !PaperdollWornMask.Valid([[[0, 0], [0, 0], [0, 0]]]), "invalid mask geometry accepted");
        using (var original = Image.CreateEmpty(10, 10, false, Image.Format.Rgba8))
        {
            original.Fill(new Color(.4f, .2f, .1f, .8f));
            using var copy = original.GetRegion(new(0, 0, 10, 10));
            PaperdollWornMask.Apply(copy, [[[0, 0], [.5f, 0], [.5f, .5f], [0, .5f]]]);
            Check(copy.GetPixel(2, 2).A == 0 && copy.GetPixel(8, 8) == original.GetPixel(8, 8)
                && original.GetPixel(2, 2).A > .7f && copy.GetPixel(2, 2).R == original.GetPixel(2, 2).R,
                "mask altered loose pixels, surviving pixels or source RGB");
        }
        var looseBefore = InventoryArt.Opaque("boots", new(.5f, .5f));
        foreach (var side in new[] { "front", "back" })
        {
            if (proof.Side != side) { InventoryClick(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames(); }
            foreach (var left in new[] { true, false })
            {
                Vector2? hole = null; var retained = false;
                for (var y = .865f; y < .985f; y += .002f)
                for (var x = left ? .1f : .65f; x < (left ? .4f : .91f); x += .002f)
                {
                    var uv = new Vector2(x, y);
                    var loose = proof.Art.Opaque(shoes, side, uv, worn: false);
                    var worn = proof.Art.Opaque(shoes, side, uv);
                    Check(!worn || loose, "mask added pixels outside the source shoe");
                    if (loose && !worn) hole ??= uv;
                    if (worn) retained = true;
                }
                Check(hole is not null && retained, "missing opening cutout or shoe erased on " + side);
                var b = proof.Art.Bounds(surface.Size); var at = b.Position + hole!.Value * b.Size;
                Check(surface.Pick(at) != "avatar-shoes" && proof.Art.Opaque(shoes, side, hole.Value, worn: false),
                    "masked opening still selects the shoe, or loose source was destroyed");
                var size = surface.Size; surface.Size = new(300, 270); b = proof.Art.Bounds(surface.Size);
                Check(surface.Pick(b.Position + hole.Value * b.Size) != "avatar-shoes", "masked picking drifted after resize");
                surface.Size = size;
            }
            await InventoryCapture("-worn-mask-" + side);
        }
        InventoryClick(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(proof.Side == "front" && sim.Save() == before && InventoryArt.Opaque("boots", new(.5f, .5f)) == looseBefore,
            "visual masking changed equipment, paid work, loose art or simulation state");
        GD.Print("PAPERDOLL_MASK_SMOKE_PASS: both feet/front-back worn holes, retained source/loose pixels, mask validation, shared alpha picking, resize and unchanged physical state.");
    }
}
