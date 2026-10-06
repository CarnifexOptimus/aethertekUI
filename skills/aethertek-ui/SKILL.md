---
name: aethertek-ui
description: Build and evolve AethertekUI or redesign FFXIV-related C# ImGui applications from approved mockups, with multilingual localization and relative colour themes. Use for library components, layouts, the showcase, consumer integration, and UX deployment readiness; use the optional Dalamud adapter only in Dalamud hosts.
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

For explicitly selected rounded outer chrome on a retained `ColorsOnly` layout,
enter `MaterialWindowChrome.Push()` after the theme and before native Begin,
keeping it active through End. It uses the current theme's already-scaled outer
radius and restores the prior window rounding without changing padding, spacing,
native IDs or saved geometry. The optional adapter's
`Draw(windows, applyMetrics, roundedCorners)` overload forwards the same scope.
Consumer PreDraw styles may override it. Native Begin forces rounding to zero
for owned platform viewports and docked windows; this style scope alone cannot
prove their rendered corners.

For selected owned-viewport corners and animated minimize in a Dalamud Window,
own one `MaterialWindowMotion` per window. Call
`Prepare(this, reducedMotion: existingPreference, roundedCorners: true)` last in
PreDraw, `DrawChrome()` first in Draw, and `Restore(this)` first in PostDraw,
after native End even when collapsed. Keep the theme and corner style scope
around the full lifecycle. Reference and ship the optional adapter DLL.
Preserve existing hook work and branch coverage, including font loading/error
windows. Enter the saved theme and chrome scope before a font-readiness early
return; linking the same status helper into public and private hosts does not
supply its caller theme. Keep borrowed body-font and layout scopes after readiness.
Prepare composes prior native size callbacks and temporarily suppresses
later managed constraint setters; Restore returns the owner's exact constraints.

For direct native Begin/End, own one `MaterialWindowFold` and one
`MaterialWindowDecorations`. Pass a cached `decorations.Prepare` delegate to the
fold's five-argument PreDraw overload after setting existing size constraints.
Call `decorations.Paint()` first in visible content and again after End, then
`fold.PostDraw()`. Paint replaces only matched native decoration meshes in that
owner's draw list, retaining emitted opacity, title controls, scrollbar grabs,
input and global styles. It does not paint a shared viewport background.
Docked windows and native popup/child/tooltip windows retain native chrome.

Native collapse arrows and title double-clicks keep their IDs. Existing
NoCollapse/NoTitleBar flags and disabled theme motion retain native behavior.
Inside Draw, size observers that save or repair geometry use
`motion.GetLogicalSize()` or `fold.GetLogicalSize()` to see expanded pixel size
during animation. Keep explicit resize actions and original position/save paths.
Verify actual owner hooks, expanded/collapsed/restored corner pixels, resize
grips, constraints, IDs and saves at both densities/scales. The offline injected
owned-viewport branch is diagnostic evidence; game/GPU acceptance stays separate.

For selected complete-window transparency, own one `MaterialWindowOpacity` per
top-level window tree. Call `Apply(windowName, opacity, enabled, autoFade,
fadedOpacity, unfocusedDelaySeconds)` once under its theme after the final native
End and motion/decorations restore, before Render, including collapsed windows.
Fractions clamp to .1–1. Defaults are 1 normal, .5 unfocused, auto-fade enabled
and a 10-second delay. Focus in owned children/popups restores normal opacity
immediately; native application focus events retain lost-focus state across idle
frames. Reopening resets the delay. Theme motion and reduced motion control the
transition; fade never raises opacity above the selected normal value.
The component multiplies emitted owner/child/popup/tooltip vertex alpha, covering
native chrome, custom meshes and image tints while retaining RGB, UVs, indices,
geometry, IDs and styles. Shared foreground/viewport drawings and independent
windows retain their own opacity. Avoid separate child instances or another
style/image alpha multiplier on that tree. SurfaceOpacity alone changes themed
backgrounds. Consumers own translated toggles/settings and their selected save
path. Verify native focus loss/gain, delay, raw image alpha, collapsed title,
native actions and saved geometry; standalone results leave host/GPU acceptance
separate.

For a borderless native child, pass `ImGuiWindowFlags.AlwaysUseWindowPadding`
when the design needs inset content; a pushed `WindowPadding` alone is ignored
by that child. Footballer also needed the job-details cursor aligned explicitly
with its character-name column. Check actual child padding, narrow-to-wide
reflow and the complete default-width card row, including a retained scrollbar.

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

