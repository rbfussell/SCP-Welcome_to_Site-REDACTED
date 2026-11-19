#!/usr/bin/env python3
"""Probe a real stdio subprocess: initialize, list tools and read game inventory.

This is a local client session, not evidence the Codex host has loaded its config.
"""
import asyncio
from datetime import datetime, timezone
import json
from pathlib import Path
import sys
import os
import signal
import subprocess
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

ROOT = Path(__file__).resolve().parents[1]


async def probe():
    server = StdioServerParameters(command=sys.executable,
        args=[str(ROOT / 'tools/agentic_mcp.py')], cwd=str(ROOT))
    async with stdio_client(server) as (read, write):
        async with ClientSession(read, write) as session:
            initialized = await session.initialize()
            listed = await session.list_tools()
            names = [tool.name for tool in listed.tools]
            required = {'workflow_init', 'workflow_inventory', 'workflow_authorize',
                        'workflow_invoke', 'workflow_collect', 'workflow_audit', 'workflow_publish'}
            if not required <= set(names):
                raise ValueError('Required lifecycle tools not exposed')
            result = await session.call_tool('workflow_inventory', {'arguments': {}})
            if result.isError:
                raise ValueError(f'Inventory tool failed: {result.content}')
            payload = getattr(result, 'structuredContent', None)
            if not isinstance(payload, dict) or 'registry' not in payload:
                texts = [c.text for c in result.content if getattr(c, 'type', None) == 'text']
                payload = json.loads(texts[0])
            ids = [role['agent_id'] for role in payload['registry']['agents']]
            if 'WIK-30' not in ids:
                raise ValueError('Actual game wiki specialist is not registered')
            return {'result': 'PASS', 'recorded_at': datetime.now(timezone.utc).isoformat(),
                    'scope': 'LOCAL_STDIO_CLIENT_PROTOCOL',
                    'host_registration': 'CONFIGURED_UNVERIFIED', 'host_tool_observed': False,
                    'protocol_version': initialized.protocolVersion,
                    'tools': names, 'roles': ids,
                    'inventory_readiness': payload['readiness'],
                    'limitations': ['The probe starts its own server subprocess.',
                                    'A live Codex host tool call is still required after MCP reload.']}


def bounded_probe(timeout=30):
    # A process-group watchdog bounds even blocked sandbox pipe/cleanup operations.
    process = subprocess.Popen([sys.executable, str(Path(__file__).resolve()), '--worker'],
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, start_new_session=True)
    try:
        output, errors = process.communicate(timeout=timeout)
    except subprocess.TimeoutExpired:
        os.killpg(process.pid, signal.SIGKILL)
        process.communicate()
        raise TimeoutError(f'Local MCP protocol exceeded {timeout}s; no connection success claimed')
    if process.returncode:
        raise ValueError(errors or output or f'MCP worker exited {process.returncode}')
    return json.loads(output)


if __name__ == '__main__':
    try:
        result = asyncio.run(probe()) if sys.argv[1:] == ['--worker'] else bounded_probe()
        print(json.dumps(result, indent=2))
    except Exception as error:
        print(f'MCP_PROTOCOL_PROBE_FAIL: {error}', file=sys.stderr)
        sys.exit(1)
