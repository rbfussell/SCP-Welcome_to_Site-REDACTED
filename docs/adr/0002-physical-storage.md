# ADR-0002: Shared physical storage with delegated hauling

**Status:** Proposed  
**Baseline revision:** 0.3  
**Decision owner:** Project owner; not yet assigned

## Context

The user specifies shaped personal inventories and specialized facility storage, including firearm racks, boxes, mixed cabinet shelves, and pallets. Local exploration must show the same arrangement that console automation manages. Pallet capacity can exceed immediately accessible stock, and nested layout scales must not create duplicate space.

## Options considered

| Option | Benefit | Cost |
|---|---|---|
| Flat stock counts with decorative furniture | Simple management | Cannot preserve location, nesting, access, or meaningful exploration |
| Separate local and console inventories | Each interface is easy to specialize | Reconciliation errors, duplicated stock, and inconsistent capacity |
| Shared container hierarchy, typed layouts, and haul jobs | Consistent physical state and useful automation | Requires occupancy mapping, ancestor access, reservations, and job recovery |

## Proposed decision

Use one acyclic item/container hierarchy with typed storage regions. Item grids, shaped mounts, box bays, and pallet layers share placement/access services. Different grid scales on one shelf map to shared capacity. Inventory summaries are queries over this state.

Console commands produce physical hauling jobs; manual interaction and staff use the same transfer rules. Destination selection honors hard restrictions, then explicit priority, then same-item consolidation and packing/haul preferences. Published inheritance and stable ordering make decisions explainable.

## Consequences

Exploration, personnel loadouts, vehicle cargo and site logistics remain consistent. Specialized furniture creates meaningful capacity, while automation handles routine sorting. Source/destination reservations and interrupted jobs require explicit persistence. Discrete authored layouts bound implementation; general 3D packing and loaded-furniture moving are deferred.

## Validation and related decisions

Prove STO-01 through STO-10 in [scope and acceptance](../04-scope-and-acceptance.md). Details are in [physical storage and delegated logistics](../05-storage-and-logistics.md). This extends [ADR-0001](0001-shared-simulation.md). Both architectural proposals remain subject to adoption; the underlying requested storage behavior is confirmed user intent.
