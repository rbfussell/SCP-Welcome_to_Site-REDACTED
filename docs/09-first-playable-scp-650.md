# First playable: SCP-650 containment breach

Status: Revision 0.6. A facility-only SCP-650 incident, direct and delegated responses, persistent consequences and approximately six discrete pose sprites are confirmed direction. The user accepts both observed transport and controlled relocation as valid recontainment approaches, and module/scenario selection of observer eligibility. The baseline uses human observation; AI-only observation is not established by the source. Specific fixture counts, tuning and procedure details remain proposals. This replaces the earlier generic off-site observation-hazard mission.

## 1. Source and adaptation boundary

The article specifies a lockable, lit room, continuous observation by at least three personnel, emergency remote observation with unbroken concentration, silent relocation and no demonstrated active hostility. It also describes varied postures and relocation across a site. These facts guide the adaptation; the algorithms below are proposed game rules. Source: [SCP-650 by Dr Talson](https://scp-wiki.wikidot.com/scp-650), CC BY-SA 3.0.

The article describes personnel/observers and explicitly allows emergency security-camera observation. It does not mention AICs, test AI-only observation or explicitly restrict all possible observers to humans. This game's baseline therefore selects human observers as a conservative adaptation; that is not proof that artificial observation is canonically ineffective. A scenario choosing AI or any-eligible observation must identify that alternate profile explicitly.

The user confirms interpreting personnel here as humans: the three-person procedure and sustained concentration are meaningful staffing constraints, whereas indefinite AI-only observation would change that design. Preserve human staffing/attention in the baseline rather than treating the resident AIC as a substitute. The same source text was checked in the [local JSON archive](../source_material/README.md).

Initial scope confines its effects to the starting facility's declared domains/floors. This is a module/scenario bound, not an engine-wide rule that SCPs cannot escape into towns. The later world architecture supports other declared scopes.

The anomaly's discovery/startling behavior does not itself start a hostile combat encounter. If its area enters tactical input waiting through an actual engagement, all local simulation holds, including observation/attention progression and module countdowns. Resume preserved progress through normal event boundaries; do not reset the anomaly or catch up missed local relocations. External feed/power/observer transitions follow the qualified incoming-event policy. [The time contract](08-player-controls-time-and-continuation.md) applies without creating a special SCP-650 combat exemption.

## 2. Opening state and incident

Start as the newly appointed regional operations director in one functioning site. Staff are on real shifts, supplies are physically stored, cafeteria and maintenance duties run, and SCP-650 is already contained with valid coverage. The player can inspect the room, staff assignments, live information, equipment and terminal controls before the incident.

A proposed reproducible incident fixture schedules a real lighting/observation-system fault or an unsuccessful coverage handoff. It changes ordinary world state. The SCP module receives the resulting sensory/observation changes and decides whether relocation is allowed. The mission does not call `teleport_650` or set a breach flag regardless of conditions.

If competent player/staff action preserves qualifying observation, no relocation occurs. A test can deliberately remove the last observer to exercise a breach, while normal play can prevent it. Logs must identify which light, obstruction, attention change, handoff or feed loss actually broke observation.

The player can respond through direct participation, delegated security/maintenance orders, or a mixture. Resolution must remain possible without possession of another character or an artificial scene transition.

## 3. Module-owned state and visuals

Track instance ID, current physical location, pose key, observation policy/profile ID and version, scenario override, eligible observer set, last observation change, relocation/cooldown state, encounter ID, per-witness reaction history, explicit per-person complacency flags, containment procedure state and module RNG stream.

The user confirms a `complacency` flag selecting threat versus relaxed presentation/placement; its eventual metric is deliberately undecided. Keep it separate from having witnessed one encounter. Threat behavior favors an immediately-behind threatening posture; a complacent target permits quieter poses at a comfortable distance without rear-only placement. Relaxed does not mean harmless to observe casually or free to move while watched. Flag changes affect the next legal relocation, not an immediate pose change. These policies belong to the SCP-650 module, with generic host queries, validation and persistence. The current debug control is not an in-world ability to alter an SCP.

Declare approximately six initial pose keys. Possible stand-ins include the default raised pose, two threatening poses, crouched, seated and reclining variants; exact art/pose selection remains an authoring decision. Poses are discrete states, each initially a one-frame illustration. Do not run them as six successive walk frames. The module chooses a pose only through a valid state transition, not every render frame.

Use the object-category placeholder with instance name, pose key, frame/total and facing marker where relevant. Later artwork replaces these keys. Different pose bounds must be validated against collision and placement.

## 4. Observation and relocation

The shared sensory service supplies eligible direct observers and remote observers according to SCP-650's selected policy. The baseline profile is **human observers**. Proposed remote qualification requires a live powered camera, clear camera sight, working feed and a focused qualified human operator. An unattended camera, recorded clip, old report, friendly map marker or overhead player view is not sufficient. Remote observation is an emergency procedure, not an automatic substitute for staffing.

A resident AIC can watch available feeds, detect/report coverage problems and request authorized responses. Under this baseline, AIC-only monitoring does not suppress relocation or fill a human observation post. A live feed attended by both AIC and an eligible human qualifies because of the human. Losing that last human's attention removes coverage even if AIC monitoring continues. Alternate AI-only or any-eligible scenario profiles use the same sensory service and must remain visibly distinct from this default.

Observation that suppresses self-relocation is separate from procedure compliance. One eligible observer can currently stop relocation while the three-person safety procedure is understaffed. The terminal must show both facts. Failing the staffing procedure is not permission to relocate while valid sight still exists.

When eligible observation becomes empty and a relocation evaluation is due:

1. Identify eligible personnel within the allowed facility scope.
2. Produce bounded candidate positions near/behind them, favoring the module's startling-placement policy.
3. Reject positions observed by any qualifying observer, outside the allowed scope, intersecting occupied bodies/geometry, or invalid for the selected pose/footprint.
4. Select among remaining candidates with the module's persisted RNG and deterministic ordering.
5. Revalidate origin observation and destination validity, then atomically commit location, pose, encounter ID and dependent spatial/sensory updates.

The module may relocate across a closed ordinary door when its declared effect permits it; pathfinding is not the relocation mechanism. A locked room alone is therefore insufficient containment. No candidate means no relocation: remain in place and schedule a bounded retry. Prevent zero-time event loops and repeated rerolls on load.

While observed, suppress its own relocation and spontaneous pose changes. How externally imposed transport interacts with this rule must be explicitly selected in the recontainment procedure below. The generic movement service must distinguish self-motion from a carrier moving an entity if that adaptation is adopted.

## 5. Discovery and personnel reactions

A startling encounter occurs when a character newly perceives the relocated statue under the module's conditions. Evaluate surprise, familiarity, training, stress, injury and doctrine. Possible consequences include a brief interruption, calling security, panic, a dropped task, blocked movement or a failed duty handoff. Record one reaction per applicable encounter/witness policy rather than firing a startle every frame.

SCP-650 has no ordinary hunt, chase, melee or attack routine in this adaptation. Secondary injuries or losses can arise from ordinary world actions and reactions, with causal records; do not add an unexplained statue attack to make the breach dangerous.

Relocation is silent. An optional player-facing atmospheric sting is presentation feedback, not an in-world sound emitted by the statue. NPCs react to what they sense; audible witness shouts can independently propagate through the normal sound system.

## 6. Recontainment and persistent failure

Use ordinary observation, personnel assignments, doors, room validation, access, equipment and task services. A procedure requires real staff readiness, not a mission success percentage. Camera operators can provide emergency coverage when their full predicate remains valid.

Support two module-authored procedure approaches, as accepted by the user:

- **Observed external transport:** a qualified team maintains sight while using a suitable trolley/carrier and moving to the receiving room. Observation prohibits the statue's self-relocation; authorized external transport remains possible. This is an accepted game adaptation, not a claim established by the article. Exact crew/equipment requirements remain tunable.
- **Controlled relocation:** staff manage placement/observation opportunities to induce relocation into a valid receiving room, then establish coverage. Candidate selection remains the module's normal rule; the mission cannot force a convenient destination. The plan may fail or require repeated attempts.

Propose observed transport as the reliable first-playable baseline and a controlled-relocation fixture as an alternative strategy. Neither approach has been implemented. Both use the same module actions for player participation and autonomous staff. Controlled relocation may legitimately fail when a suitable destination is unavailable; it does not grant the mission a forced teleport.

An open carrier must preserve actual sight geometry; loading is not automatic containment. If observation is lost and relocation occurs during transport, update the statue's parent/location, carrier occupancy, reservations and dependent procedure state atomically. It cannot remain both on the trolley and at the new location.

Proposed stable containment predicate: valid receiving room and lighting, required staffed observation/coverage, controlled access, no pending unsupported transfer, and a configured stabilization interval. Show emergency stabilization separately from full procedural compliance. Conditions are continuously evaluated after mission completion; success does not disable the module.

If recontainment fails, the statue remains at its actual location and continues its rules. Personnel retain stress/injury, missed duties, lost items and reports. The campaign can continue with an unresolved incident or end only under explicit scenario/difficulty conditions. SCP-650 itself supplies no arbitrary failure cutscene or forced player death.

## 7. Required evidence

Prove normal staffing, last-observer loss, another observer preserving stability, valid and invalid camera observation, relocation into a character's previously unseen area, no valid destination, distinct non-cycling poses, witness-specific reactions and silent relocation.

Compare AIC-only monitoring with a human attending the same live feed. Only the human qualifies under the baseline; the AIC's valid reports still update knowledge. Exercise explicit alternate profiles in isolated authoring fixtures and preserve the selected profile across save/load, without changing the campaign default silently.

Run observed transport personally and through delegated staff, and qualify the controlled-relocation alternative with a reproducible fixture. Exercise failure, player prevention of the breach, blocked equipment, loss of relief/communications, pause/acceleration and saving before/after relocation. Disable the mission wrapper and verify the module continues to behave; swap ordinary lighting/vision conditions and verify behavior follows shared rules.

Requirements SC650-01 through SC650-08 are in [scope and acceptance](04-scope-and-acceptance.md). The next module demonstration is [SCP-1499 and physical/perceived contexts](10-world-perception-and-scp-1499.md).

## 8. Current native observation proof

The [spatial prototype](15-spatial-prototype.md) now runs a native C# SCP-650 module with an independently located physical body, six discrete featureless diagnostic poses, human/AI/any profile selection, direct observation, silent host-validated relocation and per-witness discovery. The [code map](../src/SiteSimulation/README.md) separates perception facts, module policy, spatial commitment, trace and persistence. No mission wrapper forces relocation.

Chen begins watching the statue. One eligible human stops self-relocation even though the three-person staffing threshold is unmet. The inspector lists every actor's eligibility and detection reason. Prototype controls change Chen's attention, avatar blinking and local lighting through validated commands. Door occlusion and independent facing feed the same service. The renderer/inspector does not count as an observer.

Current tuning uses two module-owned placement profiles: threat selects an unobserved adjacent hex behind a human, while explicit complacency selects an unobserved position two to three hexes away without requiring rear placement. Both require clear physical sight between the candidate and intended person, and all qualifying observers must still miss the candidate. No valid destination means a 500 ms local retry; successful relocation starts a 2000 ms local cooldown. These are prototype adaptation values. Targets are selected before positions so a larger set of available cells does not bias target choice. Six poses share one declared hex footprint. The initial pose is raised; threat selects poses 2–3 and relaxed selects poses 4–6. Merely discovering it never switches the flag. A first human discovery briefly holds that witness's movement for 400 local ms; it adds a causal event and no attack or relocation sound. Events within one command boundary are evaluated immediately, with observation reevaluated after each moving actor's progress. Swept/sub-tick event qualification remains broader M0 work.

Outer schema-3 saves with SCP-650 module schema 2 preserve body/pose, effective content profile, module RNG, pending local deadline, encounter/witness state, explicit complacency flags and the bounded trace. Older module-schema-1 saves are rejected intact; they are not silently interpreted as explicit flags. Restore validates a detached candidate before changing live state. Local hold stops the entire module and all local progress while world time continues; releasing it does not consume accumulated world time as a module catch-up.

Shared [awareness movement safety](18-awareness-and-encounter-entry.md) can stop click-to-move on first sight or a new appearance, including a delivered report, without a clock pause. This applies to every qualified anomalous contact, independently of this module's pose, complacency or 400 ms witness reaction.

The default field presentation now hides actors/statue outside direct avatar sight, uses a generated textured six-pose atlas and plays a quiet non-diegetic discovery accent/caption only for a new avatar reaction. Relocation stays silent. F2 restores the unrestricted diagnostic view, F4 selects diagnostic statue sprites, J toggles the avatar's explicit module complacency flag, M mutes audio and R reduces the border effect. See [the prototype guide](15-spatial-prototype.md) and [asset provenance](../game/Assets/scp650-art.md).

Human-attended cameras, AIC infrastructure, real shifts/relief, full fear/doctrine, carrier transport, receiving-room procedures and mission acceptance remain unimplemented. The direct AI/any profile fixtures prove eligibility separation without claiming a resident AIC or surveillance network exists. This is not full M1/SC650 certification.
