namespace SiteSimulation;

public sealed record FieldPhoneState(string ActorId, bool Issued = true, bool Powered = true, bool Accessible = true);
public sealed record OscpAccess(bool Available, bool Connected, bool CanCommand, string DeviceId, string Identity, string Role, string Status, bool CanPlan);

/// <summary>Device access and player-acquired records. No widget state or hidden personnel positions.</summary>
internal static class OscpService
{
    public static bool PhoneAvailable(SimulationWorld world) => world.Operations.Definition is not null
        && world.Operations.Phone is { Issued: true, Powered: true, Accessible: true } phone && phone.ActorId == world.Player.Id
        && (!world.Inventory.Enabled || InventoryAccessService.Capability(world, world.Player.Id, "field-phone"));
    public static bool WorkstationAvailable(SimulationWorld world) => world.Operations.Definition is { } d
        && world.Operations.TerminalPowered && Point2.Distance(world.Player.Position, d.Terminal.World) <= 1.35
        && world.Map.ClearSight(world.Player.Position, d.Terminal.World);
    public static bool ReceivesRadio(SimulationWorld world) => world.Operations.NetworkOnline
        && (WorkstationAvailable(world) || PhoneAvailable(world));
    public static OscpAccess Access(SimulationWorld world, OscpDevice device)
    {
        var available = device switch { OscpDevice.Workstation => WorkstationAvailable(world), OscpDevice.Phone => PhoneAvailable(world), _ => false };
        var connected = available && world.Operations.NetworkOnline;
        var authority = world.Operations.Definition?.CommanderId == world.Player.Id;
        return new(available, connected, connected && authority && world.LocalActive && !world.Combat.Active,
            device == OscpDevice.Phone ? "FIELD-01" : "SITE-OPS-01", world.Player.Name, authority ? "Operations director" : "Personnel",
            !available ? device == OscpDevice.Phone ? "Handset unavailable · issued, powered and accessible device required" : "Approach a powered OSCP workstation"
                : !connected ? "OFFLINE · previously acquired records" : "SITE LINK CONNECTED",
            connected && authority && !world.Combat.Active && !world.LocalHold);
    }
    public static void Acquire(SimulationWorld world)
    {
        var state = world.Operations;
        state.AcquiredReports.RemoveWhere(id => !state.ReceivedReports.Any(r => r.Id == id));
        foreach (var report in state.ReceivedReports.Where(r => r.Source is not ("radio" or "scipnet") || ReceivesRadio(world))) state.AcquiredReports.Add(report.Id);
    }
    public static OperationReport[] Records(SimulationWorld world) => world.Operations.ReceivedReports
        .Where(r => world.Operations.AcquiredReports.Contains(r.Id)).ToArray();
}
