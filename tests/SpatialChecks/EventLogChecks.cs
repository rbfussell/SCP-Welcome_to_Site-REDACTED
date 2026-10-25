using SiteSimulation;

internal static class EventLogChecks
{
    public static int Run(string map, string operation)
    {
        var count = 0;
        void Check(bool ok, string detail) { if (!ok) throw new Exception("Event log: " + detail); count++; }
        var log = new PlayerEventLog();
        for (var i = 0; i < 130; i++) log.Add(i * 50, i * 50, (MessageKind)(i % 7), "message " + i);
        Check(log.Entries.Count == 100 && log.Entries[0].Id == 31 && log.Entries[^1].Id == 130, "journal bounds history to newest 100 messages");
        Check(log.Window().Length == 5 && log.Window()[0].Id == 126, "default view shows newest five");
        Check(log.Window(40)[0].Id == 40 && log.Window(40)[^1].Id == 44, "history window anchors by stable message ID");
        Check(log.Window(1)[0].Id == 31 && log.Window(999)[0].Id == 126, "evicted or future anchors clamp to available history");
        var world = new SimulationWorld(map, operation);
        world.Player.Cell = new(8, 3); world.Player.Facing = 1;
        var opponent = world.Actors.Single(a => a.Id == "tech"); opponent.Cell = new(11, 3); opponent.Facing = 4;
        world.Actors.Single(a => a.Id == "observer").Cell = new(5, 5);
        PlayerFeedback.Sense(world);
        Check(world.Events.Entries.Count(e => e.Text == opponent.Name + " comes into view.") == 1, "newly seen character is noted");
        var initialCount = world.Events.Entries.Count;
        opponent.Cell = new(12, 3); PlayerFeedback.Sense(world); PlayerFeedback.Sense(world);
        Check(world.Events.Entries.Count == initialCount, "ordinary continuously visible movement creates no spam");
        world.Player.EyesClosedUntilMs = 150; PlayerFeedback.Sense(world);
        world.LocalMs = world.WorldMs = 150; PlayerFeedback.Sense(world);
        Check(world.Events.Entries.Count == initialCount, "brief blink does not manufacture another arrival");
        world.Player.Facing = 4; PlayerFeedback.Sense(world);
        var hiddenCount = world.Events.Entries.Count;
        world.Actors.Single(a => a.Id == "observer").Cell = new(14, 3);
        PlayerFeedback.Equipment(world, opponent, "uses a special fixture.");
        PlayerFeedback.Attack(world, opponent, world.Actors.Single(a => a.Id == "observer"), true, 2);
        Check(world.Events.Entries.Count == hiddenCount, "hidden action outside player knowledge cannot leak into journal");
        PlayerFeedback.Attack(world, opponent, world.Player, true, 2);
        Check(world.Events.Entries[^1].Text.StartsWith("An unseen attacker") && !world.Events.Entries[^1].Text.Contains(opponent.Name), "attack on self is relevant without identifying hidden source");
        world.Player.Facing = 1; PlayerFeedback.Sense(world);
        Check(world.Events.Entries.Count(e => e.Text == opponent.Name + " comes into view.") == 2, "returning to genuine sight creates a new arrival episode");
        PlayerFeedback.Equipment(world, opponent, "uses a special fixture.");
        Check(world.Events.Entries[^1].Kind == MessageKind.Equipment, "visible relevant equipment action has its own category");
        world.Player.Cell = new(5, 8);
        world.Operations.Phone = world.Operations.Phone! with { Powered = false };
        world.Operations.Report(world, opponent.Name, "REMOTE SECRET REPORT", source: "radio");
        PlayerFeedback.Sense(world);
        Check(!world.Events.Entries.Any(e => e.Text.Contains("REMOTE SECRET")), "unaccessed terminal report is not player knowledge");
        var terminal = world.Operations.Definition!.Terminal;
        world.Player.Cell = Enumerable.Range(0, 6).Select(terminal.Neighbor).First(c => world.Map.Walkable(c)
            && !MovementSystem.Occupied(world, world.Player).Contains(c) && world.Map.ClearSight(c.World, terminal.World));
        PlayerFeedback.Sense(world); PlayerFeedback.Sense(world);
        Check(world.Events.Entries.Count(e => e.Text.Contains("REMOTE SECRET")) == 1, "reachable terminal acquires stored report only once");
        var snapshot = world.Events.Capture();
        var restored = new PlayerEventLog(); restored.Restore(snapshot, world.WorldMs, world.LocalMs,
            world.Actors.Select(a => a.Id).Concat(world.Anomalies.Bodies.Select(b => b.Id)).ToHashSet());
        Check(restored.Entries.SequenceEqual(world.Events.Entries), "journal preserves categories, texts and ordering");
        Check(!restored.Once("report:" + world.Operations.ReceivedReports.Last().Id), "saved delivery dedup prevents old report replay");
        for (var i = 0; i < 5000; i++) log.Once("test:" + i);
        Check(log.Capture().Consumed.Length == 4096, "message dedup metadata is also bounded");
        Console.WriteLine($"EVENT_LOG_CHECKS_PASS: {count} assertions; bounded history, acquired reports, categories, visibility episodes and hidden-action filtering.");
        return count;
    }
}
