using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunWornStorageSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof; var surface = proof.Surface; var original = sim.Save();
        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException("Worn storage: " + message); }
        async Task Finish()
        {
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while (sim.Inventory.Pending is not null && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "paid work timed out"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request) { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        Vector2 Point(string id, bool storage)
        {
            var bounds = proof.Art.Bounds(surface.Size);
            for (var y = .18f; y < .7f; y += .004f)
            for (var x = .2f; x < .85f; x += .004f)
            {
                var global = (surface.GlobalPosition + bounds.Position + new Vector2(x, y) * bounds.Size).Round();
                var at = global - surface.GlobalPosition;
                var neighbors = new[] { Vector2.Zero, Vector2.Left, Vector2.Right, Vector2.Up, Vector2.Down };
                if (neighbors.All(offset => surface.Pick(at + offset) == id
                    && (proof.Art.StorageTarget(sim, proof.Side, surface.Size, at + offset)?.Owner == id) == storage)) return global;
            }
            throw new InvalidOperationException("Worn storage: missing visible target " + id);
        }
        InventoryItem Item(string id) => sim.Inventory.DiagnosticItems.Single(i => i.Id == id);
        Vector2 Source(string id)
        {
            var p = Item(id).Placement; var grid = panel.FindGrid(p.ParentId, p.RegionId);
            return grid.GlobalPosition + new Vector2(1 + p.X * grid.CellPixels + 8, 44 + p.Y * grid.CellPixels + 8);
        }
        async Task Flip() { InventoryClick(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames(); }
        if (sim.Inventory.IsOpen("avatar-backpack", "main")) await Act(new(InventoryActionKind.Close, "avatar-backpack", RegionId: "main"));
        await Act(sim.GroundDropRequest("spare-dressing")); var before = sim.Save();
        await InventoryDrag(Source("spare-dressing"), Point("avatar-backpack", false), async () =>
        { Check(surface.StorageTarget is null && surface.DropPreview is null, "front harness became a storage target"); await InventoryFrames(); });
        Check(sim.Save() == before, "harness release moved item"); await Flip();
        var bag = Point("avatar-backpack", true);
        await InventoryDrag(Source("spare-dressing"), bag, async () =>
        { Check(surface.StoragePreview is { Allowed: false } && surface.DropPreview is null, "closed main target: " + surface.StorageTarget + " / " + surface.StoragePreview + " / fit=" + surface.DropPreview); await InventoryCapture("-worn-storage-closed"); });
        Check(sim.Save() == before && !sim.Inventory.IsOpen("avatar-backpack", "main"), "closed bag drop changed state");
        await Act(new(InventoryActionKind.Open, "avatar-backpack", RegionId: "main")); before = sim.Save();
        await InventoryDrag(Source("spare-dressing"), bag, async () =>
        {
            Check(surface.StorageTarget is { Owner: "avatar-backpack", Region: "main" } && surface.StoragePreview is { Allowed: true }, "open bag missed normal packing preview");
            var body = panel.BodyRotation; var mount = panel.AttachmentRotation; InventoryKey(Key.R); await InventoryFrames();
            Check(panel.BodyRotation == body && panel.AttachmentRotation == mount && sim.Save() == before, "R over storage changed fit/mount state");
            await InventoryCapture("-worn-storage-open");
        }); Check(sim.Save() == before && sim.Inventory.Pending is null, "paused bag release supplied free storage");
        sim.ManualPause = false; await InventoryDrag(Source("spare-dressing"), bag);
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.AutoPlace && Item("spare-dressing").Placement.ParentId.StartsWith("ground:", StringComparison.Ordinal), "storage release bypassed paid source retention");
        var pending = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(pending); Check(clone.Save() == pending, "sprite storage continuation did not restore exactly"); await Finish();
        Check(Item("spare-dressing").Placement.ParentId == "avatar-backpack", "storage commitment addressed another location");
        before = sim.Save(); await InventoryDrag(bag, bag, async () =>
        { Check(surface.StoragePreview is { Allowed: false }, "bag accepted itself"); await InventoryFrames(); });
        Check(sim.Save() == before, "self-drop changed inventory"); await Flip();
        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        await Act(new(InventoryActionKind.Move, "utility-pouch", new("utility-belt", "webbing", AttachmentPose: new(2, 0))));
        var pouch = Point("utility-pouch", true);
        panel.Inspect("avatar-backpack"); await InventoryFrames();
        var gear = panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        InventoryClick(gear.HotspotPosition("main"), true); await InventoryFrames();
        Check(panel.Browser.CurrentContents == ("avatar-backpack", "main"), "source branch was not open"); before = sim.Save();
        await InventoryDrag(Source("spare-dressing"), pouch, async () =>
        { Check(surface.StoragePreview is { Allowed: false }, "closed pouch auto-opened"); await InventoryFrames(); });
        Check(sim.Save() == before, "closed pouch release changed state");
        await Act(new(InventoryActionKind.Open, "utility-pouch", RegionId: "inside"));
        await InventoryDrag(Source("spare-dressing"), pouch, async () =>
        {
            Check(surface.StoragePreview is { Allowed: true }, "open pouch refused fitting item");
            Check(panel.Browser.CurrentContents == ("avatar-backpack", "main"), "sprite target replaced source branch"); sim.ManualPause = false; await InventoryFrames();
        }); await Finish();
        Check(Item("spare-dressing").Placement.ParentId == "utility-pouch" && panel.Browser.CurrentContents == ("avatar-backpack", "main"), "transfer lost source view or exact destination");
        await Act(sim.GroundDropRequest("chemical-suit")); before = sim.Save();
        await InventoryDrag(Source("chemical-suit"), pouch, async () =>
        { Check(surface.StoragePreview is { Allowed: false } && surface.DropPreview is null, "oversized clothing fell through to body fitting"); await InventoryCapture("-worn-storage-full"); });
        Check(sim.Save() == before, "failed packing moved or fitted clothing");
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("person:avatar", "front")));
        Check(proof.Art.StorageTarget(sim, "front", surface.Size, Point("avatar-backpack", true) - surface.GlobalPosition)?.Region == "main", "front-mounted bag lost storage registration");
        await Flip(); Point("avatar-backpack", false); await Flip();
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Inspect("avatar-shirt"); await InventoryFrames();
        GD.Print("WORN_STORAGE_SMOKE_PASS: bag-only front/back alpha targets, closed/paused/full/self refusals, paid packing and exact restore, retained source branch, worn pouch transfer and no body-fit fallback.");
    }
}
