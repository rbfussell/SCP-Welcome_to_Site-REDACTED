# Badge-controlled door runtime evidence

November 6, 2026. Current guide: [badge-controlled test door](../56-badge-door-access.md). This is a bounded security fixture, not full facility security or an SCP canon claim.

- Domain run `/tmp/scp-badge-domain-final.log`: **21,207 assertions passed**, including **67 badge-door checks**. Zero build warnings/errors. Covers hand-only presentation, displayed/covered/packed states, tiers 1/2, issuer, power, condition, optional identity, NPC queries, AP/time, interruption, commitment rechecks, exact pending continuation and atomic invalid-target rejection.
- Focused native `/tmp/scp-badge-native2.log`: **BADGE_DOOR_SMOKE_PASS**, real badge-to-hand drag and E input, paused refusal, paid presentation, covered/pocket denial and worn automatic entry. Captures under `work/preview/spatial-scene/badge-door-final-*.png` are actual game renders, not generated mockups.
- Initial native run failed because the fixture checked queued key input before Godot dispatched it. The fixture now waits a native frame; domain behavior was not bypassed. An early tactical domain fixture placed the training target at a wall boundary and did not detect contact; moved the target into the same room. Failed runs are not qualification evidence.
- Item-catalog tests: five passed. Source artwork is unchanged. Reader label/status are engineering presentation.
- Full native inventory regression `/tmp/scp-badge-full.log`: **25 PASS markers, exit 0**, including BADGE_DOOR_SMOKE_PASS and existing clothing, pocket, packing, transfer, ground, closure and continuation checks. No runtime ERROR/exception. This run preceded the final label-depth correction; simulation behavior is unchanged.
- Separate tactical UI regression `/tmp/scp-badge-combat-native.log`: COMBAT_SMOKE_PASS, exit 0.
- Final focused presentation/input regression `/tmp/scp-badge-native-final.log`: BADGE_DOOR_SMOKE_PASS, exit 0 after moving the reader label above the doorway at the correct drawing depth. Actual captures `badge-door-reader-label-*.png` were inspected for the labelled closed/open reader and denied/automatic states.
- Native builds retain three existing GroundSmoke compiler warnings. The full run reported three ObjectDB shutdown instances (previous recorded baseline two); final focused run reported two. This shutdown warning remains unresolved and is not a claim of leak-free cleanup. Unsupported V-Sync also remains an environment warning.

Documentation build/check does not run gameplay tests. Power is an authored fixture flag, identity binding remains configurable with provisional bearer default, and world clothing sprites remain baked.
