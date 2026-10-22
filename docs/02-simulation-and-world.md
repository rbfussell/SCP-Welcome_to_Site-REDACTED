# Simulation and procedural world

Status: Proposed implementation contract, revision 0.5. Confirmed gameplay direction is recorded in the linked design/scenario documents; these architecture choices require prototype evidence.

## 1. State and authority

Use one simulation kernel with serializable domain data. Rendering, audio, UI, file access, and optional LLM adapters sit outside it. Start as a modular application in one process; “backend” does not require a hosted server.

| Record | Minimum authoritative state |
|---|---|
| World | Seed, branch/epoch and tick, scenario/ruleset, content lockfile, generator versions, scheduled events |
| Organization | ID, objectives, assets, knowledge, relationships, authority and standing orders |
| Network node | Site/outpost ID, location, responsible manager, capacity, reserves, obligations |
| Network connection | Route/endpoints, transport or communication capability, travel time, capacity, availability |
| Shipment | Cargo IDs/batches, source/destination, reservations, carrier/escort, route, state, receiving procedure |
| Spatial domain | Stable ID, topology, local clock mapping, rules, generator and seed |
| Place | Parent, envelope, level, sockets, navigation, utilities, generated children |
| Entity | ID, components, location, ownership, revision |
| Character | Body, condition, persona, skills, knowledge, schedule, intent, equipment |
| Perceived context | Observer, physical/perceptual relation, apparent domain, sensory/identity effects, versioned bindings and lifetime |
| Evidence | Actor/organization, reported or perceived fact, source, context, timestamp and uncertainty |
| Device/session | Physical terminal, power/network profile, actor credentials, reachable apps/services, focus and branch lifetime |
| Digital record/message | Versioned dossier/evidence/case, sender/provenance, permissions, delivery chain, receipt and synchronization state |
| Digital personnel | Resident AIC identity/assignment, physical host deployment, clearance, evidence/known systems, workload and allowed actions; detailed authority pending; optional decision-provider interface separate from actor state |
| Target/cross-context binding | Apparent entity/position/action to authoritative target/effect relation, revision and capability scope |
| Employment | Role/workplace/post, shift and breaks, qualifications, supervisor, duty continuation, relief/coverage and status |
| Order/team agreement | Issuer and authority, objective, urgency, acknowledgements, role assignments, interruption/expiry and outcome |
| Presentation binding | Visual definition/version, state/facing/frame mapping, anchors and attachments; no authority over gameplay |
| Item/container | Instance ID, definition, shape, contents, condition, mass, access |
| Storage region | Parent fixture/container, layout type, masks/mounts/bays/layers, shared occupancy mapping, access dependencies |
| Storage policy | Effective native/user filters, source of inheritance, priority, targets, consolidation/rehandling/pinning permissions |
| Haul job | Intent, item/quantity, source/destination, reserved footprint, assigned carrier, access/staging steps, execution state |
| Operation | Objective, participants, reservations, orders, procedure state, report |
| Anomaly instance | Module ID/version, state-machine state, persistent fields, rule handles |
| Event | ID, time, phase, cause, scope, payload version, stable ordering key |

Definitions are immutable content. Instances carry mutable state. A character, item, or SCP is never a second independent copy because another UI displays it.

Commands express intent. The kernel validates them, commits transactions, and emits events. Invalid commands produce a reason. A terminal button, NPC action, and player interaction all call this boundary.

The input controller supplies an acting character and branch/context revision. Role/authority validation belongs to domain services, not a special always-authorized player path. Director and employee starts share actors and rules. Ctrl-click can express a force intent but cannot bypass those validators; equipment abilities retain their source item identity and access preconditions.

OSCP is a client of the same boundary. SCiPnet messages/query projections carry provenance and synchronization state. Order acceptance, staff acknowledgement and completion are distinct events. Digital files exist in the simulated world rather than the host filesystem. An assigned AIC submits work within its own credentials and knowledge; its staffing/authority is a lore/scenario choice, not automatic operating-system omnipotence. See [OSCP](11-oscp-terminal.md).

