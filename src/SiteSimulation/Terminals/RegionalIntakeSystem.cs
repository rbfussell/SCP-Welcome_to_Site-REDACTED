namespace SiteSimulation;

/// <summary>One authored remote-report fixture. Creates a source claim, never a hidden entity or deployed team.</summary>
internal static class RegionalIntakeSystem
{
    public static void Advance(SimulationWorld world)
    {
        var state = world.Operations;
        if (state.Definition is not { RegionalIntakeEnabled: true } || state.RegionalIntakeSent
            || !state.NetworkOnline || world.WorldMs < 1000) return;
        state.Report(world, "REGIONAL RELAY / R-017",
            "Ash Creek service road: a local contact reports unexplained figures after dark. Source unverified; exact location and cause unconfirmed. No Foundation team deployed. Assessment requested before classification.", source: "scipnet");
        state.RegionalIntakeSent = true;
    }
}
