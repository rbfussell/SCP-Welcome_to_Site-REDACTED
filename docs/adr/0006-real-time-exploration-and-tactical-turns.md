# ADR-0006: Continuing world with local turn-based tactical combat

**Status:** Accepted time split and complete local-zone waiting policy; timing/balance and boundary behavior require prototype qualification

**Date:** October 22, 2026

## Context

The user explicitly confirmed that the Fallout reference includes its exploration/combat time split and animation feel. This replaces the earlier real-time-combat assumption. They then clarified the waiting scope: hold the engaged local area's movement/action progression while choosing a turn, while the world calendar, granular wider simulation and other areas continue. A global waiting policy was considered and is not the selected design.

The [original Fallout 2 manual](https://cdn.akamai.steamstatic.com/steam/apps/38410/manuals/Fallout2_manual_EN.pdf), printed pages 119–124, describes real-time noncombat play, sequential character turns, action points, unrestricted thinking time and a five-second game-time turn. These are reference facts. Our precise turn-duration/AP-to-time mapping, initiative formula and action costs remain to be qualified; the five-second reference is not silently an adopted implementation constant.

## Decision

Keep one authoritative kernel and world calendar, with explicit per-zone execution policy:

- **Exploration/ordinary operations:** real-time local activity, ruleset-controlled explicit pause/acceleration and the wider world's normal scheduled update cadence.
- **Tactical combat:** sequential actor turns with an action-point budget, visible costs/turn ownership and explicit end-turn handling. Input waiting holds movement/action progression for everybody in the engaged local zone, rather than only the selected pawn. Other zones and the granular background simulation continue on their normal schedules; local tactical input is not a global clock pause.

Presentation remains smooth in either mode. The simulation owns hex movement progress, firing/equipment checkpoints and observation changes. Animation represents that state. Thinking time passes on the world calendar and can affect other areas; it does not give held local actors free movement/actions. Sprite frame counts and cosmetic playback cannot manufacture local progress or advance the world calendar a second time.

## World calendar and local progression

World-level planning, routes, events and other areas' ordinary work continue at their normal detail/cadence. A distant facility is not held merely because the avatar is fighting elsewhere. Workers and other physical actors inside the held local area also wait for the tactical boundary; they do not keep walking while combatants are frozen. Exact encounter-zone geometry and actor membership must be declared and qualified in M0.

Distinguish world-calendar timestamps from local movement/action progress. The global scheduler already accounts for elapsed thinking/execution time: do not add it again as a combat-exit catch-up or sum every actor's turn into a second world clock. Nor should releasing the local hold instantly perform all missed walking, work or attacks. Restore the held progress and revalidate against current committed world conditions at a safe boundary.

The user confirmed that all local simulation waits: movement, actions, bleeding, blink cycles, local SCP countdowns, employment/work and other local conditions. Keep remaining local durations/progress frozen; do not let a world-calendar deadline silently expire those local timers during the hold or bulk-catch them up on release. World-scoped events and other areas continue. Record timer ownership/clock basis explicitly; genuinely external events enter through the qualified boundary policy rather than bypassing the hold as a disguised local timer.

Use a versioned tactical timing profile describing initiative, AP grants/costs, local action/turn progression, timer bases, waiting/end-turn handling and event ordering. Define the relationship between action resolution and the continuing calendar without double charging time. Exact numerical choices are prototype tuning and must be documented before relevant tests pass.

During permitted local action resolution, process local scheduler/module events at required boundaries rather than blindly jumping to round end. Reevaluate observation after relevant effects and preserve pending progress across mode entry/exit. External events arriving at a held zone need an explicit boundary/interrupt or encounter-merge policy. Preserve their causal ordering and critical transitions; do not silently drop a remote observer/feed change or let an unregistered attacker act freely against held characters. Cross-zone interactions must be qualified before claiming support.

Input waiting is a local execution state, distinct from optional explicit manual pause. UI inspection/previews can remain responsive; world-changing local actions, including attention changes or equipment retrieval, use validated action semantics. An asynchronous provider result may affect an authorized running area at its normal commit boundary; it cannot bypass a held area's action gate.

## Departure and elapsed-time presentation

The user proposed explaining the difference between short local tactical action and the continuing world calendar through a contextual departure summary: securing the area, packing equipment or writing reports. Their 1.2-hour example is illustrative, not a fixed cost or turn-duration formula. Another player's unrelated Boston activity must not stop because this character is fighting in Siberia.

Record world-calendar entry/exit timestamps and actual local progress separately. Present an attributed interval rather than changing the world's current time or advancing it again. The summary may account for time already elapsed; it cannot silently retrieve missing equipment, consume supplies, heal injuries, complete duties or award reports as physical actions. Any such effects require their ordinary validated services. Preserve unresolved state when leaving and use context-appropriate language rather than claiming every departure included the same activities.

Whether the departure should also include additional playable/timed work is a clarification pending the user's answer. Working proposal: use the summary to explain elapsed time without an extra wait; a separately requested real departure action would consume only its actual additional duration while the world continues. Do not rewind other zones or invent a local catch-up burst. Cross-zone causal interaction remains governed by the explicit boundary policy; narrative presentation alone does not resolve arrivals, remote attacks or shared actors.

The user proposed an optional later multiplayer decision timer, for example 30 seconds before turn progression. Treat this as a configurable future rule: duration, expiry behavior and disconnect handling remain open. Multiplayer itself remains outside the current milestone. The architecture must avoid a combat wait in one zone automatically holding another player's unrelated zone; no network/session implementation is implied here.

## Actors, modes and anomalies

The player still controls one avatar. Colleagues and opponents take autonomous turns or perform their applicable scheduled work according to their area's execution state. Orders influence choices through the existing authority/employment systems. Remote AI-only engagements use their ordinary operation resolver rather than opening a player-input gate for an uncontrolled character.

Combat entry/participation/exit are explicit encounter transitions. Authorize them through actor knowledge, actions and engagement conditions; entering/exiting must not refill resources or repeatedly grant free turns. Qualified ordinary hostile engagements/attacks automatically request entry at a world-space boundary, independent of camera zoom. Unperceived anomalous movement/lethal effects may complete without warning or player input; actual acquired evidence and remaining reaction time determine whether an input opportunity exists. Do not reset deadlines or insert a guaranteed response turn. This qualifies entry into waiting, while preserving the complete local hold once a valid turn is waiting. After committed avatar death, reveal the scene as presentation only, without granting evidence to living actors. Last-known/sound indicators use acquired evidence and uncertainty. Exact radius/initiative and cross-boundary rules require a concrete control/combat pass. An anomaly sighting can instead interrupt only avatar movement through independent enabled-by-default self/report settings, without stopping clocks; see [awareness and entry](../18-awareness-and-encounter-entry.md). SCP-650 remains nonaggressive; discovery/startling is not automatically combat. Its observation policy remains module-owned under the qualified zone/timer rules. M1 proves tactical behavior in a bounded ordinary-actor fixture without inventing hostility for the anomaly.

## Persistence and qualification

Persist world-calendar state, zone modes/hold flags, encounter/membership, current actor/turn cursor, AP grants/spent state, timing-profile/clock-basis versions, action IDs, local progress, queued cross-zone events and scheduled module/work events. Preserve encounter entry/exit timestamps and departure-summary identity/state so resuming cannot charge or complete a departure twice. Save/load restores held and running areas independently; it cannot repeat AP expenditure, damage or elapsed world time. Branch restoration invalidates abandoned tactical callbacks and provider results.

M0 qualifies a held combat zone alongside a running second-zone/world counter, with no doubled time, local timer expiry or bulk replay on release. Declare the test timing/boundary profile before certifying it. M1 qualifies AP validation, equipment access, autonomous turns, observation/conditions, boundary events, transitions and save/resume. This revises the basic combat target; it adds no hostile SCP campaign, multiplayer implementation or claim of a working runtime. See [controls/time](../08-player-controls-time-and-continuation.md), [simulation](../02-simulation-and-world.md), [scope](../04-scope-and-acceptance.md) and [presentation](../06-sprites-and-placeholders.md).
