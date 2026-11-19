# Workspace model allocation

The policy covers the versioned game registry. Every role has an effort floor, a default work class and a ceiling for each model it may use. All 23 defaults use GPT-6.2 Sol at Low or Medium. The coordinator uses Medium by default and may request Sol through High. Vince uses Low by default and may request Sol through Medium. See `policy.json` for the full allocations.

The resolver caps Sol at `xhigh` and Astra at `high`. A role can have a lower ceiling or lack an Astra allocation. Light is an alias for the canonical `low` effort. If a role admits Astra, choose `difficult_work` only for a genuinely difficult assignment. Record the concrete problem, limits of the usual Sol tier, and the reasoning the assignment requires. Any request above a role's default effort, and every Astra request, needs a rationale of at least 30 characters that names the task. The resolver checks the presence and bounds of that rationale. Independent review assesses its merits.

From the game root:

```sh
python3 tools/agent_model_policy.py --role SWE-13 --task repair-wrapper --work-class routine_fixes --effort Light
python3 tools/agent_model_policy.py --role SWE-13 --task concurrency-repair --work-class difficult_work --effort high --reason 'concurrency-repair requires reconciling contradictory lock traces across three processes after the Sol investigation'
python3 tools/configure_agent_models.py --workspace ..
python3 tools/configure_agent_models.py --workspace .. --check
```

Save the resolution before the turn's dispatch. It records the task, role, work class, applied allocation, reason, requested pair, policy hash and explicit collaboration parameters. Obtain workflow authorization before spawning. For explicit collaboration overrides, use the default agent type and the emitted `model`, `reasoning_effort` and `fork_turns: none`. Put the exact authorized assignment in the host's message. Named native roles have fixed configuration in the observed collaboration tool. Use the default agent type when choosing a tier through this resolver.

Retain the actual spawn arguments separately from the requested preset and add `--spawn-args actual-spawn-arguments.json` to the same resolver command to validate the three allocation arguments and the agent type. A mismatch exits 1. The check compares supplied records and leaves worker authentication to independent review. After the actual invocation, add `--receipt ACTUAL_RECEIPT` and, when available, `--observed actual-host-metadata.json` containing host-reported model and effort. Missing or partial matching effective metadata remains `NOT_EXPOSED`. Any observed difference is `MISMATCH` with CLI exit 2. Only two matching reported fields produce effective `MATCH`. Enter host observations only when the host supplies them.

The installer sets Sol Medium for future coordinator starts and generic subagents and generates 23 native profiles under workspace `.codex/agents/`. Each profile carries its registry description, jurisdiction, prohibited actions and requested default. The installer preserves unrelated workspace settings, MCP entries and unrelated custom role files. Malformed TOML, unmanaged name conflicts and symlink targets fail before the first write. Reinstallation is idempotent, and `--check` is read-only. Use one configuration owner. [Codex configuration reference](https://learn.chatgpt.com/docs/config-file/config-reference), [Codex subagent configuration](https://learn.chatgpt.com/docs/agent-configuration/subagents).

The installer and resolver describe requested settings. Runtime model execution, usage, quota savings and task quality require separate observation. Run the focused behavior tests with `python3 -m unittest discover -s tests -p test_agent_model_policy.py -v`. The fixtures exercise the policy and installer without making model requests.
