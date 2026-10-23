# SCP module contract

Status: Proposed public API design, revision 0.6. A native C# SCP-650 observation/relocation proof now runs through an instance-bound host seam; SCP-1499 remains the second module target. Names below describe intended public semantics. The compiled proof is not a metered script runtime, published mod API, authoring application or compatibility guarantee. See [the implementation code map](../src/SiteSimulation/README.md) and [prototype guide](15-spatial-prototype.md).

## 1. Extensibility promise

A module combines descriptive data, reusable components, sandboxed behavior, procedures for staff, and acceptance scenarios. A form captures the declaration. Scripts express behavior that cannot be represented by fields alone.

The development commitment is support for published capability families, with fixtures proving representative cases. It cannot be “every SCP, fully simulated, with every other SCP.” A host-managed whole-world checkpoint restart is a staged capability; unrestricted time travel, new physical laws, fourth-wall interaction or contradictory effects may require another engine extension and API version.

For each SCP, distinguish narrative facts, required gameplay behavior, chosen adaptation, unknown behavior, and deliberately unsupported interactions. “Faithful” means passing that module's agreed behavioral scenarios. It does not promise a literal implementation of every interpretation of the fiction.

Content tiers:

| Tier | Author supplies | Engine promise |
|---|---|---|
| Definition | Form/data, assets, existing components | Validated composition within existing capabilities |
| Script | State machine, event handlers, procedures | Sandboxed execution through declared capabilities |
| Engine extension | New capability and technical implementation | Separate versioned engine work with its own acceptance gate |

A script cannot acquire an engine extension by editing an arbitrary global variable. The host validates every state-changing request.

## 2. Standard package

```text
module/
  manifest.json       identity, versions, dependencies, capabilities, credits
  definition.json     descriptions, components, spawn and containment definitions
  behavior.*          sandboxed behavior, language chosen during M0
  generation.json     optional environmental templates and deterministic population rules
  perception.json     optional perceived-context and target-binding declarations
  procedures.json     investigation, testing, containment and withdrawal steps
  state-schema.json   serializable module-owned persistent state
  assets/             declared presentation resources
  tests/              deterministic behavioral fixtures and expected outcomes
  migrations/         explicit save-state migrations where supported
```

Use namespaced immutable IDs, semantic package/API versions, pinned dependency hashes in saves, dependency-cycle detection, and explicit incompatibilities. The loader checks schemas, references, capability grants, entry points, state schema, and assets before creating an instance. Cosmetic-only packages declare that they cannot change simulation state.

Definitions/assets may include entity and equipment types, pose/state sprites, generated environments, independent NPC behavior graphs, audio/atmosphere, world/perception effects and staff procedures. Optional package sections use the same versioned reference validation as other data. Loading a module does not load an independently authoritative game subsystem.

The authoring form in [the template](../templates/scp-module-form.md) is the human-readable source for this declaration. A later editor may generate package data from it. The initial milestone requires a text workflow and validator, not a complete visual scripting IDE.

## 3. Required declaration

| Section | Required information |
|---|---|
| Identity | Namespace/ID, title, package version, API compatibility, dependencies |
| Sources | Author, article URL, adaptation notes, license and asset provenance |
| Classification | In-world containment class plus independent gameplay hazard tags |
| Existence | Unique/multiple instances, entity/object/area/distributed phenomenon, spawning rules |
| State | Named states, fields, invariants, transitions, timers, migration version |
| Perception | Observer-kind eligibility profile, module default/scenario overrides, senses, media, lighting/occlusion/attention policy |
| Context mappings | Physical/perceived relation, target bindings, sensory/identity changes and cross-context consequences |
| Effects | Trigger, target, range/domain, duration, stacking, reversible/irreversible changes |
| Space/time | Domain creation, connections, world-calendar versus local-progress timer bases, zone-wait behavior, anchors, persistence |
| Interaction | Allowed actions, costs, guards, results, rejection reasons |
| Staff procedures | Discover, investigate, contain/mitigate, transport if possible, maintain, abort |
| Organization interactions | Who can learn about, own, use, trade, transport or target the anomaly; knowledge and access prerequisites |
| Fidelity | Minimum simulation tier, safe aggregation conditions, coarse-resolver tests |
| Presentation | Required status cues, animation/audio, terminal reports, accessibility equivalents |
| Staff duties | Role qualifications, work-handler tasks, coverage/readiness and authoritative-order/override interactions |
| Recovery | Script fault policy, incompatible-save policy, interrupted-action outcomes |
| Continuation | Optional checkpoint/restart grants, activation/cost and retention policy, branch compatibility |
| Acceptance | Direct, unattended, transfer, conflict, persistence, and budget scenarios |

