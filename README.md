# CombatReborn.MaterialUI
A standalone, dark-first, Material Design 3-inspired immediate-mode C# library for Dalamud API 15. Compact desktop controls, cached seed-derived themes, scoped styles and native data tables. This is an independent prototype, with no dependency on GBR, ElliLib, RSR or plugin-specific game code.

**Status v0.3:** compiled library, optional Dalamud host adapter, eight-page interactive demo, and an independent consumer built using both project references and local packages. Coverage now includes whole-plugin settings, list editors, specialized overlay escape hatches, and large tables. This revision has not been tested in the game. Adaptive sizing and host-layout compatibility are described in the [sizing guide](docs/ADAPTIVE_SIZING.md). See the [integration guide](docs/INTEGRATION_GUIDE.md) and [whole-GBR coverage](docs/FULL_GBR_COVERAGE.md).

## Repository
```text
CombatReborn.MaterialUI.sln
PROJECT_MISSION.md                 authoritative, preserved task
AGENTS.md                          filesystem and GBR read-only rules
src/
  CombatReborn.MaterialUI/
    Core/                          colors, palettes, roles, metrics, motion, scopes
    Drawing/                       native hit behavior and vector icons
    Components/                    composable control families
    Data/                          cached options/views and key selection
  CombatReborn.MaterialUI.Dalamud/  optional WindowSystem adapter
  CombatReborn.MaterialUI.Demo/
    Plugin.cs                      /crmaterial and UI lifecycle
    Showcase*.cs                   eight interactive pages and floating overlay
samples/ConsumerContract/          independent compiled integration example
tests/
  CombatReborn.MaterialUI.Tests/    pure logic and native input/render tests
eng/                               local environment, build and verification scripts
docs/                              audit, architecture, API, integration plan, limitations
artifacts/                         ignored local builds, package, logs and previews
.cache/                            ignored repository-local tool caches
```

## Build
Prerequisites: Windows x64, .NET SDK 10.0.401 (latest patch allowed), and installed Dalamud API 15 assemblies. GBR's local csproj was verified as Dalamud.NET.Sdk/15.0.0 and net10.0-windows. The library uses Microsoft.NET.Sdk; only the demo uses the Dalamud SDK.

```powershell
Set-Location 'C:\Users\pscho\OneDrive\Documents\GitRepos\CombatReborn.MaterialUI'
pwsh -NoProfile -File .\eng\Build.ps1 -Previews
```

The script sets DOTNET_CLI_HOME, NuGet package/HTTP/plugin caches, TEMP/TMP, APPDATA and LOCALAPPDATA under .cache before tooling runs. It disables first-run/telemetry/workload notices and shared build servers. No SDK/plugin installation or remote publication occurs.
It uses an existing DALAMUD_HOME override or the usual installed XIVLauncher dev directory. To select another installed API 15 distribution:

```powershell
$env:DalamudLibPath = 'C:\path\to\installed\Dalamud\dev'
. .\eng\Enter-RepoEnv.ps1
.\eng\Build.ps1 -Previews
```

For one project/check:
```powershell
. .\eng\Enter-RepoEnv.ps1
dotnet build CombatReborn.MaterialUI.sln -c Debug --disable-build-servers -m:1
dotnet test tests/CombatReborn.MaterialUI.Tests -c Debug --disable-build-servers
.\eng\Verify-Gbr.ps1
```

Always enter the local environment first, including before restore/pack/new tooling. NuGet.Config clears inherited sources; dependency lock files are committed. NuGet vulnerability auditing is disabled for this isolated prototype build; no production security review is claimed.

## Outputs
All paths below are relative to this repository:
- Reusable DLL: artifacts/lib/Release/CombatReborn.MaterialUI.dll
- Debug DLL: artifacts/lib/Debug/CombatReborn.MaterialUI.dll
- Demo: artifacts/demo/Release/CombatReborn.MaterialUI.Demo.dll (with manifest, library and PDBs beside it)
- Optional adapter DLL: artifacts/lib/Release/CombatReborn.MaterialUI.Dalamud.dll
- Local packages: artifacts/CombatReborn.MaterialUI.0.3.0.nupkg and artifacts/CombatReborn.MaterialUI.Dalamud.0.3.0.nupkg
- Package consumer proof: artifacts/consumer-package/ConsumerContract.dll
- Normal library output: src/CombatReborn.MaterialUI/bin/{Debug|Release}/net10.0-windows/
- Normal demo output: src/CombatReborn.MaterialUI.Demo/bin/{Debug|Release}/
- Build/test logs: artifacts/build-*.log, artifacts/test-*.log, artifacts/TestResults/
- Visual previews: artifacts/previews/page-0.png through page-7.png, settings-150-percent.png, integration density/scale previews

## Use from a plugin
A normal ProjectReference is the preferred local development arrangement:
```xml
<ItemGroup>
  <ProjectReference Include="../CombatReborn.MaterialUI/src/CombatReborn.MaterialUI/CombatReborn.MaterialUI.csproj" />
</ItemGroup>
```
Adjust the path for the consumer. Alternatively reference the ordinary DLL with Copy Local enabled, or use the local NuGet package. The package contains the class library, not copies of Dalamud assemblies. A consuming Dalamud.NET.Sdk/15.0.0 project supplies the normal bindings. Microsoft.NET.Sdk consumers must also resolve Dalamud.Bindings.ImGui from the same installed distribution.

