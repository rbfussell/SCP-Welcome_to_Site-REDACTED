# Independent inventory source and destination views

**Implemented bounded inventory interaction, November 4.** The right contents pane remains available while the middle pane changes to another accessible gear object. This is ordinary equipment handling, not an SCP adaptation. See the [runtime contract](doc-docs-48-independent-inventory-transfers-md.html), [inventory](system-inventory.html), [presentation](system-presentation.html) and [ledger](status.html).

## Direct transfers

Open a backpack pocket, then inspect the worn belt: the pocket stays on the right, with its own owner/compartment breadcrumb. Drag its hook directly onto the belt webbing. A stored pouch can then go directly onto the hook's lower mount. The hand can remain occupied. [Provider geometry and clearance](mount-points.html), normal source/destination access and paid handling still decide whether either transfer succeeds. Exact grid drops and scoped container packing retain their existing semantics.

The three panes remain paperdoll, gear and contents. Opening a different compartment explicitly replaces the right branch; opening a container within that branch descends into it. Inspecting destination gear alone neither opens its compartments nor secures the source. A cancelled view callback cannot resurrect an old branch when its physical opening later completes.

## Two trails, physical dependencies

Up/× operate on their respective trails. Closing unrelated destination gear keeps the source. Closing an actual source provider, including an ancestor in an attachment chain, dismisses its dependent contents and queues eligible securing through normal time/AP handling. Right-hand Up returns to the containing compartment; right-hand × closes that contents trail while retaining destination gear. Closure history is scoped to the retained source and current gear. Esc closes the interface and leaves physical closures unchanged.

Source views retain expected placements and normal access checks. Closing an ancestor, covering a pocket or leaving a fixture's reach invalidates its unavailable contents. Merely inspecting another object does not change physical access or grant knowledge of closed storage. [Body inspection](body-inspection.html), [worn backpack inspection](worn-backpack.html) and [belt/pouch sprites](worn-belt-and-pouch.html) continue to select real items through these services.

## Source and evidence

[InventoryBrowser](file-game-presentation-inventory-inventorybrowser-cs.html) owns the independent view trails; [grid input](file-game-presentation-inventory-inventorygrid-cs.html) and [mount targeting](file-game-presentation-inventory-inventorygearsurface-attachments-cs.html) submit existing transactions. [Native transfer checks](file-game-presentation-spatialscene-transfersmoke-cs.html) cover real pointer input with an occupied hand, nested targets, paused refusal, paid source retention and pending restore, independent closure, parent dependencies, reach and coverage. See [recorded results](doc-docs-reviews-inventory-runtime-checks-md.html).

At this slice’s qualification, definitions, physical dimensions and artwork were unchanged and saves used [schema 14](system-persistence.html). UI breadcrumbs remain transient. Nearby ground storage for changing clothes, a second hand, two simultaneous contents grids and general transfer planning are not added by this slice.

Related: [engine](engine.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [mount points](mount-points.html), [body inspection](body-inspection.html), [worn backpacks](worn-backpack.html), [worn belt/pouch](worn-belt-and-pouch.html), [design library](documents.html), [ledger](status.html).

Current floor/persistence follow-up: [ground inventory and piles](ground-storage.html) now supply persistent clothes staging and top-only pile access through ordinary handling. Current snapshots use schema 15, with matching schema-14 migration; schema-14 statements on this page describe this feature’s earlier qualification. This bounded floor feature supersedes any earlier ground-as-future statement, while world floor-item sprites and general hauling remain unimplemented.

[Worn attachment chains](worn-attachment-chains.html) now render and alpha-pick the normal front belt → hook → pouch assembly through shared composed frames. Physical access, paid handling, independent trails and loaded ground transfers remain authoritative.

[Sprite equipment drops](sprite-equipment-drops.html) now add free body-hex placement, exact exposed belt/hook targets and worn-sprite drag sources through ordinary paid handling. Unsupported poses retain footprint feedback and explicit missing-art disclosure.

[Worn container drops](worn-container-drops.html) now link visible backpack-bag and pouch material to real interiors through ordinary paid packing. Harness-only pixels do not store items; closed/full refusals do not fall through to body fitting.

[Paperdoll pocket segments](paperdoll-pocket-segments.html) now qualify four coat pockets, six trouser compartments and backpack main/side/front. [Every depicted pocket is usable](visible-pockets.html); front/back cargo views share storage. Coverage and independent paid fastenings still apply; animation remains future work.

[Shirt pockets and detachable badge](shirt-pockets-and-badge.html) add two buttoned chest compartments and a real clip-mounted badge. Covered retention and manipulation are separate; this changes native kit identity to 20 definitions/40 instances while retaining schema 15. Visible-badge automatic entry is selected design; door credential services are not implemented.

[Current visible-pocket contract](visible-pockets.html): depicted pockets require real storage; multiple views share one inventory.
