# Library sizing pass — v0.3

Validated on 2026-10-02 UTC. Scope: the reusable library, optional host adapter, standalone demo, independent sample, tests and documentation. GBR was read-only throughout this pass. Prior authorized UITest changes were preserved.

- Debug and Release solution builds: successful, zero warnings/errors.
- Tests: 111 passed, zero failures/skips in each configuration.
- Both v0.3.0 packages generated; independent consumer compiled from those packages.
- Six release artifacts and assembly dependency boundaries verified. No GBR, ElliLib or RSR assembly dependencies.
- 383 generated output paths and all configured tool caches verified inside this repository.
- Native ImGui CPU previews reviewed, including the new Sizing lab at Compact/100% and Standard/150% and a narrow field with a wider searchable popup. These are diagnostic renders, not FFXIV screenshots. This revision was not installed into GBR or Dalamud.
- Git diff whitespace check passed. No commits, staging or other Git mutations performed.

GBR comparison: HEAD remained `a204f2f45cb98302dbe938921255c3a6d617c775`. Index hash and modified/untracked status matched the start-of-pass baseline. All 410 tracked/nonignored untracked file hashes matched; zero added or changed files. No GBR build or restore was run.

Reproducible build: set `DalamudLibPath` to the installed API 15 dev directory if needed, then run `pwsh -NoProfile -File ./eng/Build.ps1 -Previews` from the repository. The script establishes repo-local caches first.

Evidence remains under `artifacts/`: build-Debug.log, build-Release.log, test-Debug.log, test-Release.log, TestResults/, ARTIFACT_MANIFEST.json, gbr-library-sizing-baseline.json and gbr-library-sizing-verification.json. Outputs include `artifacts/lib/{Debug|Release}/CombatReborn.MaterialUI.dll`, the optional adapter beside it, `artifacts/demo/{Debug|Release}/CombatReborn.MaterialUI.Demo.dll`, and both v0.3.0 nupkg files.

Application column widths, pane proportions, explicit local overrides, tooltip behavior and semantic colors remain consumer responsibilities. Existing entry-point signatures remain available; consumers should rebuild against v0.3 and review the changed compatibility default described in [adaptive sizing](ADAPTIVE_SIZING.md).
