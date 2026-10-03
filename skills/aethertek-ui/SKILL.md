---
name: aethertek-ui
description: Build and evolve AethertekUI or redesign FFXIV-related C# ImGui applications from approved mockups, with standard nine-language localization and relative colour themes. Use for library components, layouts, the showcase, consumer integration, and UX deployment readiness; use the optional Dalamud adapter only in Dalamud hosts.
---

# AethertekUI adoption

Resolve this repository from the skill location (two directories above this
folder), currently `Z:\aethertekUI`. Read its `AGENTS.md` and the consumer's
instructions. This skill guides implementation; it does not authorize edits to
an unselected plugin, client installation, live-game testing or publication.
AutoParty is skipped in the foundation delivery.

## Projects and references

- `src/AethertekUI/AethertekUI.csproj`: core controls, drawing, data helpers,
  cached dark themes and OKLCH palettes; namespace `AethertekUI`. Existing
  component names remain `MaterialTheme`, `MaterialButton`, etc.
- `src/AethertekUI.Dalamud/AethertekUI.Dalamud.csproj`: optional
  `MaterialWindowHost` and host scaling; namespace `AethertekUI.Dalamud`.
- `src/AethertekUI.Demo/`: AethertekUI Showcase, command `/aethertekui`;
  eight native-rendered mock-data pages, including the sizing lab.
- `samples/ConsumerContract/ConsumerUi.cs`: compiled independent example of
  consumer-owned windows, configuration, semantic colors and overlays.

Prefer local `ProjectReference` entries to core, plus the adapter when needed.
For a sibling consumer repository, the core path is typically
`../aethertekUI/src/AethertekUI/AethertekUI.csproj`; resolve the actual path
relative to the consuming csproj. Do not copy library source into plugins.

Core targets .NET 10 / Windows x64 and references `Dalamud.Bindings.ImGui`.
Compatible FFXIV-related C# ImGui applications can use it when they supply that
binding and its matching native runtime/context. Another ImGui binding is not a
drop-in replacement. Reserve the adapter for Dalamud hosts; core does not own
Dalamud services or a renderer.

## Window and control scopes

Cache a `MaterialTheme` or `MaterialThemeController` outside Draw. Rebuild
palettes on seed changes; density, opacity and motion settings reuse them.
Place `MaterialTheme.Push` around the complete native Begin/End lifecycle,
including End when Begin returns false. For Dalamud WindowSystem, wrap
`WindowSystem.Draw` using `MaterialWindowHost.Draw` or `BeginFrame`; a theme
started inside `Window.Draw` is too late for outer-window styling.

`MaterialStyleMode.Compatible` is the default and preserves host layout.
Use `Material` explicitly for a new design that needs full library geometry,
or `ColorsOnly` / `BeginFrame(applyMetrics: false)` for retained overlay geometry.
Scopes are UI-thread/context-bound, lexical and LIFO. Do not copy or double
dispose them, or switch contexts before disposal. Preserve balanced native
Begin/End, popup, table, child, font and ID scopes.

Use Compact density for compact desktop designs, and
`MaterialControls.Push(MaterialControlContext.Toolbar)` or `Dense` for local
rows. `MaterialTable.PushControls()` aligns native and custom table editors.
Enter a control scope after selecting its font. Pass host global scale once.
Explicit component sizes are logical units; measured metrics, native ImGui
coordinates and `MaterialActionTile.Measure` results are already pixels.
Zero/negative sizing conventions vary by component; consult
[adaptive sizing](../../docs/ADAPTIVE_SIZING.md) before choosing fixed widths.

Retain native ImGui editing, focus, navigation, clipping, sorting and disabled
behavior. Consumers configure input/navigation and route any controller events.

## Identity, localization and resources

Keep IDs independent of translated labels. Use persistent model keys in
`PushID`, explicit component `id` arguments, `MaterialOption` keys and reordered
`MaterialSegment.Key` values. Index segment IDs suit fixed lists only. Raw
native controls can separate translated text and identity using `###stable-id`.
Preserve existing internal ImGui IDs during refactors and rebranding.

Measure translated labels using the active host font. Budget pane, column,
button and popup widths for longer translations; use wrapping tiles where
appropriate and check clipped compact labels. Provide tooltips when necessary.
There is no universal fixed width that fits every language. The host supplies
and manages fonts with the required CJK glyphs; the library does not load fonts
or guarantee localization coverage. Test actual host fonts and display scales
when an integration is selected.

Consumers own plugin subscriptions, commands, windows, disposal, configuration
storage, game actions, sorting/filter rules and invalidation. Keep domain status
colors explicit; an accent change must not redefine game status meaning. Fonts
and textures are borrowed and must stay alive through render submission. Do not
dispose host resources from library helpers.

Ship `AethertekUI.dll` beside the plugin and `AethertekUI.Dalamud.dll` when used.
Keep host-owned `Dalamud.dll`, `Dalamud.Bindings.ImGui.dll` and `cimgui.dll` out
of plugin distributions. Dalamud SDK hosts resolve their normal references;
Microsoft.NET.Sdk hosts must resolve the same installed binding/distribution.
Package identities are `AethertekUI` and `AethertekUI.Dalamud`, version 0.3.0;
local package generation is separate from normal build verification.

