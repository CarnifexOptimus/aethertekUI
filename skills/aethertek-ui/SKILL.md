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
IDs include their parent window, table and tree scopes. Moving an established
control into a presentation child or table can change its ID even when its label
is unchanged; preserve the prior ID root when retaining that control.
Appending `###original` to a translated label does not preserve the hash of an
existing plain `original` label. For those controls, pass the original raw label
to the native widget and paint the translation from its bounds, sizing for the
translated text. Retain native arrows, focus and disabled appearance as well.
Account/profile editors keep their viewed
selection separate from the runtime active account/character; browsing settings
must not redirect automation. Key choices by persistent IDs even when displayed
names are obfuscated or duplicate, and retain in-progress text edits across frames.

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

## GitHub Actions provisioning

For every consumer adoption, determine how its CI and release builds will obtain
AethertekUI. A local sibling checkout does not exist on a fresh Actions runner.
Keep CI changes within David's selected scope and obtain his persistent-machinery
choice before adding dependency provisioning or credentials.

When Actions provisioning is selected, use native `actions/checkout` steps with
the consumer and AethertekUI in sibling folders inside `GITHUB_WORKSPACE`.
Checkout paths cannot escape that workspace. Run the build from the consumer
folder and adjust artifact paths to include its checkout folder. Build the plugin
csproj directly; adopting AethertekUI does not imply running either repository's
test projects. Install an SDK compatible with the library's `global.json`
(currently 10.0.201 with `latestPatch` roll-forward). Pass the downloaded API 15
distribution as `DalamudLibPath` during
restore and build so Microsoft's SDK-based library project resolves the same
binding as the plugin. Restore for the same configuration, platform and runtime as the build; include
`Configuration=Release` when intermediate paths depend on configuration.
Stop on a failed restore. An old generic `win` lock target can conflict with
x64 on the pinned SDK: remove only that obsolete empty target through native
restore, inspect the lock diff, and retain every package version and local feed.
Keep one tag-release owner per repository so duplicate workflows do not race
to replace the same assets. Use the packaged manifest for untagged release
versions instead of a hard-coded version in YAML. Preserve native-server
assets and public-host/private-module boundaries; a UI adoption does not
convert protected release tools into an ordinary full-plugin release.
Verify the release ZIP includes `AethertekUI.dll` and the
adapter when used, while excluding host-owned assemblies.

A public consumer's `GITHUB_TOKEN` cannot read the private AethertekUI repository.
Prefer a separate read-only AethertekUI deploy key for each consumer, with its
private key stored only as that consumer repository's Actions secret. Botology
uses `AETHERTEKUI_DEPLOY_KEY` through the library checkout's `ssh-key` input.
Set `persist-credentials: false` so checkout removes the key before build steps.
Give the build's `GITHUB_TOKEN` only `contents: read`; grant `contents: write`
only to an already authorized release job. Never reuse a broad personal token,
commit a private key, or include it in logs or artifacts. Remove temporary local
key files after configuring GitHub. Windows SSH key generation needs a local
filesystem that supports key permission changes; a mounted drive can fail while
saving the key. Use a temporary local directory outside repositories, remove it
after provisioning, and verify read-only key metadata and the consumer secret
name without printing private material. A deploy key can be revoked on AethertekUI;
its matching consumer secret must then be replaced or removed.

Public viewers can see a secret's name in the workflow, but cannot read its
value or gain private-repository access merely by viewing or forking the consumer.
Repository secrets are available only to that repository's authorized workflows;
fork pull-request runs do not receive them. Workflow editors must be trusted:
code running with a secret can disclose it despite log masking. Do not run
untrusted pull-request code with this credential or add `pull_request_target`
access as part of adoption. Shipping the compiled library DLL makes that binary
public; it does not publish the source repository or its deploy key.

## Evolve through approved designs

Before implementation, measure the approved reference's content envelope,
region bounds and gaps, typography roles/weights, representative colours, artwork
and state assumptions. Inventory main, mini, compact, public introduction and
auxiliary windows from current source before preparing mockups. Confirm which
items are editable, read-only or disabled; do not invent actions from sample art.
Keep those inputs in the consumer's existing presentation
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

