# SCP: Welcome to Site-[REDACTED]

A working archive of the game: its anomalies, shared simulation, Godot presentation, and unfinished systems. This is an **authoring and engineering wiki**. Its access to source code and SCP reference material does not grant that knowledge to the player or the resident AIC.

The game combines personal participation with Foundation operations: you embody a regional operations director in one functioning site, issue orders to real personnel, move physical equipment, and respond to incidents in the same world. The visual target is fixed isometric, textured pixel art inspired by Fallout 2; the management reference is X4: Foundations.

Public reading: [share this wiki](https://rbfussell.github.io/SCP-Welcome_to_Site-REDACTED/). Deployment and local archive limits are described in [Development and documentation consistency](development.html).

## Start reading

- [Item catalog](items.html): browse equipment types, native item artwork, current statistics and art coverage.
- [Asset library](asset-library.html): per-item bundle naming, mask roles and migration boundaries.
- [SCP directory](scp-index.html): mechanics, adopted lore, implementation status, and engine dependencies for every SCP referenced in the design.
- [Engine overview](engine.html): authority, execution order, shared services, and presentation.
- [Systems directory](systems.html): what each subsystem does and how it connects.
- [Ground inventory and piles](ground-storage.html): permanent current-tile storage, ordered stacks and clothes staging without a spare hand.
- [Independent inventory transfers](inventory-transfers.html): retain source storage while choosing belt/hook or other gear destinations.
- [Mount points and placement extenders](mount-points.html): the belt → hook → pouch fixture, shared clearance, provider trails and paid continuation.
- [Function directory](functions.html): methods, constructors, operators, local functions, properties, and explicit accessors extracted with the C# compiler's syntax parser.
- [Source files](files.html): complete indexed C# source with stable line anchors.
- [Design library](documents.html): browsable project design, architecture decisions, reviews, and authoring contracts.
- [Paperdoll pocket segments](paperdoll-pocket-segments.html): jacket chest contour, paid opening and pocket-specific actions.
- [Worn container drops](worn-container-drops.html): visible bag/pouch material, normal packing and retained source views.
- [Sprite equipment drops](sprite-equipment-drops.html): free fitting anchors, exposed mounts and paid sprite handling.
- [Worn attachment chains](worn-attachment-chains.html): composed belt → hook → pouch art, distinct selection and loaded handling.
- [Worn belt and direct pouch](worn-belt-and-pouch.html): actual provider coordinates, front/back, six turns and paid handling.
- [Worn-only alpha masks](worn-alpha-masks.html): intact loose artwork and context-specific worn-shoe visibility.
- [Implementation ledger](status.html): runnable slices, future work, and known documentation drift.
- [Research and provenance](research.html): archive revision, credits, evidence boundaries, and refresh instructions.
- [Development and documentation consistency](development.html): required reading before changes, cross-linked wiki maintenance, and final agreement with implementation and project documents.

## The playable slice

SCP-650 is the native anomaly in the runnable facility. The prototype includes hex movement, sight and lighting, human observation, silent relocation, personnel orders, physical trolley recovery, scheduled observation shifts, workstation/phone OSCP, a training combat fixture, and a substantial physical inventory and body-fit implementation. These are bounded proofs; the project does not claim a completed M0 or M1.

SCP-1499 is the planned second demonstration. SCP-019 tests the infrastructure design on paper. Other SCP entries explain planned capability fixtures or source references; their presence here is not a playable-content promise.

## Reading the evidence

**Implemented** means there is current native code, not that a production milestone has passed. **Planned** means a documented requirement or proposed contract. **Reference only** means an article informs a design without an adopted playable module. Check pages link to existing verification sources; their presence does not mean those checks were freshly run by this documentation build.

Use the search field to find pages by designation, system, function, or source path. Every document and function has a separate URL, so links can be shared with a person or another coding agent.