Cache the theme and options outside Draw:
```csharp
using System.Numerics;
using CombatReborn.MaterialUI;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

private readonly MaterialTheme theme = new(new Vector3(.36f, .72f, .46f));
private static readonly MaterialSegment<int>[] Modes =
    [new(0, "Auto"), new(1, "Manual"), new(2, "Off")];
private int mode;
private bool enabled;
private string search = "";

public void Draw()
{
    using var scope = MaterialTheme.Push(theme, ImGuiHelpers.GlobalScale);
    // If owning raw ImGui windows, enter the scope BEFORE Begin and keep it through End.
    if (ImGui.Begin("Example"))
    {
        MaterialSwitch.Draw("enabled", "Enabled", ref enabled);
        MaterialSegmented.Draw<int>("mode", Modes, ref mode);
        MaterialTextField.SearchField("search", ref search);
        if (MaterialButton.FilledButton("start", "Start", leading: MaterialIcon.Play))
        {
            // Consumer action.
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Consumer-owned tooltip");
    }
    ImGui.End();
}
```
If using Dalamud WindowSystem, the consumer owns its lifecycle. Wrap WindowSystem.Draw when outer window styling is desired; a theme scope inside a Window.Draw method styles content after the window has already begun.

Pass logical (unscaled) explicit sizes to components. Zero means automatic; negative button/field width fills available space. Metrics exposed by MaterialTheme.Metrics are already scaled pixels. Child Surface sizes preserve native zero/negative conventions. Do not scale twice. Cache themes; regenerate only when the seed changes. SurfaceOpacity and Density can change without generating palettes.

## Components
Filled, filled-tonal, outlined and text buttons; vector icon buttons; generic segmented controls with disabled options; animated switch; checkbox/radio helpers; surface/card scopes and clickable cards; filter/action chips; search with clear and native text editing; filled text fields with read-only/error/icon support; expanded/compact navigation rail and items; list items; native secondary tabs; action tiles; badges; rounded linear progress; modal scope; section/window headers; native table style and row-state helpers.

Core generates primary/secondary/tertiary/neutral/error tonal palettes in OKLCH with gamut chroma reduction, then caches 33 semantic roles. Alpha is independent from RGB. Motion state uses stable IDs and bounded pruning. This is MD3-inspired, not exact HCT.

## Demo
/crmaterial opens:
1. Component gallery, accent/opacity/density, native secondary tabs, disabled states and dialog.
2. Compact automation preview and a separate floating overlay with shared Auto/Manual/Off state.
3. Dense mock gathering table: search/filter, native sorting/resizing and selected/hover rows. Semantic status colors remain fixed when the accent changes.
4. Action dashboard: mock Start/Pause/Stop, Move/Return/Repair, progress/status.
5. Realistic settings: switches, route combo, segmented mode, slider, preset field and nested dependency card.
6. List editor: resizable panels, tree/folder drag-and-drop, context menu, job bit filters, searchable 500-option combo, quantity/delay editors, inherited setting and multiline import.
7. Large catalog: 10,000 cached mock rows, native sorting/resizing/reordering/hiding, clipped rendering, key-based multiselection and semantic statuses.
8. Sizing lab: Regular/Toolbar/Dense contexts, aligned native and custom controls, compact table editors, independent popup widths and wrapping action tiles.

## Manually load later
No plugin was installed by this task.
1. Build first. Leave the entire artifacts/demo/Release directory together, including CombatReborn.MaterialUI.dll and the demo JSON manifest.
2. In a compatible API 15 Dalamud installation, open developer plugin settings (commonly /xlsettings → Experimental → Dev Plugin Locations).
3. Add the full path to artifacts/demo/Release/CombatReborn.MaterialUI.Demo.dll as a local development plugin location; save, then use the Dev Tools/development plugin list to scan/reload and enable it. Labels can vary by Dalamud build.
4. Run /crmaterial. Do not add the library DLL itself as a plugin.
5. Disable/unload the demo before replacing locked DLLs or rebuilding. Remove the dev plugin entry when done.

These are later manual actions, not steps performed here. Follow docs/PROTOTYPE_SMOKE_TEST.md for in-game acceptance.
The dev-plugin location workflow is described in the [official Dalamud getting-started FAQ](https://dalamud.dev/faq/getting-started/).

## Evidence and next steps
- [UI audit](docs/GBR_UI_REQUIREMENTS.md) and complete per-file [survey](docs/GBR_GUI_SURVEY.txt).
- [Architecture](docs/ARCHITECTURE.md), [design system](docs/DESIGN_SYSTEM.md), [API guide](docs/COMPONENT_API.md).
- Original prototype [read-only baseline](docs/GBR_BASELINE.md) and [comparison](docs/GBR_FINAL_VERIFICATION.json); current library pass [validation](docs/LIBRARY_SIZING_VALIDATION.md).
- [GBR integration plan](docs/GBR_INTEGRATION_PLAN.md) — planning reference predating the separately authorized UITest wiring.
- [Known limitations](docs/KNOWN_LIMITATIONS.md) and [smoke test](docs/PROTOTYPE_SMOKE_TEST.md).

The integration kit covers the audited common patterns across GBR, including non-Gui overlays. Native/domain-specific rendering remains composable under the theme. The [compiled consumer example](samples/ConsumerContract/ConsumerUi.cs) demonstrates adoption without game services. The v0.3 library pass preserves the existing GBR working tree and does not deploy this revision into GBR; evaluating its remaining application layout choices is a separate pass.
