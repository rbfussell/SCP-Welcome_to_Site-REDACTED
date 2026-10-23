using SiteSimulation;

namespace SitePresentation;

/// <summary>Direct-view filtering only. Drawing cannot create observation or change a module.</summary>
public static class FieldPresentation
{
    public static bool ShowActor(SpatialSimulation sim, ActorState actor, bool diagnostics)
        => diagnostics || actor.Id == sim.Player.Id || sim.VisibleToPlayer(actor.Position);

    public static bool ShowAnomaly(SpatialSimulation sim, AnomalyBody body, bool diagnostics)
        => diagnostics || sim.VisibleToPlayer(body.Cell.World);
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
