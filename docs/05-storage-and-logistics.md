# Physical storage and delegated logistics

Status: Proposed implementation contract, revision 0.4. Hierarchical storage, specialized layouts, physical accessibility, console sorting, and consolidation preferences are user-confirmed direction. Numeric implementation limits below are proposals. This document extends [the game design](01-game-design.md), [simulation](02-simulation-and-world.md), and [acceptance plan](04-scope-and-acceptance.md).

The October 25 [personal inventory prototype](23-personal-inventory-prototype.md) implements worn/held mounts, separate pockets, backpack sections, nested cases/packets, physical packing, timed handling and nearby crate transfers. Specialized racks, mixed shelves, pallets and delegated logistics remain proposed.

## 1. Player experience

The same inventory model applies to the player's clothing and bags, NPC equipment, vehicle cargo, loose boxes, racks, cabinets, and facility stores. Local interaction exposes their physical arrangements. A console exposes stock summaries, filters, priorities, and work orders for personnel to carry out.

The user further confirms component/material composition: small nested contents have real dimensions, divisible materials retain amount/properties, and an assembly's installed parts are distinct from accessible container contents. [Item composition and crafting](16-item-composition-and-crafting.md) records future assembly/disassembly, derived capabilities, repeatable batches, remnants and wear through this same model. It does not add a crafting implementation or increase M1's content ceiling.

The supplied screenshot is a visual reference for shaped items and distinct packing regions. It is not a request to reproduce its artwork or other interface features. Inventory windows should identify the physical container, show its usable regions and access limits, and make an item's location understandable when walking through the facility.

Building storage increases real capacity: construct a suitable fixture, place it with working clearances, and commission it. Hauling staff then use it. A console storage category alone does not create capacity.

## 2. Several layouts under one model

| Layout | Physical example | Placement rule |
|---|---|---|
| Item grid | Pocket, backpack, box interior, vehicle compartment | An item occupies a cell mask, with allowed rotations and mass/dimension limits |
| Shaped slots | Long-gun rack or pistol rack | Each outlined mount accepts one compatible item with the correct size/profile |
| Box bays | Cabinet compartment | A compatible box occupies a declared bay/footprint; contents retain their own layout |
| Mixed shelf | Cabinet with boxes and loose equipment | Boxes and loose items occupy mapped, non-overlapping regions of the same shelf |
| Layered stacks | Pallet with nine positions, three boxes high | Each column has discrete support, stack-height and access rules |

A long-gun rack may expose four gun-shaped outlines and accept only long guns. A pistol does not fit by being smaller. Classification, mount compatibility, dimensions, and attachments all participate. User filters can narrow a fixture's native acceptance rules, but cannot turn a pistol rack into a rifle rack.

A cabinet's box-layout view and small-item view can use different grid scales. They must map to a shared capacity record. Placing a box reserves its actual footprint/envelope and blocks the corresponding loose-item area. Loose items can use the remaining shelf area if they fit. A shelf lip or smaller usable region can prevent a box from fitting even when several smaller items do.

Content authors provide mounting anchors, masks, envelopes, clearances, and mappings. The baseline uses authored discrete layouts; it does not require a general physics-based 3D packing solver. An irregular item's icon may be stylized, while its authoritative footprint stays explicit and stable.

## 3. Identity and hierarchy

An item/container instance has one physical parent or world location. Moving a full box moves that container and its descendants. Ownership, permissions, stock-allocation reservations, and physical location are separate facts: a team can be allocated a weapon that still physically sits in a rack.

Example hierarchy:

```text
site armory
  cabinet
    shelf
      medical-supply box
        dressing packets
      loose flashlight in remaining shelf space
  long-gun rack
    rifle in mount 2
  pallet
    column 4
      bottom box / middle box / top box
vehicle
  cargo compartment
    equipment case
      field instruments
character
  coat
    left pocket
      radio
```

Item-bearing parents consume capacity under one containment/location graph. Shelf and mount records may be structural regions rather than independently portable items. A box placed in another container consumes its exterior capacity there; its internal grid describes separate space, not extra free shelf area.

Validate cycles, maximum supported nesting depth, item dimensions, region occupancy, mass, load-bearing capacity, and acceptance rules. Propose a supported maximum depth of eight item-bearing ancestors, measured during M0; exceeding the published limit rejects placement. General nesting is part of the target model. M1 limits authored portable combinations for testing rather than forbidding cabinet → box → items.

