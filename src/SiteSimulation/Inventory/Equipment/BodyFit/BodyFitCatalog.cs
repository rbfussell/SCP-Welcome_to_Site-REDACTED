using System.Text.Json;

namespace SiteSimulation;

/// <summary>Authored, bounded body and garment masks. Loaded independently of Godot, included in content identity.</summary>
public sealed class BodyFitCatalog
{
    public string Source { get; }
    public BodyFitContent Content { get; }
    internal BodyFitCatalog()
    {
        using var stream = typeof(BodyFitCatalog).Assembly.GetManifestResourceStream("SiteSimulation.Inventory.Content.body-fit.json")
            ?? throw new InvalidOperationException("Body-fit content is missing.");
        Source = new StreamReader(stream).ReadToEnd();
        Content = JsonSerializer.Deserialize<BodyFitContent>(Source, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new ArgumentException("Body-fit catalog is empty.");
        Validate(Content);
    }
    public BodyMask Body(string id) => Content.Bodies.Single(b => b.Id == id);
    public BodyFitShape Shape(string id) => Content.Shapes.Single(s => s.Id == id);
    internal static bool SurfaceMasksValid(BodySurfaceMask[]? masks, IReadOnlySet<BodyHex> cells)
        => masks is { Length: <= 3 } && masks.All(m => m is not null && m.Surface is "front" or "back" or "mounted"
            && m.Cells is { Length: > 0 and <= 1000 } && m.Cells.Distinct().Count() == m.Cells.Length && m.Cells.All(cells.Contains))
            && masks.Select(m => m.Surface).Distinct().Count() == masks.Length;
    internal static void Validate(BodyFitContent content)
    {
        static bool Id(string? id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 100;
        static bool Rows(string[]? rows) => rows is { Length: > 0 and <= 40 } && rows.All(r => r is { Length: > 0 and <= 25 }
            && r.Length == rows[0].Length && r.All(c => c is '.' or '#')) && rows.Any(r => r.Contains('#'));
        if (content.Version != 1 || content.Bodies is not { Length: > 0 and <= 16 } || content.Shapes is not { Length: > 0 and <= 128 }
            || content.Bodies.Any(b => b is null || !Id(b.Id) || !Id(b.Name) || !Rows(b.Rows))
            || content.Bodies.Select(b => b.Id).Distinct().Count() != content.Bodies.Length
            || content.Shapes.Any(s => s is null || !Id(s.Id) || !Rows(s.Rows) || s.DefaultPose is null
                || s.DefaultPose.Q is < -64 or > 64 || s.DefaultPose.R is < -64 or > 64 || s.DefaultPose.Rotation != 0
                || !content.Bodies.Any(b => b.Id == s.BodyFamily && b.Rows.Length == s.Rows.Length && b.Rows[0].Length == s.Rows[0].Length)
                || s.Rotations is not { Length: > 0 and <= 6 } || !s.Rotations.Contains(0) || s.Rotations.Any(r => r is < 0 or > 5)
                || s.Rotations.Distinct().Count() != s.Rotations.Length || s.Contacts is null || s.Contacts.Length > 1000
                || s.Contacts.Any(e => e is null || e.Direction is < 0 or > 5) || s.Contacts.Distinct().Count() != s.Contacts.Length)
            || content.Shapes.Select(s => s.Id).Distinct().Count() != content.Shapes.Length)
            throw new ArgumentException("Invalid body-fit catalog.");
        foreach (var body in content.Bodies)
            if (body.Zones is not { Length: > 0 and <= 32 } || body.Zones.Any(z => z is null || !InventoryWear.BodyRegions.Contains(z.Region)
                || z.Surface is not ("front" or "back") || z.FirstRow < 0 || z.LastRow < z.FirstRow || z.LastRow >= body.Rows.Length)
                || BodyFitGeometry.Cells(body.Rows).Any(c => new[] { "front", "back" }.Any(surface =>
                    body.Zones.Count(z => z.Surface == surface && c.R >= z.FirstRow && c.R <= z.LastRow) != 1)))
                throw new ArgumentException("Body zones must identify every surface cell exactly once.");
        foreach (var shape in content.Shapes)
        {
            var cells = BodyFitGeometry.Cells(shape.Rows).Select(c => new BodyHex(c.Q - shape.DefaultPose.Q, c.R - shape.DefaultPose.R)).ToHashSet();
            if (shape.Coverage is { } coverage && (!SurfaceMasksValid(coverage.Closed, cells) || !SurfaceMasksValid(coverage.Open, cells)
                    || coverage.Open.Any(open => !coverage.Closed.Any(closed => closed.Surface == open.Surface && open.Cells.All(closed.Cells.Contains)))))
                throw new ArgumentException("Invalid closed/open cover masks: " + shape.Id);
            if (shape.Contacts.Any(e => !cells.Contains(e.Cell) || cells.Contains(e.Cell.Neighbor(e.Direction)))
                || !BodyFitGeometry.Evaluate(content.Bodies.Single(b => b.Id == shape.BodyFamily), shape, shape.DefaultPose).Fits)
                throw new ArgumentException("Invalid or misaligned authored body contacts: " + shape.Id);
        }
    }
}
