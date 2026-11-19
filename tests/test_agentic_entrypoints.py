"""Real entry-point regressions in isolated, explicitly synthetic workflow fixtures.

Run with .venv-harness/bin/python so the real stdio MCP test executes. Fixture
receipts below are never production dispatch evidence or independent reviews.
"""
import asyncio
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


class AgenticEntrypointTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix='scp-agentic-entrypoint-FIXTURE-')
        self.addCleanup(self.tmp.cleanup)
        self.outer = Path(self.tmp.name)
        self.game = self.outer / 'Project'
        shutil.copytree(ROOT / 'management/harness/runtime',
                        self.game / 'management/harness/runtime')
        (self.game / 'tools').mkdir()
        for name in ('agentic.py', 'game_workflow.py', 'manage_project.py'):
            shutil.copyfile(ROOT / 'tools' / name, self.game / 'tools' / name)
        (self.outer / 'scripts').mkdir()
        self.wrapper = self.outer / 'scripts/manage_game.py'
        shutil.copyfile(ROOT / 'management/harness/manage_game.py', self.wrapper)
        (self.outer / 'config').mkdir()
        (self.outer / 'config/software-project.json').write_text(json.dumps({
            'profile': 'software-game', 'project_path': 'Project'}))
        (self.game / 'management/tasks').mkdir()
        (self.game / 'management/profile.json').write_text(json.dumps({
            'profile': 'software-game', 'execution': 'agentic'}))
        self.workflow = GameWorkflow(self.game)
        self.workflow.initialize()

    def run_json(self, argv, cwd):
        result = subprocess.run([sys.executable, *map(str, argv)], cwd=cwd,
                                capture_output=True, text=True, timeout=30)
        self.assertEqual(result.returncode, 0, result.stderr)
        return json.loads(result.stdout)

    def test_wrapper_agents_from_workspace_and_unrelated_cwd(self):
        with tempfile.TemporaryDirectory(prefix='scp-agentic-unrelated-FIXTURE-') as other:
            for cwd in (self.outer, Path(other)):
                with self.subTest(cwd=cwd):
                    result = self.run_json([self.wrapper, 'agents', 'readiness'], cwd)
                    self.assertEqual(result['status'], 'READY')

    def test_wrapper_legacy_board_and_direct_cli_remain_usable(self):
        board = self.run_json([self.wrapper, 'status'], self.outer)
        self.assertEqual(board['game_root'], str(self.game))
        self.assertEqual(board['tasks'], [])
        direct = self.run_json([self.game / 'tools/agentic.py', 'readiness'], self.game)
        self.assertEqual(direct['status'], 'READY')

    def test_wrapper_rejects_project_pointer_escape(self):
        (self.outer / 'config/software-project.json').write_text(json.dumps({
            'profile': 'software-game', 'project_path': '..'}))
        result = subprocess.run([sys.executable, str(self.wrapper), 'agents', 'readiness'],
                                cwd=self.outer, capture_output=True, text=True, timeout=30)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Invalid software project pointer', result.stderr)

    def pending_fixture_invocation(self):
        turn = self.workflow.begin({
            'kind': 'setup', 'task': 'Synthetic failure acknowledgement regression fixture only.',
            'requirements': ['Record exactly one fixture failure and preserve validation.'],
            'research_required': False, 'architecture_change': False,
            'research_reason': 'Known local interface regression; no product research is required.'})
        turn_id = turn['turn_id']
        selection = {role['agent_id']: {
            'selected': True,
            'reason': f"{role['agent_id']} is selected only to exercise this synthetic lifecycle fixture.",
            'prompt': f"Act as {role['agent_id']} only in a synthetic regression fixture; no actual worker is spawned.",
            'dependencies': []} for role in turn['registry']['agents']}
        selection['WIK-30']['dependencies'] = [role['agent_id'] for role in turn['registry']['agents']
            if role['phase'] not in ('collection', 'audit', 'documentation') and role['agent_id'] != 'WIK-30']
        self.workflow.select(turn_id, selection)
        prompt = selection['ENG-00']['prompt']
        ticket = self.workflow.authorize_dispatch(turn_id, 'ENG-00', prompt)
        self.workflow.invoke(turn_id, 'ENG-00', prompt,
                             'SYNTHETIC-FIXTURE-ONLY-not-a-host-receipt',
                             'SYNTHETIC-FIXTURE-ONLY-not-a-host', ticket['authorization_id'])
        return {'turn_id': turn_id, 'agent_id': 'ENG-00',
                'reason': 'Synthetic fixture failure; no real worker failed or was dispatched.'}

    def assert_failure_ack(self, result, arguments):
        self.assertEqual(result, {**arguments, 'status': 'FAILED', 'recorded': True})
        turn = self.workflow.turn(arguments['turn_id'])
        failures = [event for event in turn['events'] if event['kind'] == 'FAILED']
        self.assertEqual(len(failures), 1)
        self.assertEqual(failures[0]['data'], {
            'agent_id': arguments['agent_id'], 'reason': arguments['reason']})

    def test_cli_failure_acknowledges_append_and_rejects_duplicate(self):
        arguments = self.pending_fixture_invocation()
        path = self.outer / 'SYNTHETIC-FIXTURE-ONLY-failure.json'
        path.write_text(json.dumps(arguments))
        argv = [self.game / 'tools/agentic.py', 'failed', '--input', path]
        result = self.run_json(argv, self.game)
        self.assert_failure_ack(result, arguments)
        before = self.workflow.turn(arguments['turn_id'])['events']
        duplicate = subprocess.run([sys.executable, *map(str, argv)], cwd=self.game,
                                   capture_output=True, text=True, timeout=30)
        self.assertNotEqual(duplicate.returncode, 0)
        self.assertIn('No pending invocation', duplicate.stderr)
        self.assertEqual(self.workflow.turn(arguments['turn_id'])['events'], before)

    async def mcp_failure_sequence(self, arguments):
        # Importing here intentionally fails if the required real MCP dependency
        # is missing; this test must not silently skip its protocol coverage.
        from mcp import ClientSession, StdioServerParameters
        from mcp.client.stdio import stdio_client
        source = ('import sys; from pathlib import Path; '
                  f'sys.path.insert(0, {str(ROOT / "tools")!r}); '
                  'import agentic_mcp; '
                  f'agentic_mcp.ROOT = Path({str(self.game)!r}); '
                  'agentic_mcp.mcp.run(transport="stdio")')
        server = StdioServerParameters(command=sys.executable, args=['-c', source], cwd=str(ROOT))
        transcript = []
        async with stdio_client(server) as (read, write):
            async with ClientSession(read, write) as session:
                initialized = await session.initialize()
                for label, args in (
                    ('invalid-agent', {**arguments, 'agent_id': 'UNKNOWN-99'}),
                    ('invalid-reason', {**arguments, 'reason': ''}),
                    ('recorded-failure', arguments),
                    ('duplicate-rejected', arguments)):
                    result = await session.call_tool('workflow_failed', {'arguments': args})
                    transcript.append({'label': label, 'arguments': args,
                                       'response': result.model_dump(mode='json'),
                                       'events': self.workflow.turn(arguments['turn_id'])['events']})
                self.workflow.interrupt(arguments['turn_id'],
                                        'Synthetic fixture sealed-turn rejection check only.')
                result = await session.call_tool('workflow_failed', {'arguments': arguments})
                transcript.append({'label': 'sealed-rejected', 'arguments': arguments,
                                   'response': result.model_dump(mode='json'),
                                   'events': self.workflow.turn(arguments['turn_id'])['events']})
        return {'scope': 'SYNTHETIC_FIXTURE_ONLY_REAL_LOCAL_STDIO_PROTOCOL',
                'fixture_root': str(self.game), 'protocol_version': initialized.protocolVersion,
                'receipts_are_production_evidence': False, 'calls': transcript}

    def test_real_mcp_failure_ack_and_rejections_preserve_single_event(self):
        arguments = self.pending_fixture_invocation()
        transcript = asyncio.run(asyncio.wait_for(self.mcp_failure_sequence(arguments), timeout=30))
        print('MCP_FIXTURE_EVIDENCE ' + json.dumps(transcript, sort_keys=True))
        calls = transcript['calls']
        for index, text in ((0, 'No pending invocation'), (1, 'Failure reason required'),
                            (3, 'No pending invocation'), (4, 'Turn is sealed')):
            self.assertTrue(calls[index]['response']['isError'], calls[index])
            self.assertIn(text, str(calls[index]['response']['content']))
        self.assertFalse(calls[2]['response']['isError'], calls[2])
        self.assert_failure_ack(calls[2]['response']['structuredContent'], arguments)
        self.assertFalse(any(e['kind'] == 'FAILED' for e in calls[0]['events']))
        self.assertEqual(calls[0]['events'], calls[1]['events'])
        self.assertEqual(calls[2]['events'], calls[3]['events'])
        self.assertEqual(len([e for e in calls[4]['events'] if e['kind'] == 'FAILED']), 1)


if __name__ == '__main__':
    unittest.main()
