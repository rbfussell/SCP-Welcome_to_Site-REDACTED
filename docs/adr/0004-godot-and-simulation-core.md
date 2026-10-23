# ADR-0004: Godot engine selection and proposed independent simulation core

**Status:** Godot selected by the user; .NET/C# integration details proposed pending M0 qualification

**Date:** October 22, 2026

**Follow-up:** [ADR-0005](0005-retain-godot-after-falltergeist-review.md) reviews the user's Falltergeist alternative, retains Godot and selects the .NET/C# implementation direction under delegated engineering authority. Version/runtime qualification remains M0 work. The original recommendation below is retained as decision history.

## Context and drivers

The game needs modern 2D sprites, layered inventories, a substantial OSCP interface, input/audio, developer tooling and desktop packaging. Its distinctive engineering is persistent simulation, employment, procedural domains, observation, physical logistics and capability-based SCP mods. Those domain rules must behave offscreen and independently of rendering.

## Options

| Option | Benefit | Cost/tradeoff |
|---|---|---|
| Godot with all world state in scene nodes | Fast initial visual iteration | Harder to separate persistence, replay, offscreen detail and authoritative order |
| Godot front end plus custom C# simulation library | Reuses visual/UI tooling while keeping game rules testable and independent | Requires a disciplined adapter boundary and the .NET Godot distribution |
| Full custom engine | Complete control over renderer, editor and runtime | Adds rendering, UI layout/input, asset tooling, audio, platform/export and debugging work before proving gameplay |

## Recommendation

The user selected Godot on October 22, 2026, and confirmed a fixed isometric/three-quarter field view with square logical tiles and a Fallout 2-inspired detailed sprite aesthetic. This accepts the engine choice. It does not certify a runtime version, installed editor integration, exports, performance or a finished art pipeline. The .NET distribution and independent C# library below remain the recommended implementation configuration to qualify during M0.

Use a qualified Godot 4 .NET release for presentation and a plain C# simulation library with no Godot dependency. Godot renders active sprites/tiles, equipment UI, OSCP and audio. The library owns entity IDs, time/event ordering, spatial/observation rules, employment/orders, physical inventories, procedures, knowledge, generation, saves and module hosting.

Start locally in one process. Adapters submit validated commands and receive snapshots/events; headless kernel fixtures run without an editor or renderer. Scene nodes are projections of selected active entities, not the only place those entities exist. Keep Godot integration thin enough that renderer changes do not rewrite domain state.

This is a custom simulation architecture, not a recommendation to write a renderer or a general-purpose engine. Choose the full-custom option only if measured requirements cannot be met after bounded optimization; publish the failing case and alternatives first.

## Verified capabilities and limitations

- Godot provides dedicated 2D tools including sprites, tile-based scenes and lighting. [Official 2D introduction](https://docs.godotengine.org/en/stable/tutorials/2d/introduction_to_2d.html).
- Its Control-node UI supports content, layout, tabs and theming suitable for an OSCP application shell. This suitability is an engineering inference from those primitives, not an OSCP component shipped with Godot. [Official UI guide](https://docs.godotengine.org/en/stable/tutorials/ui/index.html).
- Godot supports headless execution. A plain simulation library can also be exercised separately, while headless Godot tests qualify adapters. [Headless/export guide](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_dedicated_servers.html).
- C# needs the .NET edition; desktop export is supported, while current C# web export is unavailable. The desktop target fits this proposal; a mandatory browser version would require revisiting it. [Official C# documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html).
- Godot explicitly does not guarantee deterministic physics. Keep authoritative movement, visibility and critical action order in the tested kernel; a fixed tick alone does not make Godot physics a replay guarantee. [Official physics guide](https://docs.godotengine.org/en/stable/tutorials/physics/physics_introduction.html).
- Resource packs can contain scripts/scenes and replace resource paths. They are not our public gameplay sandbox. Use validated namespaced data/assets plus the separately qualified metered scripting host; arbitrary GDScript/C# assemblies or native packs do not inherit the module contract. [Official pack/mod guide](https://docs.godotengine.org/en/stable/tutorials/export/exporting_pcks.html).

Runtime asset loading is available for user content, but built-in production assets should use the normal import pipeline. Pin the importer/schema and restrict public asset formats. [Runtime loading guide](https://docs.godotengine.org/en/stable/tutorials/io/runtime_file_loading_and_saving.html).

## M0 proof and exit conditions

Select and record the exact Godot .NET/.NET SDK/export-template versions and reference machine. Run the existing timing/state fixtures independently of Godot, then through a sprite/terminal adapter. Demonstrate four-facing markers, render-rate-independent sight, an OSCP order producing a real acknowledged assignment, save/resume, bounded script faults and a domain transfer.

Use the proposed performance envelope and report measurements; none have been run here. Profile kernel, allocations, adapter transfer and rendering separately. Only introduce worker scheduling or native optimization after finding a relevant bottleneck. Live Godot scene changes stay in their integration context; workers exchange ordinary data rather than sharing mutable scene state.

If the metered mod host cannot enforce required limits, revise its integration/runtime before claiming support. Godot does not remove that requirement. A core implementation language and a public mod language need not be the same.

## Environment evidence and status

Read-only tools on this workspace reported Godot `4.6.3.stable.fedora.7d41c59c4`, .NET SDKs `8.0.128` and `10.0.110`, and Git. This does not qualify the installed Godot binary's C# integration, target-platform exports or performance. No game project, simulation library or editor distribution was installed/created by this decision record.

Godot is the selected engine. Version/platform/runtime qualification and implementation evidence remain M0 work; reopening the engine choice requires a recorded reason and owner discussion. Related: [shared simulation](0001-shared-simulation.md), [module contract](../03-scp-module-contract.md), [OSCP design](../11-oscp-terminal.md), [sprite/art contract](../06-sprites-and-placeholders.md).
