# Lore baseline and digital operations

Status: Revision 0.6 design pass. The user requires lore-favored terminology and behavior, with questions for unresolved design ambiguities. Source-backed facts, chosen adaptations and pending decisions must remain visibly distinct. A resident AIC at the starting facility is confirmed; its exact authority profile, identity and canon/timeline remain open.

## Source register

| Source | Verified relevance | Adaptation boundary |
|---|---|---|
| [SCP-245](https://scp-wiki.wikidot.com/scp-245) | OSCP reference and isolated software containment | Does not define every OS feature; no need to execute the attached software |
| [SCP-5810](https://scp-wiki.wikidot.com/scp-5810) | Terminal login, records/evidence interaction and user-facing checks | Its narrative is not a complete operating-system manual |
| [AIAD Homescreen](https://scp-wiki.wikidot.com/aiad-homescreen) | AIC assignments, clearance, principles and specific historical portrayals | Named AICs are not interchangeable generic assistants or automatically resident at every site |
| [AI Classification Guide](https://scp-wiki.wikidot.com/ai-classification-guide) | Different AI capabilities/classifications | Do not assume every automated tool is a sentient AIC |
| [Site-87 Orientation: FAQ](https://scp-wiki.wikidot.com/site-87-faq) | A site portrayal explicitly lacking an assigned AIC | Universal resident-AIC coverage is not established by these sources |
| [Dr. Cimmerian Hits Reply All](https://scp-wiki.wikidot.com/cimmerian-sends-650-pms) | SCiPnet mail and RAISA involvement | Use the communication concepts, not the tale's exceptional mail volume as a normal operating target |
| [Foundation Facilities: Site-15](https://scp-wiki.wikidot.com/secure-facilities-locations) | IT/AIAD, electronic containment and databases/communications | Does not make all proposed Technical Services responsibilities universally canonical |

These references are SCP Wiki works under their linked attribution/license notices. Preserve individual article credits when adapting content. Existing character art is not imported by this pass.

The user has supplied a [local JSON reference archive](../source_material/README.md). Its indexes and targeted content shards support offline lookups, including SCP-650 and the AIAD hub. The archive guide records the inspected revision, actual key/path format and candidate technical-support tales. Archive presence does not establish a single canon or grant all source information to in-world actors. The exact Technical Services hub/title remains unresolved; the inspected “Technical Orientation” tale calls its department “Tech Support.”

## Proposed division of responsibility

**OSCP** is the workstation/device platform and user interface. It supplies sessions, windows/commands, local files and access to installed services. It is not presumed to be an all-knowing sentient operating system.

**SCiPnet** is the Foundation-facing intranet/service layer for authorized records, internal mail, alerts and communications. Local replicas, site services and routes determine reachability; a connection does not grant clearance or reveal all current world state.

**AICs** are in-world software personnel assigned particular work and privileges. An assigned AIC can interpret technical requests, monitor systems, investigate eligible digital incidents and operate authorized frameworks through world-backed services. It retains identity, knowledge, duties, constraints and an auditable action history. Ordinary automated tasks can still run without an AIC.

**Technical Services / site IT** is the human operational responsibility for relevant equipment and technical work. Repairs can require technicians, tools, parts, access and time. The exact department title, reporting structure and claimed auxiliary hub remain pending a specific source/owner choice; do not invent a verified hub URL. Records/clearance governance and AIAD responsibilities must not disappear into an undefined all-purpose department.

## Confirmed resident AIC

The starting facility has a resident AIC. This is an autonomous entity in the shared simulation, with identity, knowledge, authority, responsibilities and physically located computing infrastructure. Hardware, power, connectivity, permissions and available information bound its capabilities. It accesses OSCP, surveillance, communications, facility systems and personnel through normal validated services. Residency does not grant omniscience or unrestricted authority.

Initial implemented duties are bounded monitoring, reporting, operational assistance and authorized automated tasks. Advanced planning, conversation and independent behavior can expand later. Exact standing orders, escalation rights and hazardous-action permissions still need the user's authority decision; “autonomous entity” does not settle every permission.

Deterministic logic must provide a functional AIC without a language model. Keep a versioned JSON provider interface for an optional local model. Regional AIC support over SCiPnet follows later, so a scenario can support resident staff, remote support or both without conflating identities or privileges.

## AIC mechanics to qualify

Represent a conscript as a digital actor with a designated instance/identity, host or authorized deployment, reachable systems, task capacity, clearance, knowledge, doctrine/principles and assigned supervisor. Its requests use the same validated command boundary. Network/power loss or a failed service can prevent action; it cannot physically repair a disconnected cable remotely.

In-world sapience does not require a real language model. Deterministic behaviors, authored dialogue and task logic run without one. An optional LLM can later supply permitted dialogue/intents without increasing authority or replacing validated actions. Sentient presentation and reliable game-state execution are separate concerns.

### Proposed local-model JSON boundary

The AIC actor exists independently of its decision provider. A deterministic provider is the initial implementation. A future optional host adapter may exchange JSON with a configured local model; the transport/vendor and exact schema are not selected by this design pass. Public SCP scripts do not receive arbitrary HTTP or host process access through this feature.

Requests carry a protocol version, request ID, AIC/instance identity, branch and simulation tick, applicable doctrine/authorized action descriptions, a bounded knowledge snapshot with evidence timestamps, and relevant target revisions. Information is filtered through the AIC's actual services and knowledge, rather than serializing the authoritative whole world.

Responses contain typed proposed intents and optional attributed dialogue. Validate schema, size, actor, branch, freshness, target IDs, permissions and world preconditions through the normal command services. A provider cannot grant itself abilities, execute a shell command or rewrite arbitrary state. Hazardous content remains subject to authored exposure rules; it cannot become a trusted instruction merely by appearing in a dossier or model response.

Provider work is asynchronous and must not halt simulation time while inference runs. Timeout, failure, invalid output and stale replies fall back to deterministic duties or explicit escalation. Cancel obsolete requests on branch changes; record committed decisions so save/load or replay does not depend on generating the same model response again. Deadlines, call budgets and model context limits are proposed tuning to qualify later. M1 proves the provider boundary with a deterministic/mock provider, not a real model dependency.

Backups, migration, concurrent instances and identity replication need an explicit policy before implementation; copying an `.aic` game-world file must not silently create unlimited staff.

Observer eligibility is resolved as a module/scenario choice: human, AI or any eligible observing actor, with medium/attention requirements evaluated separately. An AIC processing video may gain evidence and report it without suppressing an observation-dependent anomaly. SCP-650's baseline uses human observers, including qualified emergency live-camera observation. Its article does not address AICs or test AI-only observation; human-only is the chosen conservative game adaptation, not an explicit universal prohibition in the fiction. See [SCP-650](09-first-playable-scp-650.md) and [profile contract](03-scp-module-contract.md#observer-eligibility-profiles).

## Pending questions and decision process

Questions already presented to the user:

1. **Resolved:** resident AIC at the starting site, with later regional support and an optional local-model JSON interface as described above.
2. Advisory, standing-order technical work with escalation, or broad authorized autonomy?
3. Which Technical Services hub/page and which canon/timeline should anchor the portrayal?
4. **Resolved:** module/scenario-configurable observer eligibility; SCP-650 defaults to humans and human-attended live feeds. AIC monitoring/reporting alone does not freeze it under that profile.

Additional decisions before implementing affected behavior: original conscript versus a specific established character; source/adaptation evidence for other SCPs' observer profiles; backup/instance policy; exact authority of records governance and technical staff. Keep these as questions, not hidden defaults.

When ambiguity affects behavior, identify the conflict and source, ask the user, and record their decision and adaptation notes. While awaiting an answer, progress on independent platform/UI/engine work and label alternatives as proposals. Do not promote proposed limits, schedules or a source-specific timeline into universal lore.
