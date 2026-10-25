using SitePresentation;
using SiteSimulation;
using System.Buffers.Binary;
using System.Text.Json.Nodes;

internal static class SpriteArtChecks
{
    public static int Run(string map, string path)
    {
        var count = 0;
        void Check(bool ok, string detail) { if (!ok) throw new Exception("Sprite art: " + detail); count++; }
        var text = File.ReadAllText(path); var catalog = new SpriteArtCatalog(text);
        var manifest = catalog.Manifest;
        var assets = Path.GetDirectoryName(Path.GetDirectoryName(path))!;
        foreach (var (file, size) in manifest.Images)
        {
            var bytes = File.ReadAllBytes(Path.Combine(assets, file));
            Check(bytes.Length > 24 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71,
                "each binding must resolve to a real PNG");
            Check(BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)) == size[0]
                && BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)) == size[1], "source image dimensions must match manifest");
        }
        var sim = new SpatialSimulation(map); var before = sim.Save();
        foreach (var actor in sim.Actors)
        foreach (var state in new[] { "idle", "walk" })
        for (var facing = 0; facing < 6; facing++)
        for (var frame = 0; frame < (state == "idle" ? 1 : 3); frame++)
        {
            var key = catalog.Layers(actor.Id, state, facing, state == "idle" ? 0 : frame / 3.0).Single();
            Check(manifest.Sprites.ContainsKey(key) && key.Contains(state) && key.Contains($"-{facing}-{frame}"), "all actor/state/facing/frames need explicit artwork");
            var sprite = manifest.Sprites[key];
            Check(sprite.Scale * sprite.Region[3] is > 50 and < 80, "registered character frames must share a useful scene scale");
        }
        Check(catalog.Layers("observer", "idle", 2, 0).Single() != catalog.Layers("tech", "idle", 2, 0).Single(),
            "NPC identity selects independent artwork rather than one shared NPC sprite");
        Check(catalog.Layers("unskinned-future-character", "idle", 0, 0).Length == 0, "unbound future characters must retain diagnostic fallback");
        Check(sim.Save() == before, "art resolution cannot mutate clocks, movement, visibility, RNG or module state");
        foreach (var surface in new[] { "floor", "land", "wall", "door" }) Check(manifest.Surfaces.ContainsKey(surface), "surface family complete: " + surface);
        foreach (var prop in new[] { "crate", "cabinet", "terminal", "trolley" }) Check(manifest.Objects.ContainsKey(prop), "current prop family complete: " + prop);
        var layered = JsonNode.Parse(text)!;
        var layers = layered["actors"]!["avatar"]!["layers"]!.AsArray();
        var extra = layers[0]!.DeepClone(); extra["id"] = "head-overlay-fixture"; layers.Add(extra);
        var compositor = new SpriteArtCatalog(layered.ToJsonString());
        Check(compositor.Layers("avatar", "walk", 3, .5).Length == 2, "ordered presentation layers must resolve the same rig/state/facing/phase");
        void Reject(Action<JsonNode> change)
        {
            var bad = JsonNode.Parse(text)!; change(bad);
            try { _ = new SpriteArtCatalog(bad.ToJsonString()); throw new Exception("corrupt sprite registration accepted"); }
            catch (ArgumentException) { count++; }
        }
        Reject(n => n["sprites"]!["facility-player-idle-0-0"]!["anchor"]![0] = 9999);
        Reject(n => n["sprites"]!["facility-player-idle-0-0"]!["region"]![2] = 99999);
        Reject(n => n["actors"]!["avatar"]!["layers"]![0]!["states"]!["walk"]![0]![0] = "missing-frame");
        Reject(n => n["rigs"]!["human-isometric-six-v1"]!["frames"]!["walk"] = 4);
        Reject(n => n["surfaces"]!["floor"] = "missing-floor");
        Reject(n => n["sprites"]!["facility-floor"]!["scale"] = 0);
        Console.WriteLine($"SPRITE_ART_CHECKS_PASS: {count} assertions; real sources, regions, anchors, state/facing/frame registration and independent ordered layers.");
        return count;
    }
}
