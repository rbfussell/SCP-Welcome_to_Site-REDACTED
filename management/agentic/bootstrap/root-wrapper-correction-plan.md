# Provisional correction plan for independent review

Observed root agents shortcut fails with ModuleNotFoundError game_workflow.
The wrapper uses runpy.run_path but does not make the target tools directory importable.
Direct game CLI and MCP protocol operations succeed.

After the current frozen-source review is published, assign SWE-13 a bounded correction:
prepend the resolved game tools directory to sys.path in the versioned wrapper before
running agentic.py; reinstall the overlay. QA should add a regression invoking the
installed outer agents shortcut from both workspace and unrelated working directories.
Retain direct task CLI compatibility and preserve unrelated root files.

This is a proposed technical fix, not a source modification or an accepted IA finding.
The actual IA reviewer chooses severity/disposition and source-backed corrective action.
Do not finalize the overall setup task while this entry point remains broken.
