using SiteSimulation;

namespace SitePresentation;

/// <summary>Direct-view filtering only. Drawing cannot create observation or change a module.</summary>
public static class FieldPresentation
{
    public static bool ShowActor(SpatialSimulation sim, ActorState actor, bool diagnostics)
        => diagnostics || actor.Id == sim.Player.Id || sim.VisibleToPlayer(actor.Position);

    public static bool ShowAnomaly(SpatialSimulation sim, AnomalyBody body, bool diagnostics)
        => diagnostics || sim.VisibleToPlayer(body.Cell.World);

    /// <summary>Cosmetic fade inside the real sight cone. Never used to decide whether an entity is seen.</summary>
    public static float SightShade(SpatialSimulation sim, Point2 point)
    {
        if (!sim.VisibleToPlayer(point)) return 0;
        var delta = point - sim.Player.Position;
        if (delta.Length < 0.05) return 1;
        var direction = sim.Player.Cell.Neighbor(sim.Player.Facing).World - sim.Player.Cell.World;
        var dot = (delta.X * direction.X + delta.Y * direction.Y) / (delta.Length * direction.Length);
        static double Fade(double value)
        { var t = Math.Clamp(value, 0, 1); return t * t * (3 - 2 * t); }
        return (float)(Fade((dot - ObservationService.FacingDotThreshold) / 0.22)
            * Fade((ObservationService.VisionRange - delta.Length) / 0.7));
    }
}

/// <summary>Consumes new, actual avatar discovery events once. No cue for silent relocation or remote witnesses.</summary>
public sealed class DiscoveryFeedback
{
    private long _sequence;
    private long _discoveredAt = -10000;
    public int Discoveries { get; private set; }
    public double Pulse(long localMs) => Math.Clamp(1 - (localMs - _discoveredAt) / 1200.0, 0, 1);
    public string Caption(long localMs) => localMs - _discoveredAt is >= 0 and < 3200
        ? "IT WASN'T THERE BEFORE." : "";

    public void Reset(SpatialSimulation sim)
    {
        _sequence = sim.Trace.Capture().NextSequence - 1;
        _discoveredAt = -10000;
        Discoveries = 0;
    }

    public bool Update(SpatialSimulation sim, bool fieldView)
    {
        var entries = sim.Trace.Entries.Where(e => e.Sequence > _sequence).ToArray();
        _sequence = sim.Trace.Capture().NextSequence - 1;
        if (!fieldView || !entries.Any(e => e.System == "reaction" && e.Event == "startled" && e.Subject == sim.Player.Id)) return false;
        _discoveredAt = sim.LocalMs;
        Discoveries++;
        return true;
    }
}
