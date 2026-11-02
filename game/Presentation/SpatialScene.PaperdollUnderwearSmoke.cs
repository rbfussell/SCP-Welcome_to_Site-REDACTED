using Godot;
using SiteSimulation;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    /// <summary>Prepared helper-held outer gear is fixture setup only; exposing and removing briefs still uses paid game transactions.</summary>
    private async Task RunPaperdollUnderwearSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof; var surface = proof.Surface;
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Paperdoll underwear: " + reason); }
        async Task Finish()
        {
            sim.ManualPause = false;
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while (sim.Inventory.Pending is not null && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, $"paid handling did not finish: {sim.Inventory.Pending}"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest r) { sim.ManualPause = false; Check(sim.HandleInventory(r), sim.LastMessage); await Finish(); }
        await Act(new(InventoryActionKind.AutoPlace, "avatar-socks", new("avatar-backpack", "main")));
        var staged = JsonNode.Parse(sim.Save())!;
        foreach (var (id, helper) in new[] { ("avatar-backpack", "person:observer"), ("avatar-coat", "person:tech") })
            staged["Inventory"]!["Items"]!.AsArray().Single(n => n!["Id"]!.GetValue<string>() == id)!["Placement"]
                = JsonSerializer.SerializeToNode(new ItemPlacement(helper, "hand"));
        sim.Restore(staged.ToJsonString()); _runtime.ResetAccumulator(); await InventoryFrames();
        await Act(new(InventoryActionKind.Move, "spare-backpack", new("person:avatar", "front")));
        await Act(new(InventoryActionKind.AutoPlace, "avatar-trousers", new("fixture:crate-a", "store")));
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId == "avatar-trousers"), "stored trousers remained drawn on body");
        Vector2? exposed = null; var bounds = proof.Art.Bounds(surface.Size);
        for (var y = .44f; y < .68f && exposed is null; y += .008f)
        for (var x = .15f; x < .85f; x += .008f)
            if (surface.Pick(bounds.Position + new Vector2(x, y) * bounds.Size) == "avatar-underwear") { exposed = new(x, y); break; }
        Check(exposed is not null && sim.InventoryMoveAccess("avatar-underwear").Allowed, "real underwear did not become an exposed alpha target");
        var at = surface.GlobalPosition + bounds.Position + exposed!.Value * bounds.Size;
        Input.WarpMouse(at); InventoryClick(at, true); await InventoryFrames();
        Check(panel.SelectedItem == "avatar-underwear" && panel.Browser.CurrentGear == "avatar-underwear", "visible briefs did not select/inspect their actual identity");
        await InventoryCapture("-paperdoll-underwear");
        var request = new InventoryRequest(InventoryActionKind.Move, "avatar-underwear", new("person:avatar", "hand"));
        var before = sim.Save(); Check(!sim.HandleInventory(request) && sim.Save() == before, "paused briefs removal supplied free work");
        sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage);
        Check(proof.Art.Layers(sim).Any(l => l.ItemId == "avatar-underwear"), "briefs vanished before work committed");
        var saved = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(saved); Check(clone.Save() == saved, "new loadout removal failed exact restore");
        await Finish();
        Check(!proof.Art.Layers(sim).Any(l => l.ItemId == "avatar-underwear")
            && surface.Pick(bounds.Position + exposed.Value * bounds.Size) is null, "visual modesty base became a false worn underwear item");
        await InventoryCapture("-paperdoll-modesty-base");
    }
}
