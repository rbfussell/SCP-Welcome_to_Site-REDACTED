"""Project-neutral, append-only agent selection and turn lifecycle.

The host supplies actual subagents. This module records their invocation receipts;
it does not call a paid model or pretend that recording a role spawns one.
"""
from contextlib import contextmanager
from datetime import datetime, timezone
import fcntl
import hashlib
import json
from pathlib import Path
import re
import copy
import uuid
from preflight import Preflight, TERMINAL

CORE = {'ENG-00', 'SHO-12', 'EVC-28', 'IA-11', 'DOC-10'}
MANDATORY_RUN = CORE - {'SHO-12'}
STATES = ['INITIAL_REVIEW', 'AGENT_SELECTION', 'RESEARCH_AND_EXECUTION',
          'HANDOFF_COLLECTION', 'INTERNAL_AFFAIRS_REVIEW', 'DOCUMENT_CHIEF', 'TURN_CLOSED']
HANDOFF_FIELDS = {'turn_id', 'agent_id', 'task_id', 'prompt_hash', 'assigned_scope',
                  'work_performed', 'artifacts', 'tests', 'evidence', 'limitations', 'handoff_status'}


def canonical(obj):
    return json.dumps(obj, sort_keys=True, separators=(',', ':'), ensure_ascii=False).encode()


def digest(obj):
    return hashlib.sha256(canonical(obj)).hexdigest()


def now():
    return datetime.now(timezone.utc).isoformat()


def substantive(value, minimum=12):
    return isinstance(value, str) and len(value.strip()) >= minimum and not re.search(
        r'\b(TODO|TBD|placeholder|fill here|your prompt)\b|^\s*\[.*\]\s*$', value, re.I)


def require(condition, message):
    if not condition:
        raise ValueError(message)


