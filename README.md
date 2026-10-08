# AethertekUI

A local, private C# immediate-mode UI library for FFXIV-related applications:
MD3-inspired dark surfaces, compact desktop controls, cached OKLCH seed-derived
themes, scoped styles and native ImGui interaction. Version remains **0.3.0**.
The core targets .NET 10 / Windows x64 and uses Dalamud.Bindings.ImGui; the
optional adapter integrates with Dalamud API 15 hosts.

`MaterialWindowOpacity` fades an owned window's complete native and custom
content after its final End, with a focus delay and consumer-owned preferences.
The window-motion showcase and independent consumer demonstrate its lifecycle;
see [the component contract](docs/COMPONENT_API.md).

Borrowed plugin images can use aspect-preserving `MaterialCanvas.DrawImage` in
pixel bounds and `MaterialWindowHeader.PaintTitle` after End, before opacity.
The window-motion showcase and compiled consumer demonstrate image ownership
and missing-image spacing. Native font fallback retains shaped glyph validation;
optional Hindi menu probes can leave ordinary locales usable while selected
Hindi failure offers explicit consumer-owned recovery. Host/Wine/game acceptance
remains separate from local native tests.

## Repository

```text
AethertekUI.sln
AGENTS.md                          current repository instructions
skills/aethertek-ui/SKILL.md        adoption and library evolution guidance
src/
  AethertekUI/                      Core, Drawing, Components, Data
  AethertekUI.Dalamud/              optional lifecycle-free WindowSystem adapter
  AethertekUI.Demo/                 AethertekUI Showcase, /aethertekui
samples/ConsumerContract/           independent integration example
tests/AethertekUI.Tests/           pure logic, native input and render tests
eng/                               existing local environment and checks
docs/                              API, sizing, integration and historical context
PROJECT_MISSION.md                 original machine-specific mission, historical
artifacts/                         ignored logs, test results and diagnostic previews
.cache/                            ignored repository-local tool caches
```

## Build and verify

Prerequisites: Windows x64, installed .NET SDK **10.0.201** (latest patch allowed),
and an installed Dalamud API 15 distribution. Core and adapter use
Microsoft.NET.Sdk; only the showcase uses Dalamud.NET.Sdk/15.0.0.

```powershell
Set-Location 'Z:\aethertekUI'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\Build.ps1 -Previews
```

The normal route builds the solution once in Debug/x64, runs the existing tests,
and builds/checks the independent project-reference consumer. The command sets
execution-policy bypass only for that child process, without changing machine or
user policy, and dot-sources `Enter-RepoEnv.ps1` before tooling runs. It does not pack
release artifacts or run legacy GBR tooling. Showcase build packaging is disabled;
its source manifest is copied beside its DLL. The existing environment keeps
DOTNET_CLI_HOME, NuGet caches, TEMP/TMP, APPDATA and LOCALAPPDATA under `.cache/`.

`Enter-RepoEnv.ps1` honors an existing `DalamudLibPath` or `DALAMUD_HOME`, otherwise
uses the installed XIVLauncher dev distribution. To choose another distribution,
set `$env:DalamudLibPath` before entering the environment. Tooling reads installed
host assemblies and does not install or control a client.

For targeted checks, open a child shell with
`powershell.exe -NoProfile -ExecutionPolicy Bypass`, then enter the environment:

```powershell
. .\eng\Enter-RepoEnv.ps1
dotnet build AethertekUI.sln -c Debug -p:Platform=x64 --disable-build-servers -m:1
dotnet test tests/AethertekUI.Tests -c Debug --no-build --no-restore
.\eng\Verify-Consumer.ps1
```

NuGet.Config clears inherited feeds; existing dependency lock files remain
tracked. NuGet auditing is disabled by the inherited build configuration.

## Use in a consumer

Prefer local project references, adjusting these paths relative to the csproj:

```xml
<ItemGroup>
  <ProjectReference Include="../aethertekUI/src/AethertekUI/AethertekUI.csproj" />
  <ProjectReference Include="../aethertekUI/src/AethertekUI.Dalamud/AethertekUI.Dalamud.csproj" />
</ItemGroup>
```

The adapter is optional and reserved for Dalamud hosts. Compatible C# ImGui
applications can use core when they supply the same managed binding and matching
native runtime/context. A different ImGui binding requires an explicit port.

```csharp
using AethertekUI;
using Dalamud.Bindings.ImGui;

// Cache this outside Draw.
private readonly MaterialTheme theme = new(new(.36f, .72f, .46f));
private bool enabled;

public void Draw(float hostScale)
{
    using var scope = MaterialTheme.Push(theme, hostScale, MaterialStyleMode.Material);
    if (ImGui.Begin("Example###example-window"))
        MaterialSwitch.Draw("enabled", "Enabled", ref enabled);
    ImGui.End();
}
```

Wrap the complete Begin/End lifecycle. For WindowSystem, use
`MaterialWindowHost.Draw(windows)` or a `BeginFrame` scope around `windows.Draw()`.
The default Compatible mode preserves host layout; Material applies full geometry.
Consumers own lifecycle, configuration, actions, domain colors, fonts and textures.
The appearance selector supports square swatches and rectangular colour fields
with a Palette mesh, current swatch and chevron, retaining the existing popup IDs.
Explicit control metrics also size that Palette within the field's available space.
Ship the library DLLs beside the plugin while keeping Dalamud assemblies host-owned.

