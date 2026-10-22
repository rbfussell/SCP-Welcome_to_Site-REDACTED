# Physical world, perceived contexts, and the SCP-1499 demonstration

Status: Revision 0.5. SCP-1499 is the second module demonstration in the same facility after SCP-650. Procedural environments, independent entities, physical transfers, altered perception and ordinary-world consequences are confirmed capability requirements. Exact mapping rules and demonstration bounds below are proposed adaptations.

## 1. Source interpretation

The article describes disappearance, an unusual environment and return, while its exploration log/addendum connects perceived events with an incident elsewhere. That supports preserving uncertainty rather than treating every effect as a separate map with isolated consequences. It does not settle a universal explanation of the phenomenon. Source: [SCP-1499 by Trasknari](https://scp-wiki.wikidot.com/scp-1499), CC BY-SA 3.0.

Provide explicit physical and perceptual mechanics without claiming a final ontology. The test environment is an adaptation selected for engineering coverage; the module can later refine its mapping and narrative without forcing a new engine for every interpretation.

The user supplied [creature, black-tower and mask style references](../assets/references/scp-1499-style/README.md). Their detail/material treatment guides the visual direction. Production facings, scale, architectural layers and world/inventory/worn variants remain to be specified; the images do not establish collision, traversal or sensory rules by themselves.

## 2. Distinct records

| Record | Meaning |
|---|---|
| Physical location | The authoritative domain, transform, collision and containment location of an actor/item |
| Perceived context | What an affected observer experiences: domain/view, presentation, senses and apparent identities |
| Target binding | A versioned mapping from a perceived entity/position/action to an authoritative target or explicit illusion/no-target result |
| Knowledge/evidence | What a character or organization believes, with source, time and uncertainty |
| Cross-context link | A declared relation for communication, effects, transfers or mapped consequences |

A physical transfer changes physical location atomically. An altered perception can change an actor's sensory/rendered context while its physical location remains governed by an explicit rule. A hybrid can combine transfer with mapped consequences. A perceived proxy is not automatically a second physical NPC or another copy of an item.

The engine remains authoritative in all cases. View overrides are not permission to mutate arbitrary state, and physics is not derived from whatever texture the renderer displays.

## 3. Proposed capability interface

A module can request creation/selection of a generated domain, a physical transfer, a per-observer perception context, sensory/identity changes, target mappings, behavior policy changes and effects across declared contexts. Every request uses scoped capabilities, validated commands and transaction/event semantics.

Perception effects declare affected observers, input senses, context selection, apparent geometry/entities, knowledge consequences, authority to map targets, and composition/removal rules. Multiple incompatible exclusive context overrides use the ordinary conflict contract; no hidden winner by mod load order.

An action initiated in a perceived context carries actor ID, current context/revision, selected equipment source, perceived target/position and semantic action. Resolve the mapping at commitment, validate the authoritative action, and commit resulting effects once. The module must declare whether movement/collision, damage, sound, item pickup and observation refer to physical space, the generated space or an explicit mapping.

If the mapping becomes stale, ambiguous or invalid, follow its published revalidation/rejection policy. Do not affect both an apparent entity and its real counterpart as though two attacks occurred. Explain failure through the actor's allowed information rather than revealing concealed truth. Developer traces retain the actual targets and cause.

Cross-context consequences retain causal IDs, originating equipment/ammunition, actor authority and current branch. A consequence can intentionally violate an ordinary spatial assumption through a granted effect; it cannot violate unique identity, valid ownership or transaction integrity.

## 4. The second module demonstration

Load the SCP-1499 package alongside SCP-650 in the same saved facility. The package supplies mask/item interactions, generated terrain/structure templates, towers, entity definitions, perception/behavior rules, sprites/placeholders, atmosphere/audio, procedures and persistent state. The mission wrapper requests a test; it does not implement the mask's effect.

The mask occupies the ordinary equipment slot and is subject to reach, fit, clearance, access and authorization. On qualifying equipment activation, schedule its declared effect using simulation time. M1 does not need an active mask; M2 makes it available as the second functioning module.

Propose two deterministic test profiles over one coherent engine interface:

1. **Physical-domain profile:** equipping transfers the actor and declared carried equipment to a generated domain with a return anchor. Removing returns through the declared policy. Its test intentionally qualifies physical transfer; it is not a declaration that this is the only SCP-1499 interpretation.
2. **Perception/consequence profile:** activation assigns a generated perceived context and explicit mappings to a small, ordinary-world test area/entities. At least one equipment action resolves a persistent physical consequence in that area. On removal, the observer's view returns according to the profile while consequences remain. This is an authored engineering adaptation and must be labeled as such in diagnostics and content notes.

Use one profile per test/scenario configuration, not two simultaneous contradictory explanations. Perception changes can include actual transfer to a mapped physical context, or remaining at a declared ordinary-world position; choose and record the profile's physical mapping. A purely cosmetic reskin with no authoritative consequence does not pass the second profile.

Initial demonstration bound: one generated domain/context, one wearer, a small connected set of terrain chunks with at least two structure variants/towers, and a bounded population of SCP-1499-1 instances. Propose a 3 × 3 starting chunk neighborhood and at most eight local entities, with on-demand additional chunks. These are test limits, not restrictions on future modules.

Entities use their own senses, navigation, state and seeded behavior. Include at least neutral/routine behavior, investigation/alert, and a response to a relevant proximity or player action. They are not scenery and cannot require the mission to puppeteer their responses. The exact hostility and encounter policy is module content, not a universal “monster attacks player” rule.

## 5. Persistence, return and failures

Pin generation definitions and seed streams; persist visited modifications, entities, positions, inventories and behavior continuations. Define re-entry anchors explicitly: a shared module-domain location can differ from per-wearer last position. Revisit without rerolling or discarding changes.

Declare what crosses contexts: clothing, held items, nested inventory, radio, injuries, acquired objects, sounds and knowledge. Communication links need not follow travel links. A remote researcher can have a radio conversation without becoming physically present or an eligible visual observer in the mask user's context.

Define removal, unconsciousness, death, equipment loss, destroyed/blocked return anchors, unload and interrupted transfer outcomes. The default bounded proof permits one wearer and uses the existing anchor-recovery service. A pending transfer/context change commits or remains pending at a save boundary; no second actor, mask or cargo copy is created.

Persist active context, target bindings/revisions, links, effects, domain state, random streams, return policy and causal records. Save/load must restore what other people physically see as well as what the wearer perceives. Module removal requires a migration/recovery policy and cannot strand the only authoritative actor in an orphan view.

## 6. Interaction with SCP-650

Place the wearer among SCP-650's observers in a controlled fixture. If activation transfers the wearer or changes their effective ability/attention to observe, the shared sensory service updates SCP-650's eligible observer set. Radio reports or a remembered statue position do not maintain visual observation.

If another eligible direct or remote observer remains, SCP-650 stays stable. If none remains, its normal module can relocate. Do not implement a `1499_affects_650` special-case callback. Restoration of sight or return from the mask recomputes observation and knowledge through the same services.

At the same simulation boundary, apply committed location/perception changes before dependent observation/anomaly evaluation. Render rate, view selection and loading a saved context cannot leave a stale qualified observer holding SCP-650 still.

Requirements CTX-01 through CTX-05 are in [scope and acceptance](04-scope-and-acceptance.md). These profiles qualify bounded capabilities rather than universal reality editing or all cross-module interactions.
