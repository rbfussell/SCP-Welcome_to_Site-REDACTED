using SiteSimulation;
using SitePresentation;

internal static class FieldPresentationChecks
{
    public static int Run(string json)
    {
        var passed = 0;
        void Check(bool condition, string detail)
        { if (!condition) throw new Exception("Field presentation: " + detail); passed++; }
        var sim = new SpatialSimulation(json);
        var shadingState = sim.Save();
        var left = FieldPresentation.SightShade(sim, new(3 - 0.00001, 4.4));
        var right = FieldPresentation.SightShade(sim, new(3 + 0.00001, 4.4));
        Check(left > 0 && Math.Abs(left - right) < 0.001, "cosmetic shading cannot jump at a floor-tile boundary");
        var direction = sim.Player.Cell.Neighbor(sim.Player.Facing).World - sim.Player.Cell.World;
        direction *= 1 / direction.Length;
        var edge = direction * 0.5 + new Point2(-direction.Y, direction.X) * Math.Sqrt(0.75);
        var edgePoint = sim.Player.Position + edge * 0.5;
        var edgeShade = FieldPresentation.SightShade(sim, edgePoint);
        Check(sim.VisibleToPlayer(edgePoint) && edgeShade is > 0 and < 1,
            "a cosmetic cone fade must not remove exact observation near its edge");
        Check(FieldPresentation.SightShade(sim, sim.Player.Position - direction * 0.5) == 0,
            "cosmetic shading must not brighten a point behind the viewer");
        Check(sim.Save() == shadingState, "sampling floor appearance cannot mutate observation, RNG or world state");
        var statue = sim.Anomalies.Bodies.Single();
        var feedback = new DiscoveryFeedback(); feedback.Reset(sim);
        Check(!FieldPresentation.ShowAnomaly(sim, statue, false), "closed room hides statue despite another human observing it");
        Check(FieldPresentation.ShowAnomaly(sim, statue, true), "diagnostics remains explicitly omniscient");
        Check(FieldPresentation.ShowActor(sim, sim.Player, false), "avatar remains drawable");
        sim.ToggleObserverAttention();
        Check(!FieldPresentation.ShowAnomaly(sim, statue, false), "silent relocation behind someone is not a player sighting");
        Check(!feedback.Update(sim, true) && feedback.Discoveries == 0, "silent relocation must not play discovery feedback");
        var neighbor = Enumerable.Range(0, 6).Select(statue.Cell.Neighbor).First(c => sim.Map.Walkable(c)
            && !sim.Actors.Any(a => a.Cell == c) && sim.Map.ClearSight(c.World, statue.Cell.World));
        sim.Player.Cell = neighbor; sim.Player.Route.Clear(); sim.Player.SegmentMs = 0;
        sim.Face(statue.Cell.World);
        Check(FieldPresentation.ShowAnomaly(sim, statue, false), "actual avatar sight reveals the statue");
        Check(feedback.Update(sim, true) && feedback.Discoveries == 1, "actual avatar discovery produces one presentation event");
        var authoritative = sim.Save();
        for (var i = 0; i < 20; i++) Check(!feedback.Update(sim, true), "render refresh cannot repeat the cue");
        Check(sim.Save() == authoritative, "presentation must not consume module RNG or mutate world/trace");
        var pulse = feedback.Pulse(sim.LocalMs);
        sim.LocalHold = true;
        for (var i = 0; i < 100; i++) sim.Tick();
        Check(feedback.Pulse(sim.LocalMs) == pulse, "held local time retains the reaction presentation phase");
        feedback.Reset(sim);
        Check(!feedback.Update(sim, true) && feedback.Caption(sim.LocalMs) == "", "restore baseline must not replay historical encounters");
        sim.LocalHold = false; sim.BlinkPlayer();
        Check(!FieldPresentation.ShowAnomaly(sim, statue, false), "closed eyes cannot display the statue");
        sim.Map.LightsOn = false;
        Check(!FieldPresentation.ShowAnomaly(sim, statue, false), "unlit scene cannot expose an unseen statue");
        Console.WriteLine($"FIELD_PRESENTATION_CHECKS_PASS: {passed} assertions; direct sight, no remote/relocation cues, deduplication and read-only rendering.");
        return passed;
    }
}