Set an auto-sized window's required width before native Begin, using the
consumer's PreDraw hook where available. Calling SetWindowSize from Draw can
leave that frame's work rectangle narrow and feed a collapsed width back into
the next frame, as FrenRider Mini demonstrated. Verify the actual window flags,
reopening and density changes without supplying a fixed diagnostic size.

For a retained auto-fit wizard with wrapped content, constrain the native
auto-fit width to the width selected before Begin. Krangler otherwise measured
its content using a different width and clipped the final navigation row. Reserve
the complete fractional lower edge of the last native controls before auto-fit;
verify settling, short-viewport scrolling and fresh reopening with original IDs.
Its fourteen-locale, two-density, 100%/150% checks retained a stable subpixel
scroll remainder while all button ink remained visible.
TTSL Setup separately needed its existing 560-logical-unit width constrained
before Begin: at 150% scale, unconstrained auto-fit fed wrapped content back
into a progressively narrower window. Its fourteen-locale stage/reopen checks
retain automatic height, draft edits and original navigation identities.

Measure wrapped warning children against their actual inner width, including
native padding, border and scrollbar reservation; round the full padded height
upward. DhogNav's narrow Japanese warning otherwise measured as one line but
rendered two. Use the same reservation when budgeting the enclosing card and
its table footer, and verify the final native child content and scroll range.

## Identity, localization and resources

Use only `mcvaxius` or `dhoggpt` for user attribution. Never include the
user's personal name in authored UI text, changelogs, commits, documentation,
comments, reports, or agent output. Inspect affected text before delivery
and report any identity matches without repeating the private spelling.

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
Keep account/character selection visible and separate from settings categories.
At narrow widths, label the profile picker explicitly. Follow a different profile
from selection through its editor and save path; check both account and character
identity before exposing runtime-only actions. Inspect runtime/bridge callers
before changing an active-account helper into an editing-account helper.

A horizontally scrollable native window can expand its work rectangle to prior
content width on later frames. Wrap explanatory paragraphs to the visible pane,
accounting for horizontal scroll and narrower native columns, while retaining
single-line controls and real content overflow. Footballer's Polish Settings
otherwise clipped complete notes despite using TextWrapped. Verify after content
expansion over several frames, checking complete paragraph height and glyph ink.
A helper that pushes wrap position zero overrides its caller's explicit wrap limit.
DDuck public's failure footer uses translated native text under the visible-width
limit; an outer wrap scope around its retained helper left that limit ineffective.
Measure this limit from the native InnerClipRect so scrollbar space is included.

Keep ordinary control and profile labels on one line. Measure translated text,
icons, gaps and padding; move whole controls to another row instead of squeezing
them into a letter-width cell. For native wrappers, use
`MaterialLayout.FitNextItemWidth(requestedPixels, minimumPixels)` before sizing
the widget. Its widths are already pixels; preserve a measured readable field
minimum and provide horizontal scrolling or a wider pane when that minimum
exceeds the pane. Capture pending field widths before drawing a separate label,
which consumes native NextItemWidth. Retain existing helper IDs when changing
wrapping; do not derive a new control identity from the resized label.
Numeric fields must budget the readable editor plus both retained native step
buttons and their gaps. Round native field minima upward: ImGui's item-width
calculation floors fractional pixels. Verify presses across the original and
wrapped widgets, including the subcontrol IDs and release, rather than checking
only an idle numeric group's ID. ImGui may report that group's idle ID as zero.
Inspect the installed binding's argument defaults before wrapping: its current
`InputInt` defaults to zero steps. Preserve that behavior and reserve step-button
space only when the caller actually enables steps; interaction fixtures must
pass explicit steps when testing those buttons.
For a native checkbox with an empty visible label, reserve only its rounded-up
square: there is no label gap. HFH's extra gap pushed rule checkboxes off their
regular row. Reserve adjacent actions before sizing a retained editor, as its
name field and K action required.
For retained native columns, budget the scrolling content from the sum of each
actual initial width and translated minimum, taking the larger for each column.
HFH's minimum-only budget left its German Remove column squeezed after wider
earlier defaults; its strict thirteen-caption native scroll check reproduced the
failure and passed after the complete width budget was restored. Keep existing
column initialization and resize identities; do not reset user widths to fit art.
Keep retained data sections rendered when a small viewport leaves little height;
reserve a usable native table minimum and let the containing window scroll.
HealBot Watch otherwise omitted all retained targets at its new default size.
Budget table content from translated headings and displayed row values, then
use native horizontal scrolling while retaining existing column resize IDs.
Its stretch Name column otherwise collapsed to eight pixels.
For retained two-column detail rows, compare measured translated label widths with
that column's allocated share. ADS keeps its reference ratio when labels fit,
otherwise requests a fixed native label minimum including bearing inset and cell
padding. Labels stay single-line; explanatory values wrap within their own column.
Check actual glyph ink as well as advance widths at the native column clip.
For native captions at a table's left boundary, reserve the leading glyph's
negative bearing at the active font/scale and include it in the width budget.
DAD's Chinese Task caption otherwise lost a fraction of its leading glyph.
Compare text origins or baselines when checking row alignment: accents naturally
have different ink tops. Retain complete-ink and native-clip assertions.
Give label/value telemetry tables local row padding instead of inheriting padding
intended for action or party rows. TTSL's inherited padding inflated its metric
cards, while its automatically sized label column wrapped Position at the default
width. Measured translated label minima and local metric padding corrected both
without changing the Party table's explicit row heights or native table IDs.
A cursor inset alone leaves a retained panel using its enclosing content width.
Confine the native work/content bounds and clip to the panel's inner padding,
keep the prior ID root, and restore those bounds/scopes before sibling content.
TTSL's heading separators crossed card edges until this boundary was supplied;
verify caller restoration, native actions and complete ink at narrow sizes.

