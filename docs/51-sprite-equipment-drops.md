# Sprite paperdoll equipment drops

Implemented November 4, 2026. **I → Sprites** accepts ordinary inventory drags onto the reference body and its registered exposed belt/hook points. This extends [diagnostic body drops](39-body-drag-fitting.md), [worn attachment chains](50-worn-attachment-chains.md), [inspection/removal](40-body-inspection-and-removal.md) and [independent transfers](48-independent-inventory-transfers.md). See the [wiki mechanic](../source_material/game-wiki/sprite-equipment-drops.html).

## Free placement, not default snapping

The user selected **free body-hex placement** for this slice. The pointer selects a real body-local hex in an authored projection over the reference sprite. Release submits that exact anchor and rotation to `PreviewBodyFitDrop` and normal Move handling. It never searches a nearby fit, moves blockers, chooses a different anchor, or silently forces the default pose. Blank margins and gaps between body cells refuse placement.

R over the body cycles six body orientations. Authored permitted rotations, boundary contacts, covering/restraints, fit bands, outermost packs and shared clearance remain authoritative. An item with distinct front/back mounts selects the current matching side; an explicit wrong-side mount refuses. Back-view body coordinates mirror horizontally. Existing diagnostic fitting remains available.

The manifest's optional `bodyPlane` registers `centerX`, `cellWidth` and twenty increasing normalized row heights for this one reference body. These map the existing twenty-row logical body onto the detailed artwork; they neither resize its physical mask nor infer anatomical fit from pixels. Body picking and draft outlines share the same projected cell polygons. The grid is hidden outside a fitting drag. Projection is an authored approximation, not cloth deformation or a production body-surface mapping system.

Normal registered poses show the garment sprite; other poses show the exact physical footprint and disclose missing fitted art. No waist sprite is pretended to fit a chest X. A physically valid non-default committed pose can therefore remain listed as omitted by the existing art proof. The world character remains baked artwork.

## Exposed mounts and dragging worn gear

Attachment-capable gear targets the nearest offered point within its projected hex radius. Only currently registered worn providers on the viewed side offer targets, including nested hooks. Later opaque unrelated clothing hides a target. Existing descendants do not hide an occupied target: the simulation reports its collision instead. Ties prefer the later rendered provider. These are exact point targets, not automatic free-point distribution or container packing.

R there cycles six attachment orientations, independently of square storage and body rotation. Parent-local points, inherited surfaces and composed rotations come from the existing item graph. Draft attached sprites, including registered exterior descendants, use shared transforms/alpha; contents are never rendered as attachments. Green/red feedback reflects the ordinary quote. No source image or native item definition changes.

Press and drag an exposed worn sprite to move that actual item. The pressed identity survives Godot's drag threshold, important for tiny hooks. Ordinary move access can reject covered/restrained gear. A loaded provider carries its real descendants. Clicking, double-click inspection and context actions retain their earlier behavior. Leaving the target or ending/cancelling the drag clears transient feedback. Esc closes the interface without doing physical work or securing gear.

## Paid authority, persistence and verification

Previews are read-only. Invalid or paused release keeps the exact source. A valid release starts ordinary source-dependent time/AP; the source and worn images stay until commitment, and normal access/revision checks can interrupt or reject work. Accepted requests save/resume through current schema **15**, with no new mutable save fields or kit identity. UI drafts are not saved.

[Native sprite-drop checks](../game/Presentation/SpatialScene.PaperdollDropSmoke.cs) run with the paperdoll/full inventory suites. They cover grid-to-body input, exact free anchors/rotations, unsupported-pose footprint feedback, blank/paused refusal, Esc, paid source retention/restore, belt and nested hook points, occupied refusal and loaded sprite drag sources. Existing domain fit/attachment services are reused; no new domain behavior is claimed. Actual results and warnings are in [runtime evidence](reviews/inventory-runtime-checks.md).

General body variants, arbitrary fitted sprite generation, storage insertion by dropping onto paperdoll backpack material, direct pocket segments, cloth deformation and world outfits remain future work. Use the enlarged gear/contents view for container insertion and more detailed mount work.

The `--sprite-drop-smoke` launcher mode runs only this new interaction fixture; paperdoll and full inventory modes include it in their larger regressions.

```sh
python3 tools/run_spatial_scene.py --sprite-drop-smoke work/preview/spatial-scene/sprite-drop-focused.png
python3 tools/run_spatial_scene.py --paperdoll-smoke work/preview/spatial-scene/sprite-drop.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/sprite-drop-full.png
python3 -m unittest discover -s tests -p test_wiki_items.py
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```
