#!/usr/bin/env python3
"""Invoke an actual bundled workflow MCP tool via a local stdio client.

This supports initialization/logging before a host reload exposes the same tools.
It is a real protocol call, not proof that the Codex host itself connected.
"""
import argparse
import asyncio
import json
from pathlib import Path
import sys
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

ROOT = Path(__file__).resolve().parents[1]


async def call(operation, arguments):
    parameters = StdioServerParameters(command=sys.executable,
        args=[str(ROOT / 'tools/agentic_mcp.py')], cwd=str(ROOT))
    async with stdio_client(parameters) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            result = await session.call_tool('workflow_' + operation.replace('-', '_'), {'arguments': arguments})
            if result.isError:
                raise ValueError('MCP tool failed: ' + str(result.content))
            payload = getattr(result, 'structuredContent', None)
            if payload is None:
                texts = [c.text for c in result.content if getattr(c, 'type', None) == 'text']
                payload = json.loads(texts[0])
            return payload


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation')
    parser.add_argument('--input', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    try:
        arguments = json.loads(args.input.read_text()) if args.input else {}
        result = asyncio.run(asyncio.wait_for(call(args.operation, arguments), timeout=30))
        rendered = json.dumps(result, indent=2, ensure_ascii=False) + '\n'
        if args.output:
            args.output.parent.mkdir(parents=True, exist_ok=True)
            with args.output.open('x') as output:
                output.write(rendered)
        else:
            print(rendered, end='')
    except Exception as error:
        print(f'MCP_CALL_FAIL: {error}', file=sys.stderr); sys.exit(1)