Measure and wrap translated canvas placeholders at the retained font, centering
complete glyph ink within the usable square and restoring canvas text state.
TTSL's 96-pixel empty radar clipped its fixed-x caption; fourteen-locale
checks of both embedded browser pages verify the correction. A missing origin draws no origin marker;
retain populated-point coordinates and prove both branches independently.

Natural and fill-width library buttons now reflow while preserving their full label;
positive explicit sizes retain caller control. Keep the current assembly version
visible in main title bars while preserving native window IDs. Distinguish the
runtime character from the selected settings profile.
For a selected native companion, derive its displayed build version from the
existing project version authority and retain its window class/control IDs.
TTSL's existing CMake build reads the managed project's `Version` for its native
caption instead of maintaining a second release number.

FrenRider's translated tabs require SetNextItemWidth for the complete label and
native FittingPolicyScroll when shrinking would clip it. With retained close
controls, also budget the original raw caption's native geometry. HFH's shorter
Korean caption otherwise crowded the close hit area at 150 percent; reserving
the larger original/translated width retained native navigation and full ink.
Check tab-body hover/press separately from the native close control.
Padding changed only
inside BeginTabItem did not preserve translated widths. Verify selected-tab ink
after native scrolling settles within a finite frame limit. Its selectable rows
also showed that a clip based on nominal line height or text advance can crop
fractional-height descenders and glyph bearings; retain native window/table clips
for those single-row labels. All fourteen locales passed the corrected paths.
Custom combo previews also need room for a glyph's negative left bearing inside
their existing padding. ChokeAbo and Dheacon reproduced clipped leading T ink in
Vietnamese and Turkish when the preview clip began exactly at the text origin.
The combo now admits the existing preceding gap, preserving the text origin,
arrow boundary, field dimensions and native IDs; the native regression checks
both preview routes, densities and scales with a supplied negative bearing.
BFE's scrolling tab strip contributed its complete width to a horizontally
scrollable parent's content size and expanded the next frame's work rectangle.
Bound that tab strip's contribution and reflow viewport while preserving real
body overflow and restoring the native work rectangle after drawing. Clip custom
caption ink to the native scrolling rectangle and verify the complete selected
caption after scrolling settles. Set tab padding before BeginTabBar so its height
reserves the full tab frame. Hiding ImGuiCol.Text during the first BeginTabItem
also hid its native scroll arrows; repaint the retained caption in its actual tab
background while preserving arrow ink, focus and IDs. Check visible arrow
triangles and native adjacent-tab presses, as well as widths.

DhogGPT retains translucent tab fills by clearing only the original caption's
glyph alpha in the emitted vertex buffer, then drawing translated ink inside
the native scrolling bounds. Keep arrow/close geometry intact. Native tab bars
retain scroll positions after narrowing; verify current-frame bar identity and
visible arrow hit geometry through each draw command's clip and vertex offset.
Follow native settling and the actual configuration change when checking saves;
a display-only adjacent tab may legitimately save nothing and retain composer focus.

