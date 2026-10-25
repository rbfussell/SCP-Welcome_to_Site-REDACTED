# OSCP workstation and field-client prototype

Status: October 25, 2026 implementation pass. This is a bounded command/document shell over the existing site operation, not the complete strategic game or completion of TERM-01–TERM-09.

## Launch and access

```bash
python3 tools/run_spatial_scene.py
python3 tools/run_spatial_scene.py --mode oscp
python3 tools/run_spatial_scene.py --mode phone
```

Local play opens the CRT workstation with **T** near the physical terminal. **P** opens the issued field phone. Both can also be opened with the operation-panel buttons. **Esc** closes the current device. The full terminal remains tied to its powered physical workstation. A powered offline terminal can retain the shell and previously acquired records, while live commands require the site link.

The phone automatically pauses single-player on opening. Team orders prepared while paused remain local drafts; closing the phone revalidates and submits them. One draft per recipient replaces that recipient's earlier draft; Clear prepared orders discards drafts without submitting them. If a manual pause was already active, submitted orders remain pending until normal simulation resumes. A link/access failure can reject submission, and device loss discards unsent drafts. Drafts are not persistent world orders. Closing it resumes only a pause owned by that phone session; a pre-existing manual pause is preserved. Switching from an automatically paused phone to the workstation releases the phone's pause. Handset loss closes its usable session. Space is manual pause when the command editor does not have text-input focus. The workstation does not automatically pause. Multiplayer timing remains future work.

The standalone scene uses an explicit starting context with the director physically beside the terminal. It starts real movement, observation, personnel, radio, anomaly, order and persistence services without constructing local-play renderers. It is a separate scenario identity; its saves cannot be mixed with the normal field scenario. In combined local play, the shell uses the existing simulation instance and adds no scheduler. The shared `SimulationRuntime` owns the fixed tick adapter for either host; `OscpWorkspace` never ticks the world.

## Applications and commands

[The native application manifest](../game/Content/oscp-apps.json) registers stable IDs, labels, numeric symbols, view bindings, allowed device profiles and aliases. Six initial applications supply Operations, Personnel, Records, Communications, Local plan and workstation status. Workstation status is absent from the phone profile. Adding registration alone cannot grant authority or invent a new simulation service. Native view handlers still require code; arbitrary extension scripts, installation and public mod loading are not implemented.

Commands open the same applications: `status`, `team`, `records`, `mail`, `map` and `systems` (workstation only). `help` lists supported commands. Personnel buttons and the command line call the same validated service:

```text
order tech observe
order observer follow
order tech post 8 4
order tech recover
```

These are game commands only. No command executes a host program. Current personnel IDs are shown in the personnel directory. Observe/follow use acquired target information, post uses a known requested hex, and recovery uses the existing observed trolley procedure. Submission, delivery, acknowledgement, travel, blockage and completion remain separate. The shell lists the player's own submissions and delivered reports, rather than reading hidden order execution status.

The handset is an explicit issued/powered/accessible equipment fixture owned by the acting character; it is not yet an inventory-backed item or battery simulation. Site radio range and routing remain the bounded existing site-link fixture. Neither choosing a profile nor typing its command grants commander authority. Device/service checks run again on submission and delivery. A pending order cannot relieve a standing duty while its delivery route is unavailable.

## Records and information

OSCP keeps a bounded ledger of report IDs actually acquired by the player. Phone or workstation access to a connected site link acquires radio records; disconnecting retains acquired records and prevents new ones from appearing. System/voice records use their existing delivery rules. A hidden server-side report is not player knowledge. The event log and OSCP share acquisition gates; presentation does not consume RNG, grant sight or copy the causal debug trace.

The local plan shows known construction geometry, the player's position and acquired contact positions. It does not query hidden live personnel/anomaly locations or display authoritative live door status. Contact positions can become stale. The Records view contains an operational SCP-650 brief and attributes the source separately from the game's trolley adaptation.

The standalone scenario enables one authored regional intake message, R-017, attributed to a SCiPnet relay separately from local radio reports. The world-clock network collector delivers it once after its link is available; the local reader acquires it through the ordinary report service. Its Ash Creek account is an unverified source claim, with no SCP classification, spawned anomaly or deployed field team. Triage, case editing, case acknowledgements and deployment are later work. A local tactical hold can leave incoming records at the collector pending local reader processing.

## Presentation and limits

The full view has a visible faux CRT housing with a recessed screen, vents, power indicator and readable command/document layout. The phone has separate handset framing and compact navigation. Brightness is adjustable. There are no compulsory flickering effects or unreadable scanlines. The source styling/adaptation boundaries remain in [the device source check](reviews/oscp-device-source-checks.md).

No construction/economy, full infrastructure controls, equipment logistics, surveillance operator, resident AIC runtime, local-model provider, complete personnel employment, world-map UI or inventory pane is claimed. The shared future phone/inventory menu still reserves inventory on the right as a design direction. Opening a view currently preserves the actor's existing attention state; explicit terminal attention tasks and surveillance eligibility remain to be implemented and qualified. These missing services are not simulated by decorative buttons.

Operation snapshots retain handset state, acquired record IDs and regional-message delivery state introduced in outer schema 5. Outer schema 6 now also persists the [employment slice](22-employment-prototype.md); appointments changed the scenario content identity, so earlier scene saves do not match. Matching older saves without a handset migrate without inventing an issued device. Existing content identity and detached atomic restore checks remain. UI selection, brightness and unsubmitted editor text are presentation state, separate from accepted world orders.

## Checks

```bash
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --oscp-smoke work/preview/spatial-scene/oscp-standalone.png
python3 tools/run_spatial_scene.py --operation-smoke work/preview/spatial-scene/oscp-workstation.png
```

Core checks qualify profile aliases, access/authority, real phone delivery, interrupted links, offline acquisition/cache, local holds, bounded cache eviction, older matching saves, detached rejection and once-only regional delivery on world time. Godot checks additionally exercise standalone lifecycle, physical workstation access, actual UI input, clocks, device switching and rendered CRT/phone/map views. This implementation does not complete M0/M1 acceptance or qualify performance on large simulated sites.

The Personnel app now includes employment roles, short test shifts, last delivered duty reports and reported staffing warnings. `order tech duty` releases direct orders and resumes scheduled work. Full professions remain future work.
