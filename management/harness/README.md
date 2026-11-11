## Active project: SCP game software development

The game lives in **`Project/`** (capital P) and retains its own Git repository.
Start with [the game README](Project/README.md),
[the management guide](Project/management/README.md), and
[the game wiki](Project/source_material/game-wiki/index.html).

```sh
python3 scripts/manage_game.py status
python3 scripts/manage_game.py check
cd Project
python3 tools/run_spatial_scene.py --check
```

This copy uses the `software-game` adapter: scoped tasks, dependencies, evidence,
explicit self/independent review, and mandatory game-wiki consistency. It does not
require manufacturing specialists, a permanent dispatched crew, or replaying the
existing project's initial design process. Root `config/software-project.json`
points to versioned tools/records inside the game checkout. The harness root is
currently outside the game's Git history; reinstall the small overlay after a
move with `python3 Project/tools/configure_project_harness.py --root .`.

The original **strict harness reference follows**. Its CLI, crew and preflight
rules apply only when that separate workflow is deliberately selected. The game
adapter does not claim strict-harness certification or independent IA.