Paint a retained title outside the native body's clip rectangle; intersecting that
clip can make the entire version invisible. Reserve native close/collapse controls
and check actual title glyph vertices or a render, as ChokeAbo established.
The standard appearance selector measures the selected language plus its arrow
and leading Globe mesh and padding, retaining that minimum in narrow panes.
Reflow the entire selector
group in consumer headers; native callback overloads still own their field sizing.
When the approved header places existing actions between colour and language,
use MaterialAppearanceSelector.DrawAccent and DrawLanguage with the same
selector ID. They preserve the combined selector's accent and language IDs.
MaterialComboLayout.LeadingIcon uses native meshes; retain the field's borrowed
draw list while BeginCombo makes its popup current, so the icon stays in the field.
DrawAccent's explicit positive logical size supports approved larger swatches
while retaining the same selector/accent IDs and change events. Pass a Vector2
for a rectangular colour field with Palette mesh, swatch and chevron; the whole
field opens the existing popup. Use the float overload for a square swatch.
Both preserve the accent/button identity, preset/custom events and disabled state.
The rectangular Palette respects the current pixel `IconSize`, bounded by field
height and the gap before its swatch. Footballer's approved larger field needed
explicit 30/32 logical icon sizes at compact/regular density; multiply by host
scale once when supplying metrics and verify the actual rendered icon ink.
Configure the
language field's height through an explicit MaterialControls.Push(metrics)
scope; the existing selector inherits those scaled pixel metrics.

DrawAccent's fixed Teal/Blue/Pink preset fills use opaque black labels.
Use MaterialColor.Contrast on the actual fill/foreground pair in idle,
hover and pressed states; MaterialButton's .08/.14 foreground layers can
lower contrast on press. The selector regression checks at least 4.5:1
with native rendered fill samples and glyph colours while retaining IDs,
caller sizes and separate change events.

Measure translated labels using the active host font. Budget pane, column,
button and popup widths for longer translations; use wrapping tiles where
appropriate and check clipped compact labels. Provide tooltips when necessary.
There is no universal fixed width that fits every language. The host supplies
and manages fonts with the required CJK glyphs; the library does not load fonts
or guarantee localization coverage. Test actual host fonts and display scales
when an integration is selected.

When selected typography work must retain the host atlas, a lexical
`SetWindowFontScale` scope can adjust presentation while restoring the caller's
`FontWindowScale`. Leave borrowed `ImFont.Scale` and `FontGlobalScale` unchanged.
Remeasure fields, cards and translated reflow under that window scale. DhogGPT's
passive headings reserve measured glyph ink rather than an oversized font line
box; reference-size English Main/ultra checks fit in both densities. Verify native
IDs and a non-default caller scale separately. Offline targeting must measure
the rendered caption and follow wrapped controls through native scrolling;
pre-Draw font metrics or large wheel jumps can miss a reachable control. Measure each group with its actual local font scale. DhogGPT's ultra preferences use regular-header typography in a separate top row, retaining the original root IDs and approved chat-language field widths instead of squeezing those fields to fit new selectors.

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
For a protected module host, resolve core and adapter references to the exact
assemblies already loaded by the public host; retain its dependency allow-list.
Verify compatibility against the actual deployable host as well as its build-only
ABI reference. MOM's supplied failure showed that updating the private access
DLL/JSON pair leaves an outdated installed public loader untouched. Treat public
host replacement and its version choice as a separate release step; equal version
metadata cannot distinguish older and rebuilt binaries. Offline compatibility
verification does not authorize host installation or publication.

## GitHub Actions provisioning

For every consumer adoption, determine how its CI and release builds will obtain
AethertekUI. A local sibling checkout does not exist on a fresh Actions runner.
Keep CI changes within mcvaxius's selected scope and obtain his persistent-machinery
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

For every mcvaxius-approved AethertekUI UX redesign, include the standard languages below and
whole-theme colour selection as standard features unless mcvaxius explicitly changes
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
Keep fonts/glyphs, all advertised languages and colour selectors usable in both modes.

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
| `vi` | Tiếng Việt |
| `pt-BR` | Português (Brasil) |
| `id` | Bahasa Indonesia |
| `pl` | Polski |
| `tr` | Türkçe |

The approved cross-plugin expansion selects Vietnamese (`vi`, Tiếng Việt),
Brazilian Portuguese (`pt-BR`, Português (Brasil)), Indonesian (`id`, Bahasa
Indonesia), Polish (`pl`, Polski) and Turkish (`tr`, Türkçe). Add them to every
selected surface using its existing resource/configuration route. Verify accents,
all displayed authored messages, embedded catalogs, selection/persistence and
translated layouts before advertising support. Retain every existing locale and
persisted ordinal, including existing pt-BR/zh-Hant choices; do not duplicate an
already supported locale. Apply equal nonempty key/placeholder coverage to all
advertised languages, including these additions.
Hindi (`hi`) rendering investigation is also selected, but support requires
demonstrated Devanagari shaping and cluster layout in native ImGui beyond glyph
presence. The current native diagnostic with Nirmala UI Regular (verified TTC
face 0) has every sample glyph yet leaves conjuncts/viramas unjoined and pre-base
vowels misplaced compared with Windows-shaped text. Glyph merges alone do not
qualify Hindi support. A future implementation must resolve shaped cluster
positions and native editing/layout before advertising it. This decision does
not authorize a new shaping dependency.

