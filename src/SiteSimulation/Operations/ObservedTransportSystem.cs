namespace SiteSimulation;

/// <summary>Open-trolley fixture. Each load/push is timed work followed by a validated one-hex transaction.</summary>
internal static class ObservedTransportSystem
{
    public const int WorkMs = 600;
    private static HashSet<Hex> OwnCells(OperationState state) => state.Trolley.Step is { Stage: "push" } step
        ? [Hex.FromWorld(state.Trolley.Position), step.Next] : [Hex.FromWorld(state.Trolley.Position)];
    private static AnomalyBody Body(SimulationWorld world) => world.Anomalies.Bodies.Single(b => b.Id == world.Operations.Definition!.SubjectId);
    private static bool ObservedByOther(SimulationWorld world, string handler) => ObservationService.Evaluate(world.Map,
        world.Actors.Where(a => a.Id != handler), Body(world).Position, world.LocalMs, Body(world).ObserverProfile).Observed;
    private static void ClearWork(SimulationWorld world)
    {
        var cart = world.Operations.Trolley;
        if (cart.Step is { Stage: "push" } step && step.Next != Hex.FromWorld(cart.Position))
            world.Map.DynamicBlockers.Remove(step.Next);
        world.Operations.Trolley = cart with { Step = null };
    }
    public static void DetachRelocated(SimulationWorld world, string subjectId)
    {
        if (world.Operations.Definition is null || world.Operations.Trolley.SubjectId != subjectId) return;
        ClearWork(world);
        var cart = world.Operations.Trolley;
        world.Operations.Trolley = cart with { SubjectId = null, HandlerId = null };
        world.Map.DynamicBlockers.Add(Hex.FromWorld(cart.Position));
        foreach (var order in world.Operations.Orders.Where(o => o.Task == PersonnelTask.Transport
            && o.Status is OrderStatus.Active or OrderStatus.Blocked).ToArray())
            PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Blocked, "Subject left trolley · reacquire sight and recover equipment");
        world.Log("transport", subjectId, "detached", "Relocation left the physical trolley in place; no duplicate cargo.");
    }
    public static void Advance(SimulationWorld world)
    {
        var state = world.Operations;
        if (state.Definition is not { } d) return;
        var order = state.Orders.Where(o => o.Task == PersonnelTask.Transport && o.Status is OrderStatus.Active or OrderStatus.Blocked)
            .OrderBy(o => o.Id).FirstOrDefault();
        if (order is null)
        {
            ClearWork(world); state.Trolley = state.Trolley with { HandlerId = null }; return;
        }
        var actor = world.Actors.Single(a => a.Id == order.ActorId);
        var body = Body(world);
        var cart = state.Trolley;
        if (cart.HandlerId is not null && cart.HandlerId != actor.Id) { ClearWork(world); cart = state.Trolley; }
        state.Trolley = cart = cart with { HandlerId = actor.Id };
        var cartCell = Hex.FromWorld(cart.Position);
        if (cart.SubjectId is not null && body.Cell != cartCell) { DetachRelocated(world, body.Id); return; }
        if (actor.Moving) return;
        if (actor.StoppedStep is not null || actor.Cell.Distance(cartCell) != 1)
        {
            ClearWork(world);
            var approach = Enumerable.Range(0, 6).Select(cartCell.Neighbor)
                .Where(c => world.Map.Walkable(c) && !MovementSystem.Occupied(world, actor).Contains(c))
                .Select(c => (Cell: c, Path: MovementSystem.PlanRoute(world, actor, c)))
                .Where(p => p.Path.Count > 0).OrderBy(p => p.Path.Count).FirstOrDefault();
            if (approach.Path is null) { PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Blocked, "Cannot reach trolley handle"); return; }
            PersonnelOrderSystem.Travel(world, order, actor, approach.Cell, false); return;
        }
        var knownPoint = state.Contacts.SingleOrDefault(c => c.ActorId == actor.Id)?.Position ?? order.TargetPoint;
        actor.Facing = ObservationService.FacingToward(actor.Position, cart.SubjectId is null ? knownPoint ?? cart.Position : cart.Position, actor.Facing);
        if (world.LocalMs < actor.StartledUntilMs || !world.Anomalies.AllowsObservedTransport(body.Id))
        { ClearWork(world); PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Blocked, "Handler not ready or module disallows transport"); return; }
        // Requiring real external observation makes the handler's own facing insufficient for the procedure.
        var seesBody = ObservationService.Inspect(world.Map, actor, body.Position, world.LocalMs).Detected;
        if (cart.SubjectId is not null && !ObservedByOther(world, actor.Id))
        { ClearWork(world); PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Blocked, "Observation coverage lost · trolley stopped"); return; }
        if (cart.SubjectId is null && seesBody && body.Cell.Distance(cartCell) == 1)
        {
            if (!ObservedByOther(world, actor.Id) || !world.Map.CanTraverse(body.Cell, cartCell, new HashSet<Hex> { body.Cell, cartCell }))
            { ClearWork(world); PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Blocked, "Loading needs clear access and another observer"); return; }
            if (!Work(world, order, actor, cartCell, actor.Cell, "load")) return;
            world.Map.DynamicBlockers.Remove(body.Cell);
            body.Cell = cartCell; // External transport preserves anomalous appearance/pose/encounter identity.
            world.Map.DynamicBlockers.Add(cartCell);
            state.Trolley = state.Trolley with { SubjectId = body.Id, Step = null };
            world.Log("transport", body.Id, "loaded", "Observed external loading onto open trolley; module remains active.");
            return;
        }
        Hex destination;
        if (cart.SubjectId is not null)
        {
            destination = d.Home;
            if (cartCell == destination)
            {
                // Unload into the receiving footprint; the empty trolley is parked at the handler's former hex.
                if (!ObservedByOther(world, actor.Id)) return;
                var parkedAt = actor.Cell;
                var exit = Enumerable.Range(0, 6).Select(actor.Cell.Neighbor).FirstOrDefault(c => c != cartCell && world.Map.Walkable(c)
                    && !MovementSystem.Occupied(world, actor).Contains(c) && world.Map.CanTraverse(actor.Cell, c, new HashSet<Hex> { actor.Cell }));
                if (exit == default) { PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Blocked, "No space to park trolley and step clear"); return; }
                if (!Work(world, order, actor, cartCell, actor.Cell, "unload")) return;
                world.Map.DynamicBlockers.Add(parkedAt);
                state.Trolley = new(parkedAt.World);
                actor.Cell = exit;
                actor.Facing = ObservationService.FacingToward(actor.Position, body.Position, actor.Facing);
                PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Complete, "Delivered and unloaded in receiving footprint");
                if (state.Orders.Count >= 32) state.Orders.RemoveAt(state.Orders.FindIndex(o => o.Status is OrderStatus.Complete or OrderStatus.Cancelled));
                state.Orders.Add(new(state.NextId++, order.Issuer, actor.Id, PersonnelTask.Observe, exit, order.Channel, OrderStatus.Active,
                    "Procedure follow-up: maintain observation until relieved", world.LocalMs, body.Position));
                world.Log("transport", body.Id, "unloaded", "One body retained; empty trolley parked alongside receiving footprint.");
                return;
            }
        }
        else
        {
            var contact = state.Contacts.SingleOrDefault(c => c.ActorId == actor.Id);
            var point = seesBody ? body.Position : contact?.Position ?? order.TargetPoint;
            if (point is null) { ClearWork(world); PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Blocked, "No acquired location for subject"); return; }
            if (!seesBody && cartCell.Distance(Hex.FromWorld(point.Value)) <= 1)
            { ClearWork(world); PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Blocked, "Last known area reached · reacquire subject"); return; }
            var options = world.Map.Cells.Where(c => c.Distance(Hex.FromWorld(point.Value)) == 1 && world.Map.Walkable(c)
                && c != actor.Cell && !MovementSystem.Occupied(world, actor).Contains(c))
                .Select(c => (Cell: c, Path: CartPath(world, cartCell, c, actor))).Where(p => p.Path.Count > 0).OrderBy(p => p.Path.Count).FirstOrDefault();
            if (options.Path is null) { ClearWork(world); PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Blocked, "Last known area reached · reacquire subject or clear route"); return; }
            destination = options.Cell;
        }
        var path = CartPath(world, cartCell, destination, actor);
        if (path.Count == 0) { ClearWork(world); PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Blocked, "Trolley route blocked · open access and clear personnel"); return; }
        var next = cart.Step?.Stage == "push" ? cart.Step.Next : path[0];
        var ignored = OwnCells(state);
        if (!world.Map.CanTraverse(cartCell, next, ignored) || !world.Map.CanTraverse(actor.Cell, cartCell, ignored)
            || next == actor.Cell || MovementSystem.Occupied(world, actor).Contains(next))
        { ClearWork(world); PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Blocked, "Push obstructed · reposition handler or clear next hex"); return; }
        if (!Work(world, order, actor, next, cartCell, "push")) return;
        world.Map.DynamicBlockers.Remove(cartCell);
        world.Map.DynamicBlockers.Add(next);
        actor.Cell = cartCell; actor.WalkMs += WorkMs;
        if (state.Trolley.SubjectId is not null) body.Cell = next;
        actor.Facing = ObservationService.FacingToward(actor.Position, next.World, actor.Facing);
        state.Trolley = state.Trolley with { Position = next.World, Step = null };
        world.Log("transport", actor.Id, "pushed", $"Trolley {cartCell} -> {next}; physical path validated; 600ms work.");
    }
    private static IReadOnlyList<Hex> CartPath(SimulationWorld world, Hex start, Hex end, ActorState handler)
    {
        var ignored = OwnCells(world.Operations);
        if (world.Operations.Trolley.Step is { Stage: "push" } step) ignored.Add(step.Next);
        return world.Map.FindPath(start, end, MovementSystem.Occupied(world, handler).Append(handler.Cell).ToHashSet(), ignored);
    }
    private static bool Work(SimulationWorld world, PersonnelOrder order, ActorState actor, Hex next, Hex handlerNext, string stage)
    {
        var state = world.Operations;
        var step = state.Trolley.Step;
        if (step is null || step.Stage != stage || step.Next != next)
        {
            ClearWork(world);
            step = new(next, handlerNext, 0, stage);
            if (stage == "push") world.Map.DynamicBlockers.Add(next);
        }
        step = step with { ProgressMs = step.ProgressMs + SpatialSimulation.TickMs };
        state.Trolley = state.Trolley with { Step = step };
        PersonnelOrderSystem.Set(world, order.Id, OrderStatus.Active, stage == "load" ? "Loading under observation" : stage == "unload" ? "Unloading in receiving footprint" : "Pushing trolley");
        if (step.ProgressMs < WorkMs) return false;
        ClearWork(world);
        return true;
    }
}
