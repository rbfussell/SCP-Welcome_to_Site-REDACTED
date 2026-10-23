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
