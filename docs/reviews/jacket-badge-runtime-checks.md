# Jacket badge runtime evidence

November 6, 2026. Current contract: [real jacket badge clip](../57-jacket-badge-clip.md).

- Domain `/tmp/scp-jacket-domain3.log`: **21,258 assertions passed**, including **48 jacket-badge assertions**. Zero compiler warnings/errors. Covers four pockets plus a separate clip, paid mounting/transfers, cover/access, closed-coat reader use, loaded removal, direct hand versus held jacket and exact continuation.
- Focused native `/tmp/scp-jacket-native2.log`: JACKET_BADGE_SMOKE_PASS. Both body/enlarged clip drops, actual pocket hover/menu/storage, separate badge picking, open/zipped jacket states, covering and reader entry passed. Real captures under `work/preview/spatial-scene/jacket-badge-final-*.png` were visually inspected.
- The first domain fixture had nullable-annotation compilation errors; corrected without weakening checks. The first native render placed the badge beneath the default backpack strap, so its own pixels could not be picked. Final metadata moves it slightly inward over the same chest pocket. Those failed runs are not qualification evidence.
- New badge-free atlas was copied unchanged from built-in imagegen; original atlas remains preserved. Exact prompt, source paths and hashes are in [provenance](../../game/Assets/items/worn/clothing/coats/coat/PROVENANCE.md). Only coat consumers use the new atlas. Catalog tests passed five checks; deterministic body fixture stayed current.

- Final focused `/tmp/scp-jacket-native-current.log`: JACKET_BADGE_SMOKE_PASS, exit 0, including opaque-badge drop refusal with unchanged inventory and exposed pocket access.
- Final full native `/tmp/scp-jacket-full-qualified.log`: **26 PASS markers, exit 0, no runtime ERROR/exception**. Includes the unchanged attachment fixture's exterior-context assertions, shirt/backpack/trouser/jacket pockets, masks, overlays/hooks, transfer, floor, badge-reader and full inventory continuation checks.
- The initial full run caught a real hybrid-menu regression: attachment-only belt webbing was sent to a pocket fastening menu. The final handler preserves attachment-only context behavior; the existing full attachment fixture passed after the fix. An extra focused belt setup tried to inspect gear still hidden in an unopened crate; removed this redundant setup because the existing full fixture already tests the exact menu case. A run staged with that obsolete setup was stopped, not counted as a pass.
- Native build retains three existing compiler warnings in GroundSmoke. The final full run reported unsupported V-Sync and two ObjectDB shutdown instances, matching the earlier recorded baseline. Cleanup remains unresolved; gameplay checks succeeded.

 Existing Godot/compiler environment warnings remain separately recorded; no claim of leak-free shutdown or complete world wardrobe rendering is made. Build/check does not run gameplay tests.
