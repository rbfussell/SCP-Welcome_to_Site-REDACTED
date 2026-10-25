namespace SiteSimulation;

/// <summary>Scheduled primary work, bounded relief and return after orders. Uses ordinary movement and sight.</summary>
internal static class EmploymentSystem
{
    public static bool Owns(SimulationWorld world, string actorId) => world.Employment.Definition?.Appointments?.Any(a => a is not null && a.ActorId == actorId) == true;
    internal static bool Scheduled(StaffAppointment a, long localMs)
        => (localMs % a.CycleMs - a.StartMs + a.CycleMs) % a.CycleMs < a.DurationMs;

    public static void Initialize(SimulationWorld world)
    {
        var state = world.Employment;
        if (state.Definition is not { } d) return;
        if (d.Posts is null || d.Appointments is null || d.Posts.Length == 0 || d.Appointments.Length == 0
            || d.Posts.Any(p => p is null || string.IsNullOrWhiteSpace(p.Id) || p.Id.Length > 80 || string.IsNullOrWhiteSpace(p.Name)
                || p.Name.Length > 100 || !world.Map.Inside(p.LookAt) || p.MinimumStaff is < 1 or > 16)
            || d.Posts.Select(p => p.Id).Distinct().Count() != d.Posts.Length
            || d.Appointments.Any(a => a is null || a.ActorId == world.Player.Id || !world.Actors.Any(actor => actor.Id == a.ActorId && actor.ObserverKind == "human")
                || a.SupervisorId != world.Operations.Definition?.CommanderId || !d.Posts.Any(p => p.Id == a.PostId)
                || string.IsNullOrWhiteSpace(a.Role) || a.Role.Length > 100 || !world.Map.Walkable(a.Station) || !world.Map.Walkable(a.Rest)
                || a.Rest == a.Station || a.RestFacing is < 0 or > 5 || a.CycleMs is < 1000 or > 86400000
                || a.StartMs < 0 || a.StartMs >= a.CycleMs || a.DurationMs <= 0 || a.DurationMs >= a.CycleMs
                || a.MaxOvertimeMs < 0 || a.MaxOvertimeMs > a.CycleMs - a.DurationMs
                || new[] { a.CycleMs, a.StartMs, a.DurationMs, a.MaxOvertimeMs }.Any(ms => ms % SpatialSimulation.TickMs != 0))
            || d.Appointments.Select(a => a.ActorId).Distinct().Count() != d.Appointments.Length
            || d.Appointments.Select(a => a.Station).Distinct().Count() != d.Appointments.Length)
            throw new ArgumentException("Invalid employment definitions or appointment.");
        state.Duties.AddRange(d.Appointments.Select(a => Scheduled(a, 0)
            ? new StaffDuty(a.ActorId, DutyPhase.Reporting, "Reporting for assigned duty", 0)
            : new StaffDuty(a.ActorId, DutyPhase.OffShift, "Off shift at scenario start", 0, Release: DutyRelease.OffShift)));
    }

