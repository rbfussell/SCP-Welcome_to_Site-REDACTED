namespace SiteSimulation;

/// <summary>Validated delivery and persistent duties, independent of anomaly selection and UI.</summary>
internal static class PersonnelOrderSystem
{
    public static bool Owns(OperationState state, string actorId) => state.Orders.Any(o => o.ActorId == actorId
        && o.Status is OrderStatus.Pending or OrderStatus.Active or OrderStatus.Blocked);
    public static bool Issue(SimulationWorld world, string issuerId, string actorId, PersonnelTask task, Hex destination,
        string channel, out string reason, bool allowPausedIntent = false)
    {
        var state = world.Operations;
        var actor = world.Actors.SingleOrDefault(a => a.Id == actorId);
        reason = !world.LocalActive && !(allowPausedIntent && world.ManualPause && !world.LocalHold && !world.Combat.Active) ? "Local simulation is held."
            : state.Definition is null ? "No operation configured."
            : state.Definition.CommanderId != issuerId ? "Issuer lacks site operations authority."
            : actor is null || actor.Id == issuerId || actor.ObserverKind != "human" ? "Recipient is not an assigned human employee."
            : !Enum.IsDefined(task) || channel is not ("voice" or "terminal" or "phone") ? "Unsupported task/channel."
            : task == PersonnelTask.ResumeDuty && !EmploymentSystem.Owns(world, actorId) ? "Recipient has no employment appointment."
            : task == PersonnelTask.Transport && state.Orders.Any(o => o.ActorId != actorId && o.Task == PersonnelTask.Transport
                && o.Status is OrderStatus.Pending or OrderStatus.Active or OrderStatus.Blocked) ? "Trolley already assigned to another handler."
            : !world.Map.Walkable(destination) ? "Destination is blocked."
            : channel == "terminal" && !FacilityCommunications.TerminalReachable(world) ? "Approach the powered, connected OSCP terminal."
            : channel == "phone" && (!OscpService.PhoneAvailable(world) || !state.NetworkOnline) ? "A powered, accessible handset and site link are required."
            : "accepted";
        if (reason != "accepted") return false;
        // A bounded history cannot evict an active order.
        if (state.Orders.Count >= 32)
        {
            var completed = state.Orders.FindIndex(o => o.Status is OrderStatus.Complete or OrderStatus.Cancelled);
            if (completed < 0) { reason = "Order ledger is full."; return false; }
            state.Orders.RemoveAt(completed);
        }
        state.Orders.Add(new(state.NextId++, issuerId, actorId, task, destination, channel, OrderStatus.Pending,
            "Awaiting delivery and acknowledgement", world.LocalMs,
            task == PersonnelTask.Follow ? world.Player.Position : world.Awareness.Evidence.SingleOrDefault(e => e.SubjectId == state.Definition!.SubjectId)?.Position));
        world.Log("orders", actorId, "submitted", $"{task} via {channel}; destination {destination}");
        return true;
    }
    internal static void Set(SimulationWorld world, long id, OrderStatus status, string detail)
    {
        var state = world.Operations;
        var i = state.Orders.FindIndex(o => o.Id == id);
        if (i < 0) return;
        var order = state.Orders[i];
        if (order.Status == status && order.Detail == detail) return;
        state.Orders[i] = order with { Status = status, Detail = detail };
        world.Log("orders", order.ActorId, status.ToString(), $"#{id} {detail}");
        if (FacilityCommunications.Link(world, order)) state.Report(world, world.Actors.Single(a => a.Id == order.ActorId).Name,
            $"Order #{id}: {status} · {detail}", source: order.Channel == "voice" ? "voice" : "radio");
    }
    public static void Advance(SimulationWorld world)
    {
        var state = world.Operations;
        if (state.Definition is not { } d) return;
        foreach (var pending in state.Orders.Where(o => o.Status == OrderStatus.Pending).ToArray())
        {
            if (!FacilityCommunications.Link(world, pending)) { PlayerFeedback.NoAcknowledgement(world, pending); continue; }
            if (state.Orders.Any(o => o.ActorId == pending.ActorId && o.Task == PersonnelTask.Transport
                && o.Status is OrderStatus.Active or OrderStatus.Blocked)) ObservedTransportSystem.Interrupt(world);
            foreach (var previous in state.Orders.Where(o => o.ActorId == pending.ActorId && o.Id < pending.Id
                && o.Status is OrderStatus.Active or OrderStatus.Blocked).ToArray())
                Set(world, previous.Id, OrderStatus.Cancelled, "Relieved by an acknowledged replacement order");
            var actor = world.Actors.Single(a => a.Id == pending.ActorId);
            MovementSystem.StopInPlace(actor);
            actor.Attending = true;
            Set(world, pending.Id, OrderStatus.Active, "Acknowledged"
                + (EmploymentSystem.Owns(world, actor.Id) && pending.Task != PersonnelTask.ResumeDuty ? " · reassignment may leave the employment post uncovered" : ""));
            if (pending.Task == PersonnelTask.ResumeDuty)
                Set(world, pending.Id, OrderStatus.Complete, "Released from direct orders; resuming scheduled employment");
        }
        foreach (var order in state.Orders.Where(o => o.Status is OrderStatus.Active or OrderStatus.Blocked).ToArray())
        {
            var actor = world.Actors.Single(a => a.Id == order.ActorId);
            if (world.LocalMs < actor.StartledUntilMs) { Set(world, order.Id, OrderStatus.Blocked, "Recovering from startle"); continue; }
            if (order.Task == PersonnelTask.Transport) continue; // The physical transport service owns this continuation.
            if (order.Task == PersonnelTask.Post)
            {
                if (actor.Cell == order.Destination && !actor.Moving && actor.StoppedStep is null)
                {
                    if (order.TargetPoint is { } point) actor.Facing = ObservationService.FacingToward(actor.Position, point, actor.Facing);
                    Set(world, order.Id, OrderStatus.Active, "Holding assigned post"); continue;
                }
                Travel(world, order, actor, order.Destination, false); continue;
            }
            Point2 target;
            if (order.Task == PersonnelTask.Follow)
            {
                var leader = world.Actors.Single(a => a.Id == order.Issuer);
                target = order.TargetPoint ?? actor.Position;
                actor.Facing = ObservationService.FacingToward(actor.Position, target, actor.Facing);
                if (ObservationService.Inspect(world.Map, actor, leader.Position, world.LocalMs).Detected)
                {
                    target = leader.Position;
                    var index = state.Orders.FindIndex(o => o.Id == order.Id);
                    state.Orders[index] = state.Orders[index] with { TargetPoint = target };
                }
                else if (Point2.Distance(actor.Position, target) <= 1)
                { MovementSystem.StopInPlace(actor); Set(world, order.Id, OrderStatus.Blocked, "Leader not in sight; last known position reached"); continue; }
                if (Point2.Distance(actor.Position, target) <= 1)
                { MovementSystem.StopInPlace(actor); Set(world, order.Id, OrderStatus.Active, "Following · holding separation"); continue; }
            }
            else
            {
                var contact = state.Contacts.SingleOrDefault(c => c.ActorId == actor.Id);
                // A delivered order shares the issuer's acquired point, never a hidden live transform.
                var deliveredPoint = order.TargetPoint;
                if (contact is null && deliveredPoint is null)
                { Set(world, order.Id, OrderStatus.Blocked, "No acquired sighting to investigate"); continue; }
                target = contact?.Position ?? deliveredPoint!.Value;
                actor.Facing = ObservationService.FacingToward(actor.Position, target, actor.Facing);
                var subject = world.Anomalies.Bodies.Single(b => b.Id == d.SubjectId);
                if (ObservationService.Inspect(world.Map, actor, subject.Position, world.LocalMs).Detected)
                {
                    MovementSystem.StopInPlace(actor);
                    Set(world, order.Id, OrderStatus.Active, "Maintaining direct observation"); continue;
                }
                if (ObservationService.Inspect(world.Map, actor, target, world.LocalMs).Detected)
                {
                    MovementSystem.StopInPlace(actor);
                    Set(world, order.Id, OrderStatus.Blocked, "Last known position empty · awaiting updated sighting"); continue;
                }
            }
            actor.Facing = ObservationService.FacingToward(actor.Position, target, actor.Facing);
            var vantage = world.Map.Cells.Where(c => world.Map.Walkable(c) && Point2.Distance(c.World, target) is > 0.5 and < 2
                && world.Map.ClearSight(c.World, target) && !MovementSystem.Occupied(world, actor).Contains(c))
                .Select(c => (Cell: c, Path: MovementSystem.PlanRoute(world, actor, c)))
                .Where(p => p.Path.Count > 0).OrderBy(p => p.Path.Count).ThenBy(p => p.Cell.Q).ThenBy(p => p.Cell.R).FirstOrDefault();
            if (vantage.Path is null) { Set(world, order.Id, OrderStatus.Blocked, "Last known area reached or no clear observation route"); continue; }
            Travel(world, order, actor, vantage.Cell, true);
        }
    }
    internal static void Travel(SimulationWorld world, PersonnelOrder order, ActorState actor, Hex destination, bool keepFacing)
    {
        if (actor.Moving) { Set(world, order.Id, OrderStatus.Active, "Travelling"); return; }
        var path = MovementSystem.PlanRoute(world, actor, destination);
        if (path.Count == 0) { Set(world, order.Id, OrderStatus.Blocked, "Route blocked · waiting for access or clearance"); return; }
        MovementSystem.CommitRoute(actor, path, keepFacing);
        Set(world, order.Id, OrderStatus.Active, "Travelling");
    }
}
