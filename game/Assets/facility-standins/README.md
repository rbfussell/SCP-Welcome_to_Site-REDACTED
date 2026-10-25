# Facility development sprite set

Generated October 24, 2026 with the built-in imagegen tool. These are original detailed, visibly pixelated development stand-ins for the current test facility. They replace the default rendered blocks while retaining the original deterministic diagnostic assets behind **F4**. They are not final production character identities or finished modular clothing art.

| Source image | Contents | Runtime use |
|---|---|---|
| [chen.png](chen.png) | Security employee, six facings, idle plus three walk frames | `observer` / Chen |
| [ortiz.png](ortiz.png) | Lab technician, six facings, idle plus three walk frames | `tech` / Ortiz |
| [player.png](player.png) | Operations director, six facings, idle plus three walk frames | `avatar` / player |
| [floor.png](floor.png) | Top-down worn concrete/epoxy floor | Projected square construction floor |
| [wall.png](wall.png) | Straight-on painted facility wall material | Projected wall faces on both construction axes |
| [door.png](door.png) | Straight-on closed steel security door | Projected closed doorway face |
| [land.png](land.png) | Top-down dirt/grass | Ground bordering the facility |
| [props.png](props.png) | Crate, storage cabinet, CRT workstation, empty transport trolley | Current test props and operation fixtures |

The eight PNG sources are copied byte-for-byte into this folder, including generated alpha. [Provenance](provenance.json) records original generated filenames, SHA-256 hashes, sizes and modes. [The complete prompt set](prompts.json) records the exact prompts and use of the built-in tool. No Fallout or Wiki bitmap artwork was copied into these images. They share a grounded classic PC RPG reference; existing SCP-650 art retains [its separate attribution](../scp650-art.md). Overall project distribution/licensing remains a separate decision.

## Registration and replacement

[manifest.json](manifest.json) declares 80 sprite bindings: 72 character frames, four material surfaces and four props. A character binding resolves actor identity → rig → ordered layers → state → facing → frame → atlas region/feet anchor/scale. Directions are `NE, E, SE, SW, W, NW`, matching the projected navigation directions. Character height targets approximately 58 native screen pixels. Frame phase follows committed movement, never the other way around.

[The registration tool](../../../tools/register_facility_standins.py) reads alpha to choose clear row boundaries and foot anchors. Chen's generated boot extends beyond a nominal 256-pixel row, so clear-gap regions prevent clipping. Sources are not cropped, repainted or resampled on disk; Godot reads regions through `AtlasTexture`. Registration can be reproduced or checked:

```bash
python3 tools/register_facility_standins.py
python3 tools/register_facility_standins.py --check
```

Each current human uses one `baked-outfit` layer. The renderer already composes ordered layers with common rig/state/facing/phase selection. Future body, head, skin/shade, hair, clothing and equipment layers must be authored to compatible poses, ground anchors and attachment registration. This does not mean these baked source sheets can already swap clothing or that equipment/inventory exists. Different body/fit families can use explicit rig IDs rather than silently stretching incompatible clothes.

Floor/land materials are mapped with planar UVs onto the existing exact floor mesh. Wall/door faces are mapped onto projected wall segments; an authored facade does not define collision. The presentation wall height is 68 pixels; opened-door frames and cutaways remain presentation. Enabled cutaways also fade foreground segments that overlap already-visible actors/anomalies, preserving readability without revealing unseen entities. Props have explicit ground anchors, and trolley cargo uses a small declared visual support offset. Real occupancy, observation, transport and SCP rules retain their existing simulation geometry.

The generated character faces, strides and prop perspective are first-pass stand-ins. Full walk/run/attack/reload animations, animation cleanup, procedural phenotypes, paper-doll fits, alternate uniforms and production texture/import budgets remain future qualification. The app keeps the source pixel texture with nearest filtering; large generation sources are not a final texture-memory or export target.

## Tests from here onward

Current default field/operation/combat scenes use this set plus the existing SCP-650 atlas. F2 retains unrestricted diagnostics and facing/state labels; F4 toggles all authored art back to diagnostic blocks, not just the statue. New test entities/states need an explicit appearance registration or an identifiable diagnostic fallback while their artwork is prepared. Unknown actor identities currently use the existing kind-based diagnostic set.

[Sprite registration checks](../../../tests/SpatialChecks/SpriteArtChecks.cs) validate actual PNG sizes, regions, anchors, all six facings/idle/walk frames, distinct NPC appearances, layered selection and unchanged simulation state. Existing spatial, operation and combat render fixtures also exercise the art set, including transparent sprite picking.