Containment class is descriptive metadata. It must not automatically set combat strength, aggression, or a generic success chance.

### Observer eligibility profiles

Confirmed direction: observer eligibility belongs to the SCP module or scenario, not a global assumption that human and artificial observation are equivalent. Supply named profiles for **human observers**, **AI observers** and **any eligible observer**. Modules select a default and may define more specific actor-kind/trait predicates where needed. Scenario configuration can explicitly select an alternate compatible profile; record it as an adaptation variant when it differs from the module's source-aligned default.

Observer kind and observation medium are separate dimensions. Each profile still declares eligible senses/media, attention, live-feed requirements, lighting, occlusion and other applicable conditions. “Any eligible observer” means an actual observing actor satisfying those conditions; it does not make an unattended camera, recording, database entry or overhead viewport an observer. An AI actor's identity/capabilities determine eligibility, independently of whether deterministic logic or an optional language model supplies its decisions. Unknown actor kinds need an explicit eligibility rule rather than being silently treated as human or AI.

Persist the effective profile ID/version and scenario selection with module state. Resolve eligibility through shared sensory services and expose rejection reasons in diagnostics. An authorized profile change, host/feed outage or attention transition invalidates affected observer sets at the simulation event boundary. Test human-only, AI-only and any-eligible profiles against the same observation fixture without changing kernel source. SCP-650 defaults to human observers, including its qualified emergency live-camera chain; AI-only eligibility is not established by its source article.

## 4. Capability families

Capabilities are versioned host services with queries and validated commands. A grant is scoped to an instance, domain, actor set, or campaign as declared.

| Family | Examples | First proof |
|---|---|---|
| Entity/condition | Spawn, damage, status, ownership, inventories | Basic hazard fixture |
| Storage/access | Container layouts, placement, ancestor access, stack blockers, validated pickup/place | Cabinet/pallet fixtures |
| Senses/observation | Observer eligibility, direct/live-feed sight, attention, occlusion, blink events | SCP-650 |
| Spatial domains | Create instance, generate chunk, connect/disconnect, transfer | Domain fixtures |
| Perceived contexts | Per-observer environment/identity, target mappings, mapped physical effects | SCP-1499 profiles |
| Environment | Lighting, sound propagation, temperature modifiers, utilities | Room hazard |
| Transformations | Reserve inputs, choose a recipe, replace atomically, retain provenance | Refinement fixture |
| Scoped rules | Movement, damage, interaction, perception, death policy | Conflict fixture |
| Knowledge/communication | Actor evidence, messages, dossier discoveries, media exposure | Investigation fixture |
| Operations | Procedure graphs, preconditions, staff roles, abort transitions | Delegated containment |
| Scheduling | Persisted timers, domain-local rates, event subscriptions | Save/resume fixture |
| Continuation/checkpoint | Request compatible whole-world branch restoration with validated retention/cost | M3 synthetic restart fixture |

Some families are staged after the first playable slice. A manifest cannot declare a capability as available before its prototype gate passes. Cognitohazards, distributed identities, possession, resurrection, and spatially recursive effects are extension candidates, not free consequences of having an event bus.

