# Scope, milestones, and acceptance

Status: Baseline revision 0.9, October 22, 2026. Godot .NET plus an independent C# simulation is selected; versions/integration/performance still need M0 qualification. Square construction tiles, smooth hex actor movement, fixed isometric/three-quarter presentation and detailed pixel sprites are confirmed. Real-time exploration and turn-based tactical combat replace the earlier real-time-combat assumption. All local simulation waits for tactical input; the world calendar and other areas continue. OSCP/SCiPnet and the resident deterministic AIC remain. Detailed authority, canon, numeric limits, tuning and gate groupings remain proposals.

## 1. Meaning of requirements

“Must” indicates a release gate once this baseline is adopted. “Should” indicates a target that can be adjusted through a recorded decision. “Later” is excluded from the current deliverable. Documentation and API sketches are not proof that a feature works.

The game-design document defines the experience. The simulation and module documents define behavior. This document controls which parts must ship in each milestone. Where architectural capability is described without a milestone commitment, it remains proposed future work.

The target is a persistent world inhabited by one controlled character. The initial director manages a Foundation network; later employee starts use the same simulation without automatic management authority. M1's single-site boundary is a first-playable constraint. Network management and Groups of Interest remain required product pillars, qualified in M4; a complete game release cannot omit them by treating the local slice as final scope.

## 2. First playable slice: SCP-650 inside one facility

Start as newly appointed regional operations director of one functioning site, with SCP-650 already contained. Observe real shifts, equipment, storage, communications and normal services. An ordinary observation/coverage failure can permit the module's relocation; direct action or delegated staff can prevent, stabilize or recontain the incident. Reports, stress/injury, lost work/items, repairs and unresolved containment persist. The entire operation stays inside the facility.

