# SCP: Welcome to Site-[REDACTED]

Design baseline, revision 0.10 — October 23, 2026.

An isometric SCP world simulation in the spirit of X4: Foundations. The initial scenario places the player in one character as regional operations director of one functioning site, with network expansion and autonomous Groups of Interest ahead. Later employee starts inhabit the same world with their own authority and responsibilities. Personal action, delegation, facility operations, and the terminal all affect the same people, places, equipment, and incidents.

This workspace now contains a runnable Godot spatial/observation, personnel-recovery and tactical-training prototype with a native SCP-650 module alongside the design baseline, diagnostic sprite generators and terminal design preview. It is not yet a complete game or completed M0 milestone. Godot .NET presentation uses an independent C# simulation core; this proof is pinned to Godot .NET 4.7.1 and .NET SDK 8.0.128, with broader integration/performance still to qualify. Confirmed choices remain separate from proposed contracts and limits. No delivery date, budget or legal agreement has been selected. Work is tracked in local Git.

Run the first scene from this directory:

```bash
python3 tools/run_spatial_scene.py
```

The launcher handles this workspace's path characters through a temporary build copy. See [the spatial prototype guide](docs/15-spatial-prototype.md) for dependencies, controls, verification, the Fallout mapping comparison and implementation limits.

## Read in this order

1. [Game design](docs/01-game-design.md): player experience, systems, and boundaries.
2. [Simulation and procedural world](docs/02-simulation-and-world.md): persistent state, generation, observation, and background activity.
3. [SCP module contract](docs/03-scp-module-contract.md): loading, capabilities, lifecycle, scripting, persistence, rule changes and staged anomaly tests.
4. [Scope and acceptance](docs/04-scope-and-acceptance.md): bounded playable slice, milestone gates, and change control.
5. [Module authoring form](templates/scp-module-form.md): reusable specification for an individual SCP.
6. [Architecture decision](docs/adr/0001-shared-simulation.md): alternatives and tradeoffs.
7. [Physical storage and delegated logistics](docs/05-storage-and-logistics.md): shaped slots, mixed cabinet layouts, nested boxes, pallet access, and automated hauling.
8. [Storage architecture decision](docs/adr/0002-physical-storage.md): shared placement and access rules for local and console interactions.
9. [Sprite presentation and placeholders](docs/06-sprites-and-placeholders.md): modern sprite direction, generated state/facing/frame diagnostics, and production-art replacement.
10. [Employment and orders](docs/07-employment-and-orders.md): primary shift duties, optional tasks, command authority, fear and coordinated staff behavior.
11. [Player, controls, time and continuation](docs/08-player-controls-time-and-continuation.md): director/employee starts, equipment-based mouse controls and difficulty-dependent failure policies.
12. [First playable: SCP-650](docs/09-first-playable-scp-650.md): facility breach, observation, discrete poses and two recontainment approaches.
13. [Perceived contexts and SCP-1499](docs/10-world-perception-and-scp-1499.md): second module, procedural population and physical/perceptual consequences.
14. [Physical/perceived context decision](docs/adr/0003-physical-perceived-contexts.md): architecture options and bounded proofs.
15. [OSCP and communications](docs/11-oscp-terminal.md): devices/sessions, SCiPnet, applications, outside reports, records and receipts.
16. [Godot selection and integration](docs/adr/0004-godot-and-simulation-core.md): the selected engine and proposed independent-kernel boundary.
17. [Development workflow](docs/12-development-workflow.md): Git source policy, ignored runtime/cache data and local commits.
18. [Lore and digital operations](docs/13-lore-and-digital-operations.md): SCiPnet, AIC/AIAD, technical responsibility, sources and open questions.
19. [Fallout 2 placement research](docs/14-fallout-2-placement-research.md): separate movement/terrain grids, sprite anchors, visual offsets and implications for our movement-model decision.
20. [Falltergeist comparison and retained Godot architecture](docs/adr/0005-retain-godot-after-falltergeist-review.md): the delegated engine decision, inspected code and first prototype boundary.
21. [Continuing world and local tactical turns](docs/adr/0006-real-time-exploration-and-tactical-turns.md): exploration/combat time split, local waiting, continuing other areas and deferred multiplayer timeout option.
22. [Runnable spatial prototype](docs/15-spatial-prototype.md): first Godot scene, reference projection, collision overlays, movement, doors and bounded timing/save checks.
23. [Item composition and crafting](docs/16-item-composition-and-crafting.md): future physical parts/materials, installed versus contained state, assembly/disassembly, derived properties and batches.
24. [Facility infrastructure](docs/17-facility-infrastructure.md): physical power, heating/cooling, controls, maintenance and environmental containment, with an SCP-019 source check and staged proofs.
25. [Awareness and encounter entry](docs/18-awareness-and-encounter-entry.md): shared sighting/report movement stops, last-known contact cues and qualified tactical-entry requirements.
26. [Personnel orders and recovery](docs/19-personnel-orders-and-recovery-prototype.md): the runnable voice/OSCP coordination, physical trolley recovery and operation save proof.

