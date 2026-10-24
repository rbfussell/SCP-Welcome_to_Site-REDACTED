# ADR-0007: Independently launchable gameplay modes over one simulation

**Status:** Direction accepted by the user; runtime composition and mode contracts below proposed pending qualification

**Date:** October 24, 2026

## Context

The user wants OSCP, the world map, local character gameplay and facility management to load independently into the engine, each usable like a separate game, while ultimately working together. Their screens, assets, controls and development fixtures differ. Their personnel, stock, buildings, intelligence, missions and consequences belong to the same persistent world.

The existing Godot presentation/plain C# simulation split supports this direction, but the current prototype launches one spatial scene with attached panels. It does not yet implement four standalone mode entry points.

## Options

| Option | Benefit | Cost |
|---|---|---|
| One scene with every mode permanently loaded | Simple access to shared nodes initially | Hidden dependencies make standalone launch, testing and replacement difficult |
| Four games with separate simulation implementations, connected later | Each can develop freely at first | Rules, identities, persistence and timing diverge; integration needs reconciliation |
| Independent mode entry scenes using shared simulation services | Separate development/play flows and one set of authoritative rules | Requires explicit runtime composition, dependencies and lifecycle tests |

## Decision

Adopt independently launchable modes backed by shared simulation contracts from the start. Each has an entry scene, its own presentation/workflow controllers, declared service/content dependencies and a small runnable scenario. Initially keep these within the same Godot project and process. Separate exported executables or a network server are packaging decisions for later.

| Mode | Standalone experience | Shared simulation dependencies |
|---|---|---|
| OSCP | Command/document workspace, investigations, intelligence, mission planning and strategic management | Character/device authority, acquired records, communications, personnel, orders, resources and relevant planning services |
| World map | Region/network navigation, travel, known incidents and wider operations | Places/routes, knowledge, coarse scheduling, teams/shipments and encounter handoffs |
| Local character play | Isometric movement, perception, equipment, interactions and tactical combat | Actors, local domains, navigation, observation, inventory, actions and anomaly host |
| Facility management | Layout/section views, construction/fit-out, storage, utilities, staffing and maintenance | The same spatial footprints, stock, personnel, work orders and infrastructure |

A mode owns its UI flow and uncommitted drafts. Accepted plans, jobs, allocations and their consequences are simulation state. Modes communicate through validated commands, acquired query projections, events and explicit navigation requests. They do not require another mode's scene nodes, widget callbacks or renderer to make work happen.

## Proposed runtime composition

The application bootstrap owns the session, simulation service registry, authoritative scheduler and persistence. A mode receives a bounded runtime context containing its acting character, authorized device/context where applicable, available services and read subscriptions. Concrete C# interface names will be chosen during implementation.

- A standalone launch composes the real required services and a small authored/seeded test world. OSCP can therefore issue orders to simulated staff without loading local sprites; facility work can progress without the world-map renderer.
- Test-only fakes can support narrow UI checks, but a qualifying standalone scenario exercises real simulation rules. Missing optional services are shown as unavailable; required missing services produce a clear startup diagnostic.
- Combined play attaches modes to the same session, entity IDs, content versions, knowledge and save. Attaching a mode never creates another economy, player character or simulation tick loop.
- The shared scheduler advances all enabled simulation services once. Opening, hiding or unloading a view does not reset time, AP, action progress, resources or incident state. The existing local tactical hold/world-calendar rules still apply.
- View exit releases UI subscriptions and input ownership. Any in-world attention task, session access or feed observation is ended/retained through its explicit validated lifecycle; a hidden panel cannot accidentally remain an observer.
- Mode navigation carries session/place/entity/context IDs. Opening local play from a map or construction view selects the existing place; opening OSCP retains the acting character and actual device-access checks.
- One save transaction covers authoritative state. Each standalone scenario has a separate explicit save/session identity; importing or merging unrelated test saves into a campaign is outside this decision.

For example, OSCP submits an authorized equipment/construction plan. Personnel and logistics services reserve and perform real work. Facility management displays the plan and completed placement, while local play shows the same rack and carried materials. Switching screens neither completes the work nor copies the objects. Intelligence and mission reports likewise appear only after their normal delivery/knowledge rules allow it.

## Qualification and staging

| Proof | Required evidence |
|---|---|
| Standalone launch | Each implemented mode boots without sibling presentation scenes and can exercise a meaningful scenario using its declared services |
| Early integration | As soon as two modes are implemented, an action in one has the same identity/outcome in the other; integrate incrementally rather than waiting for all four |
| Lifecycle/time | Repeated attach/detach does not duplicate commands, subscriptions, ticks, resources or observers; local tactical waiting and outside scheduling remain correct |
| Persistence | Save during a cross-mode order/action, reload through a supported entry mode and attach another; progress, reservations, permissions and outcomes restore once |

The shared bootstrap/adapter seam belongs to M0 qualification. OSCP/local/facility proofs follow the bounded M1 features as they are implemented; world-map/network standalone qualification follows its existing later milestone. This adds an architectural requirement, not four complete games to the first slice. Current debug panels remain valid during the transition. Do not add empty mode implementations simply to claim isolation.

## Consequences

Modes can be developed, debugged and presented independently, and the production HUD/OSCP can replace engineering panels through the same contracts. Simulation rules remain reusable in headless scenarios and future combined play.

The cost is explicit dependency composition, lifecycle cleanup and both standalone/integration checks. A standalone mode still needs the simulation services that make its gameplay meaningful. Keeping service contracts shared from the start reduces the risk of four incompatible implementations requiring a late rewrite.

Related: [shared simulation](0001-shared-simulation.md), [Godot/core selection](0004-godot-and-simulation-core.md), [continuing world/local turns](0006-real-time-exploration-and-tactical-turns.md), [OSCP](../11-oscp-terminal.md), [scope](../04-scope-and-acceptance.md), [current code map](../../src/SiteSimulation/README.md).