The scenario supports direct participation and delegation through the same actions/procedures. Both observed transport and controlled relocation are accepted approaches; detailed crew/equipment/placement rules remain proposals. The mission cannot force a breach, convenient relocation or success while ordinary rules say otherwise. Full behavior and adaptation notes are in [SCP-650's scenario contract](09-first-playable-scp-650.md).

Proposed content ceiling:

| Area | Included in M1 | Explicit limit |
|---|---|---|
| Campaign | One facility shift/incident loop as regional operations director | Employee-start and successor proofs are later; no arbitrary character swapping |
| Region | One operational facility including its indoor loading/garage area | No town visit, off-site expedition or strategic expansion in M1 |
| Site | Two floors; at most 16 functional rooms | Fixed structural shell; generated room allocation |
| Population | 16 site staff including the avatar; facility-only civilian/visitor placeholders only if explicitly added | No separate 36-person town population in M1 |
| Employment | Six bounded roles: security, cafeteria, janitorial, scientist, lab technician and logistics; shifts, breaks/relief, blocked-duty reporting and authorized reassignment | Uses the same 16 site staff; one scheduled shift scenario; no exhaustive profession simulation or automatic full-day coverage |
| Teams | Up to two four-person response teams drawn from actual available staff | Coverage conflicts remain real; no extra free staff or remote expedition |
| Anomalies | Active SCP-650 module; approximately six discrete pose illustrations; role/camera observation and two recontainment approaches | Site scope, non-aggressive behavior; active SCP-1499 follows in M2 |
| Personal play | Click/Ctrl-click movement/targeting, accessible-equipment functions, facing/LOS, real-time exploration, basic turn-based combat/injury, treatment and equipping | Ordinary-actor combat fixture; SCP-650 remains nonaggressive; no driven vehicles, full stealth system or detailed ballistics |
| Inventory | 24 item definitions including a reusable box/case definition, eight equipment slots, four named wearable containers | 90-degree rotation; simple wearable nesting; warmth and mass only |
| Physical storage | One four-mount long-gun rack, one four-mount pistol rack, one cabinet with three box bays and shared loose-item space, one nine-column/three-high pallet, one parked vehicle cargo compartment | Authored discrete layouts; cabinet/vehicle → box/case → items; no general 3D packing or loaded-furniture moving |
| Storage automation | Filters, priorities, same-item consolidation, reservations, hauling, blocked-access reporting and pallet rehandling | At most two simultaneous haulers drawn from existing staff; one box size; bounded authored staging layout |
| Daily life | Work, rest, meals, alarm, flee/hide, basic dialogue | No romance, family simulation, or LLM |
| Sprite presentation | Square logical tiles and fixed isometric/three-quarter view; six diagnostic category colors, generated state/frame/facing placeholders and data-driven art bindings | Detailed Fallout 2-inspired art target; M1 can use stand-ins; first spatial proof uses six facing directions/three walk frames, while production resolution/counts and pipeline remain open |
| Terminal | OSCP device/session shell, SCiPnet-backed records/mail/alerts, staffing/logistics orders with receipts, one outside-report fixture and resident AIC/technical-support entry | In-game interface, not an emulated host OS; detailed AIC authority awaits decision; no full SCP-245/SCP-5810 scenario or required model inference |
| Facility | Floor/utility views, bounded circuits with backup, two-room heating/cooling and sensor/control proof, maintenance, room fit-out, storage-fixture placement/installation | One room conversion; one authored electrical/thermal fixture reusing slice rooms/staff; repair, conversion and fixture-installation work types; no structural expansion or general fluid simulation |
| Persistence | Save/load across all scenario states; reload or permanent-ending ruleset | One exact build/content set; migration/succession/anomalous restart exercised in M3 |
| Mod workflow | Text form, package validator, sandbox fixture, scenario runner | No visual scripting editor, marketplace, or arbitrary native plugins |

The earlier proposed 12-lot settlement and off-site observation-hazard mission are removed from M1. Settlement/building hierarchy tests move to M3; the 120-house preset remains a bounded generation test without requiring finished art or individual business simulation. This is a scope reduction for the confirmed facility-first incident, not removal of the long-term world.

The eight initial equipment slots are underwear, socks, footwear, legs, torso, outerwear, belt, and face. The four containers are two trouser pockets, a coat pocket, and a backpack. Additional layers and protection properties remain later content. M2's SCP-1499 uses the face slot in this same facility/save.

Storage counts above qualify the newly clarified inventory behavior; they are proposed fixture bounds rather than user-approved content counts. The pallet's 27 box instances reuse one item definition. Storage fixtures use fixture definitions rather than increasing the portable-item catalog implicitly. Hauling jobs are logistics work separate from the three construction/maintenance work types. Vehicle cargo is tested while parked; driving remains deferred. [The storage contract](05-storage-and-logistics.md) specifies shared occupancy and access.

Primary employment duties and optional action jobs are dispatched separately. M1 role graphs can share action primitives while retaining different responsibilities and coverage. Field-team assignment must expose any resulting vacant posts. The employee count is a scenario bound, not a guarantee of uninterrupted coverage across all roles. [Employment and orders](07-employment-and-orders.md) defines arbitration. The generated reference assets in [the sprite contract](06-sprites-and-placeholders.md) already illustrate the convention; complete engine integration remains milestone work.

## 3. Milestones and gates

### M0 — feasibility and Godot qualification

Deliver a small headless kernel prototype and the minimum presentation harness needed to measure it. Demonstrate one shared entity model, command validation, deterministic event ordering, scripted bounded behavior, save restoration, and a generated room with door/occlusion queries. Prototype two connected domains, atomic inventory transfer, a nested box and shared shelf occupancy at two layout scales without finished worlds. Qualify generated sprite bindings, authoritative facing and visible missing-state fallback independently of finished art.

Measure the proposed performance envelope on a named machine. Qualify the selected Godot engine for fixed isometric sprite rendering over square logical tiles, headless operation, scripting isolation, serialization and tools. Pin the engine/runtime configuration, scripting runtime, supported desktop OS, authoritative movement model and reference hardware in short decision records. Reopening the selected engine requires evidence and owner discussion.

Godot 4 .NET plus a plain C# kernel is the selected integration direction; see [ADR-0005](adr/0005-retain-godot-after-falltergeist-review.md) for the Falltergeist comparison. Also qualify an OSCP query/command adapter with role validation and idempotent receipts. Existing installed executables are not evidence that the required .NET editor/export integration or benchmarks pass. Demonstrate square-construction/hex-navigation registration and save/resume during a smooth movement segment. Exact projection angle, native sprite scale and finished animation counts need coordinated art qualification; labeled stand-ins remain valid prototype assets.

Qualify a held tactical zone alongside a running second-zone counter and granular world scheduler. All local conditions/timers retain their remaining progress while world time advances. Declare the timing profile, zone membership and incoming-event boundary policy; release must neither replay missed local work nor charge elapsed world time twice. [ADR-0006](adr/0006-real-time-exploration-and-tactical-turns.md) records this time split. This harness does not require a second finished site or multiplayer.

Exit: observation timing survives render-rate changes; a script quota violation cannot corrupt committed state; save/resume preserves RNG and timers; domain transfer cannot duplicate inventory. If one fails, revise or reduce the architecture before producing content. Engine selection is not a reason to defer the hard tests until after art production.

### M1 — first playable slice

Deliver the bounded scenario above with personal participation, unattended resolution, terminal orders, physical equipment accounting, employment/work and optional-task handlers, role-aware NPC routines, construction/repair, and save/load. Include procedural building/room composition at slice scale and the bounded local/automated storage proof with specialized mounts, nested boxes, pallet access and hauling. All declared slice visual states use generated diagnostic sprites until production assets replace them.

Include the [bounded infrastructure proof](17-facility-infrastructure.md): actual circuit service to lights/doors and a heater/cooler, limited backup, two-room thermal response and sensor/control/repair behavior. One explicitly qualified existing worker can perform the bounded repair duty under authorized reassignment; this does not add free staff or certify all engineering jobs. Full SCP-019 content is excluded.

Prove real-time/tactical transitions, sequential turns/AP, equipment access costs and autonomous staff turns in a bounded ordinary-actor fixture using existing slice personnel. Prove local waiting, continued outside scheduling and save/resume without turning SCP-650 into a combat enemy. This revises the basic combat target; it does not add a hostile anomaly campaign or multiplayer milestone.

Exit: all M1 requirements below pass in a packaged build. A person following the documented controls can complete one operation each way, see a justified failure, recover/reload, and explain the report from the action trace. A suggested 30–60 minute shift is a pacing hypothesis to test, not an acceptance duration guarantee.

### M2 — SCP-1499 in the same facility

Load a functioning SCP-1499 package alongside SCP-650. Deliver module-provided equipment, procedural terrain/structures/towers, bounded independent entity populations, persistence/revisit, physical transfer and a separate explicitly authored perception/consequence profile. Propose one wearer, one initial 3 × 3 chunk neighborhood, at most eight local entities and one mapped ordinary-world test area. These limits are fixture proposals. See [the context/SCP-1499 contract](10-world-perception-and-scp-1499.md).

Exit: CTX-01 through CTX-05 pass. Alter a SCP-650 observer through the shared location/perception services, without pair-specific logic. Demonstrate ordinary-world consequences in the perception profile rather than a cosmetic map switch. Keep the profile's ontology and mappings explicit; do not claim this settles the source fiction or supports unrestricted reality editing.

### M3 — broader architecture, world and continuation qualification

Deliver simplified fixtures for the remaining requested cases: retail domain, descending staircase, SCP-173's distinct observation policy and item refinement. Reuse M1/M2 fixtures and qualify composed effects/conflict reporting. Add settlement/building hierarchy tests, full-size preset validation and explicit state migration.

Add a bounded ventilation/isolation fixture and SCP-019-informed environmental hazard test using shared power/thermal services. Qualify idle production, treatment heat and controlled recovery without promising a full SCP-019 campaign. This is additional environmental qualification alongside the four broader anomaly fixtures; MOD-04 does not silently certify it.

Prove non-manager role starts in a bounded harness and continuation through an eligible successor. Prove one synthetic anomaly that requests a compatible whole-world day/checkpoint restart with explicit retained information/cost and branch isolation. No named day-restart SCP or complete employee campaign is promised here. Arbitrary partial rewinds and temporal paradoxes remain excluded.

Exit: all M3 requirements pass, with individually documented adaptations/exclusions. These are representative capability fixtures rather than complete narrative campaigns, production art or exhaustive recipes. Passing them does not establish universal SCP/mod compatibility.

### M4 — bounded Foundation network

Deliver two Foundation sites, one temporary outpost, two delegated site managers, and one autonomous Group of Interest in a bounded region. The player remains the same directly controlled character and can travel between nodes. Reuse qualified M1–M3 systems rather than adding new anomaly families, driven vehicles, or a new combat model.

Include one inter-site equipment shipment, one qualified anomaly transfer with receiving requirements, standing orders for reserves/maintenance/escalation, and one group objective that can intersect those operations. Provide one authored communication/negotiation route and one interference/conflict route using existing action services. The group's resources, intelligence, and agents are bounded and persisted. Prototype capacity: two concurrent shipments and four concurrent operations total. These counts are proposed test bounds rather than final campaign scale.

Exit: NET-01 through NET-05 pass. Demonstrate that a manager handles an authorized routine problem while the player is elsewhere, that a shortage or threat escalates, and that a group operation has consequences visible in both the network display and physical world. The group loop is functional even when the player does not personally intervene. Repeat the adopted performance measurements with both sites, shipments and group activity running; M0's local benchmark alone cannot qualify the expanded workload.

### M5 — limited content release

Scope is set only after M1–M4 evidence. Select the number of polished anomalies, network nodes, Groups of Interest, available starting roles/difficulty policies, generation templates, site operations, art sets, audio, target hardware and distribution platform as explicit line items. Network management, one-character participation, alternative-role compatibility and a functioning Group of Interest loop remain required. A native facility section view, deeper economy, expanded town interiors and a mod editor are independent candidates, not automatic inclusions.

Exit: a release-specific acceptance matrix, content provenance, packaging, usability, accessibility, stability and performance criteria are agreed and pass. Shipping an unrestricted network, every profession or every lore faction is not part of M0–M4 by implication.

## 4. Traceable acceptance requirements

| ID | Gate | Requirement and reproducible evidence |
|---|---|---|
| SIM-01 | M0 | Run the same seed, content, build and recorded commands twice; compare authoritative checkpoint hashes including RNG and pending events. They match. |
| SIM-02 | M0 | Replay observation at different rendering rates; movement permissions, injury events, and order of critical effects match. |
| SIM-03 | M1 | Open terminal/facility/personal views during an incident. Tick, identities, injury, gear and incident state remain shared. Explicit pause stops all. |
| SIM-04 | M1 | Accelerate during guard handoff and an alarm. Every due critical event resolves; speed caps or stops as documented. |
| OPS-01 | M1 | Run an operation personally and unattended with equivalent scripted actions and sensory inputs. Mandatory hazard constraints and item transactions match; report causes are traceable. |
| OPS-02 | M1 | Withdraw with an injured teammate and missing equipment. Route, carrying capacity and supplies constrain evacuation; no free rescue or gear restoration occurs. |
| OPS-03 | M1 | Attempt to assign the same staff member or reserved unique item twice. The conflicting assignment is rejected without consuming resources. |
| GEN-01 | M1 | For 100 fixed seeds, validate slice counts, envelopes, required sockets, door routes and usable fixture clearances. Every seed succeeds or returns a bounded explicit diagnostic; release seeds must succeed. |
| GEN-02 | M3 | Generate a motel and house in the world-generation harness using the same bathroom definition; context parameters fit both envelopes and preserve access. This is not an M1 town visit. |
| GEN-03 | M3 | For 100 fixed seeds, generate the 120-house preset with the specified service-building counts, housing capacity, and road connectivity. No mandatory child is silently dropped. |
| GEN-04 | M3 | Change an unvisited chunk's default generator version, then load a save. Its pinned version is retained or load is blocked before mutation. Visited modifications remain intact. |
| INV-01 | M1 | Rotate and move items among the four containers. Reject overlap, invalid masks, overweight, inaccessible items, and cyclic ownership without changing the source. |
| INV-02 | M1 | Drop equipped clothing containing items, save, and reload. Every item has exactly one owner/location and correct container membership. |
| STO-01 | M1 | Inspect the same equipment on the avatar, an NPC and the parked vehicle. Manual transfers and console orders reference the same item IDs, physical locations and accepted layouts. Console orders complete only through physical work. |
| STO-02 | M1 | Place compatible long guns/pistols in their shaped mounts; reject the wrong category and incompatible size/attachments. User filters cannot widen native rack restrictions. |
| STO-03 | M1 | Put a loaded box in a cabinet bay, place loose items in remaining shelf space, and inspect both layout scales. Both views reserve shared physical capacity; overlap and cyclic nesting are rejected. Contents remain unique and recursive mass/counts are correct. |
| STO-04 | M1 | Fill the pallet's nine columns to three boxes high. It contains 27 boxes and only nine topmost boxes are directly accessible. Removing one exposes the next in its column. No lower box can be directly extracted through a blocker. |
| STO-05 | M1 | Request an item from a buried box. With rehandling allowed and valid staging space, staff move blockers, retrieve, and optionally restack. Without space, authority or capable staff, report the blocked job without moving stock invisibly. |
| STO-06 | M1 | Offer equal-priority empty and partly filled compatible boxes, then a higher-priority eligible destination. Consolidation wins the first case; explicit priority wins the second. Incompatible/full/locked candidates are excluded, and an unchanged world produces the same choice. |
| STO-07 | M1 | Two workers compete for one item or the final destination footprint; one reserves it. Concurrent child extraction and full-box pickup cannot conflict. Authorized manual cancellation releases/reassigns reservations without duplicating items. |
| STO-08 | M1 | Interrupt a worker after pickup, change destination policy, and save/reload. The item stays at its actual carried or valid set-down location; reservations and job state restore, then replan/complete once. |
| STO-09 | M1 | Attempt to enable storage without a fixture, then install a rack using a work order. Capacity exists only when commissioned; placement rejects blocked access/navigation. Rack occupancy in local view matches console stock locations. |
| STO-10 | M1 | Repeat sorting with unchanged equal-priority policies: valid stored stock remains stable. Change priority or request rebalance and verify allowed moves only, effective policy/inheritance explanations, and separate accessible/reserved/in-transit counts without double-counting nested contents. |
| NPC-01 | M1 | With no model installed, civilian routines and gunfire-triggered flee/hide/call-help behavior operate and respect blocked exits. |
| EMP-01 | M1 | Disable optional hauling/cleaning for on-shift cafeteria staff and a janitor. They still perform role-required carrying/cleaning through the work handler and do not abandon duty for higher-scored discretionary tasks. |
| EMP-02 | M1 | Remove a role prerequisite such as ingredients or lab equipment. The employee performs another valid role duty or waits/reports the blocker; they do not become general optional labor without a declared release/reassignment. |
| EMP-03 | M1 | Issue an in-scope reassignment from an authorized supervisor and a conflicting order from an issuer without applicable authority. Execute or acknowledge/queue/reject with a reason; preserve interrupted duty and physical work state. |
| EMP-04 | M1 | Apply mild fear, then a declared severe panic/incapacity condition to a trained guard under order. Respect doctrine in the first case and the authored override in the second; lost coverage and order failure are recorded without invisible replacement. |
| EMP-05 | M1 | Execute a shift boundary, scheduled break and critical-post relief. Coverage changes only when staff physically arrive/leave; absent relief triggers the declared escalation/overtime limit/fallback. No reset or indefinite forced work supplies imaginary coverage. |
| EMP-06 | M1 | Coordinate an observer/mover/relief team. A missing readiness acknowledgement or failed communication blocks/aborts the dependent step. An actual relief handoff preserves observation only when the replacement is eligible and ready. |
| EMP-07 | M1 | Queue hauling and a field deployment while personnel hold primary posts. Logistics employees receive appropriate role work; other employees require release or authorized reassignment. The same person cannot provide local coverage and be deployed remotely. |
| EMP-08 | M1 | Save during blocked duty, order interruption and pending handoff; resume offscreen then inspect locally. Shift clocks, duty continuation, orders, reservations and coverage agree with uninterrupted execution. |
| SPR-01 | M0 | Given declared category/state/direction/frame metadata, generate all requested stand-ins with matching labels and frame counts. Validate keys and assets without requiring production artwork; runtime name overlays reuse assets across NPC instances. |
| SPR-02 | M0 | Face north/east/south/west while stationary and during backward/sideways movement in the fixed isometric view. Project the green marker from world facing and retain the correct world-direction label; neither velocity nor render frame rate determines observation direction. |
| SPR-03 | M1 | In the world and sprite catalog, inspect category color, entity name, state and one-based frame/total for every declared slice animation/illustration. Walking phase advances with committed locomotion and freezes on pause; blocked movement does not show a successful stride. |
| SPR-04 | M1 | Replace a diagnostic animation with production/test art and change frame count through metadata. Anchors and state bindings remain valid; identical recorded inputs produce identical gameplay-state projections and action timing, excluding the deliberately changed cosmetic metadata/hashes. |
| SPR-05 | M0 | Remove a production frame and request an undeclared state. Use a labeled placeholder for the missing frame and a conspicuous missing-state diagnostic for the undeclared state, with no silent idle fallback or gameplay mutation. |
| SPR-06 | M1 | Keep diagnostic overlays inspectable after art replacement. Verify anchors/attachments, walk-behind layering, projected picking/footprints, stable facing and readable labels at supported zoom. Preserve the confirmed visible pixel treatment at normal gameplay zoom. Tall sprites do not change collision/LOS; visibility aids and debug labels do not leak hidden information to the release player. |
| FAC-01 | M1 | Queue room conversion with insufficient materials, then supply it. Work waits, resources are consumed once, and the activated room is physically represented. |
| FAC-02 | M1 | Isolate an actual circuit supplying light/door consumers. Delivery, illumination, declared door fail-state, local view, known terminal status and observation rules reflect the same outage. A UI toggle alone does not pass. |
| FAC-03 | M1 | Overload a protected circuit and exercise declared load shedding/trip behavior. Transfer to limited backup, deplete its stored energy under load, then restore supply. Capacity limits, transfer delay and consumer operation agree; no duplicated supply or free recharge. |
| FAC-04 | M1 | Run the two-room thermal fixture with a powered heater/cooler, outdoor boundary, door and controllable heat source. Demonstrate finite setpoint recovery, power-loss drift, door exchange, capacity shortfall and upper/lower threshold events. Energy flows and declared heat rejection are bounded; render rate does not change results. |
| FAC-05 | M1 | Lose a sensor/link and show stale/unknown status rather than safe defaults or omniscient AIC readings. An independent powered local controller continues where its declared links survive. Compare actual conditions, measured evidence, alarm delivery and authorized control changes. |
| FAC-06 | M1 | Request a utility repair requiring access, qualification, isolation, tools, parts and work. Block a prerequisite, then restore it. An authorized existing worker leaves conflicting duty explicitly; repair commits once and thermal recovery takes its real remaining time. |
| FAC-07 | M1 | Save before backup depletion and a thermal threshold, during repair and during local tactical waiting. Resume with identical source stores, topology, thermal/controller/sensor state and next critical events. Held local consumption/heat/work do not catch up against calendar time; unsupported cross-zone utility topology is rejected explicitly. |
| FAC-08 | M3 | Isolate two connected areas with a fan/damper/filter fixture and a declared transported test contaminant. Flow/transfer limits, power loss, filter capability and sensor evidence determine exposure through the same shared services; unseen areas retain required fidelity. No general pressure/fluid solver is implied. |
| FAC-09 | M3 | Exercise an explicitly adapted SCP-019-informed hazard fixture: stable 0°C idle conditions still produce approximately one entity/hour, treatment adds heat and uses its declared supplies, and power/capacity/monitoring loss or environmental shock affects authored rules. Review source facts versus tuning; direct/unattended runs and saves preserve outcomes. This does not certify a full SCP-019 module. |
| MOD-01 | M0 | Validate duplicate IDs, invalid capabilities, undeclared entry points and cyclic dependencies. Reject before instance creation with actionable errors. |
| MOD-02 | M0 | Exercise script fuel/memory/event limits and an exception mid-transaction. Preserve last committed state and enter the declared fault policy. |
| MOD-03 | M1 | Add a second definition using published observation components and procedure hooks without changing kernel source. Compare human-only, AI-only and any-eligible observer profiles; medium/attention conditions still apply. Validate explicit scenario selection, persist effective profile/version and document authoring steps. |
| MOD-04 | M3 | Qualify the four broader anomaly fixtures plus existing SCP-650/SCP-1499 scenarios, including direct/unattended behavior and critical-state saves. Publish each pass and exclusion individually. |
| MOD-05 | M3 | Combine two supported policies, then two exclusive incompatible redirects. Composition is deterministic; incompatibility is reported without load-order arbitration or partial transfer. |
| SAVE-01 | M1 | Save before/after injury, repair completion, guard handoff and containment receiving; resume and compare against uninterrupted execution. No duplicated rewards, items or scheduled effects. |
| SAVE-02 | M3 | Load with a missing mod and with a failed state migration. Block normal loading and leave the original save unchanged; successful migration writes a separate valid result. |
| PERF-01 | M0 | Record hardware, build, scenario, 10,000-step latency distribution, frame timing, memory, and save/load time. Approve measured targets or revise scope. |
| PERF-02 | M3 | Travel through 1,000 logical chunks/landings with unload/revisit. Resident geometry stays within the adopted budget; sparse saved changes and unique items persist. Save growth is measured, not claimed constant. |
| ROLE-01 | M1 | Initialize the regional director with one operational site, objectives/restrictions and scoped authority. Order/terminal services validate the character's credentials and resources rather than a universal player exemption. |
| ROLE-02 | M3 | Initialize security and cafeteria employee starts using the same actor/employment/world services. Their access, duties and equipment differ; direct input does not grant management commands or get silently replaced by the NPC work handler. |
| CTL-01 | M1 | Click to move/interact and Ctrl-click explicit targets/positions with selected functions. Validate obstacle, range, facing and authority rules; force input cannot bypass ordinary constraints. |
| CTL-02 | M1 | Use functions from hand/belt, holstered and backpack sources with different access steps. Draw/retrieve/use takes declared time, source location/readiness is visible, and dropping/locking/transferring the item invalidates its shortcut. |
| CTL-03 | M1 | Show directly visible, friendly-reported and live-surveillance information with source/time. An old report or overhead view neither tracks a hidden entity nor becomes qualifying observation or a clear physical shot. |
| CTL-04 | M1 | Click non-hostile SCP-650 and equipment/containment targets. Default context actions do not invent an enemy attack routine; explicit equipment use commits ordinary costs/effects and preserves independent facing/attention rules. |
| TIME-01 | M1 | During real-time exploration, opening inventory/terminal does not pause work/equipment/actions. Permitted explicit manual pause stops the participating world; queued intents revalidate on resume. Configured incidents normalize speed or optionally pause after commitment, and a no-manual-pause profile denies that control without removing tactical input waiting. |
| TIME-02 | M0 | Hold a tactical zone while the world calendar, granular scheduler and second-zone counter continue. Every local actor, work action, bleeding/blink cycle and SCP timer retains remaining progress. Release under the declared timing/boundary profile produces no immediate timer expiry, bulk replay of missed work or second charge of elapsed world time. Distinguish this from global manual pause. |
| TAC-01 | M1 | Enter tactical combat from real time, resolve sequential actor turns/AP and return to real time. Reject out-of-turn and unaffordable actions, expose costs/end-turn and preserve committed movement. Repeated entry/exit cannot refill resources or farm free turns. |
| TAC-02 | M1 | Retrieve/use equipment and perform physical inventory work through declared turn/action costs. UI previews grant no free transfers, facing or observation changes. Staff act autonomously under valid duties/orders; actual action checkpoints update LOS. SCP-650 discovery alone does not force combat or invent an attack routine. |
| TAC-03 | M1 | Save during input waiting and during action resolution, then restore held/running areas, world calendar, AP, local durations/progress and queued boundary events. Replay the same recorded timeline at different render rates/frame counts with identical authoritative outcomes. Resolve one external transition under the qualified boundary policy without lost events, duplicate damage or double elapsed time. |
| TERM-01 | M1 | Enter OSCP through an accessible powered device, authenticate the actor and open reachable services. Changing identity/role or power revokes invalid access; the UI never grants universal director credentials or implicit pause. |
| TERM-02 | M1 | Submit a staffing, hauling or repair request through graphical/command views. They share validation and show delivered/accepted/acknowledged/executing/completed states accurately. Retry/save/load cannot duplicate effects; accepted relief does not mean physical coverage is already restored. |
| TERM-03 | M1 | Receive the synthetic outside-anomaly report with source, times, location uncertainty, attachments and claims. Read, acknowledge and refer/request assessment without inventing a playable town, hidden true location or deployed team. |
| TERM-04 | M1 | Draft/revise a permitted dossier and attach an existing game-world record. Preserve authorship/revision/provenance; textual edits do not change physical containment and upload does not access the host computer. |
| TERM-05 | M1 | Lose a SCiPnet route while the device stays powered. Mark cached data and local queued commands; reconnect and revalidate expiry/authority/capacity. Unpowered and air-gapped devices do not retain network functions by UI accident. |
| TERM-06 | M1 | Focus a qualified live camera task, change apps/attention or lose its feed, and inspect SCP-650 observation/profile. Its human-only baseline excludes AIC-only monitoring while retaining the AIC's attributed reports. Only real module-approved attention counts; background screenshots, mail and remembered positions do not hold the anomaly stable. |
| TERM-07 | M1 | Open an authorized test record/attachment and emit scoped inert exposure/check events. The proposed inoculation status does not neutralize all hazards; a misleading/anomalous view cannot run host commands or bypass actual permissions. |
| TERM-08 | M1 | Start with a resident AIC actor and physically located computing infrastructure. Demonstrate deterministic monitoring/reporting and an authorized task; lose host power/connectivity or revoke authority and prevent invalid action. Reports use available evidence; a physical repair still requires actual staff/tools/work. Resolve the detailed authority profile before certifying specific behavior. |
| TERM-09 | M1 | Exercise the proposed versioned JSON decision-provider boundary with a deterministic/mock provider: validate an allowed intent and reject invalid, stale, unauthorized and wrong-branch output. Timeout must not block simulation; accepted decisions survive save/load without duplicate execution. A real local model and regional AIC support remain later integrations. |
| CONT-01 | M1 | Trigger avatar death under reload and permanent-ending profiles. Apply the selected policy, retain ordinary losses in the current save, and avoid an unconditional campaign reset or forced successor. |
| CONT-02 | M3 | Continue after death as an eligible living successor. One controlled actor remains; original casualties/possessions persist; the successor retains their own rank, knowledge, location and gear. Missing candidates follow the configured fallback. |
| CONT-03 | M3 | An authorized synthetic anomaly restarts a compatible whole-world day checkpoint once, with explicit retained memory/cost. All snapshot state and RNG restore into a new branch, abandoned async results are rejected, no gear/charge duplication occurs, and an unauthorized request fails without mutation. |
| SC650-01 | M1 | Begin with valid staffed containment, then reduce staffing below procedure minimum while one eligible observer remains. Report understaffing but do not relocate or confuse it with total observation loss. |
| SC650-02 | M1 | Remove the last qualifying observer. Relocate only to a valid unobserved near/behind-personnel candidate in facility scope, without pathfinding; reject collision/out-of-scope/observed candidates and handle no-candidate retry without zero-time loops. |
| SC650-03 | M1 | Compare a focused human's live emergency camera observation with AIC-only monitoring, an unattended camera, failed feed, old recording and remembered report. Only the declared human-eligible chain suppresses relocation under the baseline; losing its last human observer still permits relocation while the AIC monitors/reports. Attention, power and coverage changes take effect at the correct time boundary. |
| SC650-04 | M1 | Recontain with observed transport personally and through delegated staff, and exercise a controlled-relocation alternative using normal candidate rules. Room/coverage predicates remain real; an interruption preserves the statue's single physical parent/location and actual procedure progress. |
| SC650-05 | M1 | Inspect approximately six discrete module-selected poses. They do not cycle as animation frames; relocation itself emits no gameplay sound; first discovery triggers the authored witness reaction once per encounter policy without a statue attack AI. |
| SC650-06 | M1 | Preserve coverage to prevent a scheduled-fault breach, then exercise a real failure. Remove the mission wrapper: module behavior persists. Successful stabilization does not disable it, and failed recontainment leaves real location, staff, inventory and reports. |
| SC650-07 | M1 | Save before last-observer loss, after relocation and during observed transport/relief. Resume under different render rates and acceleration; pending events, observer eligibility, RNG, pose and unique physical location match uninterrupted execution. |
| CTX-01 | M2 | Equip/remove SCP-1499 through ordinary slot/access/timed-action rules in the same saved facility as SCP-650. The physical profile transfers and returns exactly one actor/mask/inventory set through valid anchors. |
| CTX-02 | M2 | The separate perception profile gives one observer an altered context with versioned target bindings. A perceived equipment action causes one persistent mapped ordinary-world effect; other observers retain their permitted physical evidence. Stale/invalid mappings cannot double-resolve or reveal hidden truth. |
| CTX-03 | M2 | Generate seeded terrain, towers/structures and bounded SCP-1499-1 populations with independent perception/action responses. Change the area, leave/unload, save/reload and revisit without rerolling modified locations or replacing entity identities. |
| CTX-04 | M2 | Save active physical and perceptual profiles, then test removal, interruption, incapacitation and blocked return. Restore mappings, effects, knowledge, domain state and actual body/cargo location; use the declared recovery without duplicate actors or orphaned contexts. |
| CTX-05 | M2 | Make SCP-1499 remove/change one SCP-650 observer through shared location/perception services. A remaining eligible observer keeps it stable; losing all permits normal relocation. No pair-specific callback, stale observer, load-order or render-rate dependency is allowed. |
| NET-01 | M4 | Issue orders to both sites while controlling one avatar. Staff execute through NPC actions; the avatar's location and identity do not change when another site is inspected. |
| NET-02 | M4 | Transfer equipment and a qualified anomaly between sites. Source reservation, transit, escort/procedure, receiving capacity and arrival accounting use the same cargo/instance IDs. Interruption cannot duplicate cargo or clear the anomaly's hazards. |
| NET-03 | M4 | Run standing orders while the player is away. A manager handles an authorized repair/resupply and escalates an over-budget request or containment threat without unrestricted spending or authority. |
| NET-04 | M4 | Give the Group of Interest limited evidence, agents and stock. It chooses and executes an objective, can communicate or interfere through existing services, and cannot use unknown target locations or unowned resources. |
| NET-05 | M4 | Save during a shipment and intersecting group operation; resume, promote the encounter to local detail, and compare against uninterrupted execution. Physical consequences, reports, group knowledge and accounts agree without duplicate resolution. A region-departure summary uses recorded encounter/world intervals without double charging elapsed time, completing unperformed work or repeating on reload; any additional departure action follows its separately adopted timing policy. |

These rows define full milestone evidence. The engine-independent diagnostic reference generator and the [first runnable spatial proof](15-spatial-prototype.md) now exist, including bounded simulation checks and a Godot input/render smoke run. Their evidence covers subsets of the requirements; it does not certify the complete renderer, tactical system, employment handler, module host or whole milestone. Requirement completion must be reported individually against the implemented scope.

## 5. Acceptance procedure

Each milestone produces a build, exact content lockfile, test commands/scenario steps, evidence, known limitations, and updated decision records. Acceptance is against that version and its stated reference hardware.

Blocking defects include corrupted saves, duplicated/lost unique entities, mandatory hazard bypasses, unrecoverable progression failures, and failures of the milestone's mandatory requirements. Cosmetic defects can be listed and deferred with explicit agreement. A crash-free happy-path video alone is insufficient.

Engineering owner supplies evidence; product owner decides scope acceptance. The eventual project agreement must name those people, review period, payment conditions if any, and dispute handling. None of those terms can be inferred from this design draft.

## 6. Change control

Any requested addition after adoption gets a short change record:

```text
Change ID / requested behavior / player benefit
Affected requirement IDs and modules
New content or new engine capability?
Save, performance, authoring, UI and test effects
Estimate with assumptions and confidence
Work removed, milestone moved, or extra capacity authorized
Acceptance scenario and responsible owner
Decision and date
```

Clarifying a documented behavior is not automatically new scope. Adding a new capability family, play mode, fully simulated building category, complete SCP adaptation, runtime platform, multiplayer, or persistence guarantee is a scope change. Track defects against the adopted requirements so correction is not confused with expansion.

### Infrastructure extension — October 22, 2026

The user requires physical electrical/heating/cooling infrastructure so environmental containment can succeed or fail causally. Revision 0.9 proposes extending M1 beyond light/door outages to a bounded power/thermal/control proof (FAC-02–FAC-07), followed by ventilation and an environmental hazard fixture in M3 (FAC-08–FAC-09). This adds simulation, save, UI, authoring and verification work; no delivery estimate or extra runtime implementation is claimed. Reuse current slice population/rooms; full fluid dynamics, all utility types, full SCP-019 content and cross-zone network certification are not included by implication. Routing UI and exact bounds remain reviewable proposals in [the infrastructure contract](17-facility-infrastructure.md).

## 7. Deferred backlog

Local LLM dialogue/planning; complete employee-role campaigns beyond the bounded role/succession proofs; a native side-section facility view; structural construction; complete town interiors; vehicle driving; general 3D packing and moving loaded furniture; deep markets/production; extensive injury physiology; advanced clothing layers; mod marketplace; visual scripting; multiplayer; universal cross-mod behavior; arbitrary partial rewinds and temporal paradox simulation. Bounded checkpoint-based day restart is qualified in M3 rather than included in the first playable.

These remain visible ideas. They enter a milestone only through a specific scope decision.

The user's [component/material crafting direction](16-item-composition-and-crafting.md) is recorded for the shared inventory architecture: physical small contents, material portions, installed composition, assembly/disassembly, schematics/batches, derived effects, remnants and condition. Implementation fixtures/content are to be scoped separately; this clarification does not silently add a crafting system to M1.

## 8. Decisions still needed

| Decision | Working assumption | Needed by |
|---|---|---|
| Meaning of X4 | Confirmed: X4: Foundations translated into top-down SCP network management | Resolved by user clarification |
| Direct control | Confirmed: one embodied character, with autonomous staff under orders | Resolved by user clarification |
| Strategic scope | Confirmed: Foundation network, containment, logistics and Groups of Interest | Resolved by user clarification |
| Storage behavior | Confirmed: specialized/hierarchical layouts, physical access, console distribution, item filters, priority and consolidation | Resolved by user clarification; implementation bounds proposed |
| Sprite direction | Confirmed: detailed, visibly pixelated Fallout 2-inspired sprites, supported by user-supplied creature/tower/mask references; square logical tiles and fixed isometric/three-quarter camera; generated diagnostics | Retain pixel texture at gameplay zoom; exact angle, dimensions, pixel-preserving pipeline and direction/frame counts open |
| Facility infrastructure | Confirmed: electrical/heating/cooling and physical utility dependencies affect containment; bounded M1 power/thermal proof and M3 ventilation/hazard fixture proposed | Utility routing UI pending; capacities/thermal tuning before fixture certification; cross-zone links require qualified boundary policy |
| Employment behavior | Confirmed: primary shift roles above optional actions, role-specific duties and higher-rank orders with explicit overrides | Resolved by user clarification; staffing/doctrines proposed |
| Main audience and tone | Systems-driven RPG/management with horror incidents | M1 scenario design |
| Starting rank and authority | Confirmed: newly appointed regional operations director, one functioning site, command objectives/restrictions; later employee starts | Resolved by user; exact progression/policy details remain open |
| Time and field control | Confirmed: real-time exploration and turn-based local combat; all local simulation waits for input while world calendar, granular world and other areas continue; click/Ctrl-click and equipment functions | AP/initiative, action durations, zone membership and boundary policy require prototype qualification; exact bindings open |
| Future multiplayer timing | Deferred: unrelated zones continue; optional configurable decision timeout, with 30 seconds suggested as an example | No multiplayer delivery implied; expiry action, duration and disconnect policy remain open |
| Failure/continuation | Confirmed: difficulty/scenario-driven reload, successor, anomalous restart or ending | Resolved direction; exact preset combinations remain open |
| First/second anomaly | Confirmed: SCP-650 facility breach, followed by SCP-1499 in that facility | Resolved by user; detailed procedures/mappings proposed |
| Engine | Confirmed: Godot selected by user; exact release/configuration/export integration not qualified | Version/platform qualification at M0 exit |
| Integration and script runtime | Selected Godot .NET presentation and independent C# simulation; public script host still unselected | Version/runtime qualification at M0 exit |
| Actor positioning | Confirmed: hex-based actor movement with smooth simulation-time transitions, separate square construction grid and independent facing | Grid scale/registration, progress/occupancy semantics and implementation qualification at M0 |
| Visual/control blueprint | Confirmed: Fallout 2/Falltergeist-style representation, walk/run motion, turning and point-and-click feel implemented in Godot | Compare the first prototype; resolve exact cursor/button mapping before fixing affected controls |
| Digital operations lore | OSCP/SCiPnet and resident autonomous AIC confirmed; detailed authority, identity, exact technical department and canon/timeline questions pending | Before affected behavior is committed |
| Platform and reference hardware | Desktop mouse/keyboard; OS and hardware unselected | M0 exit |
| Presentation/art budget | Detailed visibly pixelated art, user-supplied references and fixed isometric/three-quarter field view confirmed; resolution, production/filtering method, exact angle and budget open | Art qualification and M1 production planning |
| World-map travel presentation | Fallout/Wasteland-inspired direction; contextual departure summary proposed to explain elapsed world time without double charging it | Clarify whether departure adds actual timed work; routes/encounters remain later world/network design, with no M1 expansion |
| Team, budget, schedule | Unspecified; do not infer dates | Before a delivery commitment |
| Distribution/license strategy | Unselected; preserve provenance from the start | Before distribution |

The current baseline is complete enough to review the architecture and quote/prototype M0. It is not complete enough to promise a production release date or a fixed price for the entire vision.