Use stable IDs independent of translated labels, host-provided CJK fonts, and
content-aware sizing for longer translations. Component type names and signatures
remain Material-prefixed, and internal ImGui IDs are preserved. Read the
[usage skill](skills/aethertek-ui/SKILL.md),
[integration guide](docs/INTEGRATION_GUIDE.md),
[adaptive sizing guide](docs/ADAPTIVE_SIZING.md) and
[component API](docs/COMPONENT_API.md).

### Windows-shaped Hindi text

`MaterialTextRenderer` uses Windows DirectWrite and installed Nirmala UI for
Devanagari shaping, measurement and caret geometry. A consumer supplies the
texture uploader; Dalamud hosts can own `MaterialTextHost` from the optional
adapter. Enter its `Push()` scope around the window tree and dispose it outside
the native frame after render submission. No font-atlas mutation or third-party
shaping package is required.

Under that scope, `MaterialText.Measure`, `AddText` and `Text` route Devanagari
through the same shaped layout. Material buttons, action tiles and text/search
fields use that route; other retained native text/control wrappers still need
explicit integration before a consumer advertises Hindi. Non-Hindi text and
password fields retain their native path. Borrowed layouts are valid for the
current frame only.

Retained native wrappers can use `MaterialText.BeginCombo`/`Selectable` and the
text/tooltip helpers while keeping their original labels and IDs.
`MaterialText.TreeNode` and `TableHeader` also retain raw native identities and
arrows; callers own `TreePop` and table row-height reservation.
The public `MaterialShapedInput.SingleLine` bridge accepts that same native ID; reserve its
line height with `MaterialText.PushLineHeight`.
Single-line fields preserve leading Hindi ink within their existing frame padding
at zero scroll, retaining the original text origin and the padded scrolling clip.
Keep existing atlas glyph checks
for other scripts and use `renderer.CheckGlyphs` for authored Devanagari without
uploading textures. `MaterialShapedInput.Multiline` preserves an existing editor's
native ID, both scroll axes, clipboard/undo and CRLF/Unicode source values;
`MaterialInput.Multiline` uses the same route. Declare mixed tab captions with
`MaterialTabs.Begin(id, captions)` before native Begin to reserve their natural
height. Game-native DTR/chat still uses the host's text renderer.

The showcase command `/aethertekui hindi` opens an optional mock editing preview
without changing the existing eight pages. Native ImGui still owns the search
field's ID, focus and undo. Its ImWchar16 buffer uses transient proxy characters
to preserve supplementary source text and clipboard values; these proxies never
enter saved consumer strings. Supplementary keyboard input that the host has
already truncated cannot be reconstructed. Game/GPU and IME interaction require
user-controlled acceptance.

## Showcase and outputs

**AethertekUI Showcase**, command `/aethertekui`, contains eight mock-data pages:
Gallery, Automation, Gathering data, Dashboard, Settings, List editor,
Large catalog and Sizing lab. It includes a floating automation overlay.
No game actions run from the showcase.

Debug outputs relative to this repository:

- Core: `src/AethertekUI/bin/Debug/net10.0-windows/AethertekUI.dll`.
- Adapter: `src/AethertekUI.Dalamud/bin/Debug/net10.0-windows/AethertekUI.Dalamud.dll`.
- Showcase: `src/AethertekUI.Demo/bin/Debug/AethertekUI.Demo.dll` and its JSON manifest.
- Consumer: `samples/ConsumerContract/bin/Debug/net10.0-windows/ConsumerContract.dll`,
  with both renamed library DLLs beside it.
- Logs/results: `artifacts/build-Debug.log`, `artifacts/test-Debug.log`,
  `artifacts/consumer-project.log`, `artifacts/TestResults/`.
- Diagnostic previews: `artifacts/previews/page-0.png` through `page-7.png`,
  plus existing density/scale and narrow-field previews.

Previews are CPU diagnostic renders of actual native ImGui meshes using a default
font and fixed background. They are not game screenshots or GPU acceptance.
Real game/CJK fonts, GPU compositing, controller routing and unload/reload remain
for later selected plugin integrations. See [known limitations](docs/KNOWN_LIMITATIONS.md)
and the [smoke-test guide](docs/PROTOTYPE_SMOKE_TEST.md).

## Evolution and provenance

Implement approved plugin mockups faithfully. When a design exposes a component
or layout gap, improve AethertekUI and demonstrate the addition in the showcase.
Update the skill when real integrations establish better practices. AutoParty
remains skipped; this delivery establishes the foundation without plugin edits,
client installation, live-game tests, release packaging or publication.
Publication remains mcvaxius's responsibility; this repository has no remote.

Imported from the supplied `CombatReborn.MaterialUI.zip`, retaining its two
upstream Git commits and the newer working-tree source. The incoming state is
preserved in commit `1f65827` before rebranding. Original showcase attribution to
**CombatReborn** remains in project/manifest metadata. The implementation is
independent of GBR, ElliLib, RSR and game-specific plugin code. OKLab/OKLCH uses
[Bjorn Ottosson's published matrices](https://bottosson.github.io/posts/oklab/).
No root license was supplied; this conversion adds no licensing terms.

`PROJECT_MISSION.md`, GBR documents and prior verification reports remain intact
as historical context from another machine. Their paths, authorizations and test
claims do not apply to this repository. `eng/*Gbr*`, `eng/GbrUiProbe/` and
`eng/Record-GbrOutputs.targets` are legacy tooling outside the normal route.
`eng/Verify-Artifacts.ps1` retains the old release-artifact workflow separately;
normal checks do not invoke it. No GBR source is needed for the current build.
