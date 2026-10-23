# Awareness, movement interruptions and hostile encounter entry

Status: User-confirmed direction, October 23, 2026. This supersedes the briefly considered SCP-650-specific route cancellation and global auto-pause on anomaly sighting. A sighting stops the avatar's movement; hostile engagement enters local tactical combat. These are different transitions. Numeric ranges and combat-entry details below remain proposed tuning.

## Three distinct controls

| Trigger | Required result | Time behavior |
|---|---|---|
| Newly sensed anomalous encounter, with its source setting enabled | Stop the avatar exactly where they are, clear the route/destination and await another movement command | World and local simulation keep running |
| Hostile engagement satisfying the encounter boundary | Automatically enter local turn-based combat, with explicit participants, initiative and turn ownership | All local simulation waits for turn selection; calendar and other areas continue |
| Explicit manual pause | Stop the participating simulation under its ruleset | Existing global single-player manual pause remains separate |

Default movement-safety settings are **stop on my anomaly sightings** and **stop on communicated sightings**, both enabled and independently switchable. A reported sighting requires actual delivery to the player through a valid channel. Merely having another observer somewhere in the world is insufficient. Reports about a distant anomaly may interrupt travel without putting this character into combat in that distant zone.

“Anomaly” is a shared perceived-contact category, not an SCP-650 reaction or a synonym for hostile. Its module supplies physical/sensory behavior; shared awareness consumes acquired evidence and the player movement policy decides whether to stop. Complacency, fear, threatening poses and actual aggression remain separate facts. A known nonaggressive SCP does not enter combat just because it appeared nearby. A concealed or unidentified object must not reveal its true anomalous identity merely through the player setting. Recognition/perceptibility needs an authored, evidence-backed rule; this prototype uses the facility's already known registered anomaly bodies.

## New encounter versus continuous sight

A subject/encounter token distinguishes a fresh appearance from repeated sensor samples. Continuous sight, another report of that same encounter, a brief blink, save/load and repeatedly inspecting the entity do not keep cancelling deliberate movement. A newly discovered subject or a new anomalous appearance can interrupt again. A person can intentionally walk after noticing something; an enabled safety setting does not immobilize them while it remains visible.

The current physical-anomaly provider uses the body's committed relocation revision as its appearance token. A future ordinary hostile's routine walking must not create a new encounter token every step. Additional perception providers need explicit contact/episode semantics before joining this policy. Disabling one source does not disable the other. Knowledge can update without the interruption option being enabled.

Stopping is immediate at the next qualified perception/command boundary, including in the middle of a hex step. Retain the exact physical position, clear the path and show a standing sprite. Do not snap backward, finish the remaining step automatically, zero the world clock or queue a hidden continuation. Preserve the fractional edge geometry/reservation so another body cannot occupy it. A new movement command can continue or reverse that edge without teleporting. The ordinary simulation step remains 50 ms; this is not a claim of sub-frame sensing.

## Hostile engagement boundary

The Fallout reference establishes the desired real-time exploration to tactical-combat feel. Encounter distance must use **world hexes**, not screen pixels, camera zoom or window size. The user's visible-area-plus-margin example describes an engagement buffer, rather than a literal camera-dependent rule.

Propose a configurable awareness/engagement radius with a buffer outside immediate close combat. An eight-hex radius is a candidate fixture value, not an approved default. Actual weapon reach, line of attack, connected space, relevant sensory knowledge and an enemy's valid attack intent can require a larger encounter. Do not let a long-range attack bypass combat merely because its attacker is outside a short proximity radius.

Required entry cases:

- A known hostile in the qualified local engagement envelope automatically starts tactical combat. Do not require the player to stop walking and manually press a combat button.
- An actual attack about to resolve enters the combat scheduler before its costs/effects are committed. Initiative/ambush rules decide the opening action; switching modes cannot grant a free hit or refill resources.
- An unseen actor's mere existence near the avatar is not player evidence. If an enemy genuinely detects and engages the player, an encounter may begin while its location remains hidden; reveal only the attack/sound/evidence actually acquired.
- A locked wall, unrelated floor or disconnected zone is not automatically a local engagement just because projected sprites overlap. Qualify reachable/attack-capable boundaries and escalation/merge behavior.

