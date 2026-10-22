# Game design baseline

Status: Baseline revision 0.7. Confirmed direction includes an initial regional-director start, later employee starts in the same simulation, equipment-based click controls, real time with pause/acceleration, configurable continuation, SCP-650 first and SCP-1499 second. Godot, square logical tiles, a fixed isometric/three-quarter field view and detailed Fallout 2-inspired sprites are selected. Earlier network, employment, storage and diagnostic choices remain. Implementation contracts and numeric limits remain proposed.

## 1. Player promise

You are a person living inside a functioning Foundation network. You have a body, possessions, a job, relationships, and the authority of your current role. The initial director can direct sites, teams and logistics through people and communications. Later employee starts can investigate, work and interact within their own access and responsibilities. Everyone inhabits the same simulation; player control itself grants no management privilege. Others' work changes the world you later walk through.

The reference is X4: Foundations translated into an isometric SCP world: personal participation sits inside an ongoing network simulation. Management expands across locations while the player remains a person who must travel, obtain information, delegate responsibility, and live with consequences. The target game includes interactions with autonomous Groups of Interest and the operation of multiple Foundation sites.

The confirmed visual reference is Fallout 2, with a fixed isometric/three-quarter sprite view over square logical tiles. Its detailed character/environment sensibility guides the art, while Fallout/Wasteland-style world-map travel is a later presentation reference. These references coexist with the real-time X4-like simulation and existing facility/procedural systems; they do not silently replace controls or add another milestone. The terminal remains OSCP; “THOSC” was corrected by the user.

The defining tension is how to keep people and the site functioning while dealing with phenomena that invalidate ordinary assumptions. Better preparation, observation, training, supplies, and infrastructure create options. They never guarantee universal containment.

The player should be able to say: “I sent that team; this is the equipment they used; this is the room we built; this is why the incident happened.” Reports must preserve that causal connection.

## 2. Core loop

1. Receive an incomplete report, notice an anomaly, discover a maintenance problem, or identify a Group of Interest operation.
2. Investigate through fieldwork, staff, records, and controlled experiments.
3. Prepare a plan: assign people, equipment, transport, containment, and abort conditions.
4. Execute personally, delegate, or move between those roles when physically possible.
5. Handle consequences: rescue, injury, repairs, secrecy, lost equipment, containment, and new information.
6. Improve procedures, infrastructure, staffing, and network coverage, then return to daily operations.

Across several incidents, the strategic loop is to discover threats and opportunities, establish local presence, allocate responsibilities between sites, secure supply and transport routes, negotiate or counter other groups, and adapt the network to the consequences. Expansion increases coverage and capacity while adding staffing, secrecy, supply, and containment obligations. It is not automatically the best response to every problem.

A normal shift includes work, meals, rest, resupply, conversation, and interruptions. These routines supply context and constraints. They do not each require a separate minigame.

## 3. Four ways to interact

| Interface | Player activity | Authoritative result |
|---|---|---|
| A. Personal RPG | Walk, inspect, equip, talk, observe, fight, repair, participate in operations | Character actions and perception |
| B. SCP terminal | Review local/network reports, communicate, delegate site policy, assign teams, authorize tests, order supplies | Orders, messages, reservations, records |
| C. Facility management | Inspect rooms and networks, plan construction, assign work, prioritize maintenance | Blueprints and work orders in physical space |
| D. World simulation | NPC schedules, travel, demand, incidents, anomaly behavior | Persistent state changes; this is the backend, not a fourth mandatory UI |

Opening inventory or a terminal does not inherently pause the world. Ruleset-permitted explicit pause freezes simulation everywhere; acceleration and optional interruption pauses apply to the same clock. A terminal view does not grant the avatar supernatural awareness, movement, or immunity. Site access, communications, authority, and physical location still constrain available actions.

Field play uses isometric Diablo/RTS-style click commands. Click requests movement/contextual interaction; Ctrl-click designates an explicit target/position or forced action for an appropriate selected function. Abilities primarily come from actual equipment, with source position, access, readiness and resources shown. A backpack tool is not instantly usable just because it has a quick slot. [Player controls and time](08-player-controls-time-and-continuation.md) defines command validation and information limits.

The facility section view must derive rooms, doors, floors, equipment, and work progress from the same building representation used in A. Initial facility management can use a floor-plan overlay; the XCOM-like section presentation comes after this correspondence works.

