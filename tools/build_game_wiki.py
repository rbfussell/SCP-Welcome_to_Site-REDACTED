#!/usr/bin/env python3
"""Build a local, source-backed game wiki. Requires Python-Markdown and the pinned .NET SDK."""
import argparse
from collections import defaultdict
from functools import lru_cache
from datetime import datetime, timezone
import hashlib
from html import escape
from html.parser import HTMLParser
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
from urllib.parse import unquote, urlsplit
import markdown
from wiki_canon import build_canon
from wiki_items import build_items

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'source_material/game-wiki'
ARCHIVE = ROOT / 'source_material/scp-api'
PAGES = {}
SEARCH = []

def slug(s):
    return re.sub(r'[^a-z0-9]+', '-', s.lower()).strip('-')

def digest(s):
    return hashlib.sha256(s.encode()).hexdigest()

def git(path, *args):
    p = subprocess.run(['git', '-C', str(path), *args], capture_output=True, text=True)
    return p.stdout.strip() if p.returncode == 0 else 'unavailable'

def link(url, label):
    return f'<a href="{escape(url, quote=True)}">{escape(label)}</a>'

def listing(items):
    return '<ul>' + ''.join('<li>' + s + '</li>' for s in items) + '</ul>'

def md(s):
    return markdown.markdown(s, extensions=['tables', 'fenced_code', 'toc', 'sane_lists'])

def add(name, title, body, category, status='', evidence=''):
    assert name not in PAGES, name
    PAGES[name] = (title, body, category, status, evidence)
    SEARCH.append({'url': name, 'title': title, 'category': category, 'status': status,
                   'text': re.sub('<[^>]*>', ' ', body)[:18000]})