Translate all consumer-authored windows, headings, controls, tooltips and popup
text. C# consumers use keyed resources embedded in the assembly and native .NET
resource loading. Selected C++ or browser companions use their native resource
mechanisms and controls; do not introduce a managed dependency just for these
features. Cache the selected resource set. Keep key coverage equal across all
advertised languages; English fallback must not conceal unfinished translations.
Check every localized value for nonempty text wherever English is nonempty.
.NET compiles empty resource values, so matching keys and null-only lookup
guards can still leave visible controls blank. Reject empty translations in
the existing consumer resource check and inspect actual localized output.
Compare the exact key sets, including existing non-English-only keys, rather
than counts alone. Resolve every advertised locale's embedded resource set;
a selector entry does not prove that selecting or restoring that locale works.
The .NET resource compiler treats key names case-insensitively. Use one canonical
key for status spellings that differ only by case, and normalize the UI lookup
without changing runtime messages or native IDs. Check key uniqueness and format
placeholders as well as coverage. Keep private source phrases in the approved
translation environment; external translation services require explicit approval.
Preserve plugin names, external
catalog content, command tokens and logs. Format UI numbers and dates using the
selected locale without changing process-wide culture.
Include the selected culture's `NumberFormat.NumberGroupSeparator` in required
glyph text when displayed typed values use grouping. French `N0` uses U+202F,
which can be absent from resource strings and baseline font ranges. ChokeAbo,
LootGoblin, TTSL and ADS verified this correction across their advertised catalogs
and every original French font role, preserving role heights/files/merge order.
Check formatted samples against production ranges as well as required text;
digits already covered by baseline ranges are not missing product glyphs.
Translate authored interpolation arguments explicitly. Keep names, IDs and
already formatted service arguments raw, including leading zeroes; apply locale
number formatting to typed UI values instead of reparsing arbitrary text.
Generic label/value renderers need an explicit raw-value path as well. DAD's
saved preset named Regular party was translated by its KeyValue helper. Translate
the caption and provenance-known empty-selection prompt separately, then render
the exact saved name; verify label collisions in both wide and stacked layouts.
When a metadata lookup also returns an authored fallback, expose the unresolved
state separately for UI formatting. TTSL retains its public telemetry fallback
while translating Main's missing-area label; resolved names resembling that
fallback and published account identifiers remain opaque.
Match complete authored status wrappers before splitting lines so captured
LF/CRLF provider or exception detail stays intact. VerMAXion collection also
requires source-specific composition for intuition/spectral state, readiness,
cleanup and return suffixes. Use the known outcome and native supply-name
boundaries, including names containing colons; keep provider-owned detail opaque
even when it equals a UI label, and avoid translating the completed output twice.
Verify configuration round trips using the real host's serialization policy.
A seeded collection can be appended by Newtonsoft during reload: VerMAXion's
saved task order needed replacement semantics to retain custom order. Offline
checks may call verified pure serializer helpers; they must not construct host
storage services or operate clients.
Check every reached enum member, including failure/readiness values, in the
resources; localizing an interpolation does not translate a missing Busy label.
Prefer templates with authored leading text to catch-all leading placeholders.
DDuck's distance template otherwise consumed an entire member/HP diagnostic;
verify the nested displayed string and its separate empty form through the
compiled helper, with raw abbreviations and padded numbers retained.
Keep punctuation-only envelopes such as `{0} {1}: {2}` out of global
service-template matching. DAD's leveling compiler uses that shape around raw
slot/character identities; localize it through the source-specific display
formatter. Parse joined compiler details only within that context and constrain
slot captures to the producer's slot grammar so a later slot cannot swallow an
earlier summary. Keep exception details raw even inside an authored wrapper.
HealBot Watch similarly needs exact UI template composition for its retained
states and an explicitly translated authored prefix. Its complete resources did
not make leading-placeholder states reachable through the generic label lookup.
Trace composed statuses through execution/proxy getters as well as direct UI
calls. ADS verified an ownership wrapper around a Higher/Lower service summary
and retry/blocked detail. Check displayed row outcomes alongside templates.
Generated literal/wildcard message templates can use `RegexOptions.NonBacktracking`
to avoid timeouts from ambiguous adjacent captures, as DDuck's compiled status
checks verified. Check full, empty, nested, padded and LF/CRLF captures after the
change. Keep source-specific lookaround parsers separate: DAD's leveling patterns
contain lookaheads and cannot use that option.
For a catalog whose literal/wildcard templates repeatedly time out during Draw,
consider matching pre-split ordinal literal segments. LootGoblin verified this
route with ordered templates, raw/empty numbered arguments, repeated placeholders
and the retained final-newline behavior. Check actual displayed messages and
native window states; a literal matcher does not replace a parser whose grammar
needs alternation, lookarounds or other regular-expression semantics.
Before constructing or running a source-specific lookaround matcher, check a fixed
authored literal that every successful match must contain. DAD's joined leveling
blocker otherwise entered unrelated expensive patterns despite its prefix gate.
Retain template order, lookaheads, capture grammar, raw values and timeout failures;
the prefilter skips only patterns whose necessary literal is absent.
For literal/wildcard status catalogs, skip candidates whose authored first-line
prefix does not match before entering the existing bounded regex. TTSL's
already-localized Publisher display otherwise timed out in Settings Draw.
Preserve candidate order and the existing timeout, and verify every reached
template with raw captures. Absolute ordinary-regex anchors retain terminal
LF/CRLF details; TTSL checks those captures rather than only match success.
For large status catalogs, create a retained matcher only after its authored
leading literal applies. DDuck's eager non-backtracking construction exceeded
the diagnostic memory limit after the atlas was ready; deferring unused matchers
kept native selector runs below 521 MiB. Retain template order, raw captures and
LF/CRLF behavior; use a conservative first-line prefix before the existing pattern.
When hiding a native field label to retain its original ID, remove that label's
layout contribution as well as its ink. DhogGPT's measured field/card checks
caught invisible label widths expanding content bounds and scroll ranges.
Trace displayed service statuses through their getters and helper-returned
messages; resource parity alone does not prove that those statuses are covered.
When matching existing service-message templates, preserve empty arguments and
their numbered identities. Match templates before trimming whitespace: a trailing
space can represent an empty final argument. Check an actual displayed diagnostic as well as key
and placeholder parity; complete resource files can still fail to translate it.
If a producer trims an empty final detail, keep a reachable resource for that
shortened form as well as the complete template. DAD's completion summary
established this case. Translate nested captures only at positions traced to
authored role/module labels or summary/safety getters; arbitrary names, counts
and IDs remain opaque. Verify both full and empty forms through the compiled
helper, rather than changing the producer's runtime message to suit UI matching.