The terminal design uses OSCP as the device/session shell and SCiPnet as the intranet/service layer. Records, mail, containment alerts, outside reports and validated orders share the same world state. The starting facility has a resident AIC: digital personnel with physically hosted computation, duties, bounded knowledge and clearance. Deterministic behavior works initially; an optional local-model JSON provider can follow later. Technical staff and service incidents remain part of employment/physical operations. [The OSCP pass](11-oscp-terminal.md) and [lore register](13-lore-and-digital-operations.md) record references and open choices; ask before fixing ambiguous canon or authority.

## 4. Roles and progression

The initial campaign starts as a newly appointed regional operations director responsible for one functioning site's staff, equipment, finances, research, security and containment. Foundation command supplies objectives and restrictions. The director can participate in fieldwork and expand the assigned network.

Later SS13-inspired starts include security, researchers, maintenance, medical and cafeteria employees. Scenario data sets the starting role, resources, access, responsibilities and opportunities. These reuse employment and ordinary permissions rather than separate game physics. Management remains a core supported play style without being assumed for every starting character.

The player issues intents and orders to other people; direct character control stays with the avatar. Team members and site managers execute delegated tasks through normal NPC actions. Opening their status screen does not possess them or move the avatar. The player may work from a terminal, accompany transport, or join a field team; travel time and conditions remain part of the same world.

Progress comes from training, clearance, relationships, researched procedures, better equipment, and site capacity. Character skill affects action quality and available tactics; clearance controls authorization. Neither substitutes for player knowledge of a hazard.

Death or incapacitation causes persistent consequences according to scenario/difficulty policy. Supported continuation types include permitted reload, permanent ending, an eligible successor and module-authorized anomalous restart. M1 proves reload/permanent-ending policies; successor and whole-day checkpoint proofs are staged later. A successor keeps their own identity, knowledge, gear and permissions; a bounded day restart uses a whole-world checkpoint and explicit retention policy. Arbitrary partial rewinds are not implied. See [continuation](08-player-controls-time-and-continuation.md).

### Foundation network and Groups of Interest

A site is a network node with a physical location, responsible manager, personnel, capacity, stock, funding commitments, containment responsibilities, and local intelligence. Other node types may include field offices, depots, checkpoints, and temporary outposts. They reuse bounded templates rather than each becoming a separate management game.

The player can assign a site manager standing orders: staffing priorities, minimum reserves, maintenance thresholds, incident escalation, and permitted deployments. Managers act within authority and budget. Requests beyond their limits escalate to the player. Routine activity must continue while the player is travelling or handling another incident.

Connections represent transport and communication capabilities, with routes, travel times, capacity, availability, and risk. Sending specialized equipment or a contained SCP reserves the actual cargo and personnel; it leaves the source, travels, and arrives or suffers a reported interruption. A destination must have capacity and a valid receiving procedure. Network inventory is a summary of located stock, not a shared bag accessible from every site.

Groups of Interest have their own objectives, knowledge, assets, relationships, and operational limits. Their plans may involve discovery, acquisition, research, trade, concealment, rescue, interference, or attack, as appropriate to the group's authored profile. Cooperation, negotiation, competition, and conflict are possible. A group's behavior should emerge from its goals and means rather than a mandatory periodic raid.

Group-level planners assign missions to their own agents using the same routes, information limits, resource commitments, and physical action services as other organizations. Groups do not know every SCP location automatically or spawn unlimited resources. Strategic abstraction is allowed; effects that intersect the player or another operation must resolve into compatible entities and actions.

The player learns about these activities through sightings, reports, contact, intercepted evidence, and discoveries. Strategic displays show known or estimated information with source and age. Broad omniscient faction intelligence is outside the default scenario.

A complete campaign needs a bounded multi-site network and at least one functioning Group of Interest loop. The first playable remains local so the personal/management foundation can be tested. A separate network milestone proves two sites, logistics, delegated managers, and one group before expanding content.

## 5. Teams and operations

The first playable is [SCP-650 containment breach](09-first-playable-scp-650.md), entirely inside the starting facility. Routine employment, observation, equipment, communication and room systems create the conditions; the SCP module owns anomalous behavior. The player may participate or delegate. [SCP-1499](10-world-perception-and-scp-1499.md) follows in the same facility to qualify procedural populations and physical/perceived-context effects. Off-site containment becomes a later operation type.

An operation contains an objective, assigned personnel, equipment manifests, route, communications plan, rules of engagement, known hazards, containment procedure, and withdrawal conditions. Staff can refuse or fail an action because of injury, missing tools, fear, authority, or unavailable routes.

Useful objectives include investigate, observe, establish a perimeter, evacuate, recover an object, rescue, transport, and maintain containment. “Contain” must resolve to a module-defined procedure. Some phenomena can only have access controlled or their effects mitigated.

