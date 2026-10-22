# SCP module authoring form

Copy this form for each module. Replace all placeholders. A completed form describes required behavior; it does not certify an implementation.

## Identity and adaptation

- Module namespace/ID: `<author.module>`
- Display title / SCP designation: `<title>`
- Package version / required host API: `<version / range>`
- Dependencies / incompatibilities: `<IDs and versions, or none>`
- Article title / credited author / source URL: `<source>`
- Content license / asset provenance / adaptation changes: `<details>`
- In-world containment class / independent hazard tags: `<values>`
- Required fiction facts: `<observable facts the adaptation must preserve>`
- Deliberate adaptations, unknowns, exclusions: `<explicit list>`
- Unique, replicated, distributed, place, object, or actor: `<existence model>`

## Behavior and state

| State | Entry condition | Permitted actions | Exit/trigger | Persisted fields |
|---|---|---|---|---|
| `<state>` | `<condition>` | `<actions>` | `<transition>` | `<fields>` |

- Spawn/location restrictions: `<rules>`
- Invariants: `<conditions that must always hold>`
- Observer/sense/media policy: `<eyes, attention, light, occlusion, cameras, etc.>`
- Observer-kind profile/default and version: `<human, AI, any eligible observer, or explicit authored predicate; unknown-kind handling>`
- Scenario observer-profile selection/overrides: `<compatible variants, source/adaptation notes, save identity and validation>`
- Eligibility versus information: `<who can detect/report the anomaly without counting as a suppressing observer>`
- Event subscriptions and timer resolution: `<events and deadlines>`
- Persistent state schema/version and seeded randomness: `<data>`
- Creation/restore/disposal and migration: `<behavior>`

## Effects and rule changes

| Trigger | Target/scope | Effect/service | Duration | Stacking/composition | Removal |
|---|---|---|---|---|---|
| `<trigger>` | `<target>` | `<capability>` | `<time>` | `<rule>` | `<condition>` |

- Required capability grants: `<versioned capabilities and scopes>`
- Explicit overrides and compatible/conflicting modules: `<relationships>`
- Unknown item/actor/cross-effect fallback: `<reject, ordinary behavior, or authored result>`

## Spatial and inventory behavior

- Domains, entry/exit predicates, topology, generator version: `<details or not applicable>`
- Local clock and communication rules: `<details>`
- Physical/perceived contexts, affected observers, apparent identities/geometry and sensory policy: `<details or not applicable>`
- Perceived target/position to authoritative action mapping, stale-binding and cross-context consequence rules: `<details or not applicable>`
- Actor/carried-item transfer and ownership: `<transaction semantics>`
- Return anchor, occupied/destroyed origin, recovery: `<policy>`
- Inputs/outputs, item recipe precedence, output overflow: `<rules or not applicable>`
- Exterior footprint/mount compatibility, interior layout, ancestor access/closures, and mass: `<rules or ordinary defaults>`
- Effects through nested containers and behavior during carrying/hauling: `<rules>`
- Capacity/size changes, blocked placements, descendant reservations and spill/recovery: `<rules or not applicable>`
- Injury, unconsciousness, death, dropped equipment, interrupted actions: `<outcomes>`

## Staff operations

| Procedure | Roles/knowledge | Equipment and setup | Steps/guards | Success predicate | Abort/failure |
|---|---|---|---|---|---|
| Discover/investigate | `<requirements>` | `<requirements>` | `<steps>` | `<evidence>` | `<branch>` |
| Contain/mitigate | `<requirements>` | `<requirements>` | `<steps>` | `<world condition>` | `<branch>` |
| Transport or control access | `<requirements>` | `<requirements>` | `<steps>` | `<world condition>` | `<branch>` |
| Maintain/recover | `<requirements>` | `<requirements>` | `<steps>` | `<world condition>` | `<branch>` |

- What can never be solved by “capture the entity”: `<limits>`
- Minimum fidelity / safe unattended approximation: `<conditions and proof>`
- Personal participation or handoff constraints: `<rules>`
- Primary employment roles/qualifications, shift coverage, readiness/relief acknowledgements: `<requirements>`
- Authorized order interruption, panic/compulsion and other explicit override outcomes: `<rules>`
- Foundation/Group of Interest knowledge, access, objectives, and alternative procedures: `<rules>`
- Inter-site release, transport containment, and destination receiving conditions: `<rules>`
- Reports: observed facts, uncertainty, delay, communication loss: `<policy>`

## Presentation and recovery

- Required field cues and terminal information: `<what the actor can know>`
- Visual state keys, illustrations, facing set, frame totals, anchors and semantic progress bindings: `<metadata>`
- Generated category/name/state/frame/facing placeholders and production-art substitution: `<bindings>`
- Readable equivalent for essential audio/color cues: `<UI behavior>`
- Runtime quotas and fault state: `<published defaults and specific needs>`
- Missing module, invalid state, or failed migration: `<recovery policy>`
- Optional continuation/checkpoint grants, activation/cost, retained state and branch compatibility: `<policy or not applicable>`

## Acceptance scenarios

For every scenario, specify starting state, seed/inputs, action/event times, expected state, and evidence. Include:

1. Ordinary interaction and rejected interaction.
2. Direct participation and equivalent unattended execution.
3. Trigger boundary, simultaneous effects, and loss of a required condition.
4. Save/load in every critical state and just before completion.
5. Injury/death, lost equipment, blocked routes, unavailable supplies, and abort.
6. Domain unload/reload and missing/occupied return destination, if applicable.
7. Compatible rules and at least one incompatible or unsupported interaction.
8. Budget exhaustion and script fault with no partial transaction.
9. Location/perception changes affecting another module through shared services, if applicable.
10. Distinct pose keys versus animation frames, and branch/context restoration without stale actions, if applicable.

Certification status: `<unimplemented / prototype / verified against host version and fixture set>`.
