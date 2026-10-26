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
        var localCells = Cells(shape.Rows).Select(c => new BodyHex(c.Q - shape.DefaultPose.Q, c.R - shape.DefaultPose.R));
        var cells = localCells.Select(c => Transform(c, pose)).ToArray();
        var outside = cells.Where(c => !bodyCells.Contains(c)).ToArray();
        var contacts = shape.Contacts.Select(edge => new BodyContact(Transform(edge.Cell, pose), (edge.Direction + pose.Rotation) % 6)).ToArray();
        var failed = contacts.Where(edge => !bodyCells.Contains(edge.Cell) || bodyCells.Contains(edge.Cell.Neighbor(edge.Direction))).ToArray();
        var fits = outside.Length == 0 && failed.Length == 0;
        return new(fits, fits ? "Shape and boundary contacts fit."
            : outside.Length > 0 ? $"{outside.Length} worn cells fall outside the body."
            : $"{failed.Length} closed edges fail body contact; body continues through them.", cells, outside, contacts, failed);
    }
}