The player can join a team by reaching it and taking an available role. There is no instant teleport to a remote operation. Terminal orders can request a rendezvous, transfer leadership, or withdraw. A distant incident may finish before the player arrives.

Delegation uses the same hazard and action rules as personal play. A team's report reflects its knowledge and communications. Staff can be mistaken, a radio can fail, and a report can arrive late. Developer diagnostics retain the actual cause separately.

## 6. Site economy and construction

Manage a bounded set of resources: operating funds, staff hours, electrical capacity, construction materials, medical supplies, food, ammunition, and specialized containment equipment. Purchases reserve funds; delivery takes time and may fail. Equipment exists as physical items or inventory batches with explicit conversion to individual items.

Rooms declare required functions, access, capacity, utilities, maintenance needs, staffing, and hazards. Restrooms, barracks, stores, laboratories, medical rooms, workshops, and containment rooms are templates with variants. A containment room's label alone does not provide containment: installed systems, staffing, operational procedure, and the SCP's actual conditions determine effectiveness.

Construction proceeds through blueprint, validation, reservation, delivery, work, inspection, and activation. Blocked exits, absent utility connections, interrupted supplies, or dangerous working conditions can prevent completion. Demolition must account for occupants, stored items, networks, and escape routes.

The first playable economy uses scheduled suppliers and fixed catalog prices. The network milestone adds transfers, receiving capacity, delegated site budgets, and interruptions involving a Group of Interest. Deep regional production chains, simulated markets, and factories remain separate expansions.

## 7. Procedural world

Generation composes reusable spatial templates under constraints. A town's plan determines lots, roads, utilities, building counts, population capacity, and connections. Buildings generate floors and rooms within their allotted envelopes; rooms place furniture and usable fixtures while preserving access.

The full town example is one inn, one gas station, two small stores, one large store, and 120 houses. This is a configurable settlement preset, not the minimum size of every test map. A failed count or missing required connection produces a clear generation failure rather than silently deleting a building.

A motel can reuse a bedroom/bathroom vocabulary also used by houses. Shared templates accept context: dimensions, quality, occupancy, plumbing sockets, access requirements, material palette, and wear. Reuse should produce consistent function with variation in layout and appearance.

Ordinary interiors fit their building's physical envelope. A spatial anomaly must explicitly create an exceptional domain or connection. It cannot accidentally exploit an ordinary generation error.

## 8. NPCs and optional language models

NPC state separates appearance, identity, persona, competence, condition, knowledge, relationships, obligations, and current intent. Appearance includes body shape, age presentation, skin and hair palettes, clothing, and animations. Persona includes cautiousness, sociability, patience, diligence, assertiveness, curiosity, and stress response.

Personality values influence preferences; they do not prescribe every decision. Appearance and demographic identity do not determine moral character or competence. Skills, role, history, and situation provide those factors.

A work handler owns primary employment: role, workplace, shifts, duties, breaks, equipment, coverage and relief. Cafeteria staff work in the cafeteria during their shifts even if optional hauling/cleaning actions are disabled; moving ingredients and cleaning that workplace are role duties. Guards, janitors, scientists, lab technicians and logistics workers have distinct duty graphs. A blocked role reports its missing prerequisite rather than becoming available for arbitrary optional work.

A separate action-job handler assigns discretionary tasks only when the employee is eligible and released from primary duty. Shared deterministic action primitives handle walking, opening doors, fleeing, hiding, calling for help, taking cover, observing, carrying, treating, and repairing. A gunshot can trigger a role-aware threat response without a language-model call.

Higher-rank orders are evaluated against command authority, current assignment, clearance and condition. Valid reassignment/emergency orders can interrupt duty; incapacity, mandatory anomaly rules, or sufficiently strong fear can prevent compliance. Mild fear does not automatically defeat trained behavior. Security duties include coordinated roles, acknowledgement, cover, escort, readiness and real relief handoffs. [Employment and orders](07-employment-and-orders.md) defines arbitration, shift coverage and failure reporting.

An optional local LLM may later choose a high-level intent, produce dialogue, or summarize perceived information. It receives only the NPC's permitted knowledge and valid controls. Every proposed action is checked by ordinary game rules. Timeouts, invalid output, or an absent model use the normal controller. Model responses are recorded if they affect state; the simulation never depends on regenerating the same text during load.

## 9. Clothing and inventory

Items have stable identity, type, shape, mass, condition, ownership, location, and relevant components. Clothing occupies layers and body slots: underwear, socks, footwear, legs, torso, outerwear, belt, hands, head, and face. Scenario equipment may add vest or harness slots.

