# Integration kit v0.3

This kit can be referenced and wired into a full plugin. This foundation delivery does not modify consuming plugins. See [adaptive sizing](ADAPTIVE_SIZING.md) before upgrading an existing consumer. The compiled contract in samples/ConsumerContract/ConsumerUi.cs demonstrates the host boundary without game services; Build.ps1 builds it through local ProjectReferences and verifies renamed assembly identities and dependencies. Package generation is a separate, unperformed release step.

## Assemblies and installation contract

AethertekUI.dll supplies the design system and controls. AethertekUI.Dalamud.dll is an optional adapter that references core and installed Dalamud. Both are ordinary class libraries. Use ProjectReferences during development. The package identities are AethertekUI and AethertekUI.Dalamud version 0.3.0; the normal build does not generate packages. Copy their DLLs beside the consuming plugin. Do not bundle Dalamud, its ImGui binding, or cimgui from this repository.

The packages target net10.0-windows (NuGet canonical platform suffix windows7.0). API 15 / x64 is the validated host. A Dalamud SDK plugin resolves host references normally. A Microsoft.NET.Sdk consumer must resolve the same installed Dalamud and ImGui assemblies; the sample shows this explicitly and enables CopyLocalLockFileAssemblies. No installation, subscription, or configuration persistence happens inside either library.

## Host entry point

Create MaterialWindowHost once, with a MaterialThemeSettings record restored by the consumer. Replace the existing WindowSystem.Draw call with material.Draw(windowSystem) when integration is authorized. Keep the existing subscription, disposal, window registrations, focus tracking, command handlers, controller blocking, draw conditions, and config save policy.

For a mixed draw path:
```csharp
using var frame = material.BeginFrame();
windowSystem.Draw();
```
The scope must surround Begin/End, not just a Window.Draw method. Existing PreDraw/PostDraw overrides run inside it and retain their local behavior. Opt-in per-window styling is also possible using correctly paired PreDraw/PostDraw scopes owned by the consumer; do not push another global theme after Begin and expect its background to change.

For a game-anchored overlay, material.BeginFrame(applyMetrics: false) applies colors while retaining the host's geometry styles. Its Material metrics are still available if deliberately used by a new component; it does not force existing domain DrawList code to adopt those metrics.

Call Controller.Apply(newSettings) on configuration change. It validates before mutation, skips identical values and regenerates palettes only when seed RGB changes. Density, alpha and reduced motion reuse the cached palette. Keep one controller/theme per ImGui context and do not switch contexts or threads inside scopes.

## Native titlebar shortcuts

Dalamud consumers can register fixed `Window.TitleBarButtons` on their existing
owner; this needs no additional AethertekUI registration service. The compiled
ConsumerContract example shares its existing Enabled handler between a retained
body switch and a title shortcut. Keep callbacks and translated tooltips in the
consumer, rechecking current action gates at the click, including while collapsed.
Retain main body controls and move only explicitly selected mini shortcuts.

Preserve registration/priority order, native close/collapse/dragging and original
window identities. Icon offsets are logical units scaled by Dalamud. Custom title
painting must reserve the actual native icon/font/spacing widths and host controls;
it must not paint over the button cells. Popup actions retain their original body
ID scopes. See the usage skill's native-titlebar section for verification limits:
descriptor callback checks, native pointer routing and managed-host/game icon
rendering are separate evidence.

Keep the motion lifecycle on owners with custom title buttons. Dalamud reserves
font size plus inner spacing beside Close; larger native horizontal frame padding
can overlap the first custom hit region. `MaterialWindowMotion.Prepare` limits
that padding for native Begin with a one-pixel rounding allowance. `DrawChrome`
restores the original horizontal padding before body content; `Restore` also
restores it after collapsed frames. Vertical padding, IDs and caller styles stay
intact. Verify the right edge of the first custom button at fractional scales,
as well as its center and the collapsed route.

## Data and identity

Keep models, sorting rules and persistence in the plugin. Cache MaterialOptions<T> for combos, with unique stable string keys; Replace on model revision and SetQuery when filtering changes. Labels are display text. Nullable option values can represent inherited settings; MaterialOverride.Switch supplies Inherit/On/Off for bool?.

For editable native tables, enter MaterialTable.PushControls once to align Dense native and Material editors. Use PushStyle when retaining existing editor dimensions. Keep existing column IDs, flags, sorting specs, visibility, widths, context menus and game icons. MaterialDataView<T>.Rebuild performs filtering/sorting only on explicit invalidation. Comparers should include a unique-key tie-breaker. MaterialSelectionSet<TKey> stores domain keys across filtering and sorting; call Prune only when objects are actually deleted, not when hidden by filters.

