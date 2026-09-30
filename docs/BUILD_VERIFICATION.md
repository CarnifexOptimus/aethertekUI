# Integration kit v0.2 verification

Observed 2026-09-30, America/Chicago. The complete eng/Build.ps1 -Previews workflow succeeded after the final changes.

| Check | Result |
|---|---|
| Debug and Release solution builds | PASS; each 0 warnings / 0 errors |
| Tests in each configuration | PASS; 63 passed, 0 failed, 0 skipped |
| Core / adapter / demo DLLs | Present in artifacts/lib and artifacts/demo |
| Local packages | Core and Dalamud adapter 0.2.0 |
| Independent ProjectReference consumer | Compiles in both configurations |
| Independent local-package consumer | Release compiles, both dependency DLLs copied; fresh isolated cache and generated lockfile |
| Dependencies | Core: System.Runtime, System.Numerics.Vectors, System.Collections, Dalamud.Bindings.ImGui; adapter adds core and Dalamud |
| Forbidden production references | No GatherBuddy, ElliLib or RSR |
| Managed performance sample | Release: 300 warmed catalog frames, 47.05 ms total, 40 allocated bytes, 7,800 submitted rows from a 10,000-row source |
| Performance limits | Diagnostic headless CPU submission only; native allocations, GPU cost and game workload excluded; timing is an observation, not an SLA |
| Visual review | Seven demo pages plus Compact/100% and Standard/150% integration previews |
| Preview diagnostic | Debug/Release native mesh dump for large Standard catalog was byte-identical; optimized diagnostic renderer fixed by snapshotting native vectors before nested indexing |
| Output boundaries | 371 MSBuild-generated paths checked; all configured caches/temp paths local; no outside paths found |
| GBR HEAD | a204f2f45cb98302dbe938921255c3a6d617c775 unchanged |
| GBR state | Same 4 modified / 25 untracked paths as baseline |
| GBR files / index | All 431 recorded hashes match; index SHA256 unchanged |
| Mission | SHA256 D20CA1C9BEC4EDB110EFCA974C2E8FD5B9B31CDAA503B4DBC9AB362E4B5675CC unchanged |
| Remote / installation | No remote publication or Dalamud installation performed |

Release core SHA256: E5897B0660537DD53F72C36C4F81BB149ED621A4583E9613E7B8D85DB2EFE4BB.
Release adapter SHA256: 8FFC924C26FB9D64061E7932495045E75E7055D412C28B7C557F72B9A235E508.
Release demo SHA256: 9F9A57DE5B7B9DD8F7D9E886B3A8BAAE02FF2187B6ADE833F0638C4DBF09DB44.

Evidence: artifacts/ARTIFACT_MANIFEST.json, dependencies.txt, local-output-verification.json, consumer-package.log, build/test logs, TestResults/tests-{Debug,Release}.trx, previews/, and docs/GBR_FINAL_VERIFICATION.json. The baseline files are unchanged.

The boundary check is based on output manifests and process configuration, not a system-wide filesystem monitor. Runtime acceptance in FFXIV is still pending. Whitespace review excludes the immutable PROJECT_MISSION.md because its original seven-equals underline triggers Git's conflict-marker heuristic.
