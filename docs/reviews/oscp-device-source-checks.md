# OSCP workstation and field-phone source check

Date: October 24, 2026. This is design research and a device/presentation clarification; it adds no runnable phone or finished CRT scene.

## Evidence and attribution

| Source | Inspected evidence | Design boundary |
|---|---|---|
| [SCP-5810](https://scp-wiki.wikidot.com/scp-5810), Lt Flops, CC BY-SA 3.0 | OSCP boot/session panel; aged workstation, adjustable brightness, dossier editing, storage and evidence interaction. Its local `.oscp` CSS styles the boot block with muted red text, a dark red background and a ridged border. | The CSS styles that panel, not a specification for every OSCP application. CRT hardware, casing and universal OSCP palette are not established by this page. |
| [SCP-5098](https://scp-wiki.wikidot.com/scp-5098), DianaBerry, CC BY-SA 3.0 | Containment procedures provide a Foundation-issued smartphone associated with SCiPNET access. | Establishes a phone/network precedent in this portrayal, not a universal handset model, security protocol or field capability set. |
| [SCP-5457](https://scp-wiki.wikidot.com/scp-5457), Guezma, CC BY-SA 3.0 | A mission transcript depicts an agent reading SCiPNET messages on her phone. | Supports field message access; does not specify a mobile OSCP client or full management tools. |
| [That's the Spirit!](https://scp-wiki.wikidot.com/that-s-the-spirit), Zolgamax, CC BY-SA 3.0 | A Foundation-issued phone receives a mission report with coordinate, recording and provisional-object-file attachments. | This Gulf-canon story's report is part of a prank. It still depicts the delivery interface, but neither its intelligence nor all of its setting details become general game rules. |

The physical faux CRT surround and the fixed-workstation/full-management versus portable/local-mission split are user-selected game adaptations. The consulted sources do not establish a universal standardized secure phone running OSCP. Implement that client as our Foundation-issued device profile using these compatible precedents, with explicit capability/authority boundaries.

Boot-panel CSS reference: text `hsl(5, 59%, 66%)` / `#db7e75`, background `hsl(5, 100%, 11%)` / `#380500`, border `hsl(5, 60%, 44%)` / `#b4382d`. These style the article's boot block. No source graphics/images are imported by this pass.

## Research method

Read the [local archive guide](../../source_material/README.md), resolve SCP-5810 and SCP-245 through the items index and inspect their wikitext. Search available item, tale, GOI and hub bodies for Foundation-issued phones, secure/encrypted handhelds and nearby SCiPNET/OSCP mentions, parsing one shard at a time. The scan returned 95 candidate pattern matches, including unrelated email/phone-word overlaps; matches were read in context rather than counted as corroborating facts. The inspected archive revision remains `278a69a68e4b85ac7135d061b9fbd6f1027e1a96`.

Verify the live SCP-5810, SCP-5098, SCP-5457 and That's the Spirit! pages against the relevant local passages. Other candidates included explicit SCiPNET-phone mentions and phone queries in tales; these were unnecessary to establish the bounded precedents above. Absence of a standardized handset in this research is not proof that none exists anywhere on the wiki.

## Confirmed design and remaining questions

Full OSCP opens from fixed, physically accessible workstations at facilities. The interface visibly includes the CRT monitor body framing its active screen; a flat window with a scanline shader alone does not meet that presentation target. Its software view supplies the strategic construction, mission planning, intelligence and equipment/material/network-management workspace.

The second profile is a carried secure Foundation phone for local-area mapping, current mission information and communications over permitted local/SCiPNET routes. A phone does not grant full workstation features merely because the actor has senior clearance. The user confirms limited mission/team requests, including backup and assigned-team commands, as well as records and communications. Both device profiles should support additional registered applications later. Exact navigation remains open.

Both profiles use shared records, acquired knowledge, validated services and normal device/session/time rules. Standalone OSCP launch uses an explicit test session/device context; combined play requires the real accessible device. Neither profile adds automatic pause or makes unobserved personnel and anomalies visible on a map. See [the terminal contract](../11-oscp-terminal.md) and [independent-mode decision](../adr/0007-independent-gameplay-modes.md).

The phone/inventory screen is planned as a shared character menu, with inventory on the right and a phone companion pane. Exact arrangement is deferred. Whether opening it automatically pauses is an explicit outstanding clarification; the existing menu/time baseline remains in effect pending that answer. These layout/application decisions do not claim source-defined OSCP phone features.
