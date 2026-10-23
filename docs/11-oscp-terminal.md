# OSCP terminal and Foundation communications

Status: Design pass, revision 0.6, October 22, 2026. An OSCP-inspired terminal for facility/network management and outside-anomaly reports is confirmed direction. App names, layouts, command vocabulary and milestone bounds below are proposals. The references inform the interface; they do not add two more complete SCP implementations to M1.

## 1. What the references establish

[SCP-245 by Doctor Cimmerian](https://scp-wiki.wikidot.com/scp-245), CC BY-SA 3.0, names OSCP as a compatible system and describes isolated computers used to contain and monitor anomalous software. It does not supply a complete OS specification. Its download is not needed or executed for this design pass. Air-gapped research hardware is distinct from ordinary management workstations.

[SCP-5810 by Lt Flops](https://scp-wiki.wikidot.com/scp-5810), CC BY-SA 3.0, depicts OSCP startup and a named session, dossier entry/editing, brightness adjustment, an idle inoculation check, personal storage and evidence upload/preview. Those are useful interface anchors. The story is an account of an anomaly and a user's experience, not a comprehensive description of reliable system behavior.

The shell, management applications, order receipts and communication workflow below are original game design extrapolations. There is no claim that these exact screens, network architecture, menus or commands are canonical. Interface terminology can be adapted without importing the articles' full plots or anomalous effects.

The user's follow-up adds SCiPnet, AICs/AIAD and Technical Services as required lore research. [The lore register](13-lore-and-digital-operations.md) separates verified references from pending canon/department decisions. Ask about unresolved design ambiguities before committing to their behavior.

Proposed layering: OSCP is the local platform/shell; SCiPnet provides intranet records/mail/alerts; an assigned AIC is a digital colleague operating authorized services; technical personnel handle their actual responsibilities and physical faults. The starting site is confirmed to have a resident autonomous AIC with physically located computing infrastructure and bounded monitoring/reporting/assistance duties. Exact authority, identity and Technical Services portrayal remain open. Do not import a named canon AIC or treat the interface itself as performing every complex task.

## 2. Device, session and service access

An OSCP session is attached to an actual terminal/device and acting character. Track device ID/location, installed app capabilities, power, local storage, connection routes, authenticated identity, clearance/department scope, active attention, current branch and session lifetime.

Effective access is the intersection of character authority, device capability, reachable services and current world conditions. A director's badge does not power a dead workstation. An employee using a director's desk does not inherit their session. Remote services revalidate authority at command commitment, including reassignment, revoked access and succession.

| Device profile | Proposed use | Constraints |
|---|---|---|
| Fixed network workstation | Site management, records, communications and installed control services | Requires physical access, power, authentication and reachable network |
| Laptop/portable terminal | Field reports, local files and permitted remote services | Must be physically accessible; battery, link, installed tools and credentials matter |
| Security console | Authorized surveillance, doors, alarms and staffing | Restricted scope; watching a feed is an explicit attention task |
| Air-gapped research terminal | Isolated software/records and specialist tasks | No network control or automatic external report delivery while isolated |

The same shell adapts to installed functions. Offline, isolated, expired-session and unpowered are distinct states. A powered offline laptop can show cached files; an unpowered desktop cannot keep working merely because its screen was already open. Approved in-world media transfer may later bridge isolated records through explicit procedures, not an implicit network exception.

## 3. Shell and applications

Propose a restrained workstation interface: sharp readable text, compact navigation, document-oriented panels, status strip and command/search field. It may evoke old institutional systems without pixel fonts, compulsory scanlines, flicker or tiny unreadable type. Brightness, font size and reduced visual effects are accessibility settings, not a new survival minigame.

The strip shows OSCP, device/site, session identity and role, connection scope, last synchronization and world time/speed. When relevant, show the local tactical hold, active turn and AP separately, so a held area is not mistaken for a stopped world calendar. Clearance is descriptive; the service authorization still enforces it. A minimal login/boot transition can establish identity without delaying every routine interaction.

| Application | Main content | World-backed actions |
|---|---|---|
| Operations | Incidents, priorities, outstanding orders, acknowledgement failures | Assign/recall response, request assessment, escalate to command |
| Containment and surveillance | Procedure compliance, actual qualified observation, feed state, reported locations | Assign coverage/relief, watch feed, request stabilization/receiving |
| Personnel | Employment, shifts, posts, location evidence, qualification, readiness | Schedule duty/relief, issue authorized orders, request replacements |
| Logistics | Located stock, filters, accessible/reserved quantities and delivery records | Haul, consolidate, issue equipment and order supplies |
| Facilities | Room/utility state, alarms, work progress | Validated repair, conversion and fixture-installation work |
| Records | Dossiers, procedures, research notes, evidence, revisions and case links | Read, draft, attach evidence, submit and authorize appropriate revisions |
| Communications | Messages, outside reports, command directives and delivery status | Acknowledge, reply within the simulated network, refer/escalate and request information |
| Research/personal storage | Authorized experiments and personal/digital files | Select available records/equipment, prepare permitted tests and capture results |
| AIAD/AIC and technical support | Assigned digital personnel, service incidents, technical requests and escalation receipts | Request an authorized conscript task or dispatch actual technical staff; show missing/offline support honestly |

These are application domains, not eight mandatory independent desktop programs. M1 can group them into a small navigation set: Operations, Personnel, Stores, Records, Communications and Systems. Research expansion follows SCP-1499. The graphical workspace and command view call identical services; typing a command grants no extra power.

## 4. Orders, receipts and attention

A button or supported command submits an intent with actor/session, target IDs, expected revisions, branch, parameters and idempotency key. Receipt states distinguish draft, queued locally, delivered to service, accepted/rejected, acknowledged by personnel, executing, completed, blocked, cancelled and expired. Do not mark staff coverage restored at “accepted.” Staff must travel, qualify and perform the handoff.

Opening OSCP does not pause real-time exploration. During tactical input waiting, all local simulation remains held: inspecting a record or drafting an intent cannot grant free equipment work, attention changes or completed local orders. Remote running areas and SCiPnet services can continue according to their own execution policy; effects entering a held area follow the declared boundary policy. AIC actions use the same gates. See [time controls](08-player-controls-time-and-continuation.md).

Keep worker acknowledgement and communications delay meaningful. Retries cannot duplicate supply purchases, staff assignments or repairs. If a terminal closes, accepted world orders persist. If a link fails, queued drafts remain visibly pending; resynchronization revalidates authority, capacity and expiry before delivery. A command requiring fresh state cannot execute against an old cached view silently.

Using OSCP is an ordinary timed/attention activity. Inventory and terminal screens do not pause automatically. Typing or opening a dossier can change the avatar's attention according to the interaction definition. A live surveillance task qualifies as observation only while the module's device/feed/operator predicate holds; a background tab does not count indefinitely. An assigned autonomous operator can maintain a feed independently of the player browsing another app.

For SCP-650 show distinct rows for safety-procedure staffing and current qualifying observers. Understaffing can be an alert while one observer still suppresses relocation. A remembered sighting or external report cannot substitute for live observation. Display the effective module/scenario observer profile; the baseline requires humans, including a qualified human attending a live feed. AIC monitoring can report conditions without counting toward that profile. Neither merely leaving a feed open nor switching decision providers changes eligibility.

## 5. Off-facility anomaly reports

An incoming case contains immutable case/message IDs, sender/contact route, received time, claimed event time, location description/uncertainty, observations, attachments, claimed casualties/property changes, confidence/source reliability, handling restrictions and a delivery chain. Do not assign an SCP number just because someone reports an unusual event.

Proposed workflow: receive → triage → correlate evidence → request clarification/remote assessment → authorize an operation if permitted → assign resources/route → receive observations → update the case → close, monitor or escalate. Incorrect reports, false leads, late messages and uncertain locations remain legitimate states. The dossier distinguishes observed facts, claims and hypotheses.

M1 receives a bounded synthetic off-facility report through normal simulated message events. The director can read, acknowledge, attach an existing in-world record and refer/request assessment from command. This does not require a playable town or spawn an imaginary field team. Real generated-world investigation/deployment is qualified with the bounded network in M4. M1 fixtures identify their simulated external source in test evidence, not through developer jargon in the product's normal workflow.

Reports from actual later operations use the same records and receipts. Communications cannot infer hidden entity positions directly from the global simulation. A group's deception or an observer's altered perception can generate a misleading report, while developer causal traces retain what happened.

SCiPnet delivery identifies sender, route, local/remote scope and authorization independently of OSCP chrome. An AIC may triage/correlate reports within its assigned knowledge and authority, but an assessment remains an attributed assessment. Technical Services incidents use separate tickets/work orders; acknowledging a ticket cannot restore a broken network or repair physical equipment instantly.

## 6. Dossiers, evidence and hazardous information

Use structured records with item/case identity, classification, containment procedures, description, discovery/history, experiments and attachments. Support drafts, submitter, revisions, authorizations, redactions/compartment scope and links to relevant people, places, equipment and incidents. Editing narrative text does not itself change an entity's containment or physical state.

Evidence selection uses game-world files/photos/records and permitted device storage. An “upload” transfers a simulated record along an available route; it is not a host-computer file picker or an actual internet upload. Carryable digital media and portable equipment can supply sources when their game actions permit it. Preserve attachment identity, provenance and permissions rather than copying a thumbnail into an untracked blob.

The SCP-5810-inspired inoculation/check state is an interface hook: it can expose a module/role-dependent prerequisite and clear status. It does not automatically neutralize every information hazard. Opening a record, viewing an attachment, holding attention and transmitting content emit scoped exposure events for a future authorized module. M1 needs the hook and inert test fixture, not a full SCP-5810 predator or universal memetics system.

SCP-245-like software can later live on an isolated game-world machine with assigned monitoring duties. It runs as authored game content under host capabilities, not by executing downloaded native files. An anomaly may distort the character's terminal view through the existing perception contract; a misleading display does not gain actual authority or mutate host files.

## 7. Engine integration and persistence

The proposed Godot front end renders OSCP with UI controls and binds to query projections/command endpoints in the custom simulation core. UI state, device records and narrative exposure are separate. The core owns command validation, deadlines, staff work, inventories, messages and world effects. The renderer cannot bypass those boundaries through a hidden button or developer command string.

Persist devices, digital records/revisions/attachments, sessions according to authentication policy, queued message/order IDs, delivery receipts, permissions, observed timestamps and current attention tasks. Store local window arrangement separately from authoritative simulation state. A save/load or day restart cannot deliver the same request twice or retain a session from an invalid branch/identity.

The interactive design preview uses illustrative local state only. It demonstrates navigation, uncertainty, role restrictions and order receipts; it is not connected to a game kernel, real communications or host operating-system controls.

Show resident AIC assignment as confirmed; its personal identity and detailed authority profile remain undecided. Expose the actor's host status, reachable systems, workload and attributed reports through the support view. Initial deterministic duties and a later optional local-model JSON provider share validated services; see [digital operations](13-lore-and-digital-operations.md). The preview's sample host/status values illustrate a design, not a fixed hardware count or a live game service.

## 8. First implementation boundary

M0 proves one terminal query/command bridge with duplicate-request handling and role validation. M1 qualifies operations/coverage, personnel, logistics, structured records/evidence, one outside report chain and offline/isolation behavior. Other applications can share projections or remain unavailable until their actual systems exist.

The OSCP visual name/reference is confirmed direction; exact chrome and whether the default screen is a graphical workspace or command/document view remain design choices. TERM-01 through TERM-09 in [scope and acceptance](04-scope-and-acceptance.md) define the proposed gate.
