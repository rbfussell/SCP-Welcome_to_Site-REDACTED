namespace SiteSimulation;

public sealed record ObserverFact(string ActorId, string Kind, bool Detected, bool Eligible, string Reason);
public sealed record ObservationReport(string Profile, ObserverFact[] Observers)
{
    public string[] EligibleIds => Observers.Where(o => o.Detected && o.Eligible).Select(o => o.ActorId).ToArray();
    public bool Observed => EligibleIds.Length > 0;
}

/// <summary>Shared direct perception. Information detection and anomaly eligibility are separate.</summary>
public static class ObservationService
{
    public const double VisionRange = 5;
    public const double FacingDotThreshold = 0.45;
    public static bool ValidProfile(string profile) => profile is "human" or "ai" or "any";

    public static ObserverFact Inspect(FacilityMap map, ActorState actor, Point2 target, long localMs, string profile = "any")
    {
        if (!ValidProfile(profile)) throw new ArgumentException("Unknown observer profile.");
        var eligible = actor.ObserverKind is "human" or "ai" && (profile == "any" || profile == actor.ObserverKind);
        var delta = target - actor.Position;
        var direction = actor.Cell.Neighbor(actor.Facing).World - actor.Cell.World;
        var dot = delta.Length < 0.05 ? 1 : (delta.X * direction.X + delta.Y * direction.Y) / (delta.Length * direction.Length);
        var reason = !actor.Attending ? "not-attending"
            : localMs < actor.EyesClosedUntilMs ? "eyes-closed"
            : !map.LightsOn ? "unlit"
            : delta.Length >= VisionRange ? "out-of-range"
            : dot < FacingDotThreshold ? "outside-facing"
            : !map.ClearSight(actor.Position, target) ? "occluded" : "visible";
        return new(actor.Id, actor.ObserverKind, reason == "visible", eligible,
            reason == "visible" && !eligible ? "visible; ineligible-kind" : reason);
    }

    public static ObservationReport Evaluate(FacilityMap map, IEnumerable<ActorState> actors, Point2 target, long localMs, string profile)
        => new(profile, actors.OrderBy(a => a.Id, StringComparer.Ordinal).Select(a => Inspect(map, a, target, localMs, profile)).ToArray());

    public static int FacingToward(Point2 from, Point2 target, int previous)
    {
        var delta = target - from;
        if (delta.Length < 0.001) return previous;
        var dq = delta.X * 2; var dr = delta.Y * 2 - delta.X;
        var x = Math.Sqrt(3) * (dq + dr / 2); var y = 1.5 * dr;
        return Enumerable.Range(0, 6).OrderByDescending(f =>
        {
            var d = Hex.Directions[f];
            return x * Math.Sqrt(3) * (d.Q + d.R / 2.0) + y * 1.5 * d.R;
        }).First();
    }
}