Full and compact modes are standard for every selected UI, including previously
adopted Botology and Footballer. Prepare compact mockups alongside regular
mockups before adoption; include mini windows and the website generated by a
companion application when selected. Keep a small native checkbox labelled
`C` in the main header with a localized `Compact mode` tooltip; preserve
existing compact-setting IDs when reusing a control. Share its density choice
across the consumer's windows and retained layouts. Reuse existing preferences;
the consumer owns any approved persistence through its current save path.

Compact mode reduces padding, gaps, row/card height and title prominence while
keeping labels readable and actions, semantics, optional columns and saved
choices available. Use wrapping and scrolling when space is limited. Do not
treat a separate mini window as the main window's compact mode, shrink the whole
window with a global scale transform, or reset user widths to fit the mockup.
Keep fonts/glyphs, nine languages and colour selectors usable in both modes.

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
text. C# consumers use keyed resources embedded in the assembly and native .NET
resource loading. Selected C++ or browser companions use their native resource
mechanisms and controls; do not introduce a managed dependency just for these
features. Cache the selected resource set. Keep key coverage equal across all
nine languages; English fallback must not conceal unfinished translations.
The .NET resource compiler treats key names case-insensitively. Use one canonical
key for status spellings that differ only by case, and normalize the UI lookup
without changing runtime messages or native IDs. Check key uniqueness and format
placeholders as well as coverage. Keep private source phrases in the approved
translation environment; external translation services require explicit approval.
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
including CJK names when English is selected. Build and check the same required
text set, including English fallback strings and untranslated UI symbols such
as native favorites and support labels. A symbol-face merge adds only the glyphs
requested by its ranges. Prioritize the selected locale's
CJK glyphs and retain Latin/Cyrillic coverage. In Dalamud, use managed atlas
handles and explicitly merge the host's `DalamudAsset.NotoSansCjkRegular` when
needed (verified bundled TTC faces: Japanese 0, Korean 1, Simplified Chinese 2,
Traditional Chinese 3). Read the collection's PostScript face names when the
host asset changes; do not guess its face order. Do not pass neutral
codes such as `ja`, `ko` or `zh-Hans` to `AttachWindowsDefaultFont`: unsupported
cultures can silently add nothing. Check actual managed-handle readiness, load
errors and glyph coverage after atlas rebuilds. Dispose consumer-owned handles;
never distribute Windows fonts. Temporary host fonts cannot qualify for visual
acceptance.
Segoe UI text faces omit some symbols used in existing tooltips, including
U+26AB (black circle). Merge the installed `seguisym.ttf` through the managed
atlas for required symbol coverage, alongside the selected CJK merges, in every
font role that can render those strings. Validate the actual required glyphs;
report the missing code point/role instead of suppressing the check. Font files
remain on the host and are never packaged.
Read/write source and resources as UTF-8. Windows PowerShell's default native
pipe encoding can replace non-Latin translations and diacritics with `?`; use
explicit UTF-8 transport and inspect the resulting characters.
Matching keys and placeholders cannot detect encoding loss. Inspect actual
resource values for native characters and unexpected literal `?` replacements,
especially in CJK, Cyrillic and accented Latin resources, before claiming coverage.

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
  verification route. Inspect all selected plugin's batch aliases, retain their
  existing development/public/private modes, and resolve retired wrappers to the
  maintained native tool. Use `%~dp0` paths and preserve the caller's directory.
  For AethertekUI, enter `eng/Enter-RepoEnv.ps1` before
  building project references so `DalamudLibPath` is supplied. With a lockfile
  containing DalamudPackager, restore normally and then build with `--no-restore`
  and `-p:Use_DalamudPackager=false` to avoid removing it from the lockfile.
  Already-restored packager targets can still run despite that build flag.
  For a strictly compilation-only check, also use
  `-p:ImportNuGetBuildTargets=false`; this skips all NuGet build targets, so
  reserve it for that check and retain normal imports for release packaging.
- When wrapping a native toolbar, compare the last item's right edge with the
  content region's right edge before `SameLine`. After a widget advances to the
  next line, `GetContentRegionAvail()` alone describes that next line and can
  incorrectly keep every subsequent group on an overflowing row. Include the
  measured translated label, icon, track and gaps in each group width.
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
