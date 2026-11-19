"""Game agent-loop policy regression tests with isolated, explicitly synthetic fixtures.

Fixture receipts below exercise recorded-workflow rules. They are never host dispatch
or production review evidence, and no test writes the real agentic process state.
"""
import copy
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
from game_workflow import GameWorkflow
from manage_project import SoftwareProject, REQUIRED_CHECKS


class AgenticWorkflowTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix='scp-agentic-test-')
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        runtime = self.root / 'management/harness/runtime'
        for name in ('config', 'schemas'):
            shutil.copytree(ROOT / 'management/harness/runtime' / name, runtime / name)
        (self.root / 'management/agentic').mkdir(parents=True)
        self.workflow = GameWorkflow(self.root)
        # Native availability is not under test in policy fixtures. The real stdio
        # probe is executed separately against the installed project environment.
        self.workflow.readiness = lambda agent_ids=None: {
            'status': 'READY', 'fixture': 'Synthetic availability; not a host connection.'}
        self.workflow.initialize()
        if 'WIK-30' not in {a['agent_id'] for a in self.workflow.registry()['agents']}:
            wiki = copy.deepcopy(self.workflow.registry()['agents'][0])
            wiki.update(agent_id='WIK-30', name='Fixture Game Wiki Maintainer',
                        mandatory=False, mandatory_run=True, phase='specialist')
            self.workflow.register(wiki, reason='Register test-only real-wiki role for isolated policy fixture')
        self.workflow.catalog_register(['SWE-13'])
        self.turn_id = None
        self.selection = None

    def begin(self, kind='setup', **extra):
        assessment = dict(kind=kind, task='Exercise bounded agent-loop policy in an isolated fixture.',
                          requirements=['Observe actual recorded lifecycle constraints.'],
                          research_required=False, architecture_change=False,
                          research_reason='This fixture introduces no external product or material decision.')
        assessment.update(extra)
        turn = self.workflow.begin(assessment)
        self.turn_id = turn['turn_id']
        self.selection = {}
        for row in turn['registry']['agents']:
            aid = row['agent_id']
            selected = aid in {'ENG-00', 'KRA-14', 'WIK-30', 'EVC-28', 'IA-11', 'DOC-10'}
            self.selection[aid] = dict(selected=selected,
                reason=f'{aid} handles this distinct fixture responsibility.' if selected else
                       f'{aid} has no assigned deliverable in this bounded policy-only fixture.',
                prompt=f'Act as {aid} in this isolated synthetic test fixture. Produce an owned report describing the assigned lifecycle checks and limitations.',
                dependencies=[])
        self.selection['WIK-30']['dependencies'] = ['ENG-00', 'KRA-14']
        return turn

    def selected(self):
        self.begin()
        return self.workflow.select(self.turn_id, self.selection)

    def invoke(self, aid, receipt=None):
        prompt = self.selection[aid]['prompt']
        ticket = self.workflow.authorize_dispatch(self.turn_id, aid, prompt)
        return self.workflow.invoke(self.turn_id, aid, prompt,
            receipt or f'fixture-only-{self.turn_id}-{aid}', 'fixture-not-a-host',
            ticket['authorization_id'])

    def handoff(self, aid, status='COMPLETE', **extra):
        invocation = self.workflow.turn(self.turn_id)['invocations'][aid]
        report = self.workflow.path(invocation['owned_directory']) / 'report.md'
        report.write_text(f'{aid}: synthetic policy fixture; no real agent invocation.\n')
        value = dict(turn_id=self.turn_id, agent_id=aid, task_id='fixture-policy',
            prompt_hash=invocation['prompt_hash'], assigned_scope='Isolated policy test only.',
            work_performed='Exercised recorded workflow state with synthetic fixture evidence.',
            artifacts=[str(report.relative_to(self.root))], tests=[], evidence=[],
            limitations=['Synthetic fixture receipts are not production evidence.'], handoff_status=status)
        if aid == 'KRA-14':
            value['architecture'] = {'unresolved_questions': [], 'baseline_artifacts': []}
        if aid == 'WIK-30' and status == 'COMPLETE':
            page = self.root / 'source_material/game-wiki/content/development.md'
            page.parent.mkdir(parents=True, exist_ok=True)
            page.write_text('Synthetic authored game-wiki fixture; not production documentation.\n')
            build_log = report.parent / 'wiki-fixture.log'
            build_log.write_text('Synthetic command evidence for policy fixture only.\n')
            log = str(build_log.relative_to(self.root))
            value['game_wiki'] = {'pages': [str(page.relative_to(self.root))],
                                  'semantic_review': True, 'evidence': [log]}
            value['tests'] = [{'command': command, 'status': 'PASS', 'evidence': [log]}
                              for command in ('python3 tools/build_game_wiki.py',
                                              'python3 tools/build_game_wiki.py --check')]
        value.update(extra)
        return self.workflow.handoff(self.turn_id, value)

    def execution_complete(self, blocked=None, missing=None):
        roles = [aid for aid in self.selection if aid != 'WIK-30'] + ['WIK-30']
        for aid in roles:
            row = self.selection[aid]
            if row['selected'] and aid not in {'EVC-28', 'IA-11', 'DOC-10'}:
                self.invoke(aid)
                if aid != missing:
                    self.handoff(aid, 'BLOCKED' if aid == blocked else 'COMPLETE')

    def collect(self):
        self.workflow.advance(self.turn_id, 'HANDOFF_COLLECTION')
        self.invoke('EVC-28')
        package = self.workflow.build_evidence_package(self.turn_id)
        self.handoff('EVC-28', collection={'package_path': package['path']},
                     artifacts=[package['path']])
        return package['package']

    def audit_phase(self):
        self.collect()
        self.workflow.advance(self.turn_id, 'INTERNAL_AFFAIRS_REVIEW')
        self.invoke('IA-11')
        self.handoff('IA-11')

    def close_accepted_setup(self, entities=None):
        self.selected()
        return self.close_selected_setup(entities)

    def close_selected_setup(self, entities=None):
        self.execution_complete(); self.audit_phase()
        self.workflow.audit(self.turn_id, {'disposition': 'ACCEPTED',
            'summary': 'Synthetic fixture lifecycle satisfies the inspected recorded constraints.', 'findings': []})
        self.workflow.advance(self.turn_id, 'DOCUMENT_CHIEF')
        self.invoke('DOC-10'); self.handoff('DOC-10')
        return self.workflow.publish(self.turn_id, entities or [])

    def import_fixture(self):
        for name in ('history.md', 'authorization.txt'):
            (self.root / name).write_text('Synthetic imported history or authorization fixture; not a real user message.\n')
        return self.workflow.create_import({
            'baseline_commit': 'abcdef0',
            'scope': 'Import only prior fixture design as history, without qualifying game behavior.',
            'historical_sources': ['history.md'], 'authorization_evidence': ['authorization.txt']})

    def accept_import_fixture(self):
        record = self.import_fixture()
        self.close_accepted_setup([{'entity_id': 'project.import',
            'value': {'import_hash': record['hash']},
            'evidence': ['management/agentic/imported-project.json']}])
        return record

    def test_import_requires_accepted_setup_and_remains_honest_history(self):
        self.import_fixture()
        status = self.workflow.preflight_status()
        self.assertFalse(status['imported_history_accepted'])
        self.assertFalse(status['production_allowed'])
        self.close_accepted_setup()
        self.assertFalse(self.workflow.preflight_status()['production_allowed'])

    def test_explicit_accepted_import_unlocks_work_without_fabricated_stages(self):
        self.accept_import_fixture()
        status = self.workflow.preflight_status()
        self.assertTrue(status['imported_history_accepted'])
        self.assertTrue(status['production_allowed'])
        self.assertEqual(status['next_kind'], 'work')
        for field in ('question_round_complete', 'initial_research_complete', 'baseline_complete'):
            self.assertFalse(status[field], field)
        self.begin('work')
        self.assertEqual(self.workflow.turn(self.turn_id)['assessment']['kind'], 'work')

    def test_import_is_immutable_and_changed_history_is_detected(self):
        self.import_fixture()
        with self.assertRaisesRegex(ValueError, 'immutable'):
            self.import_fixture()
        (self.root / 'history.md').write_text('Rewritten history after import')
        with self.assertRaisesRegex(ValueError, 'changed'):
            self.workflow.preflight_status()

    def test_imported_project_still_blocks_unanswered_questions(self):
        self.accept_import_fixture(); self.begin('work')
        self.workflow.select(self.turn_id, self.selection); self.invoke('ENG-00')
        self.workflow.ask_question(self.turn_id, 'ENG-00', {'question_id': 'fixture-ambiguity',
            'text': 'Which disputed behavior should the later fixture implement?',
            'scope': 'Synthetic unresolved design choice.', 'blocking': True})
        status = self.workflow.preflight_status()
        self.assertFalse(status['production_allowed'])
        self.assertEqual(status['next_kind'], 'reconcile')
        self.assertEqual(status['blocking_questions'][0]['question_id'], 'fixture-ambiguity')

    def test_imported_project_can_request_later_research_and_must_stop(self):
        self.accept_import_fixture(); self.begin('work')
        self.workflow.select(self.turn_id, self.selection); self.invoke('ENG-00')
        request = self.workflow.request_research(self.turn_id, 'ENG-00', {
            'request_id': 'fixture-options', 'topic': 'Inspect alternative fixture implementations.',
            'reason': 'This later fixture needs source evidence before selecting a new approach.',
            'options_needed': ['Source-supported alternative behavior.'], 'evidence': []})
        self.assertEqual(request['production_status'], 'WAITING_FOR_RESEARCH')
        with self.assertRaisesRegex(ValueError, 'remainder of the turn'):
            self.handoff('ENG-00')
        self.handoff('ENG-00', 'BLOCKED')
        self.workflow.interrupt(self.turn_id, 'Fixture interrupted with later research request still pending.')
        status = self.workflow.preflight_status()
        self.assertEqual(status['pending_research'][0]['request_id'], 'fixture-options')
        self.assertFalse(status['production_allowed'])
        self.assertEqual(status['next_kind'], 'research')

    def software_fixture(self):
        for directory in ('management/tasks', 'source_material/game-wiki/content', 'docs'):
            (self.root / directory).mkdir(parents=True, exist_ok=True)
        (self.root / 'management/profile.json').write_text(json.dumps({'profile': 'software-game', 'execution': 'agentic'}))
        wiki = ['source_material/game-wiki/content/status.md', 'source_material/game-wiki/content/development.md']
        for name in wiki + ['docs/fixture.md', 'input.txt']:
            (self.root / name).write_text('Synthetic bounded software task fixture; not game qualification.\n')
        project = SoftwareProject(self.root)
        project.task_path('fixture-policy').write_text(json.dumps({
            'id': 'fixture-policy', 'title': 'Fixture agentic linkage', 'status': 'ready',
            'scope': 'Exercise completion policy, not game behavior.',
            'acceptance': ['Fixture linkage is enforced.'], 'wiki_pages': wiki,
            'inputs': ['input.txt'], 'depends_on': [], 'blocking_questions': []}))
        context = {'agentic_turn': self.turn_id,
                   'wiki_read': [{'path': p, 'notes': 'Read synthetic fixture ledger and workflow constraints.'} for p in wiki],
                   'docs_read': [{'path': 'docs/fixture.md', 'notes': 'Read synthetic task scope and acceptance limitations.'}]}
        review = {'mode': 'independent', 'reviewer_receipt': f'fixture-only-{self.turn_id}-IA-11',
                  'summary': 'Synthetic policy fixture review; no real independent production assurance.',
                  'implementation_docs_agree': True, 'limitations': ['Fixture only.'],
                  'acceptance': [{'criterion': 'Fixture linkage is enforced.', 'result': 'PASS',
                                  'evidence': 'Actual temporary-root policy checks.'}]}
        project.start('fixture-policy', context)
        return project, review

    def passing_software_checks(self, project):
        for label in REQUIRED_CHECKS:
            project.record('fixture-policy', label, [sys.executable, '-c',
                'print("Synthetic software completion fixture, not a game test")'])

    def test_wiki_handoff_requires_actual_authored_pages_and_semantic_review(self):
        self.selected()
        for aid in ('ENG-00', 'KRA-14'):
            self.invoke(aid); self.handoff(aid)
        self.invoke('WIK-30')
        log = self.workflow.path('work/' + self.turn_id + '/WIK-30/probe.log')
        log.write_text('Synthetic wiki fixture evidence, not a real game-wiki build.\n')
        evidence = [str(log.relative_to(self.root))]
        for pages, semantic, error in [
            (['management/agentic/publications/fake.md'], True, 'authored|actual game wiki'),
            (['source_material/game-wiki/content/nonexistent.md'], True, 'authored|Missing'),
            (['source_material/game-wiki/content/development.md'], False, 'semantic')]:
            with self.assertRaisesRegex(ValueError, error):
                self.handoff('WIK-30', game_wiki={'pages': pages, 'semantic_review': semantic, 'evidence': evidence})

    def test_cli_rejects_duplicate_json_roles_before_selection(self):
        self.begin()
        source = self.root / 'duplicate-selection.json'
        source.write_text('{"turn_id":"turn.001","selection":{"ENG-00":{"selected":true},"ENG-00":{"selected":false}}}')
        result = subprocess.run([sys.executable, str(ROOT / 'tools/agentic.py'),
            '--root', str(self.root), 'select', '--input', str(source)],
            text=True, capture_output=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Duplicate', result.stderr)
        self.assertEqual(self.workflow.turn(self.turn_id)['state'], 'AGENT_SELECTION')

    def test_old_execution_metadata_cannot_bypass_active_agentic_policy(self):
        self.selected(); project, review = self.software_fixture()
        task = project.task('fixture-policy'); task['execution'] = 'single-developer'
        project.save(task, 'Synthetic pre-recovery metadata fixture')
        self.passing_software_checks(project)
        with self.assertRaisesRegex(ValueError, 'Publish the matching'):
            project.complete('fixture-policy', review)
        self.assertEqual(project.task('fixture-policy')['status'], 'active')

    def test_new_agentic_slice_cannot_complete_before_publication(self):
        self.selected(); project, review = self.software_fixture()
        self.passing_software_checks(project)
        with self.assertRaisesRegex(ValueError, 'Publish the matching'):
            project.complete('fixture-policy', review)
        self.assertEqual(project.task('fixture-policy')['status'], 'active')

    def test_unrelated_accepted_publication_cannot_close_slice(self):
        self.selected(); project, review = self.software_fixture()
        self.close_selected_setup()
        with self.assertRaisesRegex(ValueError, 'does not bind'):
            project.agentic_acceptance(project.task('fixture-policy'), review)

    def test_matching_accepted_publication_and_wiki_allows_real_completion(self):
        self.selected(); project, review = self.software_fixture()
        self.passing_software_checks(project)
        self.close_selected_setup([{'entity_id': 'task.fixture-policy',
            'value': {'task_id': 'fixture-policy'}, 'evidence': ['input.txt']}])
        bad = {**review, 'reviewer_receipt': 'fake-unrelated-worker'}
        with self.assertRaisesRegex(ValueError, 'actual independent IA'):
            project.agentic_acceptance(project.task('fixture-policy'), bad)
        bad = {**review, 'mode': 'self-review', 'reviewer_receipt': None}
        with self.assertRaisesRegex(ValueError, 'actual independent IA'):
            project.agentic_acceptance(project.task('fixture-policy'), bad)
        completed = project.complete('fixture-policy', review)
        self.assertEqual(completed['status'], 'done')
        self.assertEqual(completed['agentic_acceptance']['turn_id'], self.turn_id)
        self.assertEqual(project.check()['result'], 'PASS')

    def test_initialize_creates_fresh_managed_store(self):
        fresh = self.root / 'fresh'
        shutil.copytree(self.root / 'management/harness/runtime', fresh / 'management/harness/runtime')
        workflow = GameWorkflow(fresh)
        workflow.initialize()
        self.assertTrue(workflow.path('.harness/registry').is_dir())

    def test_default_plan_requires_accounting_for_all_registered_roles(self):
        turn = self.begin()
        defaults = self.workflow.defaults()
        self.assertEqual({a['agent_id'] for a in defaults}, {a['agent_id'] for a in turn['registry']['agents']})
        self.assertTrue(all(a['selected'] is True for a in defaults))
        plan = self.workflow.plan(self.turn_id)
        self.assertEqual(set(plan), set(self.selection))
        self.assertTrue(all(row['selected'] is True for row in plan.values()))

    def test_omitted_and_unregistered_roles_rejected(self):
        self.begin()
        for missing in ('SHO-12', 'SWE-13', 'WIK-30'):
            bad = copy.deepcopy(self.selection); del bad[missing]
            with self.assertRaisesRegex(ValueError, 'every registered|required'):
                self.workflow.select(self.turn_id, bad)
        bad = copy.deepcopy(self.selection); bad['UNKNOWN-99'] = bad['SWE-13']
        with self.assertRaisesRegex(ValueError, 'every registered|required'):
            self.workflow.select(self.turn_id, bad)

    def test_each_exclusion_needs_unique_substantive_reason(self):
        self.begin()
        bad = copy.deepcopy(self.selection)
        bad['SHO-12']['reason'] = bad['SWE-13']['reason'] = 'Neither is needed for this task.'
        with self.assertRaisesRegex(ValueError, 'own task-specific'):
            self.workflow.select(self.turn_id, bad)
        bad = copy.deepcopy(self.selection); bad['SHO-12']['reason'] = 'No'
        with self.assertRaisesRegex(ValueError, 'substantive'):
            self.workflow.select(self.turn_id, bad)

    def test_wiki_and_core_cannot_be_negated(self):
        self.begin()
        for aid in ('WIK-30', 'ENG-00', 'EVC-28', 'IA-11', 'DOC-10'):
            bad = copy.deepcopy(self.selection); bad[aid]['selected'] = False
            with self.assertRaisesRegex(ValueError, 'mandatory'):
                self.workflow.select(self.turn_id, bad)

    def test_invalid_boolean_empty_prompt_and_cyclic_dependencies_rejected(self):
        self.begin()
        for key, value, message in [('selected', 'yes', 'boolean|mandatory'), ('prompt', 'brief', 'prompt')]:
            bad = copy.deepcopy(self.selection); bad['ENG-00'][key] = value
            with self.assertRaisesRegex(ValueError, message):
                self.workflow.select(self.turn_id, bad)
        bad = copy.deepcopy(self.selection)
        bad['ENG-00']['dependencies'] = ['WIK-30']
        with self.assertRaisesRegex(ValueError, 'Cyclic'):
            self.workflow.select(self.turn_id, bad)

    def test_dispatch_requires_authorization_exact_prompt_and_selection(self):
        self.selected()
        prompt = self.selection['ENG-00']['prompt']
        with self.assertRaisesRegex(ValueError, 'Authorize dispatch'):
            self.workflow.invoke(self.turn_id, 'ENG-00', prompt, 'fixture-unauthorized', 'fixture-host')
        with self.assertRaisesRegex(ValueError, 'Prompt differs'):
            self.workflow.authorize_dispatch(self.turn_id, 'ENG-00', prompt + ' Changed.')
        with self.assertRaisesRegex(ValueError, 'not selected'):
            self.workflow.authorize_dispatch(self.turn_id, 'SHO-12', self.selection['SHO-12']['prompt'])
        self.invoke('ENG-00')
        with self.assertRaisesRegex(ValueError, 'already recorded'):
            self.invoke('ENG-00')

    def test_dependency_handoff_must_be_complete_before_dispatch(self):
        self.begin()
        self.workflow.select(self.turn_id, self.selection)
        with self.assertRaisesRegex(ValueError, 'dependency'):
            self.invoke('WIK-30')
        self.invoke('ENG-00'); self.handoff('ENG-00', 'BLOCKED')
        with self.assertRaisesRegex(ValueError, 'dependency'):
            self.invoke('WIK-30')

    def test_receipt_reuse_is_rejected_for_independent_workers(self):
        self.selected(); self.invoke('ENG-00', receipt='fixture-worker-already-used')
        prompt = self.selection['KRA-14']['prompt']
        ticket = self.workflow.authorize_dispatch(self.turn_id, 'KRA-14', prompt)
        with self.assertRaisesRegex(ValueError, 'independent|reuse|distinct|receipt'):
            self.workflow.invoke(self.turn_id, 'KRA-14', prompt, 'fixture-worker-already-used',
                                 'fixture-not-a-host', ticket['authorization_id'])

    def test_lifecycle_cannot_skip_collector_or_audit(self):
        self.selected()
        with self.assertRaisesRegex(ValueError, 'skip'):
            self.workflow.advance(self.turn_id, 'DOCUMENT_CHIEF')
        with self.assertRaisesRegex(ValueError, 'cannot execute'):
            self.invoke('IA-11')
        with self.assertRaisesRegex(ValueError, 'Collector'):
            self.workflow.build_evidence_package(self.turn_id)
        self.execution_complete()
        self.workflow.advance(self.turn_id, 'HANDOFF_COLLECTION')
        with self.assertRaisesRegex(ValueError, 'Collector'):
            self.workflow.advance(self.turn_id, 'INTERNAL_AFFAIRS_REVIEW')

    def test_missing_work_is_visible_in_collection_and_cannot_be_accepted(self):
        self.selected(); self.execution_complete(missing='WIK-30')
        with self.assertRaisesRegex(ValueError, 'Wiki invocation'):
            self.workflow.advance(self.turn_id, 'HANDOFF_COLLECTION')
        self.workflow.failed(self.turn_id, 'WIK-30', 'Fixture worker failed before delivering its documentation report.')
        package = self.collect()
        self.assertIn('WIK-30', package['missing_handoffs'])
        self.workflow.advance(self.turn_id, 'INTERNAL_AFFAIRS_REVIEW')
        self.invoke('IA-11'); self.handoff('IA-11')
        with self.assertRaisesRegex(ValueError, 'Missing/failed work'):
            self.workflow.audit(self.turn_id, {'disposition': 'ACCEPTED', 'summary': 'Attempted false acceptance of incomplete work.', 'findings': []})

    def test_blocked_handoff_cannot_be_accepted(self):
        self.selected(); self.execution_complete(blocked='WIK-30'); self.audit_phase()
        with self.assertRaisesRegex(ValueError, 'Missing/failed work'):
            self.workflow.audit(self.turn_id, {'disposition': 'ACCEPTED', 'summary': 'Attempted false acceptance of blocked work.', 'findings': []})

    def test_changed_owned_artifact_is_detected(self):
        self.selected(); self.execution_complete()
        invocation = self.workflow.turn(self.turn_id)['invocations']['WIK-30']
        (self.workflow.path(invocation['owned_directory']) / 'report.md').write_text('Changed after handoff')
        package = self.collect()
        self.assertTrue(any('WIK-30' in name for name in package['integrity_issues']))

    def test_handoff_rejects_wrong_prompt_and_unowned_artifact(self):
        self.selected(); self.invoke('ENG-00')
        outside = self.root / 'outside-report.md'; outside.write_text('Not role owned')
        with self.assertRaisesRegex(ValueError, 'prompt hash'):
            self.handoff('ENG-00', prompt_hash='0' * 64)
        with self.assertRaisesRegex(ValueError, 'outside the role-owned'):
            self.handoff('ENG-00', artifacts=['outside-report.md'])

    def test_publication_seals_turn_and_no_worker_runs_after_document_chief(self):
        publication = self.close_accepted_setup()
        self.assertTrue(publication['setup_ready'])
        self.assertEqual(self.workflow.turn(self.turn_id)['state'], 'TURN_CLOSED')
        with self.assertRaises(ValueError):
            self.invoke('ENG-00')
        with self.assertRaises(ValueError):
            self.workflow.publish(self.turn_id, [])

    def test_accepted_setup_alone_does_not_fake_preflight_history(self):
        self.close_accepted_setup()
        status = self.workflow.preflight_status()
        self.assertTrue(status['setup_complete'])
        for field in ('question_round_complete', 'initial_research_complete', 'baseline_complete', 'production_allowed'):
            self.assertFalse(status[field], field)

    def test_interrupted_setup_cannot_unlock_work(self):
        self.selected(); self.workflow.interrupt(self.turn_id, 'Fixture interrupted before any accepted publication.')
        self.assertFalse(self.workflow.preflight_status()['production_allowed'])
        with self.assertRaisesRegex(ValueError, 'setup|blocked'):
            self.begin('work')

    def test_managed_paths_reject_escape_and_symlink(self):
        with self.assertRaisesRegex(ValueError, 'escapes'):
            self.workflow.path('../outside')
        path = self.root / 'management/agentic/state/link'
        path.symlink_to(self.root / 'management/harness/runtime/config')
        with self.assertRaisesRegex(ValueError, 'Symlink'):
            self.workflow.path('management/agentic/state/link/agents.json')


if __name__ == '__main__':
    unittest.main()