Footballer and MOM exposed terminal-newline failures in the installed runtime:
`$` can leave a final LF outside a capture, while a `NonBacktracking` pattern
ending in `\z` matched that input but returned empty argument groups. Its
bounded ordinary matcher with `\A`/`\z` retains both the name and complete
LF/CRLF detail. Test actual captured values, including an empty argument and a
terminal newline, before selecting a regex engine; a successful match alone is
insufficient. MOM's fifteen-locale startup checks also verify raw empty/padded
details and nested readiness wrappers with the bounded ordinary matcher.
Keep this runtime-specific evidence distinct from other hosts.

Structured diagnostic events need their producer schema as well as resource
coverage. AutoParty wake events use `state:wake:<phase>:<summary>`; split at
the first payload colon, then translate phase and summary separately. Preserve
colons and empty text inside the summary. In composed status rows, explicitly
translate authored enum/conditional/summary arguments before formatting; keep
owner, participant and character identities raw. DAD Mini established this
distinction through actual compiled helper checks.
Multiline status matching must tolerate the producer’s LF/CRLF literal line
breaks without normalizing captured raw details. BFE verified both prefix forms,
empty arguments, padded values and exact line endings inside the capture. XML
readers can normalize resource-value line endings; account for that in source
comparisons while checking raw helper arguments separately.

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