27. [Event log and tactical training](docs/20-event-log-and-tactical-training.md): bounded color-coded journal, turn/AP controls, autonomous training opponents and saved action continuations.

28. [Independent gameplay modes](docs/adr/0007-independent-gameplay-modes.md): standalone OSCP, world-map, local-play and facility-management entry points using shared simulation services, with staged integration and lifecycle qualification.

[Simulation code map](src/SiteSimulation/README.md) identifies the movement, perception, anomaly, diagnostics and persistence boundaries. The scene starts in a closer direct-sight field view with textured SCP-650 poses and restrained discovery feedback. **F2** opens diagnostics: **SCP-650** shows observation reasons and module flags; **Trace** shows causal events. **J** toggles the avatar's explicit module-owned complacency flag; its automatic metric is undecided. **F4** returns diagnostic statue art, **M** mutes audio and **R** reduces effects. **O** changes Chen's attention, **K** blinks the avatar, **F8** changes fixture lighting and **F6** holds local progress. This uses native C# rules; public sandboxed scripts and full containment procedures remain future work.

[Generated sprite reference sheet](assets/development/diagnostic-sprites/reference-sheet.png) and [reference generator](tools/generate_diagnostic_sprites.py) demonstrate the placeholder convention independently of an engine.

[User-supplied art references](assets/references/scp-1499-style/README.md) preserve the SCP-1499 creature, tower and mask examples that clarify the desired detailed, textured pixel-sprite appearance.

[Local SCP archive guide](source_material/README.md) describes the user-supplied JSON reference checkout, targeted article lookup and separation from the game's Git history and actor knowledge.

Click-to-move approaches blocked destinations, routes around obstacles when possible and checks actual collision on each step; unseen occupants do not reject clicks in advance. The normal scene starts three actual observers on duty. **E** opens nearby access; field controls issue voice orders. Approach the marked terminal and press **T** for the bounded OSCP command/document panel. The recovery guide above explains physical trolley work and receiving conditions.

The field event log retains 100 messages with five visible rows. Scroll for history and hover truncated messages for full text. The right panel can arm a nonlethal training drill; **V** readies the sidearm, click a visible opponent to fire, **Y** reloads and **Enter** ends the turn. Movement, facing and door use have AP costs during combat. See the training guide for provisional values and fixture limits.

## Confirmed direction

