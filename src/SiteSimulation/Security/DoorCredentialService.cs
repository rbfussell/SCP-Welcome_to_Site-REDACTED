namespace SiteSimulation;

/// <summary>Physical credential presentation shared by doors and actor inventory. No artwork or UI grants access.</summary>
internal static class DoorCredentialService
{
    public const int PresentMs = 500, PresentAp = 2;

    internal static bool Held(SimulationWorld world, ActorState actor, InventoryItem item)
        => world.Inventory.FindRoot(item.Placement.ParentId)?.ActorId == actor.Id
            && world.Inventory.Region(item.Placement.ParentId, item.Placement.RegionId).MountSlot == "hand";

    internal static bool Displayed(SimulationWorld world, ActorState actor, InventoryItem item)
    {
        var state = world.Inventory;
        if (InventoryHierarchy.Root(state, item.Id).ActorId != actor.Id || item.Placement.AttachmentPose is null
            || InventoryAttachments.BodyPosition(state, item.Id) is null) return false;
        return InventoryAttachmentExposure.Check(state, item.Placement.ParentId,
            state.Region(item.Placement.ParentId, item.Placement.RegionId), InventoryAttachments.Occupancy(state, item)).Allowed;
    }

    private static bool Valid(InventoryState state, ActorState actor, InventoryItem item, DoorReader reader)
        => item.Condition > 0 && state.Definition(item.Id).Credential is { } credential
            && credential.Issuer == reader.Issuer && credential.Clearance >= reader.Clearance
            && (credential.SubjectId is null || credential.SubjectId == actor.Id);

    internal static bool Reachable(SimulationWorld world, ActorState actor, WallDefinition door)
        => Point2.Distance(actor.Position, door.Center) <= 1.35
            && !world.Map.SolidWalls.Any(w => w.Id != door.Id && Geometry.Intersects(actor.Position, door.Center, w.Start, w.End))
            && !world.Map.Definition.Props.Any(p => p.Kind == "cabinet" && Geometry.DistanceToSegment(p.Cell.World, actor.Position, door.Center) < .17);

    internal static bool Authorize(SimulationWorld world, ActorState actor, WallDefinition door, out InventoryItem? handBadge, out string reason)
    {
        handBadge = null; reason = "Access accepted.";
        if (door.Reader is not { } reader || world.Map.OpenDoors.Contains(door.Id)) return true;
        if (!reader.Powered) { reason = "Door reader has no power; access denied."; return false; }
        if (!Reachable(world, actor, door)) { reason = "Approach the reader with clear physical access."; return false; }
        if (!world.Inventory.Enabled) { reason = "No credential equipped; access denied."; return false; }
        var candidates = world.Inventory.Items.Values.Where(i => Valid(world.Inventory, actor, i, reader)).OrderBy(i => i.Id, StringComparer.Ordinal).ToArray();
        if (candidates.Any(i => Displayed(world, actor, i))) return true;
        handBadge = candidates.FirstOrDefault(i => Held(world, actor, i));
        if (handBadge is not null) return true;
        reason = "Access denied: visibly wear a valid badge or equip it in hand. Check issuer, clearance and assigned identity.";
        return false;
    }

    internal static void ValidatePresentation(SimulationWorld world, string actorId, InventoryRequest request)
    {
        var actor = world.Actors.Single(a => a.Id == actorId);
        var door = world.Map.Definition.Walls.SingleOrDefault(w => w.Id == request.TargetId && w.Door && w.Reader is not null);
        if (request.Destination is not null || request.RegionId is not null || door?.Reader is not { } reader)
            throw new ArgumentException("Choose a secured door reader.");
        if (world.Map.OpenDoors.Contains(door.Id)) throw new ArgumentException("Door is already open.");
        if (!DoorInteraction.Validate(world, actor, door.Id, out _, out var reason)) throw new ArgumentException(reason);
        var item = world.Inventory.Items[request.ItemId];
        if (!Held(world, actor, item)) throw new ArgumentException("Equip the badge in hand before presenting it.");
        if (!Valid(world.Inventory, actor, item, reader)) throw new ArgumentException("Credential issuer, clearance, identity or condition is invalid.");
    }

    internal static void CompletePresentation(SimulationWorld world, string actorId, InventoryRequest request)
    {
        var door = world.Map.Definition.Walls.Single(w => w.Id == request.TargetId);
        DoorInteraction.Toggle(world, door);
        world.Log("security", door.Id, "badge-presented", actorId + "; " + request.ItemId);
        PlayerFeedback.Add(world, MessageKind.Equipment, "Badge accepted · " + door.Id + " opened.");
    }

    /// <summary>Walking near a reader can open it from an exposed badge. Tactical entry remains a paid E action.</summary>
    internal static void AdvanceAutomatic(SimulationWorld world)
    {
        if (!world.LocalActive || world.Combat.Active || world.Inventory.Pending is not null) return;
        foreach (var actor in world.Actors.Where(a => a.Moving && world.LocalMs >= a.StartledUntilMs))
        foreach (var door in world.Map.Definition.Walls.Where(w => w.Reader is not null && !world.Map.OpenDoors.Contains(w.Id)))
        {
            if (!Reachable(world, actor, door) || !Authorize(world, actor, door, out var hand, out _) || hand is not null) continue;
            DoorInteraction.Toggle(world, door);
            world.Log("security", door.Id, "badge-displayed", actor.Id);
            if (actor == world.Player) PlayerFeedback.Add(world, MessageKind.Equipment, "Visible badge accepted · " + door.Id + " opened.");
        }
    }
}