Grants can authorize ordinary-world changes outside the module's generated environment: entity relocation, physical conditions, inventories, NPC behavior or observer contexts in explicitly named scopes. A module is not limited to mutating only its own private map. The host still owns identity, location, occupancy, authority, transactions and persistence, and rejects undeclared scopes/capabilities. Indirect effects on another SCP flow through those shared services.

## 5. Runtime and lifecycle

Use a restricted, embeddable script runtime with metering and serializable state. Lua or WebAssembly are candidates to evaluate during M0; no language is selected here. Ordinary native DLL plugins are outside the public gameplay-mod contract.

The host provides deterministic RNG streams, simulation time, read-only state views, capability handles, transaction builders, and scheduling. It does not expose the filesystem, network, process creation, wall clock, arbitrary engine objects, reflection, or unseeded randomness. Runtime selection must demonstrate these restrictions; naming a language is insufficient.

Tactical input waiting holds all engaged-area simulation, including local anomaly timers, while the world calendar and other areas continue. Modules declare timer ownership, clock bases and cross-zone scopes. Preserve remaining local countdowns/progress through the hold; do not expire them merely because the calendar advanced. World-scoped effects continue, with local application gated by the host's qualified boundary policy. Preserve critical queued observation/effect transitions. An anomaly is not forced into an ordinary actor turn merely because combat UI is active. See [the time decision](adr/0006-real-time-exploration-and-tactical-turns.md).

Lifecycle callbacks: validate definition, create instance, handle subscribed event, respond to interaction, advance procedure, save state, restore state, migrate state, and dispose instance. Restoration reattaches persisted registrations idempotently; it does not replay creation rewards or re-roll initialized state.

Scripts store data, IDs, finite numbers, bounded strings, and declared collections. Engine object references, live coroutine stacks, and closures are not save data. Timers and continuation states are explicit persisted records. Schema checks reject invalid values, oversized collections, and unresolved required references.

Meter instruction/fuel use, memory, created entities, queued events, recursion, and spatial generation. Exact limits are set from M0 measurements and published by capability version. A quota breach aborts the current uncommitted transaction, records a reproducible fault, and puts the affected instance/domain into the declared recovery state. It must not quietly neutralize an SCP or invent mission success. Offer reload or a separate recovery save if safe continuation is impossible.

This contract constrains supported packages. It is not a security certification of an untested host/runtime integration.

## 6. Commands, queries, and events

Queries read committed state and declare their information scope. Commands carry requester, branch/context identity, target or perceived-target binding, expected state revision, parameters, and an idempotency key. The host resolves/validates authorized mappings and ordinary preconditions before committing. Accepted commands return a committed result or scheduled action ID; rejected commands return a stable reason code. Force-target input is not elevated authority.

Events carry ID, time, cause, domain, subject IDs, and versioned payload. Handlers see a stable snapshot and propose commands. They cannot recursively mutate another handler's in-progress state. Resulting events are queued in declared phases with bounded causality depth. Same-time cycles produce a diagnostic and a defined fault, rather than an infinite loop.

Illustrative pseudocode, not executable API:

```text
on ObservationSetChanged(event):
    observers = senses.eligible_observers(instance, declared_policy)
    rules.set_constraint(instance, "self_motion", allowed = observers.is_empty)

on ProcedureStepRequested(event):
    validate roles, equipment, knowledge, location and current hazard state
    submit ordinary actions through the host
    schedule the next checkpoint or take the procedure's failure branch
```

A procedure describes how agents attempt to act; it cannot assert that the anomaly is contained simply because its progress bar reached 100%.

Procedure execution participates in the employment/order arbiter. Employment-specific maintenance, observation and research duties remain primary work regardless of optional action preferences. Modules declare required roles, qualifications, team acknowledgements, coverage and interruption predicates. Anomalous fear or compulsion can impose declared overrides; scripts cannot silently release a guard from duty or ignore an existing order. Their observable effect and loss of coverage are committed and reported.

