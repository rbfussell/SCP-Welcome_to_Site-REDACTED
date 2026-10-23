namespace SiteSimulation;

/// <summary>Host validation for one module's body. Every relocation is rechecked at commit.</summary>
internal sealed class AnomalyWorldAccess(SimulationWorld world, AnomalyBody body) : IAnomalyWorld
{
    public long LocalMs => world.LocalMs;
    public AnomalyBodySnapshot Body => new(body.Id, body.ModuleId, body.Cell, body.Pose, body.Revision);
    public ObservationReport Observation(string profile) => profile != body.ObserverProfile
        ? throw new ArgumentException("Observer profile differs from instance configuration.")
        : ObservationService.Evaluate(world.Map, world.Actors, body.Cell.World, LocalMs, profile);

    private bool Free(Hex cell)
    {
        if (cell == body.Cell || !world.Map.Walkable(cell)) return false;
        return !world.Actors.Any(a => a.Cell == cell || a.Moving && a.Route[0] == cell
            || a.StoppedStep is { } stopped && stopped.Next == cell
            || Point2.Distance(a.Position, cell.World) < 0.26
            || a.Moving && Geometry.DistanceToSegment(cell.World, a.Position, a.Route[0].World) < 0.18);
    }

    public IReadOnlyList<RelocationCandidate> Candidates(string profile, RelocationQuery query)
    {
        if (profile != body.ObserverProfile || query.MinHexes < 1 || query.MaxHexes > body.MaxRelocationHexes || query.MaxHexes < query.MinHexes
            || query.NearObserverKind is not ("human" or "ai")) return [];
        var candidates = new List<RelocationCandidate>();
        foreach (var actor in world.Actors.Where(a => a.ObserverKind == query.NearObserverKind
            && (query.NearActorId is null || a.Id == query.NearActorId)).OrderBy(a => a.Id, StringComparer.Ordinal))
        foreach (var cell in world.Map.Cells)
        {
            var distance = actor.Cell.Distance(cell);
            if (distance < query.MinHexes || distance > query.MaxHexes || !Free(cell)) continue;
            if (query.RequireClearLine && !world.Map.ClearSight(actor.Position, cell.World)) continue;
            var behind = cell.World - actor.Position;
            var direction = actor.Cell.Neighbor(actor.Facing).World - actor.Cell.World;
            if (query.BehindOnly && behind.X * direction.X + behind.Y * direction.Y >= -0.01) continue;
            if (ObservationService.Evaluate(world.Map, world.Actors, cell.World, LocalMs, profile).Observed) continue;
            candidates.Add(new(cell, actor.Id, distance));
        }
        return candidates.OrderBy(p => p.Cell.Q).ThenBy(p => p.Cell.R)
            .ThenBy(p => p.NearActorId, StringComparer.Ordinal).ToArray();
    }

    public bool TryRelocate(long expectedRevision, Hex destination, int pose, string profile, RelocationQuery query, out string reason)
    {
        reason = !world.LocalActive ? "local-held"
            : profile != body.ObserverProfile ? "observer-profile-mismatch"
            : expectedRevision != body.Revision ? "stale-body-revision"
            : (pose < 0 || pose >= body.PoseCount) ? "invalid-pose"
            : Observation(profile).Observed ? "origin-observed"
            : !Free(destination) ? "destination-occupied-or-outside"
            : ObservationService.Evaluate(world.Map, world.Actors, destination.World, LocalMs, profile).Observed ? "destination-observed"
            : !Candidates(profile, query).Any(c => c.Cell == destination) ? "outside-relocation-policy" : "accepted";
        if (reason != "accepted") return false;
        var origin = body.Cell;
        world.Map.DynamicBlockers.Remove(origin);
        body.Cell = destination; body.Pose = pose; body.Revision++;
        world.Map.DynamicBlockers.Add(destination);
        world.Log("anomaly-host", body.Id, "relocated", $"{origin.Q},{origin.R} -> {destination.Q},{destination.R}; pose {pose + 1}; silent");
        return true;
    }

    public void Witnessed(string actorId, int encounter)
    {
        var actor = world.Actors.SingleOrDefault(a => a.Id == actorId);
        if (!world.LocalActive || actor is null || actor.ObserverKind != "human"
            || !ObservationService.Inspect(world.Map, actor, body.Cell.World, LocalMs).Detected) return;
        actor.StartledUntilMs = Math.Max(actor.StartledUntilMs, LocalMs + 400);
        world.Log("reaction", actorId, "startled", $"Discovered {body.Id}, encounter {encounter}; brief local movement interruption, no attack.");
    }
    public void Trace(string eventName, string detail) => world.Log(body.ModuleId, body.Id, eventName, detail);
}
