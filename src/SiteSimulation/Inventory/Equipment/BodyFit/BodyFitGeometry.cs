namespace SiteSimulation;

/// <summary>Pure hex-mask/contact fitting. Storage envelopes, clothing layers and work costs are separate validators.</summary>
public static class BodyFitGeometry
{
    public static BodyHex[] Cells(string[] rows)
    {
        var center = rows[0].Length / 2;
        return rows.SelectMany((row, r) => row.Select((value, column) => (value, column))
            .Where(p => p.value == '#').Select(p => new BodyHex(p.column - center - r / 2, r))).ToArray();
    }
    public static BodyHex Transform(BodyHex local, BodyPose pose) => local.Rotate(pose.Rotation) + new BodyHex(pose.Q, pose.R);
    public static BodyFitResult Evaluate(BodyMask body, BodyFitShape shape, BodyPose pose)
    {
        if (pose.Q is < -64 or > 64 || pose.R is < -64 or > 64 || pose.Rotation is < 0 or > 5 || !shape.Rotations.Contains(pose.Rotation))
            return new(false, "Unsupported body position or orientation.", [], [], [], []);
        if (body.Id != shape.BodyFamily) return new(false, "Equipment was authored for a different body family.", [], [], [], []);
        var bodyCells = Cells(body.Rows).ToHashSet();
        var localCells = Cells(shape.Rows).Select(c => new BodyHex(c.Q - shape.DefaultPose.Q, c.R - shape.DefaultPose.R)).ToArray();
        var localContacts = shape.Contacts;
        if (shape.LinearSpan is { } span)
        {
            // Find the first exterior cell on each end. Independent extents allow a chest diagonal
            // without shifting its chosen anchor. Never bridge a gap, shrink the core or exceed the authored range.
            var low = localCells.Min(c => c.Q); var high = localCells.Max(c => c.Q);
            while (high - low + 1 <= span.MaxCells && bodyCells.Contains(Transform(new(low - 1, 0), pose))) low--;
            while (high - low + 1 <= span.MaxCells && bodyCells.Contains(Transform(new(high + 1, 0), pose))) high++;
            if (high - low + 1 >= span.MinCells && high - low + 1 <= span.MaxCells)
            {
                localCells = Enumerable.Range(low, high - low + 1).Select(q => new BodyHex(q, 0)).ToArray();
                localContacts = [new(new(low, 0), 4), new(new(high, 0), 1)];
            }
        }
        var cells = localCells.Select(c => Transform(c, pose)).ToArray();
        var outside = cells.Where(c => !bodyCells.Contains(c)).ToArray();
        var contacts = localContacts.Select(edge => new BodyContact(Transform(edge.Cell, pose), (edge.Direction + pose.Rotation) % 6)).ToArray();
        var failed = contacts.Where(edge => !bodyCells.Contains(edge.Cell) || bodyCells.Contains(edge.Cell.Neighbor(edge.Direction))).ToArray();
        var fits = outside.Length == 0 && failed.Length == 0;
        return new(fits, fits ? shape.LinearSpan is null ? "Shape and boundary contacts fit." : $"Strap fitted to {cells.Length} cells within authored {shape.LinearSpan.MinCells}–{shape.LinearSpan.MaxCells}; both ends contact."
            : outside.Length > 0 ? $"{outside.Length} worn cells fall outside the body."
            : $"{failed.Length} closed edges fail body contact; body continues through them.", cells, outside, contacts, failed);
    }
}
