"""Behavioral policy, provenance and filesystem-preservation regressions."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import tomllib
import unittest
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
from agent_model_policy import ASTRA, SOL, load_policy, resolve, provenance, POLICY, REGISTRY
from configure_agent_models import configure


class ModelPolicyTests(unittest.TestCase):
    def test_registry_defaults_and_explicit_spawn(self):
        p = load_policy()
        self.assertEqual(set(p['roles']), {r['agent_id'] for r in json.loads(REGISTRY.read_text())['agents']})
        for role in p['roles']:
            r = resolve(role, 'setup')
            default = p['work_classes'][p['roles'][role]['default_work_class']]
            self.assertEqual(r['spawn_parameters'], {**default, 'fork_turns': 'none'})
            self.assertEqual(r['allocation'], {
                'default': default,
                'floor': p['roles'][role]['floor'],
                'ceilings': p['roles'][role]['ceilings'],
            })
            self.assertFalse(r['escalated'])
            self.assertEqual(r['effective_status'], 'NOT_EXPOSED')
            self.assertIn(r['requested']['reasoning_effort'], ['low', 'medium'])

    def test_expected_allocation(self):
        groups = [('ENG-00 IA-11 SWE-13 QA-16 PRT-05 BLD-27', 'medium'),
                  ('EVC-28 WIK-30 DOC-10 WRI-22 KRA-14 SHO-12', 'low')]
        for roles, effort in groups:
            for role in roles.split():
                self.assertEqual(resolve(role, 'setup')['requested'], {'model': SOL, 'reasoning_effort': effort})
        self.assertEqual(resolve('EVC-28', 'setup', 'evidence')['requested']['reasoning_effort'], 'low')

    def test_override_alias_escalation_and_rejections(self):
        r = resolve('SWE-13', 'setup', 'routine_fixes', 'Light')
        self.assertEqual(r['requested'], {'model': SOL, 'reasoning_effort': 'low'})
        self.assertEqual(r['selection_source'], 'work_class_override')
        self.assertEqual(resolve('SWE-13', 'setup', effort='high', reason='setup requires tracing a demonstrated cross-process deadlock')['requested']['reasoning_effort'], 'high')
        for args in [('UNKNOWN', 'setup'), ('SWE-13', ''), ('SWE-13', 'setup', 'unknown'), ('SWE-13', 'setup', ''), ('SWE-13', 'setup', None, 'ultra'), ('SWE-13', 'setup', None, 'high'), ('SWE-13', 'setup', None, 'max', 'because')]:
            with self.subTest(args=args), self.assertRaises(ValueError):
                resolve(*args)

    def test_role_floors_ceilings_and_model_escalation(self):
        task = 'repair-wrapper'
        reason = 'repair-wrapper needs cross-process diagnosis after repeated contradictory test results'
        for role, work_class, effort, rationale in [
            ('QA-16', 'documentation', None, reason),
            ('IA-11', None, 'Light', reason),
            ('DOC-10', None, 'high', reason),
            ('WIK-30', 'difficult_work', 'low', reason),
            ('ENG-00', 'difficult_work', 'medium', reason),
            ('ENG-00', None, 'xhigh', reason),
            ('SHO-12', None, 'high', reason),
            ('WEB-06', 'difficult_work', 'high', reason),
            ('SWE-13', 'difficult_work', 'xhigh', reason),
            ('SWE-13', 'difficult_work', 'max', reason),
            ('SWE-13', None, 'max', reason),
            ('SWE-13', 'difficult_work', None, None),
            ('SWE-13', 'difficult_work', None, 'This reason is detailed but omits the task ID'),
            ('KRA-14', 'coordination', None, None),
        ]:
            with self.subTest(role=role, work_class=work_class, effort=effort), self.assertRaises(ValueError):
                resolve(role, task, work_class, effort, rationale)
        for work_class, model, effort in [(None, SOL, 'xhigh'), ('difficult_work', ASTRA, 'high')]:
            result = resolve('SWE-13', task, work_class, effort, reason)
            self.assertTrue(result['escalated'])
            self.assertEqual(result['requested'], {'model': model, 'reasoning_effort': effort})

    def test_cli_checks_recorded_spawn_args_without_inventing_observation(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'spawn.json'
            command = [sys.executable, str(ROOT / 'tools/agent_model_policy.py'),
                       '--role', 'KRA-14', '--task', 'setup', '--effort', 'Light',
                       '--spawn-args', str(path), '--receipt', 'SYNTHETIC_TEST_RECEIPT']
            args = {'agent_type': 'default', 'model': SOL, 'reasoning_effort': 'low',
                    'fork_turns': 'none', 'message': 'fixture'}
            path.write_text(json.dumps(args))
            result = subprocess.run(command, capture_output=True, text=True, check=True)
            payload = json.loads(result.stdout)
            self.assertEqual(payload['spawn_arguments_status'], 'MATCH')
            self.assertEqual(payload['effective_status'], 'NOT_EXPOSED')
            for changes in ({'model': ASTRA}, {'fork_turns': 'all'}, {'agent_type': 'scp-kra-14'}):
                path.write_text(json.dumps(args | changes))
                result = subprocess.run(command, capture_output=True, text=True)
                self.assertEqual(result.returncode, 1)
                self.assertIn('BLOCKED', result.stderr)

    def test_missing_partial_matching_mismatch_provenance(self):
        r = resolve('IA-11', 'setup')
        for observed in (None, {}, {'model': SOL}):
            self.assertEqual(provenance(r, 'SYNTHETIC_TEST_RECEIPT', observed)['effective_status'], 'NOT_EXPOSED')
        self.assertEqual(provenance(r, 'SYNTHETIC_TEST_RECEIPT', r['requested'])['effective_status'], 'MATCH')
        for observed in ({'model': 'unknown'}, {'reasoning_effort': 'low'}):
            self.assertEqual(provenance(r, 'SYNTHETIC_TEST_RECEIPT', observed)['effective_status'], 'MISMATCH')
        with self.assertRaises(ValueError):
            provenance(r, '')

    def test_policy_rejects_coverage_bad_models_and_elevated_defaults(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'policy.json'
            for kind in ('coverage', 'model', 'default', 'duplicate', 'cap', 'role-cap', 'floor', 'astra-default'):
                p = json.loads(POLICY.read_text())
                if kind == 'coverage': del p['roles']['IA-11']
                if kind == 'model': p['work_classes']['documentation']['model'] = 'unknown'
                if kind == 'default': p['work_classes']['documentation']['reasoning_effort'] = 'high'
                if kind == 'cap': p['supported_models'][ASTRA].append('xhigh')
                if kind == 'role-cap': p['roles']['IA-11']['ceilings'][ASTRA] = 'xhigh'
                if kind == 'floor': p['roles']['IA-11']['floor'] = 'high'
                if kind == 'astra-default': p['roles']['IA-11']['default_work_class'] = 'difficult_work'
                path.write_text('{"schema_version":1,"schema_version":1}' if kind == 'duplicate' else json.dumps(p))
                with self.subTest(kind=kind), self.assertRaises(ValueError): load_policy(path)

    def test_install_preserve_repeat_check(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / '.codex/agents').mkdir(parents=True)
            config = root / '.codex/config.toml'
            config.write_text('model = "old"\nmodel_reasoning_effort = "high"\n# keep me\n[agents]\nmax_threads = 4\ndefault_subagent_model = \"old\"\n[features]\nabc = true\n[mcp_servers.custom]\ncommand = "custom"\n')
            custom = root / '.codex/agents/custom.toml'
            custom.write_text('name = "custom"\n')
            before = config.read_bytes()
            self.assertEqual(configure(root, check=True)['status'], 'MISSING')
            self.assertEqual(before, config.read_bytes())
            configure(root)
            parsed = tomllib.loads(config.read_text())
            self.assertEqual(parsed['mcp_servers'], {'custom': {'command': 'custom'}})
            self.assertEqual(parsed['features'], {'abc': True})
            self.assertEqual(parsed['agents'], {'max_threads': 4, 'default_subagent_model': SOL, 'default_subagent_reasoning_effort': 'medium'})
            self.assertIn('# keep me', config.read_text())
            self.assertEqual(parsed['model_reasoning_effort'], 'medium')
            self.assertEqual(custom.read_text(), 'name = "custom"\n')
            registered = json.loads(REGISTRY.read_text())['agents']
            self.assertEqual(len(list((root / '.codex/agents').glob('scp-*.toml'))), len(registered))
            for role in registered:
                role_id = role['agent_id']
                data = tomllib.loads((root / '.codex/agents' / f'scp-{role_id.lower()}.toml').read_text())
                requested = resolve(role_id, 'setup')['requested']
                self.assertEqual(data['model'], requested['model'])
                self.assertEqual(data['model_reasoning_effort'], requested['reasoning_effort'])
                self.assertEqual(data['description'], role['description'])
                for line in [role['jurisdiction'], *role['prohibited_actions']]:
                    self.assertIn(line, data['developer_instructions'])
            self.assertFalse(configure(root)['changed_paths'])
            self.assertFalse(configure(root, check=True)['changed_paths'])

    def test_conflicts_and_symlinks_fail_before_writes(self):
        for kind in ('conflict', 'symlink_file', 'symlink_directory', 'malformed'):
            with self.subTest(kind=kind), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                (root / '.codex/agents').mkdir(parents=True)
                config = root / '.codex/config.toml'
                config.write_text('model = "old"\n')
                role = root / '.codex/agents/scp-ia-11.toml'
                if kind == 'conflict': role.write_text('name = "mine"')
                if kind == 'symlink_file': role.symlink_to(config)
                if kind == 'symlink_directory':
                    (root / '.codex/agents').rmdir()
                    (root / 'outside').mkdir()
                    (root / '.codex/agents').symlink_to(root / 'outside', target_is_directory=True)
                if kind == 'malformed': config.write_text('model = "old"\nmodel = "dup"\n')
                before = config.read_bytes()
                with self.assertRaises(ValueError): configure(root)
                self.assertEqual(before, config.read_bytes())
                self.assertFalse((root / '.codex/agents/scp-eng-00.toml').exists())


if __name__ == '__main__': unittest.main()