Recursive mass includes containers and their contents. A full box may be too heavy for one person even when its external shape fits a bay. A stock query traverses contents once: it must not add a box's summarized contents to those same child records again.

## 4. Accessibility is a separate state

Determine access from the entire path: character → room/vehicle → fixture door or opening → shelf/stack → box lid → item. Check permissions, locks, seals, obstruction, reach, required tools, clearance, and current reservations. A valid storage location can still be inaccessible for retrieval.

The working interpretation of the user's “9 × 3” pallet is nine footprint positions with three boxes per column: 27 boxes at full capacity. Only the topmost box of each occupied column is directly accessible, so a full pallet offers nine accessible boxes. Removing a top box exposes the next box in that column. Empty columns or external obstructions can reduce the number accessible.

A buried box remains in stock but its contents are not directly retrievable. An order for them can be marked “requires unstacking.” If rehandling is allowed, plan removal of blockers to valid staging positions, retrieval, and optional restacking. These are actual movements with time, labor, space, and weight constraints. Without staging space or capable staff, report the blockage and wait. Never pull a lower box through the boxes above it or leave unsupported boxes floating.

Access to contents can differ from access to the container itself. A closed box may be carryable while its contents are not inspectable; a cabinet may allow a small item to be removed without enough clearance to extract a large case. Container definitions explicitly declare these distinctions.

Knowing that stock exists is also separate from access. Sealed, unknown, or stale-manifest contents remain unknown or estimated according to actor knowledge. Console availability categories distinguish known on-hand, immediately accessible, reserved, in transit, blocked, and unknown stock. Location, accessibility, and allocation are separate facets: an on-site reserved item may also be accessible. Label those counts and do not add overlapping facets into a stock total. Unknown quantities never become exact inventory merely because a summary is requested.

## 5. Storage policy

Each fixture, region, or box can declare native restrictions and a configurable policy:

- Allowed/forbidden item definitions, tags, compatibility classes, and condition ranges.
- Storage priority, maximum quantity and optional replenishment target/minimum reserve.
- Whether to accept automated deliveries, allow consolidation, or allow rehandling.
- Whether contents are pinned against automatic relocation or reserved for a team/task.
- Optional restrictions supplied by containment procedures, ownership, or security.

Defaults may inherit from room or fixture policy. An explicit local policy narrows physical/native restrictions; it cannot widen them. For the first implementation, a local policy replaces the inherited user-policy fields explicitly set there, and the UI shows the effective values and their source. Document how a field is reset to inheritance.

At receiving time, select destinations in this order:

1. Eliminate candidates that fail native restrictions, user filters, actual fit, weight, capacity reservations, authority, or feasible access/hauling conditions.
2. Prefer the highest explicit storage priority among eligible destinations.
3. At equal priority, prefer filling a compatible box/stack already holding the same item. Matching defaults to the same item definition, with condition/quality and batch distinctions preserved; a content policy may declare broader compatibility.
4. Prefer a placement that fills an existing compatible stack or compartment with the least additional fragmentation, using a bounded deterministic packing heuristic.
5. Prefer lower hauling/rehandling cost, then a stable ID tie-break for repeatability.

Thus a partly filled ammunition box wins over an empty box at equal priority; a higher-priority eligible destination wins over consolidation. Free capacity in a locked or incompatible container does not make it eligible. Worker-task urgency is a separate priority from destination preference.

Stack compatibility and maximum stack count are item-definition data. Items with distinct persistent state, including unique SCPs, cannot be merged merely because their icons match. Splitting/merging ordinary count stacks preserves quantity, condition/batch distinctions, and provenance required by the content rules.

Receiving decisions do not continuously reshuffle all stored objects. Existing valid placements stay put unless a higher-priority refill task, filter change, invalid placement, or explicit rebalance order requires a move. Pinning and task reservations constrain those moves. Equal-priority containers do not exchange stock endlessly to improve packing scores.

## 6. Console orders become physical work

An operator can request “store this delivery,” “keep this rack supplied,” “consolidate these supplies,” “prepare this team's loadout,” or “move this cabinet's contents.” The planner expands the intent into jobs using the same interaction and transfer rules as manual play.

