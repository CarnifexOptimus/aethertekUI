# Final prototype verification
Observed 2026-09-30 (America/Chicago). Build/test logs and generated artifacts remain under this repository.

| Check | Result |
|---|---|
| Debug solution build | PASS — 0 warnings, 0 errors |
| Release solution build | PASS — 0 warnings, 0 errors |
| Debug tests | PASS — 39 passed, 0 failed, 0 skipped |
| Release tests | PASS — 39 passed, 0 failed, 0 skipped |
| Reusable assembly | Present; Release SHA256 8CFB2C3634234AA91EA2DA49EDA4682067966D5514691E831C9CADD1478396E9 |
| Demo assembly | Present; Release SHA256 AA734FD8D1977FB77BAC208E97D7BEE44BDE782258BD31401649E13ACBA0B80A |
| Local package | artifacts/CombatReborn.MaterialUI.0.1.0.nupkg |
| Package contents | README and reusable DLL under lib/net10.0-windows7.0; no bundled Dalamud assemblies |
| Output-boundary check | 287 generated MSBuild output paths within repository; configured cache/temp locations within .cache; no outside paths found |
| Production dependency inspection | System.Runtime, System.Numerics.Vectors, System.Collections, Dalamud.Bindings.ImGui only |
| Source dependency scan | No GatherBuddy, ElliLib or RSR references in src/tests |
| Native behavior | Switch/segments/input/buttons/chips/checkbox/radio/nav/tiles plus modal/table/child scopes exercised in real cimgui context |
| Scope restoration | Every pushed theme color restored; nested theme and representative geometry restoration checked |
| Visual review | All five actual demo pages inspected via mesh previews; settings also inspected at 150% host scale |
| GBR HEAD | a204f2f45cb98302dbe938921255c3a6d617c775 unchanged |
| GBR working tree | Same 4 modified and 25 untracked paths as baseline |
| GBR contents/index | All 431 recorded file hashes and index SHA256 match; no differences |
| Mission | Original SHA256 D20CA1C9BEC4EDB110EFCA974C2E8FD5B9B31CDAA503B4DBC9AB362E4B5675CC unchanged |
| Remote | None configured; no remote created/pushed |
| Installation | Demo not copied/installed into Dalamud/XIVLauncher |

Evidence: GBR_BASELINE.json, GBR_FILE_BASELINE.json, GBR_FINAL_VERIFICATION.json; artifacts/ARTIFACT_MANIFEST.json; artifacts/local-output-verification.json; artifacts/build-Debug.log, build-Release.log, test-Debug.log, test-Release.log; artifacts/TestResults/*.trx.

Git diff review: source/docs/scripts reviewed and staged whitespace check passes when excluding the immutable PROJECT_MISSION.md. The full check flags its original seven-equals MISSION underline as a conflict marker. That is original prompt text, not an actual merge conflict, and must not be changed.

The output-boundary result is based on recorded generated paths and configured process environment, not a system-wide filesystem monitor. In-game plugin loading, GPU compositing, custom GBR gamepad behavior and real Dalamud fonts remain manual acceptance tasks; see PROTOTYPE_SMOKE_TEST.md and KNOWN_LIMITATIONS.md.
