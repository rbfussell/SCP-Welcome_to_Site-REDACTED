namespace SiteSimulation;

/// <summary>Scenario setup and continuously evaluated receiving conditions; never forces anomaly relocation.</summary>
internal static class OperationLifecycle
{
    public static void Initialize(SimulationWorld world)
    {
        var state = world.Operations;
        if (state.Definition is not { } d) return;
        if (d.AccessDoors is null || d.Posts is null || d.Starts is null || d.RadioActors is null
            || d.Posts.Length == 0 || d.Starts.Any(s => s is null)
            || d.StabilizeMs is < 500 or > 60000 || d.StabilizeMs % SpatialSimulation.TickMs != 0
            || !world.Anomalies.Bodies.Any(b => b.Id == d.SubjectId) || !world.Actors.Any(a => a.Id == d.CommanderId)
            || d.Starts.Select(s => s.ActorId).Distinct().Count() != d.Starts.Length
            || d.RadioActors.Distinct().Count() != d.RadioActors.Length
            || d.RadioActors.Any(id => !world.Actors.Any(a => a.Id == id))
            || d.Home == d.Terminal || !world.Map.Walkable(d.Home, world.Anomalies.Bodies.Select(b => b.Cell).ToHashSet()) || !world.Map.Walkable(d.Terminal) || !world.Map.Walkable(d.Trolley)
            || d.Terminal == d.Trolley || d.Posts.Any(p => !world.Map.Walkable(p))
            || d.AccessDoors.Any(id => !world.Map.Definition.Walls.Any(w => w.Id == id && w.Door)))
            throw new ArgumentException("Invalid operation scenario.");
        foreach (var start in d.Starts)
        {
            var actor = world.Actors.SingleOrDefault(a => a.Id == start.ActorId);
            if (actor is null || !world.Map.Walkable(start.Cell) || start.Facing is < 0 or > 5)
                throw new ArgumentException("Invalid operation actor start.");
            actor.Cell = start.Cell; actor.Facing = start.Facing;
        }
        if (world.Actors.Select(a => a.Cell).Distinct().Count() != world.Actors.Count
            || world.Actors.Any(a => a.Cell == d.Terminal || a.Cell == d.Trolley)) throw new ArgumentException("Operation fixtures overlap.");
        world.Map.DynamicBlockers.Add(d.Terminal); world.Map.DynamicBlockers.Add(d.Trolley);
        foreach (var actor in world.Actors.Where(a => a.Id != d.CommanderId && !EmploymentSystem.Owns(world, a.Id)))
            state.Orders.Add(new(state.NextId++, d.CommanderId, actor.Id, PersonnelTask.Observe, actor.Cell, "voice", OrderStatus.Active,
                "Standing observation duty", 0, world.Anomalies.Bodies.Single(b => b.Id == d.SubjectId).Position));
        state.Report(world, "OPERATIONS", "Observation shift opened. Receiving conditions require three humans, lit room and closed access. One observer can suppress self-relocation.");
    }
    public static void Advance(SimulationWorld world)
    {
        var state = world.Operations;
        if (state.Definition is not { } d) return;
        var body = world.Anomalies.Bodies.Single(b => b.Id == d.SubjectId);
        var report = ObservationService.Evaluate(world.Map, world.Actors, body.Position, world.LocalMs, body.ObserverProfile);
        var atHome = body.Cell == d.Home;
        if (!atHome) state.HadBreach = true;
        var stable = atHome && state.Trolley.SubjectId is null && state.Trolley.Step is null
            && report.Observers.Count(o => o.Kind == "human" && o.Detected && o.Eligible) >= 3
            && d.AccessDoors.All(id => !world.Map.OpenDoors.Contains(id)) && world.Map.LightsOn;
        state.StableMs = stable ? Math.Min(d.StabilizeMs, state.StableMs + SpatialSimulation.TickMs) : 0;
        var status = state.StableMs >= d.StabilizeMs ? "Contained · procedure compliant"
            : !atHome ? "Breach · outside receiving footprint"
            : report.Observed ? "Emergency stabilized · procedure incomplete" : "Coverage lost · unresolved";
        if (state.HadBreach && state.StableMs >= d.StabilizeMs) state.Completed = true;
        if (status == state.PhysicalStatus) return;
        state.PhysicalStatus = status;
        world.Log("operation", d.SubjectId, "conditions", status);
        var witness = world.Actors.FirstOrDefault(a => ObservationService.Inspect(world.Map, a, body.Position, world.LocalMs).Detected);
        if (witness is null)
        {
            // Seeing the empty receiving footprint is evidence of absence, not of the subject's hidden location.
            witness = world.Actors.FirstOrDefault(a => ObservationService.Inspect(world.Map, a, d.Home.World, world.LocalMs).Detected);
            if (witness is not null && !atHome && (FacilityCommunications.Voice(world, witness, world.Player)
                || (state.TerminalPowered || OscpService.PhoneAvailable(world)) && FacilityCommunications.Radio(state, witness.Id)))
                state.Report(world, witness.Name, "Receiving footprint empty · subject location unconfirmed",
                    source: FacilityCommunications.Voice(world, witness, world.Player) ? "voice" : "radio");
            return;
        }
        if (FacilityCommunications.Voice(world, witness, world.Player) || (state.TerminalPowered || OscpService.PhoneAvailable(world)) && FacilityCommunications.Radio(state, witness.Id))
            state.Report(world, witness.Name, $"Containment assessment: {status}", body.Position,
                source: FacilityCommunications.Voice(world, witness, world.Player) ? "voice" : "radio");
    }
}
