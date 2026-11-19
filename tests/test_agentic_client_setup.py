"""Workspace MCP registration must preserve unrelated/custom settings and truthful status."""
import json
from pathlib import Path
import sys
import tempfile
import tomllib
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
from configure_agentic_client import configure, NAME


class AgenticClientSetupTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.workspace = Path(self.tmp.name)
        self.game = self.workspace / 'Project'
        for relative in ('.venv-harness/bin/python', 'tools/agentic_mcp.py'):
            target = self.game / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text('Fixture target only; not a running MCP server.\n')
        self.target = self.workspace / '.codex/config.toml'

    def test_registers_only_workspace_server_with_unverified_status(self):
        result = configure(self.workspace, self.game)
        self.assertEqual(result['status'], 'CONFIGURED_UNVERIFIED')
        self.assertFalse(result['host_tool_observed'])
        entry = tomllib.loads(self.target.read_text())['mcp_servers'][NAME]
        self.assertEqual(entry['args'], [str(self.game / 'tools/agentic_mcp.py')])
        self.assertEqual(entry['cwd'], str(self.game))

    def test_repeated_configuration_preserves_unrelated_text(self):
        self.target.parent.mkdir()
        unrelated = '# User settings remain verbatim\nmodel = "configured-model"\n\n[mcp_servers.other]\ncommand = "other-tool"\n'
        self.target.write_text(unrelated)
        configure(self.workspace, self.game)
        before = self.target.read_bytes()
        self.assertFalse(configure(self.workspace, self.game)['changed'])
        self.assertEqual(self.target.read_bytes(), before)
        self.assertTrue(self.target.read_text().startswith(unrelated.rstrip()))

    def test_custom_same_name_entry_is_not_overwritten(self):
        self.target.parent.mkdir()
        original = f'[mcp_servers.{NAME}]\ncommand = "user-custom-server"\n'
        self.target.write_text(original)
        with self.assertRaisesRegex(ValueError, 'custom'):
            configure(self.workspace, self.game)
        self.assertEqual(self.target.read_text(), original)

    def test_check_reports_missing_without_writing(self):
        result = configure(self.workspace, self.game, check=True)
        self.assertEqual(result['status'], 'MISSING')
        self.assertFalse(self.target.exists())

    def test_missing_server_is_not_registered(self):
        (self.game / 'tools/agentic_mcp.py').unlink()
        with self.assertRaisesRegex(ValueError, 'bridge'):
            configure(self.workspace, self.game)
        self.assertFalse(self.target.exists())

    def test_configuration_cannot_escape_workspace(self):
        with self.assertRaisesRegex(ValueError, 'escapes'):
            configure(self.workspace, self.game, output=self.workspace.parent / 'outside.toml')


if __name__ == '__main__':
    unittest.main()