## Evolve through approved designs

Before implementation, measure the approved reference's content envelope,
region bounds and gaps, typography roles/weights, representative colours, artwork
and state assumptions. Keep those inputs in the consumer's existing presentation
constants and affected documentation. Map every selected window and popup to its
approved design or explicitly retained layout. New mockups are not implied for
retained windows. Native chrome, added selectors, real data and agreed semantic
changes are specific differences; do not use blanket masks to hide discrepancies.

Implement the selected approved mockup faithfully: layout, proportions,
density, hierarchy, interaction and status semantics. When it exposes a missing
component or layout capability, improve AethertekUI and demonstrate the addition
in the showcase. Do not reshape the approved design to fit the current controls.
Retain the MD3-inspired dark/OKLCH foundation while adding capabilities required
by actual integrations. Keep game behavior in the consumer.

## Standard UX redesign and deployment

For every David-approved AethertekUI UX redesign, include the nine languages and
whole-theme colour selection as standard features unless David explicitly changes
that scope. Carry them into any separately authorized deployment; they are not
Botology-only options. Keep implementation in the selected consumer and honor
explicit limits on builds, tests, packaging and client operations.

Place a compact colour swatch and language selector at the top of the main UI,
mirror them in existing Settings, and share both selections across every window
and popup. Use `MaterialAppearanceSelector` with teal/blue/pink presets and a
custom RGB picker. Use these stable codes and native dropdown names:

| Code | Dropdown name |
| --- | --- |
| `en` | English |
| `de` | Deutsch |
| `fr` | Français |
| `es` | Español |
| `it` | Italiano |
| `ru` | Русский |
| `ja` | 日本語 |
| `ko` | 한국어 |
| `zh-Hans` | 简体中文 |

Translate all consumer-authored windows, headings, controls, tooltips and popup
text using keyed resources embedded in the assembly and native .NET resource
loading. Cache the selected resource set. Keep key coverage equal across all
nine languages; English fallback must not conceal unfinished translations.
Preserve plugin names, external
catalog content, command tokens and logs. Format UI numbers and dates using the
selected locale without changing process-wide culture.

Reuse the consumer's existing configuration and save path. Prefer `UiLanguage`
(default `en`) and `UiAccentRgb` (default the approved reference accent), or reuse
equivalent preferences already present. Old configurations receive missing
defaults without resetting other values or changing their version. Commit
custom RGB on edit completion, not every draw; apply resource changes at the
next frame boundary. Consumers own translations, preferences, fonts and textures;
do not add a shared settings service.

Use relative OKLCH colours for the whole theme: backgrounds, panels, fields,
table surfaces, borders, foregrounds, gradients and neutral badges. Preserve
each measured role's lightness and relative hue/chroma; gamut-map the result and
choose contrasting foregrounds for filled accent controls. Use the existing
`MaterialColor` conversions, `GamutMap` and `Contrast`; OKLCH hue is in radians.
Keep the approved default colours exact. Grey seeds produce neutral decorative
roles; dark and pale seeds must retain a usable light/dark hierarchy. Keep assessment, ignored,
unavailable, update and DTR status meanings and external brand colours explicit.
Changing only primary controls while leaving decorative slate colours fixed is
incomplete. Build and cache the palette on preference changes, outside Draw.

Fonts must cover the selected resource set and all nine native dropdown names,
including CJK names when English is selected. Prioritize the selected locale's
CJK glyphs and retain Latin/Cyrillic coverage. In Dalamud, use managed atlas
handles and explicitly merge the host's `DalamudAsset.NotoSansCjkRegular` when
needed (TTC faces: Japanese 0, Simplified Chinese 2, Korean 3). Do not pass neutral
codes such as `ja`, `ko` or `zh-Hans` to `AttachWindowsDefaultFont`: unsupported
cultures can silently add nothing. Check actual managed-handle readiness, load
errors and glyph coverage after atlas rebuilds. Dispose consumer-owned handles;
never distribute Windows fonts. Temporary host fonts cannot qualify for visual
acceptance.

Before declaring the redesign ready for deployment, verify embedded resources,
library payloads, preference persistence, all-language layout/glyph coverage and
theme coverage across the selected windows and popups. Include presets, a custom
colour, grey/black/white selections and independent semantic status colours in
the selected verification route. Report deferred checks explicitly; follow the
separate functional, diagnostic and game acceptance evidence below. Deployment
readiness does not authorize installing, packaging or publishing anything.

Botology's first adoption establishes these practices:

- `MaterialColorScheme` roles can be assigned during initialization and stay
  immutable afterward. `MaterialTheme(colors, density)` accepts exact sampled
  roles. Apply the relative theme requirements above. A consumer that already
  owns a cached theme can wrap `WindowSystem.Draw` directly with
  `MaterialTheme.Push(theme, ImGuiHelpers.GlobalScale, mode)`; the optional
  adapter is not required for that route.