For the approved ADS tight-row layout, use `MaterialTable.PushTightRows()` after
the existing control sizing scope and before native `BeginTable`. Retain it
through `EndTable`. It sets cell padding to `(scale, 0)` while preserving editor
frame padding and font height. Use automatic row height so the tallest editor
sets the row boundary; keep inline actions within that row. Changing only
horizontal padding does not remove vertical gaps. The Sizing lab shows five
consecutive editable rows with this scope.

MaterialClipper wraps the native clipper for uniformly spaced rows and belongs inside the table/child scope. Include a selected/focused row before Step when it must be submitted even offscreen. Variable-height recipe trees and timeline charts retain their existing traversal. Do not introduce virtualization into a table that already owns a clipper without removing the duplicate owner.

Segment keys are optional for compatibility: provide Key when options can reorder. Index IDs remain valid for fixed option lists. Never use visible list position as a persistent domain identity.

## Domain preservation and input

MaterialSemantic.Text/Colors and table semantic backgrounds accept consumer colors unchanged. Never map available/upcoming/tug/warning meanings to Primary merely because their existing hue looks similar. Keep a text or icon indicator alongside status color.

MaterialCanvas.Image/ImageButton take borrowed ImTextureID values. The caller must keep the texture alive through GPU submission. GlyphButton takes a borrowed font pointer and glyph string, allowing the caller's normal FontAwesome source. The library never creates, disposes or downloads those resources.

All controls use native ImGui activation/editing. The library never enables navigation flags or consumes a global gamepad stream. Keep the consumer's controller adapter and blocking registrations. MaterialNavigationMath.NextEnabled is a pure helper for explicit host-routed bumper/selection events. Native tree, tabs, context menus, drag/drop and modal lifecycles remain usable. DragDrop payloads must be small unmanaged IDs with a unique ASCII type name; receiver size is checked and delivery occurs only on release. Managed objects and transient pointers are not supported payloads.

## Coverage and acceptance

FULL_GBR_COVERAGE.md, GBR_UI_REQUIREMENTS.md and GBR_INTEGRATION_PLAN.md are historical source-survey and migration context from the supplied archive, not current authorizations or integration results. Both list editor and 10,000-row catalog workbenches render in the actual native bindings. Tests exercise native mouse/keyboard, searchable combos, drag/drop, search focus return, numeric bounds and style restoration.

Compiled consumer compatibility is verified. End-to-end plugin compatibility requires later selected wiring and host acceptance. Real fonts, game-addon coordinates, controller routing, existing domain helpers and plugin unload/reload need that acceptance pass. These retained consumer responsibilities are not replaced with mock game implementations.

## Approved designs and localization

Measure geometry, text weights/sizes, sampled colours, artwork and state before
adoption. Exact roles use initialized `MaterialColorScheme` and
`MaterialTheme(colors)`; appearance/metric overloads support measured controls.
Use the standard appearance selector with consumer-owned settings/translations,
borrowed fonts and next-frame updates. Keep all nine native language names and
check required glyphs after host atlas rebuilds. Stable IDs include existing
English-derived helper IDs, even when visible text is translated.

Check design fidelity before adopting deterministic PNG baselines. Candidate
mode (`AETHERTEKUI_CANDIDATE_DIR`) writes only a separate ignored artifact folder;
review candidates before copying them into the test projects' `Baselines`.
Normal tests fail on missing images and compare dimensions and RGBA channels
with tolerance 2. Diagnostic CPU frames cannot establish GPU/game acceptance.
The selected consumer's final screenshot remains mcvaxius's acceptance boundary.

Follow the selected approved mockup faithfully. Add a required missing component
or layout capability to AethertekUI and demonstrate it in the showcase rather
than changing the design to fit the existing library. Update
[the usage skill](../skills/aethertek-ui/SKILL.md) when actual integrations establish
better practices. No plugin adoption is performed by the foundation delivery;
AutoParty remains skipped.

IDs and option/segment keys must stay independent of translated labels. Measure
labels with the active host font and allow for longer text in columns, panes,
buttons and popup menus. Hosts supply and manage CJK-capable fonts. Compact labels
may clip; actual localization and font acceptance belongs to each integration.
Core can serve compatible FFXIV-related C# ImGui applications that supply the
same managed binding and native context/runtime. The Dalamud adapter requires a
Dalamud host and does not apply to other renderers or ImGui bindings.