    private static bool Ready(SimulationWorld world, StaffAppointment appointment)
    {
        var actor = world.Actors.Single(a => a.Id == appointment.ActorId);
        var post = world.Employment.Definition!.Posts.Single(p => p.Id == appointment.PostId);
        return actor.Cell == appointment.Station && !actor.Moving && actor.StoppedStep is null
            && world.LocalMs >= actor.StartledUntilMs && !HasOrder(world, actor.Id)
            && !(world.Combat.Active && world.Combat.For(actor.Id) is not null)
            && ObservationService.Inspect(world.Map, actor, post.LookAt, world.LocalMs).Detected;
    }
    private static bool HasOrder(SimulationWorld world, string actorId) => world.Operations.Orders.Any(o => o.ActorId == actorId
        && o.Status is OrderStatus.Active or OrderStatus.Blocked);
    public static int Coverage(SimulationWorld world, string postId) => world.Employment.Definition?.Appointments.Count(a => a.PostId == postId
        && world.Employment.Duties.Any(s => s.ActorId == a.ActorId && s.Phase is DutyPhase.OnDuty or DutyPhase.AwaitingRelief)
        && Ready(world, a)) ?? 0;
    private static void Set(SimulationWorld world, StaffAppointment a, DutyPhase phase, string reason, long? reliefSince = null, DutyRelease release = DutyRelease.None)
    {
        var state = world.Employment;
        var index = state.Duties.FindIndex(s => s.ActorId == a.ActorId);
        var old = state.Duties[index];
        if (old.Phase == phase && old.Reason == reason && old.ReliefSinceMs == reliefSince && old.Release == release) return;
        state.Duties[index] = new(a.ActorId, phase, reason, world.LocalMs, reliefSince, release);
        world.Log("employment", a.ActorId, phase.ToString(), reason);
    }
    public static void Advance(SimulationWorld world)
    {
        if (world.Employment.Definition is not { } d) return;
        foreach (var a in d.Appointments)
        {
            var actor = world.Actors.Single(actor => actor.Id == a.ActorId);
            var state = world.Employment.Duties.Single(s => s.ActorId == a.ActorId);
            var post = d.Posts.Single(p => p.Id == a.PostId);
            var scheduled = Scheduled(a, world.LocalMs);
            if (world.Combat.Active && world.Combat.For(actor.Id) is not null)
            {
                Set(world, a, DutyPhase.Interrupted, "Tactical participation suspends employment", state.ReliefSinceMs, state.Release);
                continue;
            }
            if (HasOrder(world, actor.Id))
            {
                Set(world, a, DutyPhase.Interrupted, "Authorized order suspends employment; return when released", state.ReliefSinceMs, state.Release);
                continue;
            }
            if (world.LocalMs < actor.StartledUntilMs)
            {
                MovementSystem.StopInPlace(actor);
                Set(world, a, DutyPhase.Blocked, "Recovering from startle; post coverage unavailable", state.ReliefSinceMs, state.Release);
                continue;
            }
            // Shift edges are processed once on a local tick; a hold never replays work or handovers.
            var reliefSince = scheduled ? null : state.ReliefSinceMs;
            var release = scheduled ? DutyRelease.None : state.Release;
            if (!scheduled && release == DutyRelease.None)
            {
                var wasWorking = state.Phase is not DutyPhase.OffShift && (state.Phase != DutyPhase.Reporting || Ready(world, a));
                if (!wasWorking && reliefSince is null) release = DutyRelease.OffShift;
                else
                {
                    var phaseMs = (world.LocalMs % a.CycleMs - a.StartMs + a.CycleMs) % a.CycleMs;
                    reliefSince ??= world.LocalMs - (phaseMs - a.DurationMs);
                    var replacements = d.Appointments.Count(other => other.ActorId != actor.Id && other.PostId == a.PostId
                        && Scheduled(other, world.LocalMs) && Ready(world, other)
                        && world.Employment.Duties.Any(s => s.ActorId == other.ActorId && s.Phase == DutyPhase.OnDuty));
                    release = replacements >= post.MinimumStaff ? DutyRelease.Relieved
                        : world.LocalMs - reliefSince >= a.MaxOvertimeMs ? DutyRelease.OvertimeExpired : DutyRelease.None;
                }
            }
            if (!scheduled && release != DutyRelease.None)
            {
                var reason = release switch
                {
                    DutyRelease.OvertimeExpired => "Overtime limit reached; leaving even without relief",
                    DutyRelease.Relieved => "Relieved by ready on-duty staff; off shift",
                    _ => "Off shift; not assigned this interval"
                };
                Travel(world, a, actor, a.Rest, DutyPhase.OffShift, reason, reliefSince, release);
                if (!actor.Moving && actor.Cell == a.Rest) actor.Facing = a.RestFacing;
                continue;
            }
            var phase = scheduled ? DutyPhase.OnDuty : DutyPhase.AwaitingRelief;
            var detail = scheduled ? "At assigned observation station" : "Relief absent; bounded overtime at post";
            if (actor.Cell == a.Station && !actor.Moving && actor.StoppedStep is null)
            {
                actor.Facing = ObservationService.FacingToward(actor.Position, post.LookAt, actor.Facing);
                if (!Ready(world, a))
                { Set(world, a, DutyPhase.Blocked, "Post not ready: attention, light or sight obstructed", reliefSince); continue; }
            }
            Travel(world, a, actor, a.Station, phase, detail, reliefSince, DutyRelease.None);
        }
        Publish(world);
    }
    private static void Travel(SimulationWorld world, StaffAppointment a, ActorState actor, Hex destination,
        DutyPhase arrived, string detail, long? reliefSince, DutyRelease release)
    {
        if (actor.Cell == destination && !actor.Moving && actor.StoppedStep is null)
        { Set(world, a, arrived, detail, reliefSince, release); return; }
        if (actor.Moving && actor.Route[^1] != destination) MovementSystem.StopInPlace(actor);
        // Employment only owns this route while no acknowledged command owns the employee.
        if (!actor.Moving)
        {
            var path = MovementSystem.PlanRoute(world, actor, destination);
            if (path.Count == 0)
            { Set(world, a, DutyPhase.Blocked, release != DutyRelease.None ? "Off-shift rest route blocked; not providing coverage" : "Duty route blocked; waiting for access or clearance", reliefSince, release); return; }
            MovementSystem.CommitRoute(actor, path, false);
        }
        Set(world, a, arrived == DutyPhase.OffShift ? DutyPhase.OffShift : DutyPhase.Reporting,
            arrived == DutyPhase.OffShift ? detail + "; travelling to rest point" : "Travelling to assigned post", reliefSince, release);
    }
    private static void Publish(SimulationWorld world)
    {
        foreach (var duty in world.Employment.Duties)
        {
            var actor = world.Actors.Single(a => a.Id == duty.ActorId);
            var previous = world.Employment.Reports.SingleOrDefault(r => r.ActorId == duty.ActorId);
            if (previous is not null && previous.Phase == duty.Phase && previous.Reason == duty.Reason) continue;
            var voice = FacilityCommunications.Voice(world, actor, world.Player);
            if (!voice && !(OscpService.ReceivesRadio(world) && FacilityCommunications.Radio(world, actor.Id))) continue;
            world.Employment.Reports.RemoveAll(r => r.ActorId == actor.Id);
            world.Employment.Reports.Add(new(actor.Id, duty.Phase, duty.Reason, world.LocalMs));
            world.Operations.Report(world, actor.Name, $"Employment: {duty.Phase} · {duty.Reason}", source: voice ? "voice" : "radio");
        }
    }
}