Foundation and Group of Interest procedures can differ in intent, doctrine, resources, and knowledge, while sharing the same underlying anomaly rules. A hostile organization cannot bypass a hazard because it uses another planner. Transport between sites is an operation with a source release procedure, transit conditions, and destination acceptance predicate. Organization-specific behavior is declared data or capability-based scripting; it must not hardcode the player as the only meaningful actor.

Container-related modules declare exterior footprint/mount compatibility separately from interior layout, closure/access conditions, mass behavior, and content-effect propagation. Changing capacity or transforming a loaded box requires an explicit final placement policy and a transaction covering affected descendants. Ordinary packing, nesting, offscreen storage and console hauling do not suspend anomaly rules. Use [the storage contract](05-storage-and-logistics.md) rather than implementing a separate inventory inside each SCP script.

## 7. Rule composition and conflicts

Effects register typed policies with owner, target scope, applicability conditions, lifetime, combination rule, and provenance. Global scope requires an explicitly granted capability. No effect can override host integrity rules such as unique identity, valid storage ownership, and atomic transactions.

Policies combine according to the service contract:

- Numeric additions/multipliers apply in a published order with declared bounds.
- Ordinary action constraints combine by intersection: every applicable constraint must permit the action.
- Replacements, redirects, or exceptions require an explicit composition strategy; priority alone cannot resolve incompatible semantics.
- Multiple exclusive replacements for the same target require a declared compatibility adapter or fail validation when known in advance.
- Runtime-discovered incompatibilities preserve the last committed state, pause affected resolution, and report the competing policies. They do not choose a winner by package load order.

For example, an unobserved entity may be permitted to move while a separate binding effect prohibits movement. Both constraints yield no movement. A phenomenon intended to bypass that binding must declare an explicit supported override relationship. A teleport redirect conflicting with another world-transfer redirect requires an authored composition policy.

If two modules compose through published rules, they need no pair-specific code. Cross-effects outside those rules require compatibility tests. Publish supported pairs and known exclusions; never advertise universal mod compatibility.

## 8. Architecture tests from the requested SCPs

The source facts below come from the linked Foundation articles. The proposed mechanics and edge policies are game-design choices. Full module adaptation requires its own approved form.

### SCP-650: first playable facility module

Qualify shared observation, silent relocation, discrete poses, staff reactions and real recontainment procedures in M1. Procedural staffing compliance and current eligible sight are separate predicates. Both observed external transport and controlled relocation are accepted game approaches; their details remain authored procedures. The module reacts to ordinary sensory/location events rather than mission-owned teleport calls. See [the complete first-playable specification](09-first-playable-scp-650.md), including its article source and adaptation notes.

### SCP-3008: expandable retail domain