Godot is the selected engine. The proposed [Godot/C# integration](adr/0004-godot-and-simulation-core.md) keeps this kernel independent from Godot scene nodes; the exact runtime/configuration and implementation still need qualification. Neither engine physics nor the terminal's display becomes the source of authoritative deterministic state.

The selected field view is fixed isometric/three-quarter over square logical tiles. World coordinates, footprints, height/floor and sensory geometry remain authoritative; projection, sprite bounds, draw order and screen picking are presentation concerns. A detailed sprite can extend beyond its logical tile without changing collision or observation. The [sprite contract](06-sprites-and-placeholders.md) defines the art/projection boundary.

Square construction tiles do not settle the authoritative actor-position model. Cell-center movement, separate navigation grids and smooth sub-cell positions remain explicit alternatives; smooth positions with independent facing are the recommended real-time candidate pending the user's answer. [Fallout 2 research](14-fallout-2-placement-research.md) explains why discrete navigation, terrain tiles and visual offsets must be considered separately.

The network is a persistent organization graph over physical sites and routes. A strategic display projects this graph; it does not create a second economy. Site managers and Group of Interest planners issue the same validated orders used by the player, within their own authority and knowledge. Actor/controller ownership keeps direct player input attached to one avatar while orders flow to autonomous staff.

Network transfers progress through reservation, pickup, transit, arrival, and receiving. Ownership/location and accounting change at explicit transaction boundaries. A late or intercepted shipment cannot also remain available at its source or arrive a second time. Contained anomalies remain active under transport-specific procedures and required fidelity. Communication links and transport links can fail independently.

Local storage uses the same physical-item hierarchy. Different layout scales map to shared occupancy, and access is evaluated through ancestors and blockers. Stock aggregation is a projection: known on-hand, accessible, reserved and in-transit are distinct views over the same records, not additional quantities. Moving a loaded container locks against incompatible subtree transfers and preserves descendant identities, mass and anomaly behavior.

Console distribution creates persisted haul work; pickup and placement commit separately. A worker interrupted after pickup keeps or validly sets down the item. Destination selection filters hard constraints first, then considers storage priority, matching existing contents, packing, haul cost and stable tie-breaks. [The storage contract](05-storage-and-logistics.md) defines ordering, rehandling, inheritance and the bounded initial fixture set.

## 2. Time, event order, and determinism

Propose a 20 Hz authoritative baseline for ordinary local simulation; M0 must measure whether this is sufficient. Rendering interpolates independently. Critical actions use ordered sub-events at declared times within a tick, or subdivide the interval; an entire blind interval cannot disappear between samples.

At each time boundary:

1. Consume external commands in recorded order; establish the current topology and sensory conditions.
2. Resolve due sensory changes, including eyelids, lights, obstruction, attention, and observer eligibility.
3. Evaluate anomaly constraints and active rule policies.
4. Validate and resolve actions, movement, and effects using those constraints.
5. Commit atomic state changes; emit notifications; schedule future events.

Movement, relocation, equipment effects and perceived-context changes can alter observation during the interval. Resolve swept transitions or split at relevant boundaries, commit each effect, and re-enter sensory/dependency evaluation before further dependent actions. Complete the bounded same-time causal queue before advancing time, with published tie rules. A closed eyelid at time T provides no observation at T; an independent eligible observer still can. Module load order and presentation cannot decide the outcome or keep a stale observer valid.

Ordering uses simulation time, phase, declared handler order, and stable IDs. Random draws use separately persisted streams per system and entity/operation. Adding a cosmetic draw must not change a mission result. Use integer/fixed-point authoritative quantities where practical. Engine physics may assist presentation but cannot silently become a nondeterministic authority for containment.

The replay guarantee is scoped to the same simulation build, content hashes, settings, seed, initial state, and recorded external inputs. Cross-platform bit-identical physics is not a baseline promise. Save restoration must still preserve authoritative values across supported platforms.

When permitted by the difficulty/scenario ruleset, pause stops all domain clocks. UI can preview or queue permitted actions, but timed equipment use, work and movement do not complete. Opening a terminal/inventory does not implicitly pause. Acceleration processes more work or skips certified safe intervals without skipping critical events. Interruptions return to normal speed or optionally pause at a committed boundary according to the ruleset; fidelity limits can independently cap acceleration.

Time is monotonic within a branch. Domain-local clock mappings do not rewrite history. A later host-managed checkpoint capability can restore a compatible whole-world snapshot into a new branch with explicit retention/cost policy; stale requests/results from the abandoned branch are rejected. Unrestricted local rewinds and temporal paradoxes are not part of this contract. See [time and continuation](08-player-controls-time-and-continuation.md).

## 3. Simulation detail and unattended areas

Distance from the camera is not sufficient to decide fidelity. Dependencies include observers, hazardous entities, ongoing procedures, pursuit, utility outages, communication links, and scheduled transitions.

| Tier | Representation | Appropriate activity |
|---|---|---|
| Local | Individual positions, sensory checks, action timing | Player area, combat, observation-dependent containment |
| Operational | Named people/items, procedure graph, exact important deadlines | Team travel, staffed containment, field operations |
| Regional | Organization plans, named nodes, scheduled obligations and bounded stock changes | Suppliers, site-manager routines, Group of Interest planning and unvisited civilian life |

A module declares its minimum fidelity and what conditions allow abstraction. An observation-dependent operation can retain exact observer/deadline events without rendering the room. If no certified coarse resolver exists, run that domain at sufficient detail. Do not replace its rule with a generic success percentage.

Organization planners run at bounded scheduled decision points and react to significant evidence. Their orders reserve real resources. When opposing operations meet, promote the relevant domain and reconcile participants before resolving effects; strategic and local resolvers cannot each award an outcome for the same encounter. Persist parent plan IDs, action IDs, and idempotent results so reports, stock, casualties, and group knowledge share the same cause.

Promotion/demotion happens at committed boundaries. Preserve identities, health, inventory, orders, topology, rule effects, random streams, deadlines, and procedure progress. Promotion reconstructs presentation from state. Demotion stores sufficient state to resume without resampling completed events.

Resolve a delegated operation step by step: travel, arrival, assessment, setup, interaction, stabilization, extraction, and debrief. Each step has preconditions, action semantics, hazards, and failure transitions. Some uncertainty is seeded and stochastic, but lethal rules and resource accounting stay explicit.

Personal and unattended runs need semantic parity, not identical outcomes despite different player choices. Given the same relevant observations, actions, random draws, and timings, both must enforce the same constraints and commit the same consequences. A coarse resolver must document approximations and fail certification if they change a mandatory hazard rule.

An unsupported workload slows simulation or limits new concurrent operations with a visible reason. It must not silently suspend an anomaly or lose scheduled outcomes to preserve frame rate.

## 4. Spatial domains and exceptional topology

Represent world navigation as a graph of spatial domains and connections. Each domain owns geometry, logical coordinates, local rules, and generated chunks. A connection can join doors, stairs, floors, transport endpoints, or anomalous portals.

Ordinary coordinates are meaningful within a domain. Cross-domain paths must traverse explicit connections and apply transfer rules. Rendering can place an interior in a separate scene while retaining a validated ordinary envelope. An anomalous interior can be larger than its exterior because the connection explicitly changes domain.

“Endless” means generation can continue on demand within storage and runtime budgets. It does not mean infinite memory or infinitely many simulated actors. Use logical chunk coordinates with a large declared range, generate visited neighborhoods, and persist modified chunks and sparse events. Untouched chunks can be regenerated from their pinned generator version and seed.

Use local render origins to avoid floating-point drift. At a coordinate or storage limit, stop traversal safely or offer an explicit recovery path. Never wrap coordinates into an unrelated visited chunk. A player must not lose unique items because a distant chunk unloads.

Return anchors contain domain ID, place ID, local position, orientation, and policy. If occupied, seek the nearest valid position under the module's declared rule; if the destination is destroyed or unavailable, invoke a specified blocked-return/recovery state. Do not invent a favorable destination on load. Resolve transfers of actors and their carried items in one transaction.

### Physical state and perceived contexts

A domain is not automatically both an actor's physical world and its perceived environment. Maintain authoritative physical location/collision/ownership separately from a per-observer context, versioned target bindings and acquired evidence. Bind perceived actions to actual targets through scoped module services; commit consequences once, with causal IDs. Context-only proxies do not create duplicate physical inventory or NPCs.

Sensor queries identify observer, target, sense/media and the context policy. Distinguish direct sight, a focused live camera feed, a report and remembered location. Shared awareness can update knowledge without increasing an anomaly's eligible observer set. Camera qualification includes power, sight, feed availability and declared operator attention; each SCP decides which media count.

Observer-kind eligibility is independently module/scenario-configurable: human, AI or any eligible observing actor, with authored predicates for further distinctions. The same camera can provide evidence to an AIC without making that AIC a qualifying observer for an anomaly. Persist the effective profile/version and explicit scenario override; evaluate changes at sensory event boundaries. SCP-650's baseline requires humans, including a focused human using a qualified live feed. See [observer profiles](03-scp-module-contract.md#observer-eligibility-profiles).

SCP-650 uses this service for relocation and coverage. SCP-1499 can change physical location or perception so those observer predicates become false without pair-specific callbacks. [The context contract](10-world-perception-and-scp-1499.md) and [ADR-0003](adr/0003-physical-perceived-contexts.md) define the second demonstration and its bounds.

## 5. Procedural hierarchy

The hierarchy is world → region → settlement/site → district/lot → building → floor → room → fixture/container. Outdoor areas can substitute an area template for a building. Each node has stable identity and an interface to its parent.

Each template declares:

- Identity/version, parameters, deterministic seed derivation, and asset references.
- Required and optional children, exact counts or ranges, dimensions, and placement constraints.
- Boundary sockets: doors, stairs, roads, utility connectors, and outdoor access.
- Adjacency requirements, exclusions, privacy/access zones, capacities, and clearances.
- Furniture/activity anchors, occupancy support, loot rules, and allowed variations.
- Validation rules, retry budget, and an explicit failure/fallback policy.

Generate parent constraints first, reserve envelopes and sockets, assign children, solve connectivity, place fixtures, then validate. Child generation cannot choose a larger envelope after the parent is committed. On failure, retry a bounded number of alternatives or return a diagnostic identifying the failed node and constraint. A fallback must preserve mandatory counts and invariants or fail visibly.

Site precedence means constraints such as public reception before restricted access, storage near logistics, clean/dirty laboratory separation, security chokepoints, and sufficient barracks/restroom capacity. Encode hard requirements separately from weighted preferences. Contradictory hard requirements are errors, not low-quality layouts.

Example composition:

```text
small-town preset
  roads + service lots + 120 housing lots
  motel building
    office + parking + circulation
    one-bed rooms / two-bed rooms
      bedroom layout + bathroom.shared parameters
  house building
    living spaces + bedrooms
      bathroom.shared parameters
```

Resolve population after assigning dwelling capacity, jobs, shifts, and relationships. Vacancies and unemployment can exist deliberately. They must not be accidental consequences of spawning more residents than the town can house. Clothing and item variety are seeded independently from the layout.

## 6. Persistence and upgrades

Save snapshots plus a bounded journal of committed transactions and pending events. Full event sourcing of every frame is unnecessary. A save records world and module schema versions, content hashes, seeds and RNG state, visited topology, changed chunks, entity state, rule registrations, mission state, timers, reservations, transfer anchors, physical/perceived contexts, target bindings, evidence, ruleset and branch identity.

Continuation is host-managed. Successor control preserves the existing world's losses and uses the new actor's permissions. Whole-world checkpoint restoration starts a new branch, applies only declared retention/cost policy once, and invalidates abandoned-branch requests. Neither can duplicate inventory or count an old asynchronous result as a fresh event. M1 qualifies ordinary saves; M3 qualifies the bounded continuation proofs.

Write saves atomically with recovery to the last valid checkpoint. Save only committed transactions; requests during a transfer or transformation wait for its boundary. Pending completion events use idempotency keys so loading cannot repeat a reward or output creation.

Pin generator versions. Updated generation applies to new worlds unless a migration explicitly supports the saved world. A seed alone is not a save format.

Missing or incompatible required mods stop normal loading with an actionable compatibility report. Keep the original save intact. A recovery tool may export unaffected records or create a separate degraded copy, but cannot claim the result preserves the original campaign. Module removal with active entities/rules requires a supported migration.

## 7. NPC perception and decisions

Events such as gunfire create sensory evidence within a domain, filtered by propagation, obstruction, hearing, attention, and communications. NPCs update knowledge and stress; an assignment arbiter evaluates incapacity/anomaly constraints, acute role-aware emergency responses, authorized direct orders, scheduled employment, and discretionary tasks in that order. It owns the executable intent so the work and action-job handlers cannot issue conflicting commands. Reassessment occurs on important changes and scheduled intervals, not every frame for every civilian.

The work handler tracks role-specific duties and continuity across shifts, breaks, blockers and handoffs. The action-job handler receives only released availability; optional action toggles never disable mandatory role substeps. Both call shared primitives and resource/post reservations. Field deployment or a direct reassignment removes staff from their original duty coverage explicitly. An offscreen guard must meet the same relief/readiness conditions as a rendered one. [Employment and orders](07-employment-and-orders.md) defines the policy and persistence requirements.

A useful initial persona vector has eight bounded values: caution, sociability, patience, diligence, assertiveness, curiosity, empathy, and stress tolerance. Skills and fatigue modify performance separately. A trained cautious guard can hold a position that an untrained civilian abandons.

Future LLM decisions are asynchronous requests with actor ID, permitted evidence, valid actions, state revision, and deadline. Revalidate on arrival; discard stale replies. Record accepted intents. Cap concurrent requests and token/context budgets. Text alone cannot create an item, override a locked door, or establish containment.

Presentation selects sprite state from committed actions and progress, and facing from authoritative orientation/attention. A green diagnostic direction bar and state/frame label expose those values. Locomotion phase can drive walking frames, but rendering rate or frame-count changes cannot change perception, movement or action completion. Persist semantic action progress; derive presentation frames from it and pinned visual metadata. See [the sprite contract](06-sprites-and-placeholders.md).

## 8. Diagnostics and quality targets

Provide seed replay, event traces, entity inspection, rule provenance, generation-failure explanations, mission-step inspection, and save-difference summaries. These are required development tools because procedural and mod failures are otherwise difficult to reproduce.

Provisional measurement envelope: one site with two floors, one small settlement, 200 named NPCs, 24 locally active actors, four concurrent operations, and five active anomaly instances. The playable slice has lower content counts. These are benchmark targets, not final hardware claims.

M0 records a named reference machine and build configuration, then measures 10,000 representative authoritative steps: target p95 simulation work ≤10 ms per 50 ms step, bounded worst-case events, and 60 FPS presentation at the selected resolution. A representative snapshot should save/load within five seconds. Set memory and chunk-streaming budgets after measurement; do not sign a performance commitment with unspecified hardware.
