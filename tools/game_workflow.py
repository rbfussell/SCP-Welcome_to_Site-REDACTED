"""Game policy over the genuine copied harness. Process publications are not the game wiki.

This layer cannot start host agents or authenticate receipts. Authorize before asking
an independent host worker to run, then record the actual external receipt.
"""
import copy
import hashlib
import importlib.util
from pathlib import Path
import re
import sys

RUNTIME = Path(__file__).resolve().parents[1] / 'management/harness/runtime'
sys.path.insert(0, str(RUNTIME))
from workflow import Workflow, require, substantive, digest, now
from preflight import TERMINAL

MANDATORY = {'ENG-00', 'EVC-28', 'IA-11', 'DOC-10', 'WIK-30'}
IMPORT_PATH = 'management/agentic/imported-project.json'


class GameWorkflow(Workflow):
    def __init__(self, root=None):
        super().__init__(root or Path(__file__).resolve().parents[1])

    def path(self, value):
        value = str(value)
        for prefix, target in {
            '.harness': 'management/agentic/state',
            'evidence': 'management/agentic/evidence',
            'wiki': 'management/agentic/publications',
            'work': 'management/agentic/work',
            'config': 'management/harness/runtime/config',
            'schemas': 'management/harness/runtime/schemas',
        }.items():
            if value == prefix or value.startswith(prefix + '/'):
                value = target + value[len(prefix):]
                break
        return super().path(value)

    def validate_agent(self, agent):
        # Registry revisions repeat unchanged definitions. Validate each exact value once
        # per process; changed content always has a different digest and is revalidated.
        key = digest(agent)
        cache = getattr(self, '_validated_game_agents', set())
        if key not in cache:
            super().validate_agent(agent)
            cache.add(key)
            self._validated_game_agents = cache
        return agent

    def initialize(self):
        self.path('.harness').mkdir(parents=True, exist_ok=True)
        initial = super().initialize()
        if getattr(self, '_registering_game_roles', False):
            return initial
        self._registering_game_roles = True
        try:
            roles_path = self.path('config/game_roles.json')
            require(roles_path.is_file(), 'Missing versioned software/Wiki role definitions')
            for role in self.read(roles_path)['agents']:
                if role['agent_id'] not in {a['agent_id'] for a in self.registry()['agents']}:
                    self.register(role, reason=f"Register game software profile role {role['agent_id']}")
            return self.registry()
        finally:
            self._registering_game_roles = False

    def inventory(self):
        return {'registry': self.registry(), 'defaults': self.defaults(),
                'readiness': self.readiness(), 'preflight': self.preflight_status(),
                'host_connection': 'NOT_OBSERVED_BY_THIS_LOCAL_RUNTIME'}

    def status(self, turn_id=None):
        if turn_id:
            return self.turn(turn_id)
        turns = sorted(self.path('evidence').glob('turn.*.json'))
        return {'current_turn': self.turn(turns[-1].stem) if turns else None,
                'preflight': self.preflight_status(), 'readiness': self.readiness()}

    def plan(self, turn_id):
        """All YES initially. A draft cannot select until individual reasons/prompts are filled."""
        turn = self.turn(turn_id)
        require(turn['state'] == 'AGENT_SELECTION', 'Planning requires the selection phase')
        return {a['agent_id']: {'selected': True, 'reason': '', 'prompt': '', 'dependencies': []}
                for a in turn['registry']['agents']}

    def create_import(self, value):
        """Record genuine existing decisions; review/publication must still accept this import."""
        require(not self.path(IMPORT_PATH).exists(), 'Existing-project import is immutable')
        require(re.fullmatch(r'[a-f0-9]{7,40}', value.get('baseline_commit', '')) is not None,
                'Import needs a real baseline commit identifier')
        require(substantive(value.get('scope'), 30), 'Define the imported scope and its limits')
        require(value.get('historical_sources') and value.get('authorization_evidence'),
                'Import requires historical sources and actual user authorization evidence')
        record = {'schema_version': 1, 'baseline_commit': value['baseline_commit'],
                  'scope': value['scope'], 'timestamp': now(),
                  'historical_sources': self.references(value['historical_sources']),
                  'authorization_evidence': self.references(value['authorization_evidence']),
                  'status': 'IMPORTED_HISTORY_NOT_NEW_QUALIFICATION'}
        record['hash'] = digest(record)
        with self.locked():
            self.create(IMPORT_PATH, record)
        return record

    def import_record(self):
        if not self.path(IMPORT_PATH).exists():
            return None
        record = self.read(IMPORT_PATH)
        require(record.get('hash') == digest({k: v for k, v in record.items() if k != 'hash'}),
                'Existing-project import hash mismatch')
        self.references(record['historical_sources'])
        self.references(record['authorization_evidence'])
        return record

    def preflight_status(self):
        status = super().preflight_status()
        record = self.import_record()
        accepted = bool(record and any(
            row['accepted'] and row['doc'].get('setup_ready') and any(
                entity['entity_id'] == 'project.import' and isinstance(entity.get('value'), dict)
                and entity['value'].get('import_hash') == record['hash']
                for entity in row['doc']['entities'])
            for row in self.history()))
        status['imported_history_accepted'] = accepted
        status['import_basis'] = {'path': IMPORT_PATH, 'hash': record['hash'],
                                  'status': record['status']} if record else None
        if accepted:
            if status['open_ia_cases']:
                status['next_kind'] = 'reconcile'
            elif status['pending_research']:
                status['next_kind'] = 'research'
            elif status['unanswered_questions']:
                status['next_kind'] = 'reconcile'
            else:
                status['next_kind'] = 'work'
            status['production_allowed'] = status['next_kind'] == 'work'
        return status

    def initial_prerequisites_ready(self):
        status = self.preflight_status()
        return status['initial_research_complete'] or status['imported_history_accepted']

    def readiness(self, agent_ids=None):
        # Actual CLI execution and host workers are independent of MCP transport loading.
        ids = set(agent_ids or [a['agent_id'] for a in self.registry()['agents']])
        known = {a['agent_id'] for a in self.registry()['agents']}
        checks = {'jsonschema': importlib.util.find_spec('jsonschema') is not None,
                  'runtime_config': self.path('config/agents.json').is_file(),
                  'requested_roles_registered': ids <= known}
        return {'status': 'READY' if all(checks.values()) else 'BLOCKED',
                'execution': 'CLI_WITH_EXTERNAL_HOST_WORKERS', 'checks': checks,
                'stdio_mcp_dependency': importlib.util.find_spec('mcp') is not None,
                'host_mcp_connection': 'NOT_OBSERVED',
                'limitations': ['Host worker availability and receipt authenticity require external review.',
                                'A local protocol probe does not establish Codex host registration.']}

    def select(self, turn_id, selection):
        # The parent enforces exact key coverage, unique substantive exclusions and DAGs.
        turn = self.turn(turn_id)
        require(MANDATORY <= set(selection), 'Coordinator, Collector, independent IA, Document Chief and WIK-30 required')
        for aid in MANDATORY:
            require(selection[aid].get('selected') is True, f'{aid} is mandatory for the game agentic loop')
        require('WIK-30' in {a['agent_id'] for a in turn['registry']['agents']}, 'Register actual game wiki maintainer WIK-30 first')
        # Wiki must see every selected execution specialist before finalizing the knowledge base.
        specialists = {a['agent_id'] for a in turn['registry']['agents']
                       if a['phase'] not in ('collection', 'audit', 'documentation')
                       and a['agent_id'] != 'WIK-30' and selection.get(a['agent_id'], {}).get('selected')}
        require(specialists <= set(selection['WIK-30'].get('dependencies', [])),
                'WIK-30 must depend on every selected execution role')
        return super().select(turn_id, selection)

    def _check_dispatch(self, turn, agent_id, prompt):
        selected = super()._check_dispatch(turn, agent_id, prompt)
        role = next(a for a in turn['registry']['agents'] if a['agent_id'] == agent_id)
        if role['phase'] not in ('collection', 'audit', 'documentation') and agent_id != 'WIK-30':
            require('WIK-30' not in turn['invocations'], 'Game source work is frozen once Wiki starts')
        return selected

    def invoke(self, turn_id, agent_id, prompt, receipt, backend, authorization=None):
        # Receipts are externally supplied evidence, not proof generated by this process.
        # Never accept the same host worker as different registered roles in one turn.
        turn = self.turn(turn_id)
        require(receipt not in {v['receipt'] for v in turn['invocations'].values()},
                'Each role requires a distinct actual host worker/session receipt')
        return super().invoke(turn_id, agent_id, prompt, receipt, backend, authorization)

    def build_evidence_package(self, turn_id):
        result = super().build_evidence_package(turn_id)
        result['path'] = str(self.path(result['path']).relative_to(self.root))
        return result

    def handoff(self, turn_id, value):
        if value.get('agent_id') == 'EVC-28' and value.get('collection', {}).get('package_path'):
            value = {**value, 'collection': {**value['collection'], 'package_path':
                str(self.path(value['collection']['package_path']).relative_to(self.root))}}
        if value.get('agent_id') == 'WIK-30' and value.get('handoff_status') == 'COMPLETE':
            wiki = value.get('game_wiki', {})
            require(wiki.get('semantic_review') is True, 'Wiki handoff needs semantic review')
            require(isinstance(wiki.get('pages'), list) and wiki['pages'], 'Wiki handoff lists affected authored pages')
            for path in wiki['pages']:
                require(self.path(path).is_relative_to(self.path('source_material/game-wiki/content'))
                        and self.path(path).is_file() and self.path(path).suffix == '.md',
                        'WIK-30 must identify existing authored game-wiki Markdown pages')
            refs = self.references(wiki.get('evidence', []))
            require(refs, 'Wiki handoff needs real build/check evidence')
            value = {**value, 'game_wiki': {**wiki, 'evidence': refs}}
            tests = value.get('tests', [])
            require(any(t.get('status') == 'PASS' and 'build_game_wiki.py' in t.get('command', '')
                        and '--check' not in t.get('command', '') for t in tests), 'Wiki rebuild PASS evidence required')
            require(any(t.get('status') == 'PASS' and 'build_game_wiki.py' in t.get('command', '')
                        and '--check' in t.get('command', '') for t in tests), 'Wiki freshness PASS evidence required')
        return super().handoff(turn_id, value)

    def advance(self, turn_id, next_state):
        if next_state == 'HANDOFF_COLLECTION':
            turn = self.turn(turn_id)
            require('WIK-30' in turn['handoffs'] or 'WIK-30' in turn['failures'],
                    'Actual Wiki invocation must report before evidence collection (failures remain visible)')
        return super().advance(turn_id, next_state)

    def publish(self, turn_id, entities, resolutions=None):
        turn = self.turn(turn_id)
        imports = [e for e in entities if e.get('entity_id') == 'project.import']
        if imports:
            record = self.import_record()
            require(record and turn['assessment']['kind'] == 'setup', 'Imported history is reviewed in setup')
            require(all(e.get('value', {}).get('import_hash') == record['hash'] for e in imports),
                    'Publication must identify the exact reviewed existing-project import')
            expected = self.path(IMPORT_PATH)
            require(all(any(self.path(r['path'] if isinstance(r, dict) else r) == expected
                            for r in e.get('evidence', [])) for e in imports), 'Import publication needs record evidence')
        return super().publish(turn_id, entities, resolutions)
