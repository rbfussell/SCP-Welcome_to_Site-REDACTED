# SCP: Welcome to Site-[REDACTED]

Design baseline, revision 0.6 — October 22, 2026.

A top-down SCP world simulation in the spirit of X4: Foundations. The initial scenario places the player in one character as regional operations director of one functioning site, with network expansion and autonomous Groups of Interest ahead. Later employee starts inhabit the same world with their own authority and responsibilities. Personal action, delegation, facility operations, and the terminal all affect the same people, places, equipment, and incidents.

This workspace contains a design baseline, diagnostic sprite generator and terminal design preview, not an implemented game. Confirmed choices are separate from proposed contracts and numeric limits. Godot .NET with an independent C# simulation core is the recommended engine candidate, pending adoption/M0 qualification. No delivery date, budget or legal agreement has been selected. Local Git version control is initialized on `main`.

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
16. [Godot recommendation](docs/adr/0004-godot-and-simulation-core.md): Godot versus a custom engine and the independent-kernel boundary.
17. [Development workflow](docs/12-development-workflow.md): Git source policy, ignored runtime/cache data and local commits.
18. [Lore and digital operations](docs/13-lore-and-digital-operations.md): SCiPnet, AIC/AIAD, technical responsibility, sources and open questions.

[Generated sprite reference sheet](assets/development/diagnostic-sprites/reference-sheet.png) and [reference generator](tools/generate_diagnostic_sprites.py) demonstrate the placeholder convention independently of an engine.

[Local SCP archive guide](source_material/README.md) describes the user-supplied JSON reference checkout, targeted article lookup and separation from the game's Git history and actor knowledge.

## Confirmed direction

- “X4” means X4: Foundations as the reference for personal participation alongside network management, translated into SCP settings and top-down play.
- The player embodies one character. “First person” describes that character's participation in the world; the visual camera is top-down/isometric.
- Managing the Foundation network and dealing with Groups of Interest are core product pillars. A single site is the first implementation slice, not the whole game's intended scale.
- Personal, personnel, vehicle, and facility storage share physical inventory rules. Console distribution is delegated work over those same containers, with filters, priorities, consolidation, and access constraints.
- Visuals use modern sprites. Generated development stand-ins identify category, name, state/frame and facing with a green direction marker; finished artwork uses the same visual bindings.
- Primary employment is handled separately from optional action jobs. Shift duties take precedence over discretionary work; authorized orders and explicit emergency/condition overrides remain meaningful.
- Start as a newly appointed regional operations director with one functioning site, Foundation-command objectives and restrictions. Later scenarios can start in ordinary employment; management authority is not inherent in player control.
- Real time, ruleset-controlled pause and acceleration; inventory/terminals do not inherently stop the world. Field commands use click/Ctrl-click with equipment functions and physical accessibility.
- M1 is an SCP-650 breach inside the facility. Six approximate pose illustrations are discrete states. Both observed transport and controlled relocation are accepted approaches.
- Observer eligibility is module/scenario-configurable: human, AI or any eligible observer, with senses/media/attention evaluated separately. SCP-650's baseline uses humans, including qualified human-attended live feeds; AIC-only monitoring reports information without suppressing relocation. The article does not establish AI-only eligibility.
- M2 adds SCP-1499 in the same facility and qualifies physical transfer plus a bounded perception/consequence profile. Death/continuation and checkpoint-based anomalous restart are configurable policy types.
- OSCP/SCiPnet is the lore-favored device/network interface for management, records and communications, including outside-anomaly reports. The starting site has a resident AIC with physical computing infrastructure, bounded knowledge/authority and deterministic duties. Preserve a JSON provider interface for optional local-model integration and later regional AIC support. Exact authority remains under discussion; technical staff retain real operational duties.
- Ask the user about unresolved behavior/lore ambiguities before settling them. Do not silently combine source-specific timelines or make proposed counts permanent.
- Use Git for version control. The initial local commit preserves the earlier design and diagnostic fixtures.

## Working assumptions

- Single-player desktop and mouse/keyboard are the proposed platform/input envelope; exact bindings, hardware and supported OS remain open.
- Top-down or isometric presentation is primary. A facility section view is a later projection of the same spatial model.
- M0 proves feasibility; M1 qualifies SCP-650; M2 adds SCP-1499; M3 qualifies broader anomalies/continuation/world generation; M4 proves the bounded network; M5 scopes release. Counts and performance limits are proposed, and broader fixtures are not promised launch campaigns.
- LLM support is an optional later decision provider. The world runs fully without it.
- A bounded network prototype follows the local-play and anomaly-architecture gates, before a production release is scoped.

The immediate development step is the M0 technical prototype in the scope document. Engine selection follows its measurable requirements.
