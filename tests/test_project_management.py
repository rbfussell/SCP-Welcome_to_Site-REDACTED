"""Software workflow guards, portability and non-destructive harness integration."""
import copy
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
from manage_project import SoftwareProject, REQUIRED_CHECKS
from configure_project_harness import configure, BEGIN, END


class ProjectManagementTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / 'Project'
        self.root.mkdir()
        for directory in ('management/tasks', 'source_material/game-wiki/content', 'docs'):
            (self.root / directory).mkdir(parents=True)
        (self.root / 'management/profile.json').write_text(json.dumps({'profile': 'software-game', 'execution': 'single-developer'}))
        self.wiki = ['source_material/game-wiki/content/status.md', 'source_material/game-wiki/content/development.md']
        for name in self.wiki + ['docs/design.md', 'input.txt']:
            (self.root / name).write_text('Known bounded behavior, not a complete game certification.\n')
        self.project = SoftwareProject(self.root)
        self.seed('slice')
        self.context = {'wiki_read': [{'path': p, 'notes': 'Read current mechanics and linked documentation boundaries.'} for p in self.wiki],
                        'docs_read': [{'path': 'docs/design.md', 'notes': 'Read accepted scope and current verification limits.'}]}
        self.review = {'mode': 'self-review', 'summary': 'Compared the declared implementation with documentation and real fixture check results.',
                       'implementation_docs_agree': True, 'limitations': ['Fixture commands only; not game qualification.'],
                       'acceptance': [{'criterion': 'observable criterion', 'result': 'PASS', 'evidence': 'Actual fixture command logs.'}]}

    def seed(self, name):
        value = {'id': name, 'title': 'Concrete bounded software slice', 'status': 'ready', 'scope': 'One fixture behavior only.',
                 'acceptance': ['observable criterion'], 'wiki_pages': self.wiki, 'inputs': ['input.txt'], 'depends_on': [], 'blocking_questions': []}
        self.project.task_path(name).write_text(json.dumps(value))

    def started(self):
        return self.project.start('slice', self.context)

    def passing_checks(self):
        for name in REQUIRED_CHECKS:
            self.project.record('slice', name, [sys.executable, '-c', 'print("Fixture check executed")'])

    def test_context_and_canon_are_not_interchangeable(self):
        with self.assertRaisesRegex(ValueError, 'wiki AND'):
            self.project.start('slice', {'docs_read': self.context['docs_read']})
        bad = copy.deepcopy(self.context)
        bad['wiki_read'][0]['path'] = 'docs/design.md'
        with self.assertRaisesRegex(ValueError, 'ledger'):
            self.project.start('slice', bad)
        bad = copy.deepcopy(self.context)
        bad['wiki_read'].append({'path': 'docs/design.md', 'notes': 'This is not a game-wiki page despite being a useful document.'})
        with self.assertRaisesRegex(ValueError, 'Canon dump'):
            self.project.start('slice', bad)
        self.assertEqual(self.started()['status'], 'active')

    def test_path_escape_rejected(self):
        with self.assertRaisesRegex(ValueError, 'escapes'):
            self.project.path('../outside')
        with self.assertRaisesRegex(ValueError, 'Invalid task'):
            self.project.task_path('../../unsafe')

    def test_one_active_slice_and_dependencies(self):
        self.started(); self.seed('other')
        with self.assertRaisesRegex(ValueError, 'active slice'):
            self.project.start('other', self.context)

    def test_design_blocker_prevents_start(self):
        task = self.project.task('slice'); task['blocking_questions'] = ['Material design question']
        self.project.save(task, 'fixture blocker')
        with self.assertRaisesRegex(ValueError, 'questions'):
            self.started()

    def test_missing_checks_prevent_completion(self):
        self.started()
        with self.assertRaisesRegex(ValueError, 'missing passing tests'):
            self.project.complete('slice', self.review)

    def test_actual_failed_command_cannot_complete(self):
        self.started(); self.passing_checks()
        record = self.project.record('slice', 'tests', [sys.executable, '-c', 'raise SystemExit(3)'])
        self.assertEqual(record['exit_code'], 3)
        with self.assertRaisesRegex(ValueError, 'missing passing tests'):
            self.project.complete('slice', self.review)

    def test_changed_inputs_invalidate_checks(self):
        self.started(); self.passing_checks()
        (self.root / 'input.txt').write_text('Changed after tests')
        with self.assertRaisesRegex(ValueError, 'Stale tests'):
            self.project.complete('slice', self.review)

    def test_modified_log_rejected(self):
        self.started(); self.passing_checks()
        entry = self.project.task('slice')['checks']['tests']
        self.project.path(entry['log']).write_text('Rewritten evidence')
        with self.assertRaisesRegex(ValueError, 'Changed check log'):
            self.project.complete('slice', self.review)

    def test_wiki_check_must_follow_build(self):
        self.started(); self.passing_checks()
        self.project.record('slice', 'wiki-build', [sys.executable, '-c', 'print("Later fixture build")'])
        with self.assertRaisesRegex(ValueError, 'follow rebuild'):
            self.project.complete('slice', self.review)

    def test_independent_review_needs_receipt_and_acceptance_coverage(self):
        self.started(); self.passing_checks()
        bad = copy.deepcopy(self.review); bad['mode'] = 'independent'
        with self.assertRaisesRegex(ValueError, 'actual worker'):
            self.project.complete('slice', bad)
        bad = copy.deepcopy(self.review); bad['acceptance'][0]['criterion'] = 'Different criterion'
        with self.assertRaisesRegex(ValueError, 'every acceptance'):
            self.project.complete('slice', bad)
        bad = copy.deepcopy(self.review); bad['reviewer_receipt'] = 'fake-independent-worker'
        with self.assertRaisesRegex(ValueError, 'cannot claim'):
            self.project.complete('slice', bad)

    def test_complete_preserves_historical_snapshot(self):
        self.started(); self.passing_checks()
        self.assertEqual(self.project.complete('slice', self.review)['status'], 'done')
        (self.root / 'input.txt').write_text('Changed in a later slice')
        self.assertEqual(self.project.check()['result'], 'PASS')
        self.assertEqual(len(list((self.root / 'management/evidence/slice').glob('*.log'))), 4)

    def test_overlay_is_repeatable_and_wrapper_uses_nested_game(self):
        outer = self.root.parent
        for name in ('AGENTS.md', 'README.md', 'workflow.py'):
            (outer / name).write_text(f'Original strict harness {name}\n')
        shutil.copytree(ROOT / 'management/harness', self.root / 'management/harness')
        (self.root / 'tools').mkdir()
        shutil.copyfile(ROOT / 'tools/manage_project.py', self.root / 'tools/manage_project.py')
        self.assertEqual(configure(outer, self.root)['result'], 'PASS')
        self.assertEqual(configure(outer, self.root, check=True)['changed'], [])
        for name in ('AGENTS.md', 'README.md'):
            text = (outer / name).read_text()
            self.assertEqual(text.count(BEGIN), 1); self.assertEqual(text.count(END), 1)
            self.assertTrue(text.endswith(f'Original strict harness {name}\n'))
        (outer / 'workflow.py').write_text('User changes remain untouched\n')
        configure(outer, self.root)
        self.assertEqual((outer / 'workflow.py').read_text(), 'User changes remain untouched\n')
        result = subprocess.run([sys.executable, str(outer / 'scripts/manage_game.py'), 'status'], cwd=outer, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(json.loads(result.stdout)['game_root'], str(self.root))
        result = subprocess.run([sys.executable, str(self.root / 'tools/manage_project.py'), 'status'], cwd=outer, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        (outer / 'scripts/manage_game.py').write_text('Unexpected stale wrapper')
        self.assertEqual(configure(outer, self.root, check=True)['result'], 'FAIL')

    def test_malformed_overlay_does_not_write_partial_files(self):
        outer = self.root.parent
        for name in ('AGENTS.md', 'README.md', 'workflow.py'):
            (outer / name).write_text('Original strict content\n')
        shutil.copytree(ROOT / 'management/harness', self.root / 'management/harness')
        (outer / 'README.md').write_text(BEGIN + '\nMissing ending marker')
        with self.assertRaisesRegex(ValueError, 'Malformed'):
            configure(outer, self.root)
        self.assertEqual((outer / 'AGENTS.md').read_text(), 'Original strict content\n')


if __name__ == '__main__':
    unittest.main()