Fonts must cover the selected resource set and every native dropdown name,
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
For .NET diagnostic runners, set `Console.OutputEncoding` to UTF-8 before
capturing text and decode the log accordingly. DhogNav's Japanese captions
otherwise became question marks despite intact resource values and passing native
glyph checks. Distinguish diagnostic encoding loss from damaged product catalogs.
When integrating a saved translation payload, read it directly as UTF-8 and
compare the written values with that payload before mirroring shared phrases.
DAD's tail passed structural checks after PowerShell had changed its accented
text to question marks; Python UTF-8 mode cannot recover text already damaged
by the native pipe. Keep the intact payload as the input for the correction.
Matching keys and placeholders cannot detect encoding loss. Inspect actual
resource values for native characters and unexpected literal `?` replacements,
especially in CJK, Cyrillic and accented Latin resources, before claiming coverage.
Inspect unexpected single question marks as well as repeated ones: DAD's Duty
Finder help lost Latin accents while retaining valid keys and placeholders.
Compare written values with intact UTF-8 input; ordinary question punctuation
remains valid. Trace a short label's displayed caller when checking semantic
quality: combat Party means a group, Ready describes readiness, and unrelated
second lines can survive structural checks in colour presets and status labels.

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
  ChocoboColourized's save, copy, support and feeding-order affordances also use
  `MaterialIcon.Save`, `Copy`, `Heart` and `List`. Reuse those native meshes in
  retained controls while preserving their IDs, actions and localized tooltips.
  HealBot's combat actions and priority rules use appended `Sword` (crossed blades) and `Star`
  meshes under the same ownership and identity rules.
  ChokeAbo's preset workspace and breeding affordances use appended `Folder` and
  `Egg` meshes; preserve their native controls and localized tooltips.
  ADS uses appended Wrench/DoorExit/Cube/Chest/Cart/Chat/Group/Warning/Plus;
  LootGoblin uses Pulse/Utensils/Crystal; DAD uses Calendar/Crown/Book/Sparkle/Bell/Terminal.
  FrenRider's account-wide enable action uses the appended Power mesh.
  LootGoblin's Current Run uses Target, the appended crosshair; Location stays a pin.
  BFE's area heading and support action use appended MapPin and HeartOutline.
  Community actions use appended Discord (66) and KoFi (67) meshes, retaining
  native actions and caller sizing/opacity. The shared Discord has transparent
  eye openings; Ko-fi retains its red heart. Check the smallest consumer size:
  antialias fringes can fill a tiny eye, and a Bezier handle's complete stroke can
  exceed its control-point bounds. Native 14/36-pixel checks cover both cases.
  Append required artwork without changing existing ordinals. Keep icon drawing
  independent of font and texture resources, with actions and identities in consumers.
- `MaterialControlAppearance` supplies fill, foreground, border and logical
  radius to button, badge, search and combo overloads. Badges accept logical
  dimensions; switches accept an explicit logical track size. Rounded gradient
  `MaterialCanvas.Surface` keeps pixel coordinates/radius. Explicit
  `MaterialControls.Push(metrics)` values are already scaled pixels;
  parameterless nested scopes inherit them, explicit contexts remeasure, and
  theme/control scope disposal restores the prior state. Showcase and compiled
  consumer examples accompany demonstrated reusable gaps.
- Measure retained table columns from translated headings and actual row text.
  Native fixed-column requests are content widths; budget cell padding and
  borders separately in the scrolling table's inner width. ChocoboColourized's
  narrow Timers otherwise starved its stretch column despite a wide inner table.
  Keep ordinary cells on one line and verify native scrolling reaches the final
  column. Bound the table height to its rows and viewport instead of filling a
  mostly empty retained surface; preserve IDs, raw names and model data.
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
- Check the installed binding API before choosing newer ImGui helpers. ChokeAbo
  established that this host lacks `ImGuiChildFlags` and `AddEllipseFilled`: an
  autosizing native table can provide a balanced card scope, while a closed
  convex path draws the required ellipse. Retain the previous ID root when
  introducing the table. A copied helper from another binding is not build evidence.
- For an autosizing card built from a retained native table, measure cell content
  before `EndTable` and paint its rounded fill/border between
  `ImGuiP.TablePushBackgroundChannel()` and `TablePopBackgroundChannel()`.
  DhogNav's public introductions otherwise painted the measured background over
  the foreground. `PadOuterX` retains the horizontal cell inset; keep the previous
  ID root and balance the channel/table scopes. Verify translated content and
  safe native actions at narrow widths, both densities and supported scales.
- Inside native BeginGroup, a positive SameLine offset is relative to the group.
  Do not include the window-local cursor X again; DDuck Mini stock rows otherwise
  double-counted the left inset and exceeded the viewport. Use group-relative
  offsets there and retain window-local coordinates for SetCursorPosX. The fixed
  rows passed native action, target-edit and width checks in all fourteen locales.
- Offline fonts may differ from the live managed atlas and conceal a broken
  merge. A clean build or CPU render does not prove host font readiness. Give
  loading/error windows containing wrapped text an explicit width every frame,
  including after opening; retain their native IDs and balanced Begin/End.