def extract():
    # Work around colons in the workspace path; no packages or network are needed.
    with tempfile.TemporaryDirectory(prefix='site-wiki-indexer-') as temp:
        target = Path(temp)
        for p in (ROOT / 'tools/WikiIndexer').iterdir():
            if p.is_file(): shutil.copy2(p, target / p.name)
        env = dict(os.environ, DOTNET_CLI_HOME=str(target / 'cli'), DOTNET_NOLOGO='1',
                   DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
        subprocess.run(['dotnet', 'build', str(target / 'WikiIndexer.csproj'), '-v', 'q'],
                       env=env, check=True)
        result = subprocess.run(['dotnet', str(target / 'bin/Debug/net8.0/WikiIndexer.dll'), str(ROOT)],
                                env=env, check=True, capture_output=True, text=True)
        return json.loads(result.stdout)

FUNCTION_NOTES = {
('SpatialSimulation', 'Tick'): 'Advances the world calendar unless manually paused, advances bounded regional intake, then gates local progress. The local branch advances inventory and either tactical continuation or exploration/personnel movement. It reevaluates observation and player evidence at the committed boundaries visible in the source. It does not simulate a complete offsite world.',
('SpatialSimulation', 'RequestMove'): 'Routes tactical requests through TacticalCombatSystem. Exploration requires a ready local simulation, no unfinished player route, and a reachable known path. Accepted travel cancels pending inventory work and commits a physical route; it does not pre-reveal unseen blockers.',
('SpatialSimulation', 'EvaluatePlayerAwareness'): 'Collects direct observation and bounded delivered voice/radio evidence while local execution is active. It updates acquired contacts and player feedback. A voice or radio report creates evidence but does not itself qualify as visual observation holding an SCP still.',
('SpatialSimulation', 'ReceivePerceivedContact'): 'Accepts a new acquired contact, records why it was noticed, and stops a moving player exactly in place. It clears travel while preserving fractional physical progress and lets the world continue.',
('SpatialSimulation', 'IssueOrderOnChannel'): 'Checks local/tactical readiness, with a bounded prepared-phone exception during manual pause, then delegates authority and channel validation to PersonnelOrderSystem. Submission creates feedback but still awaits delivery and acknowledgement.',
('SpatialSimulation', 'SetModuleFlag'): 'Authoring/debug entry point that validates readiness and forwards a flag change to its owning native anomaly module. This is not an in-world power or a public sandbox permission bypass.',
('SpatialSimulation', 'IssueOscpOrder'): 'Checks the selected device is available and connected, then submits through the phone or terminal channel. Native equipment access and communication delivery still govern the order.',
('SpatialSimulation', 'ApproachTerminal'): 'Chooses a reachable neighbor of the authored workstation, preferring the shortest preview route, then requests ordinary movement. Failure reports lack of physical access instead of opening a remote workstation.',
('Scp650Module', 'Evaluate'): 'Reads qualified observation and records changes. After a relocation it emits one witness reaction per human per encounter. Observation suppresses self-relocation. When the local deadline permits, it builds sorted legal candidates, chooses a person before a position, selects a pose, and requests a host-validated move. No destination schedules retry; rejected commitment restores RNG; success increments the encounter and schedules cooldown.',
('Scp650Module', 'Placement'): 'Chooses module-owned destination constraints from the explicit per-human complacency flag: relaxed two-to-three-hex placement or threatening adjacent rear placement. Both demand clear geometry. The shared host validates the query without interpreting complacency itself.',
('Scp650Module', 'NextRandom'): 'Advances the saved xorshift32 random stream. Relocation selection saves its previous state so a rejected commit can roll the random stream back.',
('Scp650Module', 'TrySetFlag'): 'Accepts only the complacency flag for human targets, adding or removing that actor ID in module state. Other flags and nonhuman targets are rejected with reasons.',
('Scp650Module', 'Inspect'): 'Builds read-only diagnostics from current observation and saved module state, including pose, relocation count, deadline, witnesses and explicit flags. It does not select a new destination or consume RNG.',
('Scp650Module', 'Capture'): 'Serializes module state as a schema-2 ModuleSnapshot with instance and module identity. The physical body is stored by the host rather than duplicated in this state.',
('Scp650Module', 'Restore'): 'Requires schema 2 and validates identity, nonzero RNG, counts, deadlines, observer IDs, witness encounters and human complacency targets before assigning state. Earlier implicit-familiarity snapshots are rejected instead of inventing flags.',
('ObservationService', 'Inspect'): 'Evaluates current sensory detection facts for an actor and target. Callers must distinguish detection from the module-specific eligibility policy. Consult the returned result and rejection reasons in the implementation.',
('AnomalyWorldAccess', 'TryRelocate'): 'Commit boundary for a requested anomaly move. The host rechecks revision, execution and spatial/observation constraints before allowing authoritative body movement. Module intent alone is insufficient permission.',
('PrototypeSaveCodec', 'Restore'): 'Restores through detached candidate validation, checking schema, content identity and subsystem continuation before returning replacement state. A caller must only replace the live world after this succeeds.'
}

SYSTEMS = {
'world': ('World and geometry', 'World/',
    'Owns facility geometry, doors, lighting, physical occupancy, shared state and dynamic blockers. Square construction geometry is distinct from hex actor positions.',
    'Actors validate movement against this state; perception asks about light and occlusion; anomaly commits recheck spatial legality. The renderer reads the same committed facts.',
    'World/FacilityMap.cs', ['02-simulation-and-world.md', '15-spatial-prototype.md'],
    'Lighting is a prototype switch. Circuits, HVAC, thermal state and cross-zone infrastructure are planned.'),
'actors': ('Actors and navigation', 'Actors/',
    'Owns actor state, independent facing, hex routes, movement progress, reservations, rerouting, doors and bounded patrol.',
    'PlayerNavigation plans against known layout and perceived occupants. Movement checks actual collision, updates physical progress, and prompts perception/anomaly reevaluation. Awareness can stop a fractional step without snapping.',
    'Actors/MovementSystem.cs', ['15-spatial-prototype.md', '18-awareness-and-encounter-entry.md'],
    'Full movement animation content, general crowd behavior and broader swept/sub-tick qualification remain pending.'),
'perception': ('Perception and awareness', 'Perception/',
    'Computes detection and qualified observation using facing, attention, blink, lighting, geometry and observer kind. PlayerAwareness tracks acquired contacts and interruption preferences.',
    'SCP-650 consumes eligible observation; feedback consumes acquired evidence. A report can stop player travel but cannot substitute for actual visual observation. Godot camera visibility is not observer eligibility.',
    'Perception/ObservationService.cs', ['18-awareness-and-encounter-entry.md', '09-first-playable-scp-650.md'],
    'General hearing, live human-attended camera feeds and physical/perceived contexts remain future work.'),
'anomalies': ('Anomaly host and SCP modules', 'Anomalies/',
    'Owns native module registration, evaluation, state binding, scoped world queries and validated effects. SCP-650 owns its own rule, target/pose selection, RNG, retry and witness state.',
    'ObservationService supplies facts; AnomalyWorldAccess validates requested moves at commitment. Operations may transport the physical body externally. Save codecs capture body and module state separately.',
    'Anomalies/AnomalySystem.cs', ['03-scp-module-contract.md', '09-first-playable-scp-650.md'],
    'Native interfaces are not a security sandbox. Public scripts, domain generation, perception overrides and modules other than SCP-650 are unimplemented.'),
'operations': ('Personnel orders and recovery', 'Operations/',
    'Owns authored duties, orders, bounded communications, received reports, trolley loading/pushing/unloading and receiving-footprint compliance.',
    'Personnel work through movement and sight; employment arbitrates scheduled duties; OSCP sends through validated channels. One human can suppress relocation while the receiving procedure still requires three. Relocation detaches trolley cargo atomically.',
    'Operations/ObservedTransportSystem.cs', ['19-personnel-orders-and-recovery-prototype.md'],
    'General logistics, all carrier types, a regional command network and full scenario acceptance remain pending.'),
'employment': ('Employment and relief', 'Employment/',
    'Owns scheduled appointments, physical observation posts, relief, bounded overtime, delivered staffing reports and saved continuation.',
    'PersonnelOrderSystem can interrupt duties; actors must physically reach observation posts. Coverage reflects actual qualified presence, not a staffing checkbox. OSCP shows acquired reports.',
    'Employment/EmploymentSystem.cs', ['07-employment-and-orders.md', '22-employment-prototype.md'],
    'Current short local fixture shifts do not implement full professions, needs, hiring, jobs or world-calendar employment.'),
'inventory': ('Physical inventory, clothing and body fit', 'Inventory/',
    'Owns one physical item graph, nested containers, rectangular packing, physical closures, ancestor access, paid manipulation, symmetric body-local hex fit, explicit stored-clothing fit drops and bounded strap spans, pocket masks, actual worn order, overlay bands, nested mount providers and shared exterior clearance.',
    'The facade submits shared transactions; tactical combat charges source-dependent AP and time. Equipment access gates handset/radio services. Body-fit geometry and clothing cover decide real access independently of UI inspection and art. Exact paid plans survive saves.',
    'Inventory/Transactions/InventoryActions.cs', ['23-personal-inventory-prototype.md', '24-body-equipment-and-container-navigation.md', '29-gear-data-standard.md', '30-body-fit-prototype.md', '31-pocket-coverage-prototype.md', '32-belt-and-attachment-prototype.md', '34-wear-fit-rules.md', '35-actual-worn-order.md', '36-overlay-sublayers.md', '37-placement-extenders-and-mount-points.md', '38-body-fit-cleanup.md', '39-body-drag-fitting.md', '26-wearable-appearance-pipeline.md', '28-wearable-metadata-and-surface-mapping.md', '40-body-inspection-and-removal.md', '41-paperdoll-sprite-proof.md', '42-full-body-paperdoll.md', '43-paperdoll-mosaic.md', '44-worn-backpack-paperdoll.md', '45-worn-alpha-masks.md', '46-item-asset-library.md'],
    'General crafting, material portions, spillage, item-backed weapons, protective effects and composited world clothing art are not completed.'),
'combat': ('Tactical turns and training', 'Combat/',
    'Owns local turn/AP scheduling, training equipment and points, pending actions, acquired contacts and autonomous training-opponent decisions.',
    'Movement, facing, doors, equipment and inventory handling use the scheduler. Tactical input holds local simulation while world-calendar time continues. Feedback and saved continuations preserve acquired consequences.',
    'Combat/TacticalCombatSystem.cs', ['20-event-log-and-tactical-training.md', 'adr/0006-real-time-exploration-and-tactical-turns.md'],
    'Full hostility/factions, wounds/death, production weapons, lethal SCP timing and aftermath reveal are not qualified by a training drill.'),
'oscp': ('OSCP and communications', 'Terminals/',
    'Owns native applications, workstation/handset service access, acquired records and a bounded regional-intake fixture.',
    'Device access uses actual inventory and facility reachability. Commands flow to personnel order services; results require delivery. Godot OscpWorkspace can attach to local play or standalone runtime. Phone drafts submit on closing the paused interface.',
    'Terminals/OscpService.cs', ['11-oscp-terminal.md', '13-lore-and-digital-operations.md', '21-oscp-runtime-prototype.md'],
    'Full SCiPnet, resident AIC simulation/provider integration and production regional operations remain planned.'),
'feedback': ('Player feedback and journal', 'Feedback/',
    'Owns bounded acquired player messages, visibility episodes, witness/discovery presentation events and delivery feedback.',
    'Perception and received reports create evidence. The journal presents that evidence; Godot log/audio/overlay adapters render it. Hidden diagnostic state does not automatically become a player message.',
    'Feedback/PlayerFeedback.cs', ['20-event-log-and-tactical-training.md'],
    'General conversation and the complete production information interface remain future work.'),
'diagnostics': ('Diagnostics and causal trace', 'Diagnostics/',
    'Owns a bounded causal trace with world/local timestamps, subjects, causes and reasons.',
    'Domain services record committed events; diagnostic panels inspect true state. Read-only inspection must not consume RNG, create events or grant character knowledge.',
    'Diagnostics/SimulationTrace.cs', ['15-spatial-prototype.md'],
    'Developer diagnostics are not an in-world sensor or AIC information source.'),
'persistence': ('Persistence and continuation', 'Persistence/',
    'Owns outer versioned snapshot validation and detached candidate restore. Domain codecs preserve their own state and pending work.',
    'Content identity binds maps, operations and active inventory definitions. Restore validates actor, body, item, command and module continuation before replacing the live world. An invalid restore leaves current state intact.',
    'Persistence/PrototypeSaveCodec.cs', ['35-actual-worn-order.md', '36-overlay-sublayers.md', '37-placement-extenders-and-mount-points.md', '38-body-fit-cleanup.md', '39-body-drag-fitting.md', '40-body-inspection-and-removal.md', '41-paperdoll-sprite-proof.md', '42-full-body-paperdoll.md', '43-paperdoll-mosaic.md', '44-worn-backpack-paperdoll.md', '45-worn-alpha-masks.md', '46-item-asset-library.md'],
    'Current inventory requires outer schema 14; SCP-650 module schema is 2. General migration and module removal/recovery are not a finished public contract.'),
'presentation': ('Godot presentation and runtime', 'game/',
    'Owns scene lifecycle, input bindings, simulation tick adaptation, sprites, projection, wall cutaways, HUD, inventory windows, OSCP and smoke drivers.',
    'SimulationRuntime connects views to one SpatialSimulation. Panels submit commands and read acquired/query state. WorldView draws committed bodies. Visual wall lowering and diagnostic art selection leave physical collision and sight intact.',
    '', ['06-sprites-and-placeholders.md', '15-spatial-prototype.md', '21-oscp-runtime-prototype.md', '37-placement-extenders-and-mount-points.md', '38-body-fit-cleanup.md', '39-body-drag-fitting.md', '26-wearable-appearance-pipeline.md', '28-wearable-metadata-and-surface-mapping.md', '40-body-inspection-and-removal.md', '41-paperdoll-sprite-proof.md', '42-full-body-paperdoll.md', '43-paperdoll-mosaic.md', '44-worn-backpack-paperdoll.md', '45-worn-alpha-masks.md', '46-item-asset-library.md'],
    'Production sprites and composited clothing remain incomplete. World-map/facility-management entry scenes are planned.'),
'facade': ('Simulation command facade', 'SpatialSimulation.cs',
    'Coordinates validated commands, shared queries, explicit time controls, tick ordering and snapshot entry points.',
    'Godot and OSCP enter through this facade. Domain services own the detailed rules. Tick advances the calendar, then qualified local work; training and exploration follow different local paths.',
    'SpatialSimulation.cs', ['adr/0004-godot-and-simulation-core.md'],
    'The facade is native prototype integration, not a completed public mod API.')
}

def system_for(path):
    if path.startswith('game/'): return 'presentation'
    tail = path.removeprefix('src/SiteSimulation/')
    for key, s in SYSTEMS.items():
        if tail.startswith(s[1]): return key
    return 'facade'

def file_url(path): return 'file-' + slug(path) + '.html'
def doc_url(path): return 'doc-' + slug(path) + '.html'

def build_code(files):
    members, types = [], defaultdict(list)
    by_name = defaultdict(list)
    for f in files:
        for t in f['types']:
            t['path'] = f['path']; t['url'] = 'type-' + slug(t['owner'] + '.' + t['name']) + '.html'
            types[t['url']].append(t)
        for m in f['members']:
            m['path'] = f['path']; m['url'] = 'function-' + digest(f['path'] + ':' + m['owner'] + ':' + m['name'] + ':' + m['kind'] + ':' + re.sub(r'\s+', ' ', m['signature']))[:20] + '.html'
            members.append(m); by_name[m['name']].append(m)
    incoming = defaultdict(set)
    for m in members:
        candidates = set()
        for call in m['calls']:
            name = re.sub(r'<.*>', '', call.split('.')[-1])
            options = by_name.get(name, [])
            qualifier = call.rsplit('.', 1)[0] if '.' in call else ''
            exact = [x for x in options if qualifier == x['owner'] or (not qualifier and x['owner'] == m['owner'])]
            for x in exact or options:
                if x['url'] != m['url']:
                    candidates.add(x['url']); incoming[x['url']].add(m['url'])
        m['candidates'] = candidates
    lookup = {m['url']: m for m in members}
    def member_link(m): return link(m['url'], m['owner'] + '.' + m['name'])
    for m in members:
        f_url = file_url(m['path'])
        system = system_for(m['path'])
        body = '<h1>' + escape(m['owner'] + '.' + m['name']) + '</h1>'
        body += '<p>' + link('system-' + system + '.html', SYSTEMS[system][0]) + ' / ' + link(f_url + '#L' + str(m['line']), m['path'] + ':' + str(m['line'])) + '</p>'
        body += '<p><strong>Declaration kind:</strong> ' + escape(m['kind']) + '. Extracted from current native source; presence does not certify a production feature.</p>'
        body += '<h2>Declaration</h2><pre><code>' + escape(m['signature']) + '</code></pre>'
        if m['documentation']:
            body += '<h2>Source documentation</h2><pre>' + escape(m['documentation']) + '</pre>'
        if (m['owner'], m['name']) in FUNCTION_NOTES:
            body += '<h2>Behavior</h2><p>' + escape(FUNCTION_NOTES[m['owner'], m['name']]) + '</p>'
        body += '<h2>Role in the engine</h2><p>' + escape(SYSTEMS[system][2]) + '</p><p>The source below defines this member’s exact conditions, arguments, effects, and failure paths. The subsystem summary describes its context; it is not an inferred per-function contract.</p>'
        body += '<h2>Candidate outgoing calls</h2><p>Syntax-derived links; overloads and dynamic/interface dispatch are not resolved.</p>'
        body += listing(member_link(lookup[u]) for u in sorted(m['candidates'])) if m['candidates'] else '<p>No repository declaration matched an invocation name.</p>'
        if m['calls']: body += '<details><summary>All invocation expressions, including external calls</summary><pre>' + escape('\n'.join(m['calls'])) + '</pre></details>'
        body += '<h2>Candidate incoming calls</h2>' + (listing(member_link(lookup[u]) for u in sorted(incoming[m['url']])) or '<p>None indexed.</p>')
        related_types = [u for u, ts in types.items() if ts[0]['name'] in m['identifiers'] or ts[0]['name'] == m['owner'].split('.')[-1]]
        body += '<h2>Related types</h2>' + listing(link(u, types[u][0]['name']) for u in sorted(related_types))
        body += '<h2>Implementation</h2><pre><code>' + escape(m['source']) + '</code></pre>'
        add(m['url'], m['owner'] + '.' + m['name'], body, 'Function', 'Native source', m['path'] + ':' + str(m['line']))
    for u, ts in types.items():
        names = {t['name'] for t in ts}
        own = [m for m in members if m['owner'].split('.')[-1] in names and any(m['path'] == t['path'] for t in ts)]
        refs = [f for f in files if f['path'] not in {t['path'] for t in ts} and any(re.search(r'\b' + re.escape(n) + r'\b', f['source']) for n in names)]
        body = '<h1>' + escape(ts[0]['name']) + '</h1><h2>Declarations</h2>' + listing(link(file_url(t['path']) + '#L' + str(t['line']), t['path'] + ':' + str(t['line'])) for t in ts)
        body += '<h2>Members</h2>' + listing(member_link(m) for m in own)
        body += '<h2>Files mentioning this type name</h2><p>Textual references; not resolved dependencies.</p>' + listing(link(file_url(f['path']), f['path']) for f in refs)
        add(u, ts[0]['name'], body, 'Type', 'Native declaration')
    for f in files:
        system = system_for(f['path'])
        body = '<h1>' + escape(Path(f['path']).name) + '</h1><p>' + escape(f['path']) + '</p><p>' + link('system-' + system + '.html', SYSTEMS[system][0]) + '</p>'
        body += '<h2>Types</h2>' + listing(link(t['url'], t['name']) for t in f['types'])
        body += '<h2>Functions and properties</h2>' + listing(member_link(m) + ' · line ' + str(m['line']) for m in f['members'])
        body += '<h2>Source</h2><pre class="source"><code>' + '\n'.join(f'<span id="L{i}"><a class="line" href="#L{i}">{i}</a> {escape(line)}</span>' for i, line in enumerate(f['source'].splitlines(), 1)) + '</code></pre>'
        add(file_url(f['path']), f['path'], body, 'Source', 'Indexed C#')
    add('functions.html', 'Function directory', '<h1>Function directory</h1><p>All declared C# callable members, properties and explicit accessors in the game and simulation. Use search to narrow the catalog. Lambdas and implicit/compiler-generated functions remain in their containing source. Relationships are syntax candidates.</p>' + listing(member_link(m) + ' <small>' + escape(m['path']) + ':' + str(m['line']) + '</small>' for m in sorted(members, key=lambda x: (x['owner'], x['name'], x['path'], x['line']))) , 'Directory')
    add('files.html', 'C# source directory', '<h1>C# source directory</h1>' + listing(link(file_url(f['path']), f['path']) for f in files), 'Directory')
    add('types.html', 'Type directory', '<h1>Type directory</h1>' + listing(link(u, ts[0]['name']) for u, ts in sorted(types.items())), 'Directory')
    for key, s in SYSTEMS.items():
        selected = [f for f in files if system_for(f['path']) == key]
        checks = [p for p in (ROOT / 'tests/SpatialChecks').glob('*.cs') if any(t['name'] in p.read_text() for f in selected for t in f['types'])]
        body = '<h1>' + escape(s[0]) + '</h1><h2>Implemented responsibility</h2><p>' + escape(s[2]) + '</p><h2>Interactions</h2><p>' + escape(s[3]) + '</p><h2>Limits and pending work</h2><p>' + escape(s[6]) + '</p>'
        body += '<h2>Source files and function pages</h2>' + listing(link(file_url(f['path']), f['path']) for f in selected)
        body += '<h2>Design and qualification notes</h2>' + listing(link(doc_url('docs/' + d), d) for d in s[5])
        if key in ('inventory', 'presentation', 'persistence'):
            body += '<h2>Related mechanics</h2><p>' + link('mount-points.html', 'Mount points and placement extenders') + ' · ' + link('body-fit-cleanup.html', 'Symmetric body fit and adjustable straps') + ' · ' + link('body-drag-fitting.html', 'Direct body-fit drops') + ' · ' + link('body-inspection.html', 'Front/back body inspection and removal') + ' · ' + link('full-body-paperdoll.html', 'Full-body sprite qualification and base garments') + ' · ' + link('paperdoll-mosaic.html', 'Engine-rendered paperdoll mosaic') + ' · ' + link('worn-backpack.html', 'Worn backpacks and harnesses') + ' · ' + link('worn-alpha-masks.html', 'Worn-only alpha masks') + ' · ' + link('items.html', 'Item catalog') + ' · ' + link('asset-library.html', 'Asset organization') + '</p>'
        if key in ('inventory', 'presentation'):
            body += '<p>' + link('wearable-appearance.html', 'Confirmed clothing silhouettes and character-matched paperdoll') + '</p>'
        body += '<h2>Existing checks mentioning these types</h2><p>Evidence leads from textual matches, not a fresh test result.</p>' + listing(link('check-' + slug(p.stem) + '.html', p.name) for p in checks)
        body += '<h2>Other shared systems</h2>' + listing(link('system-' + k + '.html', v[0]) for k, v in SYSTEMS.items() if k != key)
        add('system-' + key + '.html', s[0], body, 'Engine system', 'Bounded native prototype')
    add('systems.html', 'Systems directory', '<h1>Systems directory</h1>' + listing(link('system-' + k + '.html', s[0]) + ' — ' + escape(s[2]) for k, s in SYSTEMS.items()), 'Directory')
    (OUT / 'symbols.json').write_text(json.dumps([{'url': m['url'], 'owner': m['owner'], 'name': m['name'], 'kind': m['kind'], 'path': m['path'], 'line': m['line'], 'signature': m['signature'], 'candidate_outgoing': sorted(m['candidates']), 'candidate_incoming': sorted(incoming[m['url']])} for m in members], indent=2))
    return members, types


def build_docs():
    paths = [ROOT / 'README.md', ROOT / 'src/SiteSimulation/README.md', ROOT / 'source_material/README.md']
    paths += sorted((ROOT / 'docs').rglob('*.md')) + sorted((ROOT / 'templates').glob('*.md'))
    path_set = {p.resolve() for p in paths}
    for p in paths:
        relative = p.relative_to(ROOT).as_posix()
        text = p.read_text()
        def rewrite(match):
            label, dest = match.groups()
            if '://' in dest or dest.startswith('#'): return match.group(0)
            parts = dest.split('#', 1); target = (p.parent / parts[0]).resolve()
            if target in path_set:
                url = doc_url(target.relative_to(ROOT).as_posix())
                if len(parts) > 1: url += '#' + parts[1]
            elif target.parent == OUT.resolve() and target.suffix == '.html':
                # Generated wiki pages may not exist yet during a rebuild.
                # Preserve their links; the final link check validates targets.
                url = target.name
                if len(parts) > 1: url += '#' + parts[1]
            elif target.is_file() and target.suffix == '.cs' and str(target).startswith(str(ROOT)) and target.is_relative_to(ROOT / 'src') or target.is_file() and target.suffix == '.cs' and target.is_relative_to(ROOT / 'game'):
                url = file_url(target.relative_to(ROOT).as_posix())
            elif target.is_file() and target.is_relative_to(ROOT):
                url = 'evidence-' + slug(target.relative_to(ROOT).as_posix()) + '.html'
                if url not in PAGES:
                    try: raw = target.read_text()
                    except (UnicodeError, OSError): return label + ' (asset: ' + parts[0] + ')'
                    add(url, target.relative_to(ROOT).as_posix(), '<h1>' + escape(target.name) + '</h1><pre>' + escape(raw) + '</pre>', 'Evidence')
            else:
                return label + ' (`' + dest + '`; repository path)'
            return '[' + label + '](' + url + ')'
        rendered = md(re.sub(r'\[([^\]]+)\]\(([^)]+)\)', rewrite, text))
        title = re.search(r'^# (.+)', text, re.M)
        title = title.group(1) if title else p.stem
        body = '<aside class="notice">Project document snapshot. Read historical status and version statements in context; see the ' + link('status.html', 'current implementation ledger') + '. Source: ' + escape(relative) + '.</aside>' + rendered
        add(doc_url(relative), title, body, 'Design document', 'Source document', relative)
    add('documents.html', 'Design library', '<h1>Design library</h1><p>Original project documents rendered with internal links. Historical claims are preserved; current-state synthesis is in the implementation ledger.</p>' + listing(link(doc_url(p.relative_to(ROOT).as_posix()), p.relative_to(ROOT).as_posix()) for p in paths), 'Directory')
    for p in sorted((ROOT / 'tests/SpatialChecks').glob('*.cs')):
        add('check-' + slug(p.stem) + '.html', p.name, '<h1>' + escape(p.name) + '</h1><p>Existing verification source; this documentation build does not execute this check.</p><pre><code>' + escape(p.read_text()) + '</code></pre>', 'Check source', 'Not run by wiki build', p.relative_to(ROOT).as_posix())
    return paths

SCP = {
'650': ('Native prototype',
    'A featureless humanoid statue with varied startling postures and silent relocation. The adopted baseline uses human attention; AI-only observation does not suppress movement under that profile. Three personnel is a containment procedure, while one eligible observer is sufficient to suppress self-relocation. There is no ordinary hunt, chase or attack routine.',
    'When unobserved, the module selects a human target, then a legal destination. Threat placement is one hex behind the target; explicit complacency permits two to three hexes without rear-only placement. Both require clear geometry and an unobserved destination. No candidate schedules a 500 local-ms retry. Success schedules a 2000 local-ms cooldown. These are game values. Six poses share one body footprint; internal pose IDs are zero-based. Discovery does not automatically enable complacency. First human discovery holds that witness for 400 local ms; the shared awareness service can separately clear player travel without pausing clocks.',
    'Native module, deterministic saved RNG, direct observation, profile fixtures, host-validated relocation, witness/complacency state, textured pose presentation, bounded voice/device orders, trolley recovery and receiving checks are present. Chen and Ortiz now have actual scheduled observation appointments and relief in the bounded employment fixture.',
    'Human-attended live cameras, full fear/doctrine, automatic complacency metric, general logistics, complete containment procedures, production AIC and full M1 certification are pending. AI/any profile tests are not an implemented surveillance network.',
    ['anomalies', 'perception', 'operations', 'employment', 'feedback', 'persistence'], ['09-first-playable-scp-650.md', '19-personnel-orders-and-recovery-prototype.md', '22-employment-prototype.md'],
    'ObservationService → Scp650Module.Evaluate → candidate query → AnomalyWorldAccess.TryRelocate → one committed body/pose/revision → witness/awareness/feedback. External observed transport is explicitly permitted by this adaptation. If relocation occurs while loaded, operations detach cargo without duplicating the statue.'),
'1499': ('Planned second module',
    'A gas mask whose account describes disappearance, an unusual environment, and return. The exploration material links perceived events with an incident elsewhere. The game preserves that uncertainty rather than declaring a universal alternate-dimension explanation. Creature, tower and mask references establish art direction, not physical rules.',
    'The planned demonstration distinguishes physical location, per-observer perceived context, evidence, versioned target mappings and cross-context links. Wearing/removing the mask would request validated transfer/context changes. A bounded first proof proposes one wearer and explicit return/anchor recovery. Generated spaces, independent inhabitants and ordinary-world consequences are requirements, not currently playable systems.',
    'Design contracts, architecture decision and user-supplied art references exist. No native Scp1499 module, mask activation, procedural domain, context resolver or transfer continuation was found in the indexed C# source.',
    'The entire playable demonstration: authored mask item behavior, domain generation, context/target mapping, entities, return policy, persistent linked effects and acceptance checks. Exact mapping/ontology remains open.',
    ['anomalies', 'perception', 'inventory', 'world', 'persistence'], ['10-world-perception-and-scp-1499.md', 'adr/0003-physical-perceived-contexts.md'],
    'A committed transfer or sensory change must update shared observer eligibility before anomaly evaluation. SCP-650 could then move if its last eligible observer loses sight. Another remaining observer still suppresses it. This interaction should emerge through shared services; no special 1499-to-650 callback is adopted.'),
'019': ('Infrastructure design reference',
    'The source describes a vase producing SCP-019-2 entities. Idle containment specifies 0°C; production still occurs at rest. Disturbance, threats, extreme temperatures, abrupt environmental change and inserted matter can worsen emergence. Source uncertainty and treatment resistance matter.',
    'A proposed environmental fixture would connect actual cooling capacity and room temperature to authored emergence rules, observation and powered treatment. Incineration introduces heat and supply use, followed by physical stabilization. Cold is an operating condition, not a universal off switch. No per-degree multiplier or device rating has been selected.',
    'An infrastructure design check exists; the prototype has only debug lighting. No native SCP-019 module, thermal resolver, incinerator process or SCP-019-2 population is present.',
    'Electrical topology, thermal integration, sensors/controllers, incinerator dependencies, spawn/disturbance rules, resistant entities and a reviewed adaptation. No SCP-019 campaign is promised.',
    ['world', 'anomalies', 'operations', 'perception'], ['17-facility-infrastructure.md'],
    'Future shared environmental services would supply actual temperature and supply; modules would request scoped effects and entity creation. Qualified sensors create evidence separately from true conditions. Local tactical hold must stop local heat/fuel/exposure progress.'),
'087': ('Planned capability fixture',
    'A staircase with no established endpoint, restricted visibility, unusual light behavior and a distant sound that descent does not bring closer. The game uses it as an extensible-domain reference.',
    'Proposed descending-domain segments would retain persistent identity, traversal and sensory constraints without materializing an actually infinite map. Exact geometry, light and sound rules require an authored fixture.',
    'Module-contract design only. No staircase generator or SCP-087 native module is present.',
    'Generated descending domain, segment persistence, sensory behavior and recovery/unload policies.',
    ['anomalies', 'world', 'perception', 'persistence'], ['03-scp-module-contract.md'],
    'Would require qualified domain generation, spatial links, sensory policies and persistent segment state. Those capabilities are broader than the current single-facility map.'),
'173': ('Planned observation/lethality fixture',
    'Direct sight immobilizes the entity in the source; coordinated blinking is a containment concern. This hostile reference is distinct from nonaggressive SCP-650.',
    'The design requires a separate media policy and an unseen lethal approach that may complete without guaranteed player input. Actual qualified sight before commitment must apply its movement constraint. Camera/UI knowledge alone cannot freeze it. Exact speed and attack deadlines are game tuning, not article measurements.',
    'Shared observation and awareness infrastructure exists, but no SCP-173 native module or lethal action implementation is present.',
    'Hostile module, qualified media rules, lethal timing, after-death reveal and acceptance fixtures. This wiki imports no sculpture photograph.',
    ['anomalies', 'perception', 'combat', 'feedback'], ['03-scp-module-contract.md', '18-awareness-and-encounter-entry.md'],
    'Would consume shared sight/attention while owning its movement/attack policy. Encounter transitions must preserve deadlines, and actual acquired evidence determines any reaction window.'),
'096': ('Timing comparison only',
    'Viewing its face, including eligible recorded media, is the cited source trigger; seeing the body is a different event.',
    'Used to explain why noticing an already pursuing threat does not reset distress/pursuit or guarantee a turn. A synthetic pursuit fixture is proposed to test zero and nonzero response windows.',
    'Awareness design discussion only; no native SCP-096 module or pursuit campaign is present.',
    'A full adaptation is outside the adopted milestone. Face/media exposure, pursuit and lethal behavior have no implementation here.',
    ['perception', 'combat', 'anomalies'], ['18-awareness-and-encounter-entry.md', '04-scope-and-acceptance.md'],
    'Any later adaptation would distinguish exposure evidence from mere detection and preserve pursuit state across tactical mode changes and saves.'),
'914': ('Planned transformation fixture',
    'An intake/output apparatus with multiple settings that refines placed objects. It is a reference for atomic item transformation, not a universal crafting recipe table.',
    'The proposed fixture would validate input ownership, settings, costs and output before committing one transformation. General composition/crafting direction does not specify particular SCP-914 outcomes.',
    'Physical inventory and transactions exist. No native SCP-914 module, refining device or output rules are implemented.',
    'Authored transformation outputs/settings, transaction capability, failure/recovery behavior and module acceptance.',
    ['inventory', 'anomalies', 'persistence'], ['03-scp-module-contract.md', '16-item-composition-and-crafting.md'],
    'Would operate through validated item transactions and persistent identities. Outputs must not silently duplicate input matter/items or bypass declared anomalous effect permissions.'),
'3008': ('Planned expandable-domain fixture',
    'A retail interior much larger than the exterior, trapped inhabitants and hostile entities; literal infinity remains speculative in the project’s source interpretation.',
    'The proposed fixture uses persistent generated chunks, inhabitants and domain links. Unloading a visual chunk must not erase occupants, equipment or consequences.',
    'Contract/design only; no native SCP-3008 module, generated retail domain or population exists.',
    'Domain generator, persistent chunks/inhabitants, access/exit rules, qualified coarse simulation and save continuation.',
    ['anomalies', 'world', 'actors', 'persistence'], ['03-scp-module-contract.md'],
    'Would extend shared world/domain services with bounded generation and persistent state. A renderer-only endless map cannot satisfy the design.'),
'127': ('Illustrative content reference',
    'Referenced by the game’s anomalous-ammunition/component discussion. The project has not selected a playable lore adaptation for this SCP.',
    'No particular SCP-127 interaction, multiplier or construction recipe is adopted. Proposed assembled weapons, magazines, rounds and anomalous traits are future physical composition examples.',
    'A design mention only. Native inventory exists, but not an SCP-127 module or item-based anomalous weapon.',
    'A reviewed adaptation and all SCP-specific behavior; general crafting and material accounting are also pending.',
    ['inventory', 'combat', 'anomalies'], ['16-item-composition-and-crafting.md'],
    'If later adopted, its item behavior would need shared ownership, composition, access, equipment and effect-validation services. Current training equipment is not evidence of that integration.'),
'245': ('OSCP lore source only',
    'Used as a source for the OSCP name and isolated software containment. Its narrative does not specify every operating-system feature or require executing the attached software.',
    'No playable anomaly mechanics are adopted. The game’s OSCP platform, sessions and service split are its own chosen adaptation.',
    'Native OSCP exists; an SCP-245 module and its anomalous software do not.',
    'No SCP-245 campaign or software-execution fixture is selected.',
    ['oscp'], ['11-oscp-terminal.md', '13-lore-and-digital-operations.md'],
    'Informs the diegetic interface. Native applications issue validated simulation commands; they do not execute SCP downloads or grant host shell access.'),
'5810': ('OSCP presentation source only',
    'Provides precedent for terminal login, records/evidence interaction and user-facing checks. It is not a complete OS specification.',
    'No playable anomaly rule is adopted. Terminal screen references inform the interface; the visible faux CRT casing is a separate user-selected art direction.',
    'OSCP records, commands and workstation/phone shells exist. No SCP-5810 module exists.',
    'No full adaptation has been scoped.',
    ['oscp', 'presentation', 'feedback'], ['13-lore-and-digital-operations.md', 'reviews/oscp-device-source-checks.md'],
    'Informs presentation and acquired records; underlying world/service access remains governed by the same simulation.'),
'5098': ('SCiPnet handset source only',
    'Cited for a Foundation-issued smartphone associated with SCiPNET access. It does not establish a universal handset or security specification.',
    'No playable anomaly mechanic is adopted; this source supports the plausibility of a Foundation phone.',
    'A native phone interface and physical device access exist. No SCP-5098 anomaly exists.',
    'No SCP-specific implementation is scoped; the complete SCiPnet network remains future work.',
    ['oscp', 'inventory', 'presentation'], ['13-lore-and-digital-operations.md', 'reviews/oscp-device-source-checks.md'],
    'The phone uses actual equipment access and authorized services. The article reference grants neither omniscience nor a universal network connection.'),
'5457': ('Field communications source only',
    'Cited as a mission portrayal in which an agent reads SCiPNET messages on a phone. It does not establish workstation-equivalent phone capabilities.',
    'No playable anomaly mechanic is adopted. Mission/team communication on the secure phone is a game adaptation.',
    'Bounded phone commands, records and communications are native. No SCP-5457 module exists.',
    'No SCP-specific module is scoped. General field communications and full networking remain future work.',
    ['oscp', 'operations', 'presentation'], ['13-lore-and-digital-operations.md', 'reviews/oscp-device-source-checks.md'],
    'Informs the device/service split; command authority, access and delivery are still validated by shared simulation services.')
}


def build_scps(docs, files):
    texts = {p.relative_to(ROOT).as_posix(): p.read_text() for p in docs}
    for folder in ['game/Content', 'schemas']:
        for p in (ROOT / folder).glob('*.json'): texts[p.relative_to(ROOT).as_posix()] = p.read_text()
    for f in files: texts[f['path']] = f['source']
    designations = sorted(set(re.findall(r'SCP-(\d+)', '\n'.join(texts.values()))), key=int)
    unknown = set(designations) - SCP.keys()
    if unknown: raise RuntimeError('Write researched SCP entries for: ' + ', '.join(sorted(unknown)))
    index_path = ARCHIVE / 'docs/data/scp/items/index.json'
    metadata = json.loads(index_path.read_text())
    shards = {}
    article_evidence = {}
    for number in designations:
        entry = metadata.get('SCP-' + number, {})
        if entry.get('content_file'):
            base = index_path.parent.resolve()
            p = (base / entry['content_file']).resolve()
            if not p.is_relative_to(base): raise ValueError('Unsafe archive shard')
            if p not in shards: shards[p] = json.loads(p.read_text())
            article = shards[p].get('SCP-' + number, {})
            raw = article.get('raw_source', '')
            # Kept as research evidence, never rendered as trusted archive HTML.
            article_evidence['SCP-' + number] = {'url': entry.get('url'), 'creator': entry.get('creator'),
                'shard': p.relative_to(ROOT).as_posix(), 'source_sha256': digest(raw),
                'opening_text': raw[:5000]}
        status, lore, mechanics, implemented, pending, systems, sources, interaction = SCP[number]
        name = 'SCP-' + number
        body = '<h1>' + name + '</h1><p class="status">' + escape(status) + '</p>'
        body += '<h2>In-game lore and adaptation</h2><p>' + escape(lore) + '</p>'
        body += '<h2>In-game mechanics</h2><p>' + escape(mechanics) + '</p>'
        body += '<h2>Implemented</h2><p>' + escape(implemented) + '</p><h2>Not implemented / unresolved</h2><p>' + escape(pending) + '</p>'
        body += '<h2>How it interacts with the engine</h2><p>' + escape(interaction) + '</p>'
        body += listing(link('system-' + k + '.html', SYSTEMS[k][0]) for k in systems)
        body += '<h2>Project evidence</h2>' + listing(link(doc_url('docs/' + s), s) for s in sources)
        mention_docs = [p for p, text in texts.items() if re.search(r'\b' + re.escape(name) + r'\b', text)]
        body += '<details><summary>All indexed project mentions</summary>' + listing(link(file_url(p) if p.endswith('.cs') else doc_url(p) if p.endswith('.md') else 'evidence-' + slug(p) + '.html', p) for p in mention_docs if p.endswith(('.cs', '.md'))) + '</details>'
        body += '<h2>Source and attribution</h2><p>' + link(entry.get('url', 'https://scp-wiki.wikidot.com/scp-' + number), name + ' — SCP Wiki') + '. Archive creator metadata: ' + escape(str(entry.get('creator', 'not supplied'))) + '. SCP-derived summary: <a href="https://creativecommons.org/licenses/by-sa/3.0/">CC BY-SA 3.0</a>. Consult the original article for full credits and canon context.</p>'
        body += '<p>The local archive was consulted as reference data. The game’s adopted rules and implementation status above remain distinct from that article. ' + link('research.html', 'Research boundaries and archive provenance') + '.</p>'
        add('scp-' + number + '.html', name, body, 'SCP', status)
    (OUT / 'article-evidence.json').write_text(json.dumps(article_evidence, ensure_ascii=False, indent=2))
    add('scp-index.html', 'SCP directory', '<h1>SCP directory</h1><p>Every SCP referenced in the indexed game/design sources. Only SCP-650 has a native module; reference entries are not promised playable content.</p><table><thead><tr><th>File</th><th>Game status</th></tr></thead><tbody>' + ''.join('<tr><td>' + link('scp-' + n + '.html', 'SCP-' + n) + '</td><td>' + escape(SCP[n][0]) + '</td></tr>' for n in designations) + '</tbody></table>', 'Directory')
    return designations


def cross_archive_research():
    # Stream one shard at a time; do not load the whole clone or trust article markup.
    findings = []
    for collection in ['items', 'tales', 'goi', 'hubs']:
        base = ARCHIVE / 'docs/data/scp' / collection
        paths = sorted(base.glob('content_*.json')) if collection != 'hubs' else [base / 'index.json']
        for p in paths:
            if p.name == 'content_index.json': continue
            records = json.loads(p.read_text())
            for key, record in records.items():
                if not isinstance(record, dict): continue
                raw = record.get('raw_source') or record.get('raw_content') or ''
                matches = sorted(set(re.findall(r'\bSCP-(?:650|1499)\b', raw, re.I)))
                if not matches: continue
                if key in ['SCP-650', 'SCP-1499']: continue
                passages = []
                for match in re.finditer(r'\bSCP-(?:650|1499)\b', raw, re.I):
                    passages.append(re.sub(r'\s+', ' ', re.sub('<[^>]*>', '', raw[max(0, match.start()-150):match.end()+220])))
                    if len(passages) == 2: break
                findings.append({'page': key, 'url': record.get('url') or 'https://scp-wiki.wikidot.com/' + key.lower(),
                                 'creator': record.get('creator', 'unknown'), 'mentions': matches, 'passages': passages,
                                 'shard': p.relative_to(ROOT).as_posix()})
    (OUT / 'archive-research.json').write_text(json.dumps(findings, ensure_ascii=False, indent=2))
    body = '<h1>Cross-archive research register</h1><p>Discovery leads for SCP-650 and SCP-1499 from item, tale, GOI and hub text in the local archive. Navigation links, quoted titles and license fragments can produce mentions. These excerpts have not been reconciled into one canon or adopted as game rules. Existing design research remains the adaptation authority.</p>'
    body += '<p>' + str(len(findings)) + ' matching pages. ' + link('archive-research.json', 'Machine-readable register') + '.</p>'
    for r in findings:
        body += '<h2>' + link(r['url'], r['page']) + '</h2><p>Archive creator: ' + escape(str(r['creator'])) + ' · ' + escape(r['shard']) + '</p>'
        body += ''.join('<blockquote>' + escape(s) + '</blockquote>' for s in r['passages'])
    body += '<p>Excerpts from the linked SCP Wiki works, CC BY-SA 3.0; follow each source for full attribution.</p>'
    add('archive-research.html', 'Cross-archive research register', body, 'Research', 'Discovery leads')
    return len(findings)


NAV = [('index.html', 'Main archive'), ('items.html', 'Item catalog'), ('scp-index.html', 'SCP files'), ('engine.html', 'Engine overview'),
       ('systems.html', 'Shared systems'), ('functions.html', 'Function directory'), ('types.html', 'Types'),
       ('files.html', 'Source files'), ('documents.html', 'Design library'), ('status.html', 'Implementation ledger'),
       ('research.html', 'Research & credits'), ('canon-index.html', 'Canon sources'), ('archive-research.html', 'Archive research')]

def render():
    for name, (title, body, category, status, evidence) in PAGES.items():
        nav = ''.join('<a' + (' aria-current="page"' if name == u else '') + ' href="' + u + '">' + t + '</a>' for u, t in NAV)
        html = '<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>' + escape(title) + ' | Site-[REDACTED] archive</title><link rel="stylesheet" href="assets/wiki.css"><script src="search-index.js" defer></script><script src="assets/wiki.js" defer></script></head><body>'
        html += '<a class="skip" href="#content">Skip to content</a><header><div class="masthead"><a href="index.html" class="brand">SCP FOUNDATION<span>Welcome to Site-[REDACTED]</span></a><span class="archive-label">GAME DEVELOPMENT ARCHIVE</span></div><div class="redline"><span>Secure. Contain. Protect.</span><span>Internal reference • source-backed edition</span></div></header>'
        html += '<div class="layout"><aside class="sidebar"><label for="wiki-search">Search the archive</label><input id="wiki-search" type="search" placeholder="SCP, function, system…" autocomplete="off"><div id="search-info" role="status" aria-live="polite"></div><nav aria-label="Archive navigation">' + nav + '</nav><p class="side-note">Game adaptation and engineering evidence.<br>Not player knowledge.</p></aside><main id="content"><div class="breadcrumb">' + link('index.html', 'Site archive') + ' / ' + escape(category) + '</div><section id="search-results" hidden><h1>Search results</h1><div id="results"></div></section><article id="article">' + body + '</article><footer>Generated from local project sources. ' + link('research.html', 'Evidence, credits & refresh') + ((' · ' + escape(evidence)) if evidence else '') + '</footer></main></div></body></html>'
        (OUT / name).write_text(html)
    (OUT / 'search-index.js').write_text('window.WIKI_INDEX=' + json.dumps(SEARCH, ensure_ascii=False).replace('<', '\\u003c') + ';\n')


class Links(HTMLParser):
    def __init__(self): super().__init__(); self.links = []; self.ids = set()
    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if 'id' in attrs: self.ids.add(attrs['id'])
        for attr in ['href', 'src']:
            if attr in attrs: self.links.append(attrs[attr])

def check():
    errors = []; pages = {}
    for p in OUT.glob('*.html'):
        parser = Links(); parser.feed(p.read_text()); pages[p] = parser
    for p, parser in pages.items():
        for url in parser.links:
            split = urlsplit(url)
            if split.scheme or split.netloc: continue
            target = (p.parent / unquote(split.path)).resolve() if split.path else p.resolve()
            if not target.exists(): errors.append(f'{p.name}: missing {url}')
            elif split.fragment and target.suffix == '.html' and target in pages and split.fragment not in pages[target].ids:
                errors.append(f'{p.name}: missing anchor {url}')
    manifest = json.loads((OUT / 'manifest.json').read_text())
    actual_files = {p.relative_to(ROOT).as_posix() for base in ['src', 'game'] for p in (ROOT / base).rglob('*.cs') if not any(x in p.parts for x in ['obj', 'bin', '.godot'])}
    indexed = set(manifest['source_hashes'])
    if actual_files != indexed: errors.append('C# source coverage differs; rebuild wiki.')
    for path, expected in manifest['source_hashes'].items():
        if digest((ROOT / path).read_text()) != expected: errors.append('Stale source: ' + path)
    for path, expected in manifest['evidence_hashes'].items():
        if digest((ROOT / path).read_text()) != expected: errors.append('Stale evidence: ' + path)
    for path, expected in manifest.get('binary_hashes', {}).items():
        file = ROOT / path
        if not file.is_file() or hashlib.sha256(file.read_bytes()).hexdigest() != expected: errors.append('Stale item artwork: ' + path)
    for path, expected in manifest.get('archive_evidence_hashes', {}).items():
        if digest((ARCHIVE / path).read_text()) != expected: errors.append('Stale canon source: ' + path)
    @lru_cache(maxsize=2)
    def archive_records(path):
        return json.loads(path.read_text())
    for key, entry in json.loads((OUT / 'canon-index.json').read_text()).items():
        if not entry['available']: continue
        local = json.loads((OUT / entry['local_json']).read_text())
        original = archive_records(ARCHIVE / entry['dataset_path'])[entry['record_key']]
        if local != original: errors.append('Canon JSON differs from archive record: ' + key)
        if digest((OUT / entry['local_json']).read_text()) != entry['record_sha256']: errors.append('Canon JSON hash differs: ' + key)
    if errors: raise SystemExit('\n'.join(errors[:100]) + f'\n{len(errors)} validation errors')
    print(f'Wiki validation passed: {len(pages)} HTML pages; all local links and anchors; {len(indexed)} C# files; source/evidence hashes current.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Validate existing pages, links, anchors and freshness; do not rebuild.')
    args = parser.parse_args()
    if args.check: check(); return
    OUT.mkdir(parents=True, exist_ok=True)
    # Only remove files recorded by an earlier build, never authored material.
    old = OUT / 'manifest.json'
    if old.exists():
        for name in json.loads(old.read_text()).get('generated_pages', []):
            if Path(name).name == name and name.endswith('.html'): (OUT / name).unlink(missing_ok=True)
    files = extract()
    members, types = build_code(files)
    docs = build_docs()
    scps = build_scps(docs, files)
    matches = cross_archive_research()
    for p in sorted((OUT / 'content').glob('*.md')):
        name = 'index.html' if p.stem == 'home' else p.stem + '.html'
        text = p.read_text()
        if p.stem == 'research': text += '\n\n[Cross-archive discovery register](archive-research.html) · [Article evidence and source hashes](article-evidence.json)\n'
        title = re.search(r'^# (.+)', text, re.M).group(1)
        add(name, title, md(text), 'Guide')
    item_sources, binary_hashes, item_count, item_categories = build_items(ROOT, OUT, add)
    archive_hashes, canon_count = build_canon(PAGES, add, ARCHIVE, OUT, slug, digest, link, listing, git(ARCHIVE, 'rev-parse', 'HEAD'))
    evidence_paths = docs + sorted((OUT / 'content').glob('*.md')) + sorted((ROOT / 'game/Content').glob('*.json')) + sorted((ROOT / 'schemas').glob('*.json')) + sorted((ROOT / 'tests/SpatialChecks').glob('*.cs'))
    evidence_paths += item_sources + [OUT / 'assets/wiki.css', ROOT / 'tools/build_game_wiki.py', ROOT / 'tests/test_wiki_items.py']
    manifest = {'built_utc': datetime.now(timezone.utc).isoformat(), 'game_revision': git(ROOT, 'rev-parse', 'HEAD'),
        'archive_revision': git(ARCHIVE, 'rev-parse', 'HEAD'), 'source_files': len(files), 'declared_members': len(members),
        'types': len(types), 'scp_entries': ['SCP-' + n for n in scps], 'cross_archive_matches': matches, 'canon_snapshots': canon_count, 'archive_evidence_hashes': archive_hashes,
        'item_catalog': {'definitions': item_count, 'categories': item_categories}, 'binary_hashes': binary_hashes,
        'source_hashes': {f['path']: digest(f['source']) for f in files},
        'evidence_hashes': {p.relative_to(ROOT).as_posix(): digest(p.read_text()) for p in evidence_paths},
        'generated_pages': sorted(PAGES), 'call_graph': 'syntax candidate links; not semantically resolved'}
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2))
    render(); check()
    print(f'Built {len(scps)} SCP entries, {len(SYSTEMS)} system pages, {len(members)} member pages, {len(types)} type pages.')

if __name__ == '__main__': main()