Dispatch respects [employment and orders](07-employment-and-orders.md). Logistics employees haul as primary work. Other workers are eligible only when released for optional tasks, when carrying is part of their own duty, or after an authorized reassignment. A physically capable cafeteria worker is not automatically available to abandon service for a stock-sorting request. Optional hauling settings do not disable role-required carrying.

The work sequence is plan → reserve source and destination capacity → assign capable personnel → reach/access source → pick up → carry/transport → access destination → place → report. A long job is not one global transaction: pickup and placement are separate atomic changes. While carrying, the item has the worker/carrier as its actual parent or location.

Reserve exact items or quantities, packing footprint/slot, and relevant access resources. Moving a box reserves its subtree against incompatible concurrent transfers; staff cannot remove its contents midway through a reserved pickup. Reservations expire or are released on cancellation/replanning. Revalidate state revisions at each interaction boundary.

If an item, door, policy, worker, or destination changes, revalidate and replan. A worker interrupted after pickup retains the item or places it in a valid reachable location; cancellation does not teleport it back. A blocked destination cannot receive it invisibly. Persist both the work state and physical inventory state on save.

Manual actions are subject to the same invariants. If a player tries to take a reserved item, report the reservation and offer an authorized explicit cancellation/reassignment rather than silently duplicating it or trapping it behind an invisible lock. Emergency permissions can change allocation, but not physical access or mass.

Fixture relocation also requires a valid plan: contents and mounted items may need unloading, carried mass must fit the chosen method, and the new position must preserve access and navigation. Commissioned fixtures remain unavailable during a move. The initial slice supports installation/replacement after unloading; moving loaded furniture is later scope.

## 7. What the interfaces show

Local UI shows named compartments, shaped mounts, boxes as containers, per-column pallet layers, remaining capacity, restrictions, and reasons a move is invalid. Opening a child container provides a breadcrumb back to its physical parent. Inaccessible contents can be shown from known records but are disabled with a reason.

The console shows stock grouped by type with drill-down to site, room, fixture, compartment, and box. Show policies, reserved/accessible counts, outstanding haul jobs, blocked reasons, and estimated labor. Selecting “auto-distribute” previews destinations and creates work; it does not complete the rearrangement immediately. Remote knowledge and communication rules still apply.

Place fixture footprints and access zones in the facility/world representation. Visible rack occupancy and box positions must correspond to inventory records; visual abstraction can omit individual packets inside a closed box. Walking into the armory should reveal the storage arrangements chosen through management.

## 8. SCP integration and persistence

Ordinary boxes provide only their declared physical protection. Hazard suppression, shielding, exposure propagation, teleportation, or anomalous internal capacity require explicit module capabilities and rules. An active SCP does not stop updating simply because it was boxed, nested, assigned to stock, or moved offscreen.

Effects that change an item's size, container capacity, or topology must define invalid-placement outcomes. Stage prospective changes and validate final placement atomically. A full shelf cannot silently gain overlapping items; a transformation can reject, place declared outputs in a valid spill area, or enter the module's authored exceptional state. Preserve unique identity and recovery traces.

Save parent relationships, region placements, per-column stack order, policies, known manifests, subtree revisions, reservations, haul state, and pending access/rehandling actions. Restore from committed boundaries with no duplicate pickup, placement, or receiving credit. Inter-site shipments and local hauling share these inventory semantics.

## 9. Bounded first proof

The revised M1 proposal includes the four wearable containers, one four-mount long-gun rack, one four-mount pistol rack, one cabinet with three box bays and a shared loose-item shelf, one 27-box pallet, and one parked vehicle cargo compartment. At most two staff execute hauling simultaneously, drawn from the existing site population. Generic boxes reuse one definition and may contain different compatible stock.

This proves shaped slots, mixed-scale occupancy, a two-level facility container hierarchy, buried-box access, priority/consolidation, and equivalent manual/console results. Powered vehicle movement, general 3D packing, loaded-furniture moving, and an optimized warehouse scheduler remain excluded from M1. Acceptance requirements STO-01 through STO-10 are in the scope document.

The user confirms realtime personal inventory with actual handling time and compartment-specific AP in fights. References guide grid/location relevance and rotation; the visual target is rendered object sprites, without item cards.