- A selected server-rendered browser companion can retain a strict script-free
  CSP while loading a same-origin stylesheet. AutoParty administration uses
  native GET appearance controls, validated URL preferences and translated
  embedded resources, preserving those preferences in each POST action and its
  successful local redirect. Keep anti-forgery fields, exact Host/Origin gates,
  action bounds, escaped aliases and redacted identities intact. Verify actual
  compiled HTML/CSS and browser layout without starting production services;
  a presentation check does not prove a live administrative action.

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
later host acceptance. Offline hosts that merge CJK may produce multiple native
atlas textures. Refresh and assign each texture its own ID, then resolve the
draw command's actual texture when rendering; sampling texture zero for every
command corrupts otherwise valid glyph UVs. Sample RGBA texture colour as well
as alpha: a font-only alpha sampler turns coloured supplied images into white
blocks. Footballer exposed this in its job icons; the shared renderer now checks
opaque colour, tint and transparency. Earlier coloured-image diagnostic renders
need regeneration before visual acceptance. HealBot's Japanese helper diagnostic
verified this distinction. It does not establish managed-host readiness.
Give a standalone diagnostic atlas explicit dimensions suitable for its required
glyphs and roles, and record those dimensions with the result. BFE and
ChocoboColourized's complete Korean windows passed with their unchanged fonts,
ranges and merge order at 4096 by 4096, after default packing exceeded its bound.
Keep finite time and memory bounds; include measured host overhead when setting
the full-window bound. A successful standalone bake does not prove the managed
host's atlas configuration or readiness.
When a complete offline multi-role atlas exceeds its memory bound during font configuration,
check required glyphs in separate original-role processes and native interaction with
the actual role used by those controls. ADS's nine roles and four CJK merges used
763 MiB before baking; separate role checks stayed below 236 MiB. Retain the
required text, original heights and merge order, and record every process bound.
Separate glyph checks do not prove combined-atlas, whole-window or managed-host readiness.
Later ADS Main/Quick runs retain all nine original roles and merges at 4096x4096
within a finite 90-second/1536-MiB combined diagnostic bound. The fifteen-locale
matrix passes; root independently verifies English and Russian, with the latter
using 1316.922 MiB. Keep the earlier smaller-bound failure distinct and budget
measured native/render overhead. This still does not establish managed-host readiness.
For standalone checker references, inspect MSBuild's actual ReferencePath and
ResolvedFrom after ResolveAssemblyReferences. Retained ignored DLL outputs can
resolve through {CandidateAssemblyFiles} before an explicit HintPath; the
shared checker's search paths exclude those candidates. Use the normal build's
actual core output, and require identical core bytes in checker and product
before accepting results. A successful rebuild or equal file sizes is insufficient.
For a retained whole-window fixture, read the owner's actual flags after PreDraw
and add only the diagnostic NoSavedSettings flag. DAD Shopping and TTSL Main
otherwise inherited a horizontal scrollbar they do not use in production;
its prior-content width changed later work rectangles and produced false overflow.
Keep intentional scrolling helper fixtures separate from actual-window evidence.
Historical mission/GBR reports do not prove current
integration compatibility or grant permission to run legacy tooling.

Keep three kinds of evidence separate: functional native interaction checks,
diagnostic visual renders, and mcvaxius's game screenshot acceptance. Extend the
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

Native rounded borders have separate horizontal and vertical extrema. Their
texture-antialiased vertices also include transparent atlas fringes. Measure the
connected stroke path or rendered ink; requiring one corner vertex at both
extrema can fail a correct scaled layout. Retain the reference tolerance and
prove that shifted regions still fail, as BFE's 100%/150% checks established.
Cover selected windows/popups, all locales, scale/size limits, accents and
functional states. Open interactive popups through their native controls and
check that they remain visible with usable bounds after the opening frame.
Choose click positions after measured native layout bounds have settled. When an
action caption also occurs in explanatory prose, find the action through its retained
native ID and check that occurrence's complete ink; DDuck public's Ko-fi caption
otherwise selected text in its community paragraph. Child
scrollbar changes can alter available width and move controls over several
frames; Botology's Indonesian popup check exposed this. Require stable geometry
with a finite frame limit that fails if layout does not settle.

Before inspecting a retained table after an exhaustive scrolling text sweep,
expose its row again and target wheel input inside `InnerRect` intersected with
`ClipRect`. DhogNav's fresh narrow workflow left its table above the viewport;
retain the scrollbar, row-access and edit/cancel/save requirements when correcting
that observation order.

Auto-sized popups containing wrapped content may need an explicit width each
frame; an appearing-only width can collapse later. Builds, test counts and
self-generated baselines alone do not establish completion. Deliver as
"locally verified; game visual acceptance
pending" until mcvaxius supplies and accepts the game screenshot; never auto-accept
or operate clients without explicit authorization.
