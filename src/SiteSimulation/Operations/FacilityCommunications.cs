namespace SiteSimulation;

/// <summary>Bounded local voice and issued-radio fixture. No faction-wide knowledge sharing.</summary>
internal static class FacilityCommunications
{
    public static bool Voice(SimulationWorld world, ActorState a, ActorState b) => Point2.Distance(a.Position, b.Position) <= 4
        && world.Map.ClearSight(a.Position, b.Position);
    public static bool Radio(SimulationWorld world, string actorId) => world.Operations.NetworkOnline
        && world.Operations.Radios.Any(r => r.ActorId == actorId && r.Powered)
        && (!world.Inventory.Enabled || InventoryAccessService.Capability(world, actorId, "site-radio"));
    public static bool TerminalReachable(SimulationWorld world) => OscpService.WorkstationAvailable(world) && world.Operations.NetworkOnline;
    public static bool Link(SimulationWorld world, PersonnelOrder order)
    {
        var issuer = world.Actors.Single(a => a.Id == order.Issuer);
        var recipient = world.Actors.Single(a => a.Id == order.ActorId);
        return order.Channel == "voice" ? Voice(world, issuer, recipient)
            : (order.Channel == "phone" ? OscpService.PhoneAvailable(world) && world.Operations.NetworkOnline : TerminalReachable(world))
                && Radio(world, recipient.Id);
    }
    public static void SenseAndReport(SimulationWorld world)
    {
        var state = world.Operations;
        if (state.Definition is not { } d) return;
        var subject = world.Anomalies.Bodies.Single(b => b.Id == d.SubjectId);
        foreach (var actor in world.Actors)
        {
            if (!ObservationService.Inspect(world.Map, actor, subject.Position, world.LocalMs).Detected) continue;
            state.Contacts.RemoveAll(c => c.ActorId == actor.Id);
            state.Contacts.Add(new(actor.Id, subject.Id, subject.Position, subject.Revision, world.LocalMs));
            // Continuous coordinates are evidence internally; reports are bounded by encounter/meaningful movement.
            var sent = state.Reports.LastOrDefault(r => r.Author == actor.Name && r.SubjectId == subject.Id);
            if (sent is not null && sent.Encounter == subject.Revision && sent.Position is { } prior
                && Point2.Distance(prior, subject.Position) < 0.5) continue;
            if (actor == world.Player) continue; // Direct player perception has its own acquired-evidence ledger.
            if (!Voice(world, actor, world.Player) && !((state.TerminalPowered || OscpService.PhoneAvailable(world)) && Radio(world, actor.Id))) continue;
            state.Report(world, actor.Name, $"SCP sighting at {subject.Position.X:0.0}, {subject.Position.Y:0.0} · encounter {subject.Revision}",
                subject.Position, subject.Id, subject.Revision, Voice(world, actor, world.Player) ? "voice" : "radio");

        }
    }
}
