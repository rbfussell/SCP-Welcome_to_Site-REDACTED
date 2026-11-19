# Observed host MCP response defect during recovery

The actual Codex-exposed `workflow_failed` call for turn.002 / DOC-10 recorded its genuine usage-limit failure event, then returned an MCP response-schema error:

`Error executing tool workflow_failed: 1 validation error for DictModel; Input should be a valid dictionary; input_value=None, input_type=NoneType`.

Read-back through GameWorkflow confirmed the failure event exists. The subsequent actual host `workflow_interrupt` call succeeded and preserved turn.002 as INTERRUPTED with its independent REJECTED audit and IA-002-001. No Document Chief output or publication exists. This is an observed response-contract defect, not proof that recording failed. Do not repeat the mutation or hide the original error.

At observation, `Workflow.failed` returns None while `tools/agentic_mcp.py` declares every operation result as a dictionary. SWE-13 owns a bounded correction in turn.003, with an independent real MCP fixture regression; all synthetic test receipts must remain clearly fixture-only. This record is a parent transcription of the actual host tool result, not an invented independent review.
