# Active software-game workflow

This workspace contains an existing game in `Project/`, with its own Git repository.
The user requested adapting this harness for software development while preserving
that game's development/wiki process. **This section overrides the generic harness
rules below for work on this game and its development tools.**

- Read `Project/AGENTS.md`, `Project/management/README.md`, the task board, relevant
  game-wiki pages and linked source/design documents before planning changes.
  The user's earlier root-relative wiki/tool paths now resolve from `Project/`.
- Use `python3 scripts/manage_game.py status` at this outer root, or
  `python3 tools/manage_project.py status` from `Project/`. The same records drive both.
- Work in small acceptance-defined software slices. Keep scope, dependencies,
  design questions, reading notes, real command logs and review limitations in
  `Project/management/`. Update the game wiki and other applicable Markdown
  alongside implementation; run the existing game checks and final consistency gate.
- One developer may perform coordination, implementation, evidence collection,
  self-review and documentation. Declare self-review honestly. Separate agents,
  fixed crews, manufacturing roles, Vince, Designer/Builder dispatch and independent
  IA are **not mandatory** in this profile. Delegate only when explicitly requested
  by the user or applicable instructions. Never invent independent reviews.
- Import existing decisions/evidence as history; do not restart the accepted game's
  setup/questions/research as three artificial turns. Ask about material ambiguity
  and research relevant new decisions as needed; do not fabricate answers.
- Keep `Project/.git`, `.github`, wiki tooling, source and canon archive in place.
  Run game commands from `Project/`; inspect and preserve unrelated changes.
- The generic strict workflow below and `scripts/select_agents.py` remain available
  for an explicitly chosen strict project. Their mandatory dispatch/preflight
  sequence is not a completion claim made by this software adapter.
- No MCP installation, paid accounts, hardware tools or infrastructure services are
  required for the adapter. Its Python CLI uses the standard library.

The versioned overlay sources and reinstall command are in
`Project/management/harness/` and `Project/tools/configure_project_harness.py`.
