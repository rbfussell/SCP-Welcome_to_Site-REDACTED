using System.Text.Json;

namespace SitePresentation;

public sealed record ArtSprite(string File, int[] Region, double[] Anchor, double Scale);
public sealed record ArtRig(string[] Directions, Dictionary<string, int> Frames);
public sealed record ArtLayer(string Id, Dictionary<string, string[][]> States);
public sealed record ActorArt(string Rig, ArtLayer[] Layers);
public sealed record SpriteArtManifest(int Version, Dictionary<string, int[]> Images, Dictionary<string, ArtSprite> Sprites,
    Dictionary<string, ArtRig> Rigs, Dictionary<string, ActorArt> Actors, Dictionary<string, string> Objects, Dictionary<string, string> Surfaces, Dictionary<string, double[]>? Mounts = null);

/// <summary>Presentation-only registration: atlas bounds, feet anchors, rig/state/facing and ordered layers.</summary>
public sealed class SpriteArtCatalog
{
    public SpriteArtManifest Manifest { get; }
    public SpriteArtCatalog(string json)
    {
        Manifest = JsonSerializer.Deserialize<SpriteArtManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new ArgumentException("Empty sprite-art manifest.");
        var m = Manifest;
        static bool FileName(string path) => !string.IsNullOrWhiteSpace(path) && !path.StartsWith('/') && !path.Contains('\\')
            && !path.Contains(':') && path.Split('/').All(part => part is not ("" or "." or ".."));
        if (m.Version != 1 || m.Images is null || m.Sprites is null || m.Rigs is null || m.Actors is null || m.Objects is null || m.Surfaces is null
            || m.Images.Any(e => !FileName(e.Key) || e.Value is not { Length: 2 } || e.Value.Any(n => n <= 0)))
            throw new ArgumentException("Invalid sprite-art schema or image declaration.");
        if (m.Mounts is not null && m.Mounts.Any(e => string.IsNullOrWhiteSpace(e.Key) || e.Value is not { Length: 2 } || e.Value.Any(n => !double.IsFinite(n))))
            throw new ArgumentException("Invalid presentation mount offset.");
        foreach (var (key, s) in m.Sprites)
            if (string.IsNullOrWhiteSpace(key) || s is null || !m.Images.TryGetValue(s.File, out var size)
                || s.Region is not { Length: 4 } || s.Region[0] < 0 || s.Region[1] < 0 || s.Region[2] <= 0 || s.Region[3] <= 0
                || (long)s.Region[0] + s.Region[2] > size[0] || (long)s.Region[1] + s.Region[3] > size[1]
                || s.Anchor is not { Length: 2 } || s.Anchor.Any(n => !double.IsFinite(n))
                || s.Anchor[0] < 0 || s.Anchor[0] > s.Region[2] || s.Anchor[1] < 0 || s.Anchor[1] > s.Region[3]
                || !double.IsFinite(s.Scale) || s.Scale <= 0)
                throw new ArgumentException("Invalid sprite region, scale or ground anchor: " + key);
        foreach (var (key, rig) in m.Rigs)
            if (string.IsNullOrWhiteSpace(key) || rig is null || rig.Directions is not { Length: 6 }
                || !rig.Directions.SequenceEqual(new[] { "NE", "E", "SE", "SW", "W", "NW" }) || rig.Frames is null
                || !rig.Frames.ContainsKey("idle") || !rig.Frames.ContainsKey("walk") || rig.Frames.Any(e => string.IsNullOrWhiteSpace(e.Key) || e.Value < 1))
                throw new ArgumentException("Invalid directional character rig: " + key);
        foreach (var (id, actor) in m.Actors)
        {
            if (string.IsNullOrWhiteSpace(id) || actor is null || !m.Rigs.TryGetValue(actor.Rig, out var rig) || actor.Layers is not { Length: > 0 }
                || actor.Layers.Any(layer => layer is null || string.IsNullOrWhiteSpace(layer.Id) || layer.States is null)
                || actor.Layers.Select(layer => layer.Id).Distinct().Count() != actor.Layers.Length)
                throw new ArgumentException("Invalid actor appearance: " + id);
            foreach (var layer in actor.Layers)
            foreach (var (state, count) in rig.Frames)
                if (!layer.States.TryGetValue(state, out var directions) || directions is not { Length: 6 }
                    || directions.Any(frames => frames is null || frames.Length != count || frames.Any(key => !m.Sprites.ContainsKey(key))))
                    throw new ArgumentException($"Missing art binding: {id}/{layer.Id}/{state}");
        }
        if (m.Objects.Concat(m.Surfaces).Any(e => string.IsNullOrWhiteSpace(e.Key) || !m.Sprites.ContainsKey(e.Value)))
            throw new ArgumentException("Unknown surface/object artwork.");
    }
    public string[] Layers(string actorId, string state, int facing, double phase)
    {
        if (!Manifest.Actors.TryGetValue(actorId, out var actor)) return [];
        if (facing is < 0 or > 5 || !double.IsFinite(phase) || phase is < 0 or >= 1 || !Manifest.Rigs[actor.Rig].Frames.TryGetValue(state, out var count))
            throw new ArgumentException("Invalid visual state/facing/phase.");
        var frame = Math.Min(count - 1, (int)(phase * count));
        return actor.Layers.Select(layer => layer.States[state][facing][frame]).ToArray();
    }
}