- “X4” means X4: Foundations as the reference for personal participation alongside network management, translated into SCP settings and top-down play.
- The player embodies one character. “First person” describes that character's participation in the world; the visual camera is fixed isometric/three-quarter.
- Managing the Foundation network and dealing with Groups of Interest are core product pillars. A single site is the first implementation slice, not the whole game's intended scale.
- Personal, personnel, vehicle, and facility storage share physical inventory rules. Console distribution is delegated work over those same containers, with filters, priorities, consolidation, and access constraints.
- Future crafting builds from actual parts/material amounts and their properties through those same inventory transactions. Installed components stay sealed until supported disassembly; explicit service access remains an open detail; material portions, batch schematics, remnants and equipment condition retain physical accounting. This records architectural direction without adding crafting to the current prototype.
- Godot is the game engine. Square logical tiles support a fixed isometric/three-quarter view. Detailed, visibly pixelated Fallout 2-inspired sprites and textured environments are the finished visual target, clarified by the user's SCP-1499 references. Retain authored pixel texture at normal gameplay zoom. Generated development stand-ins identify category, name, state/frame and facing with a green direction marker; finished artwork uses the same visual bindings.
- Actor movement uses a hex grid with smooth transitions during exploration and committed tactical moves, with independent facing; construction/floor tiles remain square. Use Falltergeist/Fallout implementations as technical references; the game uses Godot and its own authoritative simulation rather than a Falltergeist fork.
- Fallout 2/Falltergeist is the confirmed blueprint for the on-screen aesthetic, movement, controls, animation and exploration/combat time split. Implement it in Godot alongside the persistent management simulation; exact cursor/button mappings and tactical tuning remain to be specified.
- Physical facility infrastructure is required: electrical supply/distribution, heating/cooling and later qualified utility services create actual operating conditions and containment risks. Sensors, controls, backup capacity and physical maintenance matter. The proposed first power/thermal proof is in M1; current F8 lighting is only a debug switch. Utility-routing UX remains under discussion.
- Newly sensed anomalies can stop the avatar exactly in place and clear click-to-move travel without pausing the world. Independent self/report settings default on; communicated sightings require actual delivery. This is shared awareness behavior. Qualified ordinary hostile engagements automatically enter local tactical combat; ranges use world hexes and contact arrows use acquired evidence, not hidden transforms. Unseen lethal anomalies can act without warning or a guaranteed player turn. The scene is revealed after committed death, without granting actor knowledge. A bounded AP/initiative/training-equipment proof now runs. Full ordinary combat, lethal-anomaly timing, aftermath reveal and general remote communications remain future work.
- Primary employment is handled separately from optional action jobs. Shift duties take precedence over discretionary work; authorized orders and explicit emergency/condition overrides remain meaningful.
- Start as a newly appointed regional operations director with one functioning site, Foundation-command objectives and restrictions. Later scenarios can start in ordinary employment; management authority is not inherent in player control.
- Real-time exploration/operations with ruleset-controlled explicit pause and acceleration; turn-based tactical combat. All local simulation waits while choosing, including movement/actions, work, bleeding, blinks and SCP timers. The world calendar, granular wider simulation and other areas continue. Inventory/terminals do not inherently pause the world. Field commands retain equipment functions, click/Ctrl-click and physical access.
- M1 is an SCP-650 breach inside the facility. Six approximate pose illustrations are discrete states. Both observed transport and controlled relocation are accepted approaches.
- Observer eligibility is module/scenario-configurable: human, AI or any eligible observer, with senses/media/attention evaluated separately. SCP-650's baseline uses humans, including qualified human-attended live feeds; AIC-only monitoring reports information without suppressing relocation. The article does not establish AI-only eligibility.
- M2 adds SCP-1499 in the same facility and qualifies physical transfer plus a bounded perception/consequence profile. Death/continuation and checkpoint-based anomalous restart are configurable policy types.
- OSCP/SCiPnet is the lore-favored device/network interface for management, records and communications, including outside-anomaly reports. The starting site has a resident AIC with physical computing infrastructure, bounded knowledge/authority and deterministic duties. Preserve a JSON provider interface for optional local-model integration and later regional AIC support. Exact authority remains under discussion; technical staff retain real operational duties.
- Ask the user about unresolved behavior/lore ambiguities before settling them. Do not silently combine source-specific timelines or make proposed counts permanent.
- Use Git for version control. The initial local commit preserves the earlier design and diagnostic fixtures.

## Working assumptions

- Single-player desktop and mouse/keyboard are the proposed platform/input envelope; exact bindings, hardware and supported OS remain open.
- Fixed isometric/three-quarter presentation is primary. A facility section view is a later projection of the same spatial model. Exact projection angle, sprite scale, production pipeline and finished direction/frame counts remain open.
- Fallout/Wasteland-style world-map travel is a presentation reference for the later network/world stage; travel timing, encounters and detailed interaction still need design. It does not expand the first facility-only milestone.
- Multiplayer remains later; retain compatibility with independently running zones and a possible configurable turn timeout, with 30 seconds proposed as an example rather than a fixed initial rule.
- M0 proves feasibility; M1 qualifies SCP-650; M2 adds SCP-1499; M3 qualifies broader anomalies/continuation/world generation; M4 proves the bounded network; M5 scopes release. Counts and performance limits are proposed, and broader fixtures are not promised launch campaigns.
- LLM support is an optional later decision provider. The world runs fully without it.
- A bounded network prototype follows the local-play and anomaly-architecture gates, before a production release is scoped.

The spatial subset of M0 and a bounded SCP-650 coordination/recovery proof are runnable. Continue qualifying the remaining M0 simulation, module-host and domain-transfer boundaries before treating the first facility scenario as implemented.