The encounter owns participants, local hold membership, AP, current turn, entry/exit guards and pending actions. A player turn waits locally; autonomous allies/enemies act through their turns. Other regions continue, as already agreed. Repeated detection or crossing a threshold cannot create duplicate encounters, award new AP or reset ongoing hazards. Exiting requires explicit disengagement/no remaining engagement, not merely looking away. [The timing decision](adr/0006-real-time-exploration-and-tactical-turns.md) still governs clocks and cross-zone effects.

## Direction and uncertainty indicators

Store acquired contact facts separately from authoritative entities: subject identity or unknown-contact identity, classification, source/observer, encounter token, last known point or estimated region, uncertainty and observation/delivery time. A sound may supply only an approximate bearing. It must not disclose the exact source entity, species or current position through the indicator.

Indicators use these records, never a hidden actor's live transform. Direct vision can establish a known position; delivered reports retain their provenance/age; sound estimates retain uncertainty. A hostile's last seen location stays where it was when contact was lost. Movement elsewhere does not drag its arrow around. Distinguish confirmed hostile, reported contact and unidentified noise visually and in text; color alone is insufficient. Fade or age stale indications, and qualify negative evidence such as finding an old location empty.

The prototype's bearing arrows use up to four recent acquired records outside direct view, labeled `last seen`, `reported` or `sound ~` with age. Amber denotes ordinary anomalous evidence; a future confirmed-hostile provider can use the hostile category. Fifteen local seconds is a provisional display window; expiry hides the arrow without deleting remembered evidence. Positions come only from the recorded contact. If the uncertainty region contains the avatar, do not render a precise directional arrow. Unidentified sound records can persist without an authoritative emitter ID. There is no working sound-propagation or full hostility provider yet.

## Current implementation and bounds

[PlayerAwareness](../src/SiteSimulation/Perception/PlayerAwareness.cs) owns generic contact records, acknowledgements and the two enabled-by-default settings. [SpatialSimulation](../src/SiteSimulation/SpatialSimulation.cs) supplies direct sightings of all registered anomalous bodies and a bounded nearby-voice report fixture. The latter requires a human who actually sees the subject, within four construction units of the avatar, with an unobstructed communication path. This approximates local voice only. Radios, equipment-backed communication, relayed surveillance and SCiPnet delivery require their later validated transports; no global faction information sharing is assumed.

[MovementSystem](../src/SiteSimulation/Actors/MovementSystem.cs) stops the avatar in place; a serialized `StoppedStep` retains a fractional edge without an active path. This is ordinary movement state, not module state. Rendering derives stance/position from it. [EncounterOverlay](../game/Presentation/EncounterOverlay.cs) draws acquired bearings. No SCP module interprets either player setting, and neither setting changes anomaly behavior or supplies omniscience.

Outer save schema 3 stores settings, evidence, acknowledgements and stopped fractional movement. Compatible schema-2 saves load with both settings enabled and an empty new awareness ledger; a subsequent real acquired contact can therefore interrupt once. SCP-650 module schema stays 2. Invalid restoration is detached and leaves the live world intact.

**Not implemented by this pass:** complete AP/initiative combat, automatic hostile engagement execution, ordinary hostility/faction inference, hearing propagation, radio/SCiPnet delivery and multiplayer encounter coordination. The automatic entry and uncertainty rules above are required M1 behavior, not claims that a working combat game now exists. Existing F6 is still a hold fixture and cannot certify those requirements.

Acceptance is tracked as AWR-01 through AWR-05 in [scope](04-scope-and-acceptance.md). The [awareness checks](../tests/SpatialChecks/AwarenessChecks.cs) qualify generic subjects, independent settings, actual report reachability, exact stopping, explicit continuation/reversal, continuing clocks, uncertainty records and save compatibility.
