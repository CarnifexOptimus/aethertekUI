# Whole-GBR integration kit delivered

Repository: C:\Users\pscho\OneDrive\Documents\GitRepos\CombatReborn.MaterialUI
Version: 0.2.0. Local main branch; use git rev-parse HEAD for the delivery commit. No remote publication.

## Structure and architecture

CombatReborn.MaterialUI.sln contains:
- src/CombatReborn.MaterialUI: reusable Microsoft.NET.Sdk assembly with Core, Drawing, Components and Data.
- src/CombatReborn.MaterialUI.Dalamud: optional Microsoft.NET.Sdk host adapter.
- src/CombatReborn.MaterialUI.Demo: Dalamud.NET.Sdk/15.0.0 showcase with normal ProjectReference.
- tests/CombatReborn.MaterialUI.Tests: pure logic plus real native ImGui interaction/render checks.
- samples/ConsumerContract: independent compiled consumer, verified with both project and package references.
- docs and eng: audit, integration contracts, reproducible local build/verification scripts.
All projects target .NET 10, Windows x64. Libraries do not own plugin lifecycle.

## Components and themes

The original buttons, icon buttons, switches, checkboxes/radios, segmented controls, cards, surfaces, chips, fields, navigation, tabs, tiles, badges, progress, modal/header and native-table styles remain available.

Added: searchable typed combos, numeric inputs/sliders, multiline editing, bit filters, inherited bool settings, native trees/context menus, typed drag/drop, resizable split panes, cached filter/sort views, persistent key selection, native clipping, borrowed image/glyph controls, semantic color scopes, explicit navigation traversal, cached serializable theme settings and the WindowSystem adapter. Segment keys support reorderable options; host-owned search state restores typing after clear.

RGB generation remains deterministic cached OKLCH with chroma reduction for sRGB gamut. Alpha, density and reduced motion update independently. Colors-only theme scopes preserve geometry styles for specialized game overlays. Exact HCT is not claimed.

## Demo

Seven interactive pages: Gallery, Automation (plus floating overlay), Gathering data, Dashboard, Settings, List editor and Large catalog. The editor exercises 500 searchable options, folders/drag-drop/context actions, numeric/inherited settings and multiline input. The catalog exercises 10,000 rows, native sorting/reordering/hiding/resizing, clipped submission and persistent selection.

## Validation

Debug and Release: 0 warnings, 0 errors, 63 tests passed each. The independent consumer also builds using both freshly packed packages, with dependency DLLs beside its output. The final large-table sample submitted 26 rows/frame and allocated 40 managed bytes total across 300 warmed frames; this excludes native allocation and GPU work. Full evidence and caveats are in BUILD_VERIFICATION.md.

## Exact deliverables

Under C:\Users\pscho\OneDrive\Documents\GitRepos\CombatReborn.MaterialUI\:
- artifacts\lib\Release\CombatReborn.MaterialUI.dll
- artifacts\lib\Release\CombatReborn.MaterialUI.Dalamud.dll
- artifacts\lib\Debug\CombatReborn.MaterialUI.dll
- artifacts\lib\Debug\CombatReborn.MaterialUI.Dalamud.dll
- artifacts\demo\Release\CombatReborn.MaterialUI.Demo.dll
- artifacts\demo\Debug\CombatReborn.MaterialUI.Demo.dll
- artifacts\CombatReborn.MaterialUI.0.2.0.nupkg
- artifacts\CombatReborn.MaterialUI.Dalamud.0.2.0.nupkg
- artifacts\consumer-package\ConsumerContract.dll

## Rebuild and later loading

```powershell
Set-Location 'C:\Users\pscho\OneDrive\Documents\GitRepos\CombatReborn.MaterialUI'
pwsh -NoProfile -File .\eng\Build.ps1 -Previews
pwsh -NoProfile -File .\eng\Verify-Gbr.ps1
```

The scripts redirect tool caches/temp before tooling runs. To load later, keep the entire artifacts/demo/Release directory together, add its Demo DLL to compatible Dalamud API 15 developer plugin locations, enable/reload it, and run /crmaterial. Do not load the core or adapter as plugins. No installation was performed here; README and PROTOTYPE_SMOKE_TEST contain the manual procedure.

## Readiness and remaining acceptance

The library supports the common patterns found across GBR, including UI outside Gui. FULL_GBR_COVERAGE and INTEGRATION_GUIDE specify the migration boundary. The ConsumerContract compiles the intended host entry point and component usage.

This is ready for a separately authorized wiring pass. Complete end-to-end plug-and-play behavior inside GBR has not been proven because wiring and live testing are intentionally excluded. Preserve existing domain state, ColorId semantics, textures, item tooltips, table abstractions, fish-timer charts, spearfishing coordinates and controller routing.

Next improvements should follow live acceptance: fonts/contrast over game scenes, controller focus and popup behavior, specialized overlay coordinates, localization/long labels and keyboard splitter resizing. Dark-only and pre-1.0 API limitations are documented in KNOWN_LIMITATIONS.

## GBR unchanged

The final comparison reports no differences: HEAD a204f2f45cb98302dbe938921255c3a6d617c775; original modified/untracked state unchanged; index SHA256 unchanged; all 431 recorded file hashes match. PROJECT_MISSION.md is also hash-identical. See GBR_FINAL_VERIFICATION.json. No GBR wiring, build, package restore or repository mutation was performed.
