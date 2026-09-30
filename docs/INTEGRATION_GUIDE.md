# Integration kit v0.2

This kit can be referenced and wired into a full plugin. No GBR files have been changed. The compiled contract in samples/ConsumerContract/ConsumerUi.cs demonstrates the host boundary without game services; Build.ps1 compiles it from ProjectReferences and again from the two freshly built local packages.

## Assemblies and installation contract

CombatReborn.MaterialUI.dll supplies the design system and controls. CombatReborn.MaterialUI.Dalamud.dll is an optional adapter that references core and installed Dalamud. Both are ordinary class libraries. Use ProjectReferences during development or packages CombatReborn.MaterialUI and CombatReborn.MaterialUI.Dalamud version 0.2.0 from artifacts/. Copy their DLLs beside the consuming plugin. Do not bundle Dalamud, its ImGui binding, or cimgui from this repository.

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

## Data and identity

Keep models, sorting rules and persistence in the plugin. Cache MaterialOptions<T> for combos, with unique stable string keys; Replace on model revision and SetQuery when filtering changes. Labels are display text. Nullable option values can represent inherited settings; MaterialOverride.Switch supplies Inherit/On/Off for bool?.

For native tables, enter MaterialTable.PushStyle once. Keep existing column IDs, flags, sorting specs, visibility, widths, context menus and game icons. MaterialDataView<T>.Rebuild performs filtering/sorting only on explicit invalidation. Comparers should include a unique-key tie-breaker. MaterialSelectionSet<TKey> stores domain keys across filtering and sorting; call Prune only when objects are actually deleted, not when hidden by filters.

MaterialClipper wraps the native clipper for uniformly spaced rows and belongs inside the table/child scope. Include a selected/focused row before Step when it must be submitted even offscreen. Variable-height recipe trees and timeline charts retain their existing traversal. Do not introduce virtualization into a table that already owns a clipper without removing the duplicate owner.

Segment keys are optional for compatibility: provide Key when options can reorder. Index IDs remain valid for fixed option lists. Never use visible list position as a persistent domain identity.

## Domain preservation and input

MaterialSemantic.Text/Colors and table semantic backgrounds accept consumer colors unchanged. Never map available/upcoming/tug/warning meanings to Primary merely because their existing hue looks similar. Keep a text or icon indicator alongside status color.

MaterialCanvas.Image/ImageButton take borrowed ImTextureID values. The caller must keep the texture alive through GPU submission. GlyphButton takes a borrowed font pointer and glyph string, allowing the caller's normal FontAwesome source. The library never creates, disposes or downloads those resources.

All controls use native ImGui activation/editing. The library never enables navigation flags or consumes a global gamepad stream. Keep GBR's controller adapter and blocking registrations. MaterialNavigationMath.NextEnabled is a pure helper for explicit host-routed bumper/selection events. Native tree, tabs, context menus, drag/drop and modal lifecycles remain usable. DragDrop payloads must be small unmanaged IDs with a unique ASCII type name; receiver size is checked and delivery occurs only on release. Managed objects and transient pointers are not supported payloads.

## Coverage and acceptance

See FULL_GBR_COVERAGE.md plus GBR_UI_REQUIREMENTS.md for the whole source survey, and GBR_INTEGRATION_PLAN.md for ordering. Both list editor and 10,000-row catalog workbenches render in the actual native bindings. Tests exercise native mouse/keyboard, searchable combos, drag/drop, search focus return, numeric bounds and style restoration.

Compiled consumer compatibility is verified. End-to-end GBR compatibility cannot be verified without the later wiring and game session. Real fonts, game-addon coordinates, controller routing, existing domain helpers and plugin unload/reload need that acceptance pass. These retained consumer responsibilities are not replaced with mock game implementations.
