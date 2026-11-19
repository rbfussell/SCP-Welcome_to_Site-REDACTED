#!/usr/bin/env python3
"""Actual stdio MCP facade for the same game workflow CLI/runtime.

Run with the project harness interpreter. No model API, network listener, purchases
or automatic host spawning. Tool errors are returned by the MCP server as errors.
"""
from typing import Any
from mcp.server.fastmcp import FastMCP
from agentic import ROOT, OPERATIONS, execute

mcp = FastMCP('SCP game agentic workflow', instructions=(
    'Each turn accounts for every registered role exactly once. All default YES; '
    'each NO needs its own substantive task-specific reason. Authorize dispatch '
    'before actual independent host spawning, then record the real receipt and '
    'exact prompt. Recording a role never starts an agent or authenticates receipts. '
    'WIK-30 maintains the actual game wiki after specialists; then Evidence Collector, '
    'independent Internal Affairs and Document Chief last. Preserve failed/interrupted '
    'turns and new question/research/findings gates. Process JSON is not the game wiki. '
    'A local stdio protocol test or configuration file never proves Codex host connection.'))

DESCRIPTIONS = {
    'init': 'Initialize the append-only registry including the software/Wiki crew. No host worker is started.',
    'inventory': 'Read registered roles, all-YES defaults, readiness and explicit imported-project preflight.',
    'status': 'Read current turn and readiness. Optional arguments: turn_id.',
    'begin': 'Begin an assessed setup/work turn. arguments.assessment contains kind/task/requirements/research screening.',
    'plan': 'Get the all-YES draft for arguments.turn_id. Fill individual reasons and exact prompts before selection.',
    'select': 'Freeze every registered role exactly once. arguments: turn_id, selection keyed by role ID.',
    'authorize': 'Call BEFORE host spawning. arguments: turn_id, agent_id, exact prompt. Returns authorization_id.',
    'invoke': 'Record actual host receipt AFTER authorized spawn. arguments: turn_id, agent_id, prompt, receipt, backend, authorization.',
    'handoff': 'Record owned artifacts, real tests and limitations. arguments: turn_id, value (structured handoff).',
    'advance': 'Advance one phase only. arguments: turn_id, next_state.',
    'collect': 'EVC-28 builds the genuine hashed evidence inventory. arguments: turn_id.',
    'audit': 'Record independent IA disposition/findings after IA handoff. arguments: turn_id, report.',
    'publish': 'Document Chief publishes process outcome last. arguments: turn_id, entities, optional resolutions.',
    'interrupt': 'Preserve and interrupt an unfinished turn. arguments: turn_id, reason.',
    'import': 'Record immutable scoped historical import for later actual setup review. arguments.value has provenance refs.',
    'register': 'Register/amend a scoped role before selection. arguments: agent, optional amendment/reason.',
    'question': 'Record scoped ambiguity from an active worker. arguments: turn_id, agent_id, question.',
    'answer': 'Record actual user answer and evidence. arguments: turn_id, question_id, answer, evidence.',
    'request-research': 'Queue new research; requesting specialist stops this turn. arguments: turn_id, agent_id, request.',
    'respond-research': 'Record later accepted-source research response. arguments: turn_id, request_id, findings, evidence.',
    'failed': 'Record actual invocation failure without concealing it. arguments: turn_id, agent_id, reason.',
    'readiness': 'Check local execution prerequisites; never asserts a live Codex host connection. Optional agent_ids.',
}


def operation_tool(operation):
    def tool(arguments: dict[str, Any]) -> dict[str, Any]:
        return execute(operation, arguments, ROOT)
    tool.__name__ = 'workflow_' + operation.replace('-', '_')
    tool.__doc__ = DESCRIPTIONS[operation]
    return tool


for operation in OPERATIONS:
    mcp.tool()(operation_tool(operation))

if __name__ == '__main__':
    mcp.run(transport='stdio')