- For compact rectangular toolbars and status cells, use
  `MaterialButton.Draw(id, label, cornerRadius: 4, ...)` and
  `MaterialStatus.Badge(label, cornerRadius: 4, ...)`. Radius and explicit sizes
  use logical units; the original overloads retain pill shapes and native IDs.
  Generic Document, Database, Refresh, Chart, ExternalLink and Download icons
  use native meshes without font or texture ownership.
- `MaterialControlAppearance` supplies fill, foreground, border and logical
  radius to button, badge, search and combo overloads. Badges accept logical
  dimensions; switches accept an explicit logical track size. Rounded gradient
  `MaterialCanvas.Surface` keeps pixel coordinates/radius. Explicit
  `MaterialControls.Push(metrics)` values are already scaled pixels;
  parameterless nested scopes inherit them, explicit contexts remeasure, and
  theme/control scope disposal restores the prior state. Showcase and compiled
  consumer examples accompany demonstrated reusable gaps.
- Reserve footer height before sizing a scrolling table. Preserve optional
  columns and allow horizontal scrolling instead of resetting saved preferences.
  Assign native column user IDs from stable column identities, consume native
  sorting specs, and include a persistent row-key tie-breaker. Sort captured
  versions numerically and keep unavailable metadata sortable.
- A mockup's sample text is not a data contract. Bind source badges to actual
  provenance, installed cells to captured versions, and update cells to
  availability unless the host supplies a target version. Keep assessment
  colours with warnings and DTR controls with actual visibility state.
- A sibling `ProjectReference` supports local development, but a consumer-only
  checkout cannot build it. Identify library provisioning as a release
  prerequisite; do not change publication or CI as part of an approved local
  UI pass. Use `-p:Use_DalamudPackager=false` for Dalamud SDK build checks when
  release packaging is outside the selected scope.
- Repair existing consumer build launchers to enter the library's required
  environment, select its pinned SDK and build the plugin csproj directly.
  Propagate the build exit code; keep test execution in the explicitly selected
  verification route. For AethertekUI, enter `eng/Enter-RepoEnv.ps1` before
  building project references so `DalamudLibPath` is supplied. With a lockfile
  containing DalamudPackager, restore normally and then build with `--no-restore`
  and `-p:Use_DalamudPackager=false` to avoid removing it from the lockfile.
- Offline fonts may differ from the live managed atlas and conceal a broken
  merge. A clean build or CPU render does not prove host font readiness. Give
  loading/error windows containing wrapped text an explicit width every frame,
  including after opening; retain their native IDs and balanced Begin/End.

Read [component API](../../docs/COMPONENT_API.md) for control signatures and
[integration guide](../../docs/INTEGRATION_GUIDE.md) for host/data boundaries.
Update this skill as real integrations establish better practices; record
demonstrated guidance, not speculative infrastructure.

## Local verification

Use installed SDK 10.0.201 and the repository-local environment:

```powershell
Set-Location 'Z:\aethertekUI'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\Build.ps1 -Previews
```

The child process dot-sources `Enter-RepoEnv.ps1` before tooling and leaves
machine/user execution policy unchanged. For direct toolchain commands, enter
that environment in the calling process first. The normal route builds Debug/x64,
runs existing library tests and checks the
independent consumer. Inspect `artifacts/previews/page-0.png` through
`page-7.png` as diagnostic CPU renders of actual native ImGui meshes. They use
a default font and fixed background; GPU compositing and game/CJK fonts require
later host acceptance. Historical mission/GBR reports do not prove current
integration compatibility or grant permission to run legacy tooling.

Keep three kinds of evidence separate: functional native interaction checks,
diagnostic visual renders, and David's game screenshot acceptance. Extend the
existing renderer/runners with supplied snapshots; do not construct the real
plugin or start game services in offline tests. Fix canvas, scale, data, locale,
fonts, timestamps, pointer and motion. Missing baselines/fonts fail. Compare
RGBA dimensions and channels with tolerance 2; emit expected/actual/difference
previews under ignored artifacts. Normal runs never overwrite baselines.

Compare actual rendered bounds, typography, colours and artwork with the
approved design before initializing baselines (Botology: ±2 logical pixels and
±6 RGB for representative surfaces; glyph antialiasing/raster grain excluded).
Generate baseline candidates separately, inspect them, resolve discrepancies,
then deliberately adopt them through a reviewable Git change. Prove the
comparator catches shifted regions, wrong fills and missing elements. Baselines
prove regression stability, not initial design fidelity or GPU verification.
Cover selected windows/popups, all locales, scale/size limits, accents and
functional states. Open interactive popups through their native controls and
check that they remain visible with usable bounds after the opening frame.
Auto-sized popups containing wrapped content may need an explicit width each
frame; an appearing-only width can collapse later. Builds, test counts and
self-generated baselines alone do not establish completion. Deliver as
"locally verified; game visual acceptance
pending" until David supplies and accepts the game screenshot; never auto-accept
or operate clients without explicit authorization.
