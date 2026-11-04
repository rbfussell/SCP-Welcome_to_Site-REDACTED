# Worn backpacks and harnesses

**Implemented bounded presentation, November 4.** **I → Sprites** displays an actual back-mounted pack as front straps/rear bag, reversing those views for a front-mounted pack. Two worn packs retain separate identities and inventories. See the [runtime guide](doc-docs-44-worn-backpack-paperdoll-md.html), [full-body fixture](full-body-paperdoll.html), [appearance](wearable-appearance.html), [inventory](system-inventory.html), [presentation](system-presentation.html) and [ledger](status.html). This is ordinary equipment presentation, not an SCP canon adaptation.

## A view of the existing item graph

Visible bag and harness pixels select one actual own worn pack. Double-click opens its enlarged gear view; right-click uses ordinary actions. Transparent gaps expose lower garment targets. The enlarged illustration retains its existing main/side segments, each opening its own contents view through physical access and paid closure services. Inspecting the pack alone reveals no contents. Decorative art does not add compartments.

[The adapter](file-game-presentation-inventory-inventorypaperdollart-cs.html) supports optional mount-slot-to-frame bindings, choosing pack/harness destinations from the committed mount and worn order. Stored, held, foreign and unqualified poses/mounts stay omitted. It shares alpha/letterbox picking with drawing. It does not modify fit, physical coverage, restraint, access or carrying capacity. The [inspection](body-inspection.html), [body fitting](body-fit-cleanup.html), [body drops](body-drag-fitting.html) and [provider graph](mount-points.html) retain their authority.

Normal paid handling keeps source art until completion and carries a loaded pack intact. [Persistence](system-persistence.html) reconstructs its appearance from the item graph and retains exact pending work. No content identity, mutable field or schema change: inventory schema remains 14.

## Evidence and limits

[Native pack checks](file-game-presentation-spatialscene-paperdollbackpacksmoke-cs.html) exercise actual identity, both mount directions, independent main/side contents, paused/paid closures, loaded removal/restore, held omission and two distinct packs. [The parent paperdoll suite](file-game-presentation-spatialscene-paperdollsmoke-cs.html) retains clothing picking/closure assertions outside pack alpha. [Recorded results](doc-docs-reviews-inventory-runtime-checks-md.html) distinguish actual checks from requirements; wiki building does not run gameplay tests.

The runtime guide links the untouched built-in-generated atlas and exact prompt/source/hash provenance. Worn bag art is static across lid states; existing menus/contents expose actual closure state. Direct small-body pocket segments, worn closure animations, general cloth fitting, arbitrary poses, body variants, belt/attachment body art and world wardrobe composition remain future work.

Related: [full-body paperdoll](full-body-paperdoll.html), [appearance](wearable-appearance.html), [inspection](body-inspection.html), [body fit](body-fit-cleanup.html), [body drops](body-drag-fitting.html), [mounts](mount-points.html), [engine](engine.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [ledger](status.html), [design library](documents.html).