class Workflow(Preflight):
    def __init__(self, root=None):
        self.root = Path(root or Path(__file__).parent).resolve()
        self._agent_validator = None

    def path(self, value):
        p = Path(value)
        if not p.is_absolute():
            p = self.root / p
        require(p.resolve().is_relative_to(self.root), 'Path escapes this copied project root')
        # Managed stores cannot be redirected through symlinks, including internal ones.
        for part in (p, *p.parents):
            if part == self.root:
                break
            require(not part.is_symlink(), f'Symlink in managed path: {part}')
        return p

    def read(self, name):
        return json.loads(self.path(name).read_text())

    def create(self, name, value):
        p = self.path(name)
        p.parent.mkdir(parents=True, exist_ok=True)
        with p.open('x') as handle:
            json.dump(value, handle, indent=2, ensure_ascii=False)
            handle.write('\n')
        return str(p.relative_to(self.root))

    @contextmanager
    def locked(self):
        p = self.path('.harness/workflow.lock')
        p.parent.mkdir(exist_ok=True)
        with p.open('a') as lock:
            fcntl.flock(lock, fcntl.LOCK_EX)
            yield

    def validate_agent(self, agent):
        from jsonschema import Draft202012Validator
        if self._agent_validator is None:
            schema = self.read('schemas/agent.schema.json')
            Draft202012Validator.check_schema(schema)
            self._agent_validator = Draft202012Validator(schema)
        self._agent_validator.validate(agent)
        require(substantive(agent['description'], 50), 'Full role description required')
        require(substantive(agent['jurisdiction']), 'Jurisdiction is incomplete')
        require(bool(agent['use_cases']) and bool(agent['prohibited_actions']), 'Use cases and exclusions required')
        if agent['agent_id'] in CORE:
            require(agent['mandatory'], 'A core crew role cannot be removed')
        if agent['agent_id'] == 'SHO-12':
            require(not agent['mandatory_run'], 'Vince is mandatory crew, not mandatory execution')
        if agent['agent_id'] in MANDATORY_RUN:
            require(agent['mandatory_run'], 'A required execution role cannot become optional')
        return agent

    def registry(self):
        folder = self.path('.harness/registry')
        files = sorted(folder.glob('*.json')) if folder.exists() else []
        if not files:
            return self.read('config/agents.json')
        previous = None
        old_ids = set()
        for index, p in enumerate(files, 1):
            item = self.read(p)
            require(item['revision'] == index and item['previous_hash'] == previous,
                    'Registry history has a missing or changed revision')
            require(item['hash'] == digest({k: v for k, v in item.items() if k != 'hash'}), 'Registry hash mismatch')
            ids = {a['agent_id'] for a in item['agents']}
            require(old_ids <= ids and CORE <= ids, 'Registered agents cannot be removed')
            require(len(ids) == len(item['agents']), 'Duplicate agent identifier')
            for agent in item['agents']:
                self.validate_agent(agent)
            old_ids, previous = ids, item['hash']
        return item

    def initialize(self):
        with self.locked():
            if self.path('.harness/registry').exists():
                return self.registry()
            seed = self.registry()
            require({a['agent_id'] for a in seed['agents']} == CORE, 'Seed registry must contain only the permanent crew')
            for agent in seed['agents']:
                self.validate_agent(agent)
            return self._registry_revision(seed['agents'], 'Initial permanent core', None)

    def _registry_revision(self, agents, reason, previous):
        revision = previous['revision'] + 1 if previous else 1
        record = dict(schema_version=1, revision=revision, previous_hash=previous.get('hash') if previous else None,
                      reason=reason, timestamp=now(), agents=agents)
        record['hash'] = digest(record)
        self.create(f'.harness/registry/{revision:06d}.json', record)
        return record

    def register(self, agent, amendment=False, reason='New role registration'):
        self.initialize()
        self.validate_agent(agent)
        require(substantive(reason), 'A substantive registration/amendment reason is required')
        with self.locked():
            old = self.registry()
            agents = {a['agent_id']: a for a in old['agents']}
            exists = agent['agent_id'] in agents
            require(exists == amendment, 'Duplicate ID or amendment of unknown ID')
            if amendment:
                require(agent['registration_revision'] == agents[agent['agent_id']]['registration_revision'] + 1,
                        'Amendment revision must increment; IDs cannot be changed')
            else:
                require(agent['registration_revision'] == 1, 'New agents start at registration revision 1')
            agents[agent['agent_id']] = agent
            return self._registry_revision(list(agents.values()), reason, old)

    def catalog_register(self, ids):
        catalog = {a['agent_id']: a for a in self.read('config/agent_catalog.json')['agents']}
        for aid in ids:
            require(aid in catalog, f'Unknown catalog role: {aid}')
            if aid not in {a['agent_id'] for a in self.registry()['agents']}:
                self.register(catalog[aid], reason=f'Register catalog role {aid} for this project')
        return self.registry()

    def defaults(self):
        return [{**a, 'selected': True} for a in self.registry()['agents']]

    def closed_turns(self):
        files = sorted(self.path('wiki').glob('turn.*.json')) if self.path('wiki').exists() else []
        previous = None
        for p in files:
            record = self.read(p)
            require(record['hash'] == digest({k:v for k,v in record.items() if k != 'hash'}), 'Canonical wiki hash mismatch')
            require(record['previous_wiki_hash'] == previous, 'Canonical wiki history is missing or reordered')
            previous = digest(record)
        return files

    def setup_complete(self):
        return any(self.read(p).get('setup_ready') is True for p in self.closed_turns())

    def outstanding(self):
        self.closed_turns()  # Verify canonical chain before reading dispositions.
        cases = {}
        for p in sorted(self.path('evidence').glob('turn.*.json')):
            turn = self.turn(p.stem)
            wiki = self.path(f'wiki/{turn["turn_id"]}.json')
            doc = self.read(wiki) if wiki.exists() else None
            if doc is None and turn['state'] != 'INTERRUPTED': continue
            audit = doc['audit'] if doc else turn['audit']
            if not audit: continue
            for finding in audit['findings']:
                if finding['status'] == 'CONFIRMED': cases[finding['case_id']] = finding
            for resolution in (doc or {}).get('resolutions', []): cases.pop(resolution['case_id'], None)
        return list(cases.values())

    def begin(self, assessment):
        self.initialize()
        assessment = copy.deepcopy(assessment)
        # Legacy naming is only an alias for the setup stage, never a shortcut to work.
        if assessment.get('kind') == 'readiness': assessment['kind'] = 'setup'
        require(substantive(assessment.get('task')), 'Task assessment required')
        require(isinstance(assessment.get('requirements'), list) and assessment['requirements'], 'Requirements required')
        require(type(assessment.get('research_required')) is bool and type(assessment.get('architecture_change')) is bool,
                'Research and architecture screening decisions must be explicit booleans')
        require(substantive(assessment.get('research_reason')), 'Research screening reason required')
        conditional, preflight = self._prepare_preflight(assessment)
        self.catalog_register(conditional)
        with self.locked():
            existing = sorted(self.path('evidence').glob('turn.*.json')) if self.path('evidence').exists() else []
            if existing:
                previous = self.turn(existing[-1].stem)
                require(previous['state'] in ('TURN_CLOSED', 'INTERRUPTED'), 'Close or interrupt the active turn first')
            number = max([int(p.stem.split('.')[1]) for p in existing] or [0]) + 1
            turn_id = f'turn.{number:03d}'
            registry = self.registry()
            required = {a['agent_id'] for a in registry['agents'] if a.get('mandatory_run')}
            if assessment['kind'] == 'questions':
                required |= {a['agent_id'] for a in registry['agents'] if a['agent_id'] not in TERMINAL | {'SHO-12'}}
            header = dict(schema_version=2, turn_id=turn_id, manager_id='ENG-00', assessment=assessment,
                          registry=registry, required_agents=sorted(required | set(conditional)),
                          prior_findings=self.outstanding(), preflight=preflight, timestamp=now())
            from jsonschema import validate
            validate(header,self.read('schemas/turn.schema.json'))
            self.create(f'evidence/{turn_id}.json', header)
            self._event(turn_id, 'PHASE', {'state': 'INITIAL_REVIEW'})
            self._event(turn_id, 'PHASE', {'state': 'AGENT_SELECTION'})
        return self.turn(turn_id)

    def _event(self, turn_id, kind, data):
        events = self.path(f'evidence/{turn_id}.events')
        files = sorted(events.glob('*.json')) if events.exists() else []
        prior = self.read(files[-1])['hash'] if files else digest(self.read(f'evidence/{turn_id}.json'))
        event = dict(sequence=len(files)+1, timestamp=now(), kind=kind, data=data, previous_hash=prior)
        event['hash'] = digest(event)
        self.create(f'evidence/{turn_id}.events/{len(files)+1:06d}.json', event)
        return event

    def turn(self, turn_id):
        require(re.fullmatch(r'turn\.\d{3,}', turn_id) is not None, 'Invalid turn identifier')
        header = self.read(f'evidence/{turn_id}.json')
        result = {**header, 'state': 'INITIAL_REVIEW', 'selection': {}, 'invocations': {}, 'handoffs': {},
                  'failures': {}, 'events': [], 'audit': None, 'dispatches': {}}
        previous = digest(header)
        for index, p in enumerate(sorted(self.path(f'evidence/{turn_id}.events').glob('*.json')), 1):
            e = self.read(p)
            require(e['sequence'] == index and e['previous_hash'] == previous, 'Missing or changed turn event')
            require(e['hash'] == digest({k:v for k,v in e.items() if k != 'hash'}), 'Turn event hash mismatch')
            previous = e['hash']
            result['events'].append(e)
            kind, data = e['kind'], e['data']
            if kind == 'PHASE': result['state'] = data['state']
            elif kind == 'SELECTION': result['selection'] = data
            elif kind == 'DISPATCH_AUTHORIZED': result['dispatches'][data['agent_id']] = data
            elif kind == 'INVOKED': result['invocations'][data['agent_id']] = data
            elif kind == 'HANDOFF': result['handoffs'][data['agent_id']] = data
            elif kind == 'FAILED': result['failures'][data['agent_id']] = data
            elif kind == 'AUDIT': result['audit'] = data
        canonical_path = self.path(f'wiki/{turn_id}.json')
        if canonical_path.exists():
            record = self.read(canonical_path)
            require(record['hash'] == digest({k:v for k,v in record.items() if k != 'hash'}), 'Canonical wiki hash mismatch')
            n = len(record['events'])
            require(result['events'][:n] == record['events'], 'Sealed event history differs from canonical record')
            require(all(e['kind'] == 'PHASE' and e['data']['state'] == 'TURN_CLOSED' for e in result['events'][n:]), 'Events appended after canonical publication')
            result['state'] = 'TURN_CLOSED'
        return result

    def select(self, turn_id, selection):
        with self.locked():
            turn = self.turn(turn_id)
            require(turn['state'] == 'AGENT_SELECTION', 'Selection phase is closed')
            agents = {a['agent_id']:a for a in turn['registry']['agents']}
            require(set(selection) == set(agents), 'Account for every registered agent exactly once')
            exclusions = [row.get('reason','').strip() for row in selection.values() if row.get('selected') is False]
            require(len(exclusions) == len(set(exclusions)), 'Each exclusion needs its own task-specific reason')
            for aid, row in selection.items():
                require(type(row.get('selected')) is bool, f'{aid}: selected must be boolean')
                require(substantive(row.get('reason')), f'{aid}: individual substantive reason required')
                if aid in turn['required_agents']:
                    require(row['selected'], f'{aid} is mandatory for this turn')
                if row['selected']:
                    require(substantive(row.get('prompt'), 60), f'{aid}: complete exact spawning prompt required')
                    require(isinstance(row.get('dependencies'), list), f'{aid}: dependencies must be explicit')
                    require(all(d in selection and selection[d].get('selected') for d in row['dependencies']), 'Invalid dependency')
                    require(aid not in row['dependencies'], 'Self dependency')
            def visit(aid, stack):
                require(aid not in stack, 'Cyclic specialist dependencies')
                for dep in selection[aid].get('dependencies', []): visit(dep, stack | {aid})
            for aid, row in selection.items():
                if row['selected']: visit(aid, set())
            for case in turn['prior_findings']:
                row = selection.get(case['responsible_agent'], {})
                require(row.get('selected') and case['case_id'] in row.get('corrective_cases', []),
                        f'Assign outstanding correction {case["case_id"]} to its responsible role')
            if turn['assessment']['kind'] == 'work':
                require(self.readiness([aid for aid,row in selection.items() if row['selected']])['status'] == 'READY', 'Selected roles need additional tools/MCP connections; run a readiness turn')
            self._event(turn_id, 'SELECTION', selection)
            self._event(turn_id, 'PHASE', {'state':'RESEARCH_AND_EXECUTION'})
        return self.turn(turn_id)

    def _check_dispatch(self, turn, agent_id, prompt):
        roles = {a['agent_id']:a for a in turn['registry']['agents']}
        require(agent_id in roles, 'Unregistered agent')
        selected = turn['selection'].get(agent_id, {})
        require(selected.get('selected'), 'Agent is not selected for execution; crew membership does not force a run')
        require(selected['prompt'] == prompt, 'Prompt differs from the approved exact prompt')
        require(agent_id not in turn['invocations'], 'Invocation already recorded; corrections belong in the next turn')
        phase = roles[agent_id]['phase']
        expected = {'collection':'HANDOFF_COLLECTION', 'audit':'INTERNAL_AFFAIRS_REVIEW',
                    'documentation':'DOCUMENT_CHIEF'}.get(phase, 'RESEARCH_AND_EXECUTION')
        require(turn['state'] == expected, f'{agent_id} cannot execute in {turn["state"]}')
        for dep in selected['dependencies']:
            if phase != 'collection':
                require(turn['handoffs'].get(dep,{}).get('handoff_status') == 'COMPLETE', f'Missing complete dependency handoff {dep}')
        if turn['assessment']['research_required'] and phase == 'specialist':
            require(turn['handoffs'].get('RES-09', {}).get('handoff_status') == 'COMPLETE', 'Researcher must complete first')
        if agent_id == 'BLD-27' and turn['assessment'].get('milestone'):
            require(turn['handoffs'].get('PRT-05',{}).get('handoff_status') == 'COMPLETE', 'Prototype Designer must deliver before Builder')
        return selected

    def authorize_dispatch(self, turn_id, agent_id, prompt):
        """Obtain this BEFORE asking the host to spawn an agent. It is not host authentication."""
        with self.locked():
            turn = self.turn(turn_id)
            self._check_dispatch(turn,agent_id,prompt)
            require(agent_id not in turn['dispatches'], 'Dispatch is already authorized')
            data = {'agent_id':agent_id,'authorization_id':uuid.uuid4().hex,
                    'prompt_hash':hashlib.sha256(prompt.encode()).hexdigest()}
            self._event(turn_id,'DISPATCH_AUTHORIZED',data)
        return data

    def invoke(self, turn_id, agent_id, prompt, receipt, backend, authorization=None):
        with self.locked():
            turn = self.turn(turn_id)
            self._check_dispatch(turn,agent_id,prompt)
            ticket = turn['dispatches'].get(agent_id,{})
            require(authorization and authorization == ticket.get('authorization_id'), 'Authorize dispatch before host spawning, then supply its ticket with the real receipt')
            require(substantive(receipt, 3) and substantive(backend, 3), 'Actual host invocation receipt and backend required')
            used = {i['receipt'] for i in turn['invocations'].values()}
            require(receipt not in used, 'Each role requires an independent distinct host worker/session receipt')
            data = dict(agent_id=agent_id, prompt_hash=hashlib.sha256(prompt.encode()).hexdigest(),
                        receipt=receipt, backend=backend, authorization_id=authorization,
                        owned_directory=f'work/{turn_id}/{agent_id}')
            self.path(data['owned_directory']).mkdir(parents=True, exist_ok=True)
            self._event(turn_id, 'INVOKED', data)
        return data

    def references(self, artifacts):
        result = []
        for value in artifacts:
            name = value['path'] if isinstance(value, dict) else value
            p = self.path(name)
            require(p.is_file(), f'Missing evidence artifact: {name}')
            actual = hashlib.sha256(p.read_bytes()).hexdigest()
            if isinstance(value,dict) and 'sha256' in value:
                require(value['sha256'] == actual, f'Artifact changed since it was recorded: {name}')
            result.append({'path':str(p.relative_to(self.root)), 'sha256':actual})
        return result

    def handoff(self, turn_id, value):
        with self.locked():
            turn = self.turn(turn_id)
            aid = value.get('agent_id')
            require(HANDOFF_FIELDS <= value.keys(), 'Incomplete structured handoff')
            require(value['turn_id'] == turn_id and aid in turn['invocations'], 'Unknown turn or invocation')
            role = next(a for a in turn['registry']['agents'] if a['agent_id'] == aid)
            expected = {'collection':'HANDOFF_COLLECTION','audit':'INTERNAL_AFFAIRS_REVIEW', 'documentation':'DOCUMENT_CHIEF'}.get(role['phase'], 'RESEARCH_AND_EXECUTION')
            allowed = {expected}
            if expected == 'RESEARCH_AND_EXECUTION' and 'EVC-28' not in turn['handoffs']: allowed.add('HANDOFF_COLLECTION')
            require(turn['state'] in allowed, 'Handoff outside assigned phase')
            require(aid not in turn['handoffs'] and aid not in turn['failures'], 'Handoff/failure already recorded')
            require(value['prompt_hash'] == turn['invocations'][aid]['prompt_hash'], 'Handoff prompt hash mismatch')
            require(value['handoff_status'] in ('COMPLETE','PARTIAL','FAILED','BLOCKED'), 'Invalid handoff status')
            for field in ('artifacts','tests','evidence','limitations'):
                require(isinstance(value[field], list), f'{field} must be an explicit list')
            for field in ('task_id','assigned_scope','work_performed'):
                require(substantive(value[field], 3), f'{field} required')
            owned = self.path(turn['invocations'][aid]['owned_directory'])
            require(all(self.path(v['path'] if isinstance(v,dict) else v).is_relative_to(owned) for v in value['artifacts']), 'Artifact is outside the role-owned directory')
            value = {**value, 'artifacts':self.references(value['artifacts']), 'evidence':self.references(value['evidence'])}
            for test in value['tests']:
                require(test.get('status') in ('PASS','FAIL','BLOCKED','NOT_RUN'), 'Invalid test status')
                require(substantive(test.get('command'), 3), 'Test command or NOT_RUN explanation required')
                test['evidence'] = self.references(test.get('evidence', []))
                require(test['status'] != 'PASS' or test['evidence'], 'Passing tests need actual log evidence')
            if aid in self.waiting_agents(turn):
                require(value['handoff_status'] in ('BLOCKED','PARTIAL'), 'Requesting research pauses this agent production for the remainder of the turn')
                require(value['limitations'], 'Waiting-for-research limitation must be disclosed')
            self._validate_question_review(turn,value)
            self._validate_research(turn,value)
            self._validate_architecture(turn,value)
            self._validate_prototype(turn,value)
            self._validate_collection(turn,value)
            self._event(turn_id, 'HANDOFF', value)
        return value

    def failed(self, turn_id, agent_id, reason):
        with self.locked():
            turn = self.turn(turn_id)
            require(turn['state'] not in ('TURN_CLOSED','INTERRUPTED'), 'Turn is sealed')
            require(agent_id in turn['invocations'] and agent_id not in turn['handoffs'] and agent_id not in turn['failures'], 'No pending invocation')
            require(substantive(reason), 'Failure reason required')
            self._event(turn_id, 'FAILED', {'agent_id':agent_id,'reason':reason})

    def advance(self, turn_id, next_state):
        with self.locked():
            turn = self.turn(turn_id)
            allowed = {'RESEARCH_AND_EXECUTION':'HANDOFF_COLLECTION','HANDOFF_COLLECTION':'INTERNAL_AFFAIRS_REVIEW',
                       'INTERNAL_AFFAIRS_REVIEW':'DOCUMENT_CHIEF'}
            require(allowed.get(turn['state']) == next_state, 'Cannot skip lifecycle phases')
            if next_state == 'INTERNAL_AFFAIRS_REVIEW':
                require(turn['handoffs'].get('EVC-28',{}).get('handoff_status') == 'COMPLETE', 'Evidence Collector must deliver its completeness package before IA')
            if next_state == 'DOCUMENT_CHIEF': require(turn['audit'] is not None, 'IA report must precede documentation')
            self._event(turn_id, 'PHASE', {'state':next_state})
        return self.turn(turn_id)

    def audit(self, turn_id, report):
        with self.locked():
            turn = self.turn(turn_id)
            require(turn['state'] == 'INTERNAL_AFFAIRS_REVIEW' and 'IA-11' in turn['handoffs'], 'IA handoff required')
            require(turn['audit'] is None, 'Audit already recorded')
            require(report.get('disposition') in ('ACCEPTED','REJECTED','BLOCKED'), 'IA disposition required')
            require(substantive(report.get('summary')), 'IA summary required')
            require(isinstance(report.get('findings'), list), 'Findings list required')
            ids = set()
            for finding in report['findings']:
                require(all(k in finding for k in ('case_id','responsible_agent','severity','status','evidence','independently_verified','required_action')), 'Incomplete finding')
                require(finding['case_id'] not in ids, 'Duplicate case ID')
                ids.add(finding['case_id'])
                require(finding['responsible_agent'] in turn['selection'], 'Finding owner must be registered')
                require(finding['status'] in ('CONFIRMED','UNCONFIRMED','INCONCLUSIVE','RESOLVED'), 'Invalid finding status')
                require(finding['severity'] in ('CRITICAL','MAJOR','MINOR'), 'Invalid severity')
                require(type(finding['independently_verified']) is bool and bool(finding['evidence']), 'Finding provenance required')
                finding['evidence'] = self.references(finding['evidence'])
                require(substantive(finding['required_action']), 'Corrective action required')
                if finding['status'] in ('CONFIRMED','RESOLVED'): require(finding['independently_verified'], 'Confirmed findings and resolutions must be independently verified')
            incomplete = [aid for aid,row in turn['selection'].items() if row['selected'] and aid != 'DOC-10'
                          and turn['handoffs'].get(aid,{}).get('handoff_status') != 'COMPLETE']
            critical = any(f['status'] == 'CONFIRMED' and f['severity'] == 'CRITICAL' for f in report['findings'])
            if report['disposition'] == 'ACCEPTED':
                require(not incomplete and not critical, 'Missing/failed work or critical findings block acceptance')
                self.references(self.evidence_inventory(turn))
                package = self.read(turn['handoffs']['EVC-28']['collection']['package_path'])
                require(not package['integrity_issues'], 'Collector identified changed evidence; IA cannot accept')
            require(all(any(f['responsible_agent'] == aid for f in report['findings']) for aid in incomplete), 'IA must identify every incomplete selected role')
            self._event(turn_id, 'AUDIT', report)
        return report

    def publish(self, turn_id, entities, resolutions=None):
        with self.locked():
            turn = self.turn(turn_id)
            require(turn['state'] == 'DOCUMENT_CHIEF' and 'DOC-10' in turn['handoffs'], 'Document Chief must execute last')
            require(turn['handoffs']['DOC-10']['handoff_status'] == 'COMPLETE', 'Documentation incomplete')
            require(turn['audit'] is not None and isinstance(entities, list), 'IA and entity revisions required')
            if turn['audit']['disposition'] == 'ACCEPTED':
                self.references(self.evidence_inventory(turn))
            for entity in entities:
                require(re.fullmatch(r'[A-Za-z0-9_.-]+', entity.get('entity_id','')) is not None, 'Entity identifier required')
                require('value' in entity and bool(entity.get('evidence')), 'Entity value and provenance required')
                entity['evidence'] = self.references(entity['evidence'])
            resolutions = resolutions or []
            outstanding = {c['case_id']:c for c in turn['prior_findings']}
            for resolution in resolutions:
                case = outstanding.get(resolution.get('case_id'))
                require(case is not None and bool(resolution.get('evidence')), 'Resolution needs original case and retest evidence')
                require(turn['handoffs'].get(case['responsible_agent'],{}).get('handoff_status') == 'COMPLETE', 'Responsible role must deliver correction')
                require(resolution.get('ia_confirmed') is True and any(f['case_id'] == case['case_id'] and f['status'] == 'RESOLVED' for f in turn['audit']['findings']), 'IA must verify a resolution')
                resolution['evidence'] = self.references(resolution['evidence'])
            candidates = turn['handoffs'].get('KRA-14',{}).get('architecture',{}).get('baseline_artifacts',[])
            if turn['audit']['disposition'] == 'ACCEPTED' and candidates:
                require(any(e['entity_id'] == 'architecture.baseline' for e in entities), 'Document Chief must publish the reviewed architecture baseline revision')
            ready = (turn['assessment']['kind'] == 'setup' and turn['audit']['disposition'] == 'ACCEPTED'
                     and self.readiness([aid for aid,row in turn['selection'].items() if row['selected']])['status'] == 'READY')
            # Canonical publication is one write-once commit. No specialist can run after it.
            record = dict(schema_version=1, turn_id=turn_id, timestamp=now(), setup_ready=ready,
                          assessment=turn['assessment'], registry=turn['registry'], selection=turn['selection'],
                          events=turn['events'], audit=turn['audit'], entities=entities, resolutions=resolutions,
                          engineering_complete=turn['audit']['disposition']=='ACCEPTED', audit_complete=True,
                          documentation_complete=True, previous_wiki_hash=None)
            previous = self.closed_turns()
            if previous: record['previous_wiki_hash'] = digest(self.read(previous[-1]))
            record['hash'] = digest(record)
            self.create(f'wiki/{turn_id}.json', record)
            self._event(turn_id, 'PHASE', {'state':'TURN_CLOSED'})
        return record

    def interrupt(self, turn_id, reason):
        with self.locked():
            require(self.turn(turn_id)['state'] not in ('TURN_CLOSED','INTERRUPTED'), 'Already finalized')
            require(substantive(reason), 'Interruption reason required')
            self._event(turn_id, 'INTERRUPTION', {'reason':reason, 'completion':'INCOMPLETE'})
            self._event(turn_id, 'PHASE', {'state':'INTERRUPTED'})
        return self.turn(turn_id)

    def readiness(self, agent_ids=None):
        from readiness import inspect
        return inspect(self.root, agent_ids)

    def run_tool(self, turn_id, agent_id, tool, args, cwd, timeout=120, purpose='production'):
        import harness
        turn = self.turn(turn_id)
        require(turn['state'] == 'RESEARCH_AND_EXECUTION', 'Tool execution outside specialist phase')
        require(agent_id not in self.waiting_agents(turn), 'WAITING_FOR_RESEARCH: this agent production is paused until a later researched turn')
        require(purpose in ('production','verification','setup','research','correction'), 'Explicit tool-use purpose required')
        if turn['assessment']['kind'] != 'work':
            require(purpose != 'production', 'Production cannot run in preflight or reconciliation turns')
        require(agent_id in turn['invocations'] and agent_id not in turn['handoffs'] and agent_id not in turn['failures'], 'No active invocation')
        role = next(a for a in turn['registry']['agents'] if a['agent_id'] == agent_id)
        require(tool in role['tools'], 'Tool is outside this role jurisdiction')
        folder = self.path(cwd)
        require(folder.resolve().is_relative_to(self.path(turn['invocations'][agent_id]['owned_directory'])), 'Working directory belongs to another role')
        # Engineering adapters resolve their root from their copied source file.
        result = harness.execute(tool,args,folder,timeout)
        result['log_evidence'] = self.references([result['log']])
        with self.locked():
            require(self.turn(turn_id)['state'] == 'RESEARCH_AND_EXECUTION', 'Phase changed during tool execution')
            self._event(turn_id,'TOOL_RESULT',{'agent_id':agent_id,**result})
        return result
