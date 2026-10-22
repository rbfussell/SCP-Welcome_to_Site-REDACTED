# ADR-0001: One persistent simulation with multiple interfaces

**Status:** Proposed  
**Date:** October 21, 2026  
**Decision owner:** Project owner; not yet assigned

## Context

The game combines personal RPG play, a management terminal, facility planning, and background events. The user's clarified reference is X4: Foundations translated into top-down SCP play: the initial regional director inhabits a persistent Foundation network with Groups of Interest. Later employee starts use the same simulation without automatic management authority. SCP behavior must remain meaningful while the player is elsewhere. Procedural maps, teams, inventories, containment, shipments, and organization plans cannot diverge when the interface changes.

## Drivers

Persistent consequences, meaningful delegation, reusable procedural templates, mod extensibility, observable failure causes, and a bounded first deliverable.

## Options considered

| Option | Benefit | Cost |
|---|---|---|
| Separate simulation per mode | Each UI can be built independently | Duplicate state, invented mission outcomes, difficult handoffs and saves |
| Fully detailed simulation everywhere | Fewer abstraction boundaries | Unbounded processing/storage, especially in expandable domains |
| Shared kernel with certified detail levels | Shared rules and identity with bounded processing | Requires explicit transitions, fidelity contracts, and diagnostics |

## Proposed decision

Use one authoritative kernel with persistent entity IDs, validated commands, typed effects, explicit spatial domains, versioned saves, and detail levels chosen by causal relevance. Each interface projects this state. Personal and unattended operations use shared action/hazard semantics. Modules declare when lower detail is safe.

Represent the network as persistent organization/node/route records over physical places. Site managers and group planners issue bounded orders through shared services. Direct player control stays with one avatar; remote management does not transfer embodiment. Network-scale simulation is a core product requirement, with a separate bounded prototype after the local and anomaly gates.

Begin as one local process with a headless-capable kernel. Choose the presentation engine and sandbox runtime after M0 measures observation timing, persistence, domain transfer, and script isolation.

## Consequences

The terminal and facility UI can share the same orders and records used by personal play. New SCPs can compose published capabilities. Debugging can follow causes across modes.

The kernel and authoring tools require early investment. Some offscreen anomalies remain expensive. Exact cross-platform engine-physics replay and universal SCP compatibility are excluded. Lower detail requires evidence; lack of that evidence keeps the affected domain at sufficient fidelity.

## Validation and related documents

Prove SIM-01 through SIM-04, OPS-01, SAVE-01, MOD-04, and NET-01 through NET-05 in [scope and acceptance](../04-scope-and-acceptance.md). State and temporal rules are in [simulation and world](../02-simulation-and-world.md). Extension boundaries are in [the module contract](../03-scp-module-contract.md).

Accept this decision only when the project owner adopts the baseline; later changes should supersede it with a new ADR.
