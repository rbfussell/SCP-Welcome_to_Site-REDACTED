# SCP-650 native prototype checks

Date: October 22, 2026. Implementation: `7ec8a31`, with a subsequent source whitespace cleanup and this evidence note. The proof is a subset of M0/M1 behavior, not full milestone acceptance.

## Commands and results

- `python3 tools/run_spatial_scene.py --check`: passed. The C# build completed with zero warnings/errors. There were 52 SCP-650 assertions and 18,926 assertions total, including the existing grid/picking checks.
- `python3 tools/run_spatial_scene.py --smoke work/preview/spatial-scene/scp650.png`: passed. The Godot build completed with zero warnings/errors, asset import succeeded, and the input/render harness reported `SCENE_SMOKE_PASS`.
- Documentation link/fence checks, 89 unique acceptance IDs, Python syntax and deterministic regeneration of 72 raster stand-ins: passed. Six discrete SCP-650 pose bindings are present; no obsolete pose image remains in the manifest or asset directory.

Runtime: Godot .NET `4.7.1.stable.mono.official.a13da4feb`, .NET SDK `8.0.128`, runtime `8.0.28`, Linux. The virtual display used Mesa llvmpipe software rendering. Its unsupported V-Sync warning did not prevent rendering; this run establishes no hardware performance target.

## Behavioral evidence

Tests cover one human holding an understaffed statue, redundant observers during a blink, last-human blink/loss releasing relocation, same-boundary attention changes, profile selection/persistence, AI detection without human eligibility, unknown-kind exclusion, door/lighting/facing effects, destination occupancy and visibility rejection, stale revision/profile/hold rejection, cross-door relocation, bounded no-candidate retry and stable observed poses.

Discovery is once per witness/encounter and produces a short local movement interruption. Unfamiliar targets receive threatening poses; familiar targets can receive variants. Relocation emits no world sound or attack. Save/load retains body, pose, RNG, local deadline, encounters and trace. An invalid restore leaves the running world unchanged. Local waiting preserves module/timer state while world time advances.

The visual harness submits a floor click and verifies the actual intended hex, then submits O and verifies relocation. It submits F6, waits for input delivery, checks continuing world time and restores a snapshot for comparison. Visual inspection confirmed the inspector text/buttons fit, debug labels avoid collisions, and the dynamic blocked-cell outline follows the statue.

## Limits

These are native C# rules with bounded host requests, not sandboxed user scripts. Cameras/AIC infrastructure, employment, transport/recontainment procedures, fine swept-time observation, full fear/knowledge and tactical AP remain future work. Inspector visibility is deliberately unrestricted debug information. Inventory/crafting is documented architecture only; no assembly, ammunition or material transformation runtime was added. The explicit-service-slot design question remains open.

## October 23 encounter and explicit-complacency update

This update supersedes the earlier default full-map presentation and implicit familiar-after-discovery selection described above. It remains a native module/presentation proof, not complete M0/M1 certification.

- `python3 tools/run_spatial_scene.py --check`: passed, zero compiler warnings/errors; 52 original SCP-650 assertions, 210 explicit-complacency assertions, 32 field-presentation assertions and **19,168 assertions total**.
- `python3 tools/run_spatial_scene.py --smoke work/preview/spatial-scene/scp650-encounter.png`: passed, zero compiler warnings/errors; asset import and the input/render harness passed. Field and diagnostic captures were inspected. The virtual display again used llvmpipe, with the same nonfatal unsupported V-Sync warning.
- The smoke fixture uses ordinary O/F6 commands, then positions the avatar for a reproducible look-away/RMB discovery input. It never assigns or forces the statue's destination. It verifies hidden/revealed drawing policy, one avatar discovery cue, J changing module state without moving/re-posing an observed statue, and F2/M/R presentation controls. The dummy audio driver verifies playback control, not subjective speaker balance.

`complacency` is now an explicit **per-person module flag**, with no automatic metric. Threat placement is adjacent and behind; relaxed placement is two to three hexes away, without a rear-only restriction. Clear physical sight between target and destination is required in both cases, and qualifying observation still prohibits relocation. Fixed-seed checks exercise both profiles, including genuinely non-rear relaxed positions when people are distracted. A person's available candidate count does not bias target selection. Flag changes while observed do not change pose/location. Unknown instances/flags/actors, nonhuman targets and held changes are rejected; restore revalidates targets and kinds before committing.

The shared host has generic flag dispatch and constrained spatial queries. Only the SCP-650 implementation interprets complacency and selects its placement/pose policy. Complacency remains separate from witness history. Outer save schema stays 2; the SCP-650 module schema is now 2. Earlier module-schema-1 saves are rejected intact with an explanation rather than assigned inferred flags.

The field presentation hides unseen people/statue using direct perception and uses a separate generated, textured six-pose atlas. F4 retains diagnostic art. Only a fresh avatar `startled` event produces discovery feedback; silent relocation, remote discovery and repeated refreshes do not. Inspecting/drawing does not mutate authoritative state, RNG or trace. Local caption/border progression respects local holds, and restore establishes a new presentation baseline without replaying historical encounters. Audio is non-diegetic and emits no NPC hearing event; mute and reduced effects are available, with no flash or camera shake.

The final captures show clear statue artwork without overlapping actor diagnostic labels in field view, the readable encounter caption/controls and the module inspector's persisted avatar complacency flag in diagnostics. This remains first-pass fixed-view artwork and a partial visibility filter: full acquired knowledge, remembered sightings, surveillance sharing, furniture-specific poses, final art/facing qualification and the public sandboxed mod loader remain unimplemented. No automatic complacency progression is implied.