A garment can provide warmth, protection, encumbrance, visibility, access constraints, and named containers. A coat has separate pockets; a backpack is another container. Carried gear appears in equipment and inventory visuals; visible world representations are limited to the release's declared art set.

Storage regions use the layout appropriate to their function: rotatable item grids, gun-shaped mounts, box bays, mixed shelves, or layered pallet stacks. The same model applies to the avatar, other personnel, vehicle compartments, and facility furniture. Each region has native acceptance rules, physical capacity, configurable filters/priority, and an access state. A long-gun rack can accept only compatible long guns even if a smaller object appears to fit.

Containers can contain other containers. A cabinet holds boxes whose interiors hold items; remaining shelf area can hold loose equipment on another grid scale. Both views use the same physical shelf capacity, preventing overlap or double-counted space. The working pallet example has nine positions stacked three high: 27 boxes, with only the topmost box in each occupied column directly accessible. Retrieving buried stock requires real unstacking and staging work.

Local play exposes these layouts for direct manipulation. Console orders automatically distribute stock through hauling personnel. Highest-priority eligible storage wins; at equal priority, a compatible box already holding the same item is preferred before opening another. Staff follow fit, access, security, reservations, and carrying limits. Expanding storage requires building and placing fixtures with usable access clearances. Valid existing stock stays put unless a policy or explicit order calls for redistribution.

General storage is an acyclic hierarchy with a published nesting limit. M1 proves cabinet → box → contents and vehicle cargo → case → contents, while keeping wearable combinations simple. No bags-inside-bags feature is needed for that first slice. [Physical storage and delegated logistics](05-storage-and-logistics.md) defines capacity mapping, access, prioritization, and job execution in detail.

Individual transfers validate location, authority, reachability, destination space, reservations, and access, then move atomically. A full haul has separate pickup, carry, and placement states: while in transit the item is physically with its carrier. An invalid move leaves the item at its current location. Wearing, dropping, dying, transforming, or teleporting cannot duplicate possessions. Warmth uses a simple combined rating initially; detailed wetness, fabric chemistry, and body-temperature physiology are deferred.

## 10. Knowledge and fairness

The simulation knows what happened; a character knows what they perceived or were told. Terminal displays, anomaly descriptions, risk estimates, and reports use the latter, including source, time and uncertainty. Friendly personnel and surveillance can deliver information without granting direct sight, current tracking or qualifying observation of an anomaly. Unknown anomalies reveal their rules through evidence and research.

Physical location, perceived context and knowledge are distinct. A module can transfer a body, alter an observer's apparent environment/identities, or map actions to consequences in another declared context through host capabilities. The engine validates and persists actual effects. Modules are not restricted to isolated personal maps or cosmetic hallucinations.

Observation mechanics use in-world eyes, attention, lighting, obstruction, and module rules. Seeing an object in the isometric camera is not automatically eye contact. The UI shows the character's facing, observation target, relevant obstruction, and blink warning so lethal outcomes are understandable.

## 11. Sprite presentation during development

Use detailed, textured Fallout 2-inspired sprite art in a fixed isometric/three-quarter view. The classic PC RPG reference is confirmed; low-color NES/RPG Maker-style art is not the intended target. Square tiles define construction/navigation, while sprite anchors, bounds and layers support taller and multi-tile artwork. Generate diagnostic stand-ins for every declared illustration, state, direction and animation frame before art is available. World categories have distinct stand-in colors: landscape/map base, map objects, building tiles, objects, NPCs and player.

Character placeholders show the NPC's name, state/frame such as `walk:2/3`, and a green edge bar projected from world facing into screen space, with the world-direction label retained. The existing generator's north-up sample remains a diagnostic reference; the game uses the confirmed fixed isometric projection. Frame totals are declared data. The marker follows authoritative perception-facing rather than movement velocity, so stationary observation and backward movement remain understandable. Finished art replaces visual bindings without changing game rules; diagnostics remain inspectable.

[Sprite presentation and placeholders](06-sprites-and-placeholders.md) defines the contract and links an engine-independent generated reference sheet. Palette, direction count and sample frame totals are proposed implementation choices.

## 12. Explicit boundaries

The vision supports further SCPs, sites, scenario roles, and settlement templates. The initial product does not include multiplayer, a continent simulated at individual-item detail, universal destructibility, vehicle driving, a general manufacturing economy, full social-life simulation, or every SCP interaction ever written.

Every new feature must name the loop it improves, the systems it affects, its acceptance scenario, and what it displaces in the current milestone. Mod support expands content within published capabilities; engine capability additions remain separately scoped work.