The article describes an interior far larger than the building, trapped inhabitants, and hostile entities; infinity is speculative. [SCP-3008](https://scp-wiki.wikidot.com/scp-3008).

Prove demand-generated retail chunks, persistent camps/items, explicit entry/exit conditions, local environmental cycles, and ongoing activity while the player is elsewhere. Determine entrance anchoring, lost exits, radio behavior, and mission abort rules in the module specification. A team's objective can be perimeter control or rescue; it cannot assume the whole interior is cleared.

Acceptance fixture: modify a distant chunk, leave, save/reload, and return to identical authoritative modifications. A delegated rescue uses the same entry/exit predicates as personal traversal. Resource limits cannot delete occupants.

### SCP-087: descending domain

The article describes a staircase of unknown endpoint, restricted visibility, unusual light behavior, and a distant sound that descent does not bring closer. [SCP-087](https://scp-wiki.wikidot.com/scp-087).

Prove persistent logical depth independent of the host building, streamed landings, module-specific light and sound policies, return navigation, and psychological/encounter events. Do not make a confirmed bottom a universal engine rule. Exploratory steps can consume time and supplies without generating every deeper landing.

Acceptance fixture: traverse and unload many landings, reverse direction, and recover the correct entry without depth reset. Save/resume preserves depth, equipment, encounter state, and scheduled events.

### SCP-173: observer-dependent movement

The article specifies immobility under direct sight and coordinated blinking during personnel entry. [SCP-173](https://scp-wiki.wikidot.com/scp-173).

Prove an observer set derived from actual actors: eligible eyes open, attention/facing, range, obstruction, and the adaptation's lighting policy. NPC eyes count. Management cameras and the player's overhead viewport do not count under the initial adaptation. Mirrors, recorded footage, nonhuman senses, darkness, and remote video are explicitly excluded until authored.

These are the SCP-173 fixture's media rules, not a global camera prohibition. SCP-650 has its own emergency live-feed qualification policy over the same service.

Acceptance fixtures: one observer blinks while another watches; all observers blink; a door blocks the last observer; an observer loses consciousness; a guard resumes sight mid-action; the terminal opens while the avatar watches; a remote team's shift changes during accelerated time. Resolve the constraint at the actual event boundary and validate the whole movement path. No moving through walls or killing during an interval when movement is forbidden.

### SCP-914: atomic transformation

The article describes an intake/output device with multiple settings that refines placed objects. [SCP-914](https://scp-wiki.wikidot.com/scp-914).

Prove transactionally reserved inputs, setting-dependent rules, persisted processing, deterministic recipe selection, and output creation exactly once. Recipe matching order: exact item rule, tag/category rule, then an explicit fallback. The initial fallback rejects before consuming inputs. Arbitrary objects and living subjects require authored behavior rather than an implied universal recipe generator.

Acceptance fixtures: process an equipped container, submit overlapping jobs, fill the output area, interrupt a job, and save/reload before completion. Each case must preserve ownership and the declared result. Input conservation may be intentionally broken by a recipe; identity and transaction integrity may not.

### SCP-1499: second module, physical and perceived contexts

The article describes a different experienced environment, return and communication, with later evidence complicating a simple separate-world interpretation. [SCP-1499](https://scp-wiki.wikidot.com/scp-1499).

Prove module-owned equipment triggers, procedural terrain/structures/populations, timed physical transfer, per-observer contexts, return anchors and cross-context effects/communication. Define carried-item and injury propagation, loss/death, mask ownership, blocked return and re-entry. One profile qualifies real transfer; another qualifies explicit perceived-target mapping to an ordinary-world consequence. Both are labeled test adaptations rather than competing claims about a settled reality.

Acceptance fixtures: equip, generate/visit, affect an entity/environment, save/reload, remove and re-enter with persistent results; perform a mapped action with one physical consequence; alter a SCP-650 observer through these shared services. No duplicate wearer/mask/item, stale observer or double-resolved action may exist. [The full second-module/context contract](10-world-perception-and-scp-1499.md) specifies the two bounded profiles and source interpretation.

## 9. Authoring and validation tools

The first toolkit needs package validation, a headless scenario runner, seed/event tracing, observation visualization, procedure debugging, and a state inspector. These are smaller and more useful initially than an all-purpose visual editor.

Visual definitions declare illustration/animation states, directions, frame counts and semantic progress bindings. The development generator supplies conspicuous category/state/facing/frame placeholders while art is absent. Production sprites replace presentation bindings; gameplay callbacks cannot depend on an artist preserving a particular frame index. The [sprite contract](06-sprites-and-placeholders.md) governs this separation.

A package is certified against an API version only after it passes loader/state validation, direct and unattended scenarios, save/load at critical states, budget limits, and its declared interactions. Certification covers the declared scenario set, not every possible campaign state.

## 10. Source and release metadata

Capture article title, credited author, source URL, adaptation changes, and separate asset provenance at authoring time. The Foundation's guide describes attribution and share-alike requirements for derivative works and has a game-specific section. Its SCP-173 section treats the former sculpture image separately and recommends an original or suitable redesign. Release licensing and asset choices must be resolved before distribution. [Official licensing guide](https://scp-wiki.wikidot.com/licensing-guide).

This specification does not assign a software license or certify legal enforceability. In particular, modular packaging alone does not establish a licensing exemption for the engine or game.
