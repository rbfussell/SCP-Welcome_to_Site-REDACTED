namespace SiteSimulation;

/// <summary>Body-local axial coordinates. No relationship to terrain size, projection or movement.</summary>
public readonly record struct BodyHex(int Q, int R)
{
    private static readonly BodyHex[] Directions = [new(1, -1), new(1, 0), new(0, 1), new(-1, 1), new(-1, 0), new(0, -1)];
    public BodyHex Neighbor(int direction) => new(Q + Directions[direction].Q, R + Directions[direction].R);
    public BodyHex Rotate(int sixthTurns)
    {
        if (sixthTurns is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(sixthTurns));
        var cell = this;
        for (var i = 0; i < sixthTurns; i++) cell = new(-cell.R, cell.Q + cell.R);
        return cell;
    }
    public static BodyHex operator +(BodyHex a, BodyHex b) => new(a.Q + b.Q, a.R + b.R);
}

public sealed record BodyPose(int Q, int R, int Rotation = 0);
public sealed record BodyContact(BodyHex Cell, int Direction);
public sealed record BodyZone(string Region, string Surface, int FirstRow, int LastRow);
public sealed record BodyMask(string Id, string Name, string[] Rows, BodyZone[]? Zones = null);
/// <summary>Rows share the body's authoring canvas; contacts are local to DefaultPose. Other boundary edges are open.</summary>
public sealed record BodyFitShape(string Id, string BodyFamily, string[] Rows, BodyPose DefaultPose, int[] Rotations, BodyContact[] Contacts, BodyCoverageProfile? Coverage = null);
public sealed record BodyFitContent(int Version, BodyMask[] Bodies, BodyFitShape[] Shapes);
public sealed record BodyFitResult(bool Fits, string Reason, BodyHex[] Cells, BodyHex[] Outside,
    BodyContact[] Contacts, BodyContact[] FailedContacts);
public sealed record BodyFitPreview(BodyFitResult Geometry, InventoryActionQuote Handling, InventoryRequest Request);

public sealed record BodySurfaceMask(string Surface, BodyHex[] Cells);
public readonly record struct BodySurfaceCell(string Surface, BodyHex Cell);
public sealed record BodyCoverageProfile(BodySurfaceMask[] Closed, BodySurfaceMask[] Open);
public sealed record PocketCoverage(string ItemId, string RegionId, bool Covered, string Reason, BodySurfaceCell[] Mouth,
    BodySurfaceCell[] Blocked, string[] Blockers, bool CoarseFallback);
