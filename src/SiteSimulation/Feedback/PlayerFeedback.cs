namespace SiteSimulation;

/// <summary>Evidence gates and feedback producers shared by realtime and tactical play.</summary>
internal static class PlayerFeedback
{
    public static void Add(SimulationWorld world, MessageKind kind, string text) => world.Events.Add(world.WorldMs, world.LocalMs, kind, text);
    public static bool Sees(SimulationWorld world, ActorState actor) => actor == world.Player
        || ObservationService.Inspect(world.Map, world.Player, actor.Position, world.LocalMs).Detected;
    public static void Sense(SimulationWorld world)
    {
        if (!world.LocalActive) return;
        if (world.LocalMs >= world.Player.EyesClosedUntilMs)
        {
            var visible = world.Actors.Where(a => a != world.Player && Sees(world, a)).Select(a => a.Id)
                .Concat(world.Anomalies.Bodies.Where(b => ObservationService.Inspect(world.Map, world.Player, b.Cell.World, world.LocalMs).Detected).Select(b => b.Id)).ToHashSet();
            foreach (var id in visible.Where(id => !world.Events.Visible.Contains(id)).Order(StringComparer.Ordinal))
            {
                var actor = world.Actors.SingleOrDefault(a => a.Id == id);
                Add(world, MessageKind.Observation, $"{actor?.Name ?? id} comes into view.");
            }
            world.Events.Visible.Clear(); world.Events.Visible.UnionWith(visible);
        }
        foreach (var report in world.Operations.Reports)
        {
            if (report.Source == "radio" && !FacilityCommunications.TerminalReachable(world)) continue;
            if (!world.Events.Once("report:" + report.Id)) continue;
            Add(world, report.SubjectId is null ? MessageKind.Orders : MessageKind.Observation, $"{report.Author}: {report.Text}");
        }
    }
    public static void NoAcknowledgement(SimulationWorld world, PersonnelOrder order)
    {
        if (world.LocalMs - order.IssuedMs < 1000 || !world.Events.Once("no-ack:" + order.Id)) return;
        var name = world.Actors.Single(a => a.Id == order.ActorId).Name;
        Add(world, MessageKind.Warning, order.Channel == "voice"
            ? $"No acknowledgement from {name}. I don't think they heard me."
            : $"Still no acknowledgement from {name}; the order may not have arrived.");
    }
    public static void Equipment(SimulationWorld world, ActorState actor, string action)
    {
        if (Sees(world, actor)) Add(world, MessageKind.Equipment, $"{(actor == world.Player ? "You" : actor.Name)} {action}");
    }
    public static void Attack(SimulationWorld world, ActorState source, ActorState target, bool hit, int damage)
    {
        var seeSource = Sees(world, source); var seeTarget = Sees(world, target);
        if (!seeSource && !seeTarget) return;
        var who = seeSource ? source == world.Player ? "You" : source.Name : "An unseen attacker";
        var whom = seeTarget ? target == world.Player ? "you" : target.Name : "an unseen target";
        var result = !seeTarget ? "impact not observed" : hit ? $"hit, {damage} training points" : "miss";
        Add(world, MessageKind.Combat, $"{who} fire{(source == world.Player ? "" : "s")} at {whom}: {result}.");
    }
}
