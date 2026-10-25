namespace SiteSimulation;

/// <summary>Validation for detached operation restores. Does not execute orders or replay reports.</summary>
internal static class OperationSaveCodec
{
    public static void Restore(SimulationWorld world, OperationSnapshot? saved)
    {
        var state = world.Operations;
        if (state.Definition is not { } d)
        {
            if (saved is not null) throw new ArgumentException("Operation state has no matching scenario.");
            return;
        }
        if (saved is null || saved.Orders is null || saved.Contacts is null || saved.Reports is null || saved.Radios is null || saved.Radios.Any(r => r is null)
            || saved.Trolley is null || saved.NextId < 1 || saved.Orders.Length > 32 || saved.Contacts.Length > world.Actors.Count
            || saved.Reports.Length > 40 || saved.StableMs < 0 || saved.StableMs > d.StabilizeMs
            || saved.StableMs % SpatialSimulation.TickMs != 0 || saved.Completed && !saved.HadBreach
            || saved.PhysicalStatus is not ("Awaiting coverage" or "Contained · procedure compliant" or "Breach · outside receiving footprint"
                or "Emergency stabilized · procedure incomplete" or "Coverage lost · unresolved"))
            throw new ArgumentException("Invalid operation snapshot.");
        var actorIds = world.Actors.Select(a => a.Id).ToHashSet();
        var ids = saved.Orders.Select(o => o?.Id ?? 0).Concat(saved.Reports.Select(r => r?.Id ?? 0)).ToArray();
        if (ids.Any(id => id <= 0 || id >= saved.NextId) || ids.Distinct().Count() != ids.Length
            || saved.Orders.Any(o => o is null || o.Issuer != d.CommanderId || !actorIds.Contains(o.ActorId) || o.ActorId == o.Issuer
                || !Enum.IsDefined(o.Task) || !Enum.IsDefined(o.Status) || o.Channel is not ("voice" or "terminal" or "phone")
                || o.IssuedMs < 0 || o.IssuedMs > world.LocalMs || o.Detail is null || o.Detail.Length > 250
                || !world.Map.Inside(o.Destination.World) || o.TargetPoint is { } target && !world.Map.Inside(target))
            || saved.Orders.Where(o => o.Status is OrderStatus.Active or OrderStatus.Blocked).GroupBy(o => o.ActorId).Any(g => g.Count() > 1)
            || saved.Contacts.Any(c => c is null || !actorIds.Contains(c.ActorId) || c.SubjectId != d.SubjectId || !world.Map.Inside(c.Position)
                || c.LocalMs < 0 || c.LocalMs > world.LocalMs || c.Encounter < 0 || c.Encounter > world.Anomalies.Bodies.Single(b => b.Id == d.SubjectId).Revision)
            || saved.Contacts.Select(c => c.ActorId).Distinct().Count() != saved.Contacts.Length
            || saved.Reports.Any(r => r is null || r.LocalMs < 0 || r.LocalMs > world.LocalMs || r.Author is null || r.Text is null
                || r.Author.Length > 100 || r.Text.Length > 500 || r.Source is not ("system" or "voice" or "radio" or "scipnet")
                || r.SubjectId is not null && (r.SubjectId != d.SubjectId || r.Position is null || !world.Actors.Any(a => a.Name == r.Author)
                    || r.Encounter < 0 || r.Encounter > world.Anomalies.Bodies.Single(b => b.Id == d.SubjectId).Revision)
                || r.Position is { } point && !world.Map.Inside(point))
            || !saved.Radios.Select(r => r.ActorId).Order().SequenceEqual(d.RadioActors.Order()))
            throw new ArgumentException("Invalid operation orders, evidence or radios.");
        if (saved.RegionalIntakeSent && !d.RegionalIntakeEnabled) throw new ArgumentException("Unexpected regional intake state.");
        if (saved.Phone is { } phone && phone.ActorId != d.CommanderId
            || saved.AcquiredReports is { } acquired && (acquired.Distinct().Count() != acquired.Length
                || acquired.Any(id => !saved.Reports.Any(r => r.Id == id))))
            throw new ArgumentException("Invalid field device or acquired record ledger.");
        var cart = saved.Trolley;
        var cell = Hex.FromWorld(cart.Position);
        var body = world.Anomalies.Bodies.Single(b => b.Id == d.SubjectId);
        if (body.Cell == d.Terminal || !world.Map.Inside(cart.Position) || Point2.Distance(cart.Position, cell.World) > 1e-8 || cell == d.Terminal
            || cart.SubjectId is not null && (cart.SubjectId != d.SubjectId || body.Cell != cell)
            || cart.SubjectId is null && body.Cell == cell || cart.HandlerId is not null && !actorIds.Contains(cart.HandlerId)
            || !world.Map.Walkable(cell, new HashSet<Hex> { body.Cell })
            || world.Actors.Any(a => a.Cell == cell || a.Moving && a.Route[0] == cell || a.StoppedStep?.Next == cell))
            throw new ArgumentException("Invalid trolley/cargo occupancy.");
        world.Map.DynamicBlockers.Add(d.Terminal); world.Map.DynamicBlockers.Add(cell);
        if (cart.Step is { } step)
        {
            var handler = world.Actors.SingleOrDefault(a => a.Id == cart.HandlerId);
            var active = saved.Orders.Any(o => o.ActorId == cart.HandlerId && o.Task == PersonnelTask.Transport
                && o.Status is OrderStatus.Active or OrderStatus.Blocked);
            if (handler is null || !active || handler.Moving || handler.StoppedStep is not null || handler.Cell.Distance(cell) != 1
                || step.ProgressMs is <= 0 or >= ObservedTransportSystem.WorkMs || step.ProgressMs % SpatialSimulation.TickMs != 0
                || step.Stage is not ("load" or "push" or "unload")
                || step.Stage == "push" && (step.HandlerNext != cell || !world.Map.CanTraverse(cell, step.Next, new HashSet<Hex> { cell })
                    || world.Actors.Any(a => a.Cell == step.Next || a.Moving && a.Route[0] == step.Next || a.StoppedStep?.Next == step.Next))
                || step.Stage != "push" && (step.Next != cell || step.HandlerNext != handler.Cell)
                || step.Stage == "load" && (cart.SubjectId is not null || body.Cell.Distance(cell) != 1)
                || step.Stage == "unload" && (cart.SubjectId is null || cell != d.Home))
                throw new ArgumentException("Invalid in-progress trolley work.");
            if (step.Stage == "push") world.Map.DynamicBlockers.Add(step.Next);
        }
        // Older matching saves did not issue a handset or retain a separate OSCP cache.
        state.RegionalIntakeSent = saved.RegionalIntakeSent;
        state.Phone = saved.Phone ?? new(d.CommanderId, Issued: false);
        state.AcquiredReports.Clear();
        state.AcquiredReports.UnionWith(saved.AcquiredReports ?? saved.Reports.Where(r => r.Source is not ("radio" or "scipnet")).Select(r => r.Id).ToArray());
        state.Orders.Clear(); state.Orders.AddRange(saved.Orders);
        state.Contacts.Clear(); state.Contacts.AddRange(saved.Contacts);
        state.Reports.Clear(); state.Reports.AddRange(saved.Reports);
        state.Radios.Clear(); state.Radios.AddRange(saved.Radios);
        state.NextId = saved.NextId; state.Trolley = cart; state.TerminalPowered = saved.TerminalPowered;
        state.NetworkOnline = saved.NetworkOnline; state.StableMs = saved.StableMs; state.HadBreach = saved.HadBreach;
        state.Completed = saved.Completed; state.PhysicalStatus = saved.PhysicalStatus;
    }
}
