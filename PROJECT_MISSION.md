Before doing any other work, persist the complete task specification that follows verbatim into:

C:\Users\pscho\OneDrive\Documents\GitRepos\CombatReborn.MaterialUI\PROJECT_MISSION.md

Requirements:

- Preserve the entire prompt exactly as provided. Do not summarize, rewrite, shorten, or normalize it.
- This file is the authoritative project mission and constraint document for the duration of the work.
- Re-read PROJECT_MISSION.md at the beginning of each major implementation phase and whenever context may have been compacted, lost, or become uncertain.
- If your working assumptions ever conflict with PROJECT_MISSION.md, PROJECT_MISSION.md wins.
- Do not delete, replace, or substantially edit this file during the project.
- You may append a clearly separated "Agent Notes" section at the very end only if useful, but the original task text must remain untouched.
- Commit PROJECT_MISSION.md with the rest of the repository when appropriate.

After writing and verifying the file, proceed with the task described below.

--- BEGIN PROJECT MISSION ---
MISSION
=======

Build a new, standalone C# Material Design 3-inspired immediate-mode UI library intended for use by Dalamud plugins, with GatherBuddy Reborn (GBR) as its first planned consumer.

This is an IMPLEMENTATION task, not merely a research/planning task.

Start from ground zero and continue through a functioning, compiled prototype library and a standalone Dalamud showcase/demo plugin.

Do NOT modify GatherBuddy Reborn.

Do NOT wait for or depend upon the unpublished RSR Material Design work.

The purpose of this exercise is specifically to determine how far a clean implementation can get without knowledge of LTS's RSR implementation.

The target visual language is:

- Material Design 3-inspired
- dark/translucent in-game surfaces
- compact desktop density rather than mobile density
- strongly rounded outer surfaces and controls
- tonal surface hierarchy rather than heavy borders/shadows
- seed/accent-derived colors
- segmented controls
- filled / tonal / outlined / text buttons
- switches
- icon buttons
- cards
- search/text fields
- chips
- navigation
- action tiles
- polished hover/press/selected states
- subtle animation where it improves the interface
- dense data tables preserved where appropriate

The screenshots that motivated this project show the general desired feel:
clean translucent dark overlays, green-derived Material tonal colors, pill-shaped segmented controls, rounded cards, low-contrast outlines, icon-first actions, compact information density, and interfaces that no longer look like stock ImGui.


ABSOLUTE FILESYSTEM BOUNDARY
============================

You have full read/write permission ONLY inside:

C:\Users\pscho\OneDrive\Documents\GitRepos\CombatReborn.MaterialUI

Create that directory and initialize a new Git repository there.

You may READ GatherBuddy Reborn from:

C:\Users\pscho\OneDrive\Documents\GitRepos\GatherBuddyReborn

GBR IS READ-ONLY.

You may also READ existing installed SDK/toolchain/Dalamud files as necessary to compile the new project.

You MUST NOT:

- edit any GBR file
- create any file in GBR
- build GBR
- restore packages in GBR
- update GBR submodules
- fetch/pull/checkout anything in GBR
- alter GBR's .git directory
- let an IDE create .vs/.idea/etc. in GBR
- install/copy the demo plugin into XIVLauncher/Dalamud directories
- write source, caches, temporary files, build artifacts, documentation, logs, or generated files anywhere outside CombatReborn.MaterialUI
- create or push a remote GitHub repository
- inspect unpublished/private RSR Material UI work
- use an existing third-party "Material ImGui" framework as the implementation

Public Material Design documentation may be used as design reference.

Do not inspect RSR as an implementation reference. This is intentionally an independent implementation.


KEEP TOOLING WRITES LOCAL
=========================

Before invoking dotnet/NuGet or other tooling that normally uses user-profile caches, create repo-local cache/temp directories and redirect tooling there.

At minimum, use repo-local equivalents for:

DOTNET_CLI_HOME
NUGET_PACKAGES
NUGET_HTTP_CACHE_PATH
TEMP
TMP

Also set:

DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
DOTNET_CLI_TELEMETRY_OPTOUT=1

Create an eng/Enter-RepoEnv.ps1 script that establishes this environment reproducibly.

Add all tool cache directories to .gitignore.

All bin/, obj/, package caches, temporary files and artifacts must remain under the new repository.


PHASE 0 — SAFETY AND BASELINES
==============================

Before implementation:

1. Create:
   C:\Users\pscho\OneDrive\Documents\GitRepos\CombatReborn.MaterialUI

2. Initialize a local Git repository with main as the initial branch.

3. Record the current GBR HEAD commit.

4. Record GBR's current working-tree state WITHOUT modifying its index.
   Use GIT_OPTIONAL_LOCKS=0 for Git read operations.

5. Record any existing modified/untracked GBR files so we can prove at the end that we did not alter the repository.

Do NOT require GBR to already be clean.

Create:

docs/GBR_BASELINE.md

containing:
- GBR path
- observed HEAD
- existing working-tree state
- date/time of inspection
- explicit statement that GBR is a read-only reference


PHASE 1 — READ-ONLY GBR UI REQUIREMENTS AUDIT
=============================================

Inspect the existing GBR source in depth.

Do not implement yet.

The goal is to determine what a reusable MaterialUI library needs to support so that GBR can eventually be migrated without the library depending on GBR.

At minimum inspect:

GatherBuddy/Gui/
GatherBuddy/Config/
GatherBuddy/GatherBuddy.csproj

Pay particular attention to:

- Interface.cs
- Interface.Header.cs
- Interface.*Tab.cs
- ImGuiEx.cs
- UiHelpers.cs
- VulcanUiStyle.cs
- VulcanUiScaling.cs
- VulcanWindow.cs
- VulcanWindow.*.cs
- CraftingListEditor.cs
- CraftingMaterialsWindow.cs
- CraftingTreeWindow.cs
- CraftingStatusWindow.cs
- CollectablesWindow.cs
- RecipeCraftSettingsPopup.cs
- VendorBuyListWindow.cs

Survey the entire Gui directory, not only these files.

Determine:

- every common button pattern
- checkboxes and boolean toggles
- mutually-exclusive option controls
- combo boxes
- text/search inputs
- filters
- table usage
- child windows/panels
- popups/modals
- tooltips
- progress displays
- tab/navigation usage
- header/tool-bar patterns
- icon usage
- item/action tiles
- status colors
- custom DrawList rendering
- sizing/scaling helpers
- controller/gamepad concerns
- disabled states
- hover/click helpers
- places where hard-coded colors/styles are currently used
- areas that must retain high information density

Explicitly distinguish:

A. APP CHROME / DESIGN SYSTEM COLOR
   Examples:
   surface, primary accent, border, button, selected navigation, hover state

B. GAME/DOMAIN SEMANTIC COLOR
   Examples:
   available item, upcoming node, tug strength, warnings, fish availability

The future Material theme must NOT destroy or repurpose GBR's domain-semantic colors.

Create:

docs/GBR_UI_REQUIREMENTS.md

Include a matrix like:

Existing GBR pattern
→ frequency/locations
→ proposed MaterialUI component
→ migration difficulty
→ special behavioral requirements

Do not make any GBR changes.


PHASE 2 — ARCHITECTURE THE LIBRARY
==================================

The library must be a REAL standalone assembly.

Proposed namespace:

CombatReborn.MaterialUI

It must NOT reference:

- GatherBuddy
- ElliLib
- RSR
- Combat Reborn plugin-specific code

Prefer dependencies to be limited to:

- .NET
- Dalamud
- Dalamud.Bindings.ImGui
- other assemblies already supplied as part of the normal Dalamud environment

The library should not own plugin lifecycle.

A consuming plugin should be able to reference the library and use it during its normal Draw() path.

Design the library around immediate-mode rendering.

Recommended major areas:

Core/
    MaterialTheme
    MaterialColorScheme
    MaterialPalette
    MaterialMetrics
    MaterialDensity
    MaterialState
    MaterialMotion
    MaterialStyleScope

Drawing/
    MaterialDraw
    Geometry helpers
    Color helpers
    State-layer helpers

Components/
    Button
    IconButton
    SegmentedControl
    Switch
    Checkbox helpers
    Radio helpers
    Card
    Surface
    Chip
    SearchField
    TextField
    NavigationRail / navigation item
    Tabs
    ActionTile
    Badge
    LinearProgress
    Dialog
    SectionHeader
    ListItem
    Table style helpers
    WindowHeader

The exact file organization is yours to improve.

Do not create one 5,000-line "MaterialUI.cs" god file.

Create:

docs/ARCHITECTURE.md
docs/DESIGN_SYSTEM.md
docs/COMPONENT_API.md

before or alongside implementation so architecture decisions remain explicit.


PHASE 3 — SOLUTION/PROJECT STRUCTURE
====================================

Create a solution approximately structured as:

CombatReborn.MaterialUI.sln

src/
    CombatReborn.MaterialUI/
        CombatReborn.MaterialUI.csproj
        ...

    CombatReborn.MaterialUI.Demo/
        CombatReborn.MaterialUI.Demo.csproj
        ...

tests/
    CombatReborn.MaterialUI.Tests/
        CombatReborn.MaterialUI.Tests.csproj
        ...

docs/
eng/
artifacts/

The main library should target the same modern runtime generation GBR currently targets.

Determine the correct project SDK/reference arrangement by inspecting the installed toolchain and GBR.

The Demo project should use the current Dalamud SDK compatible with GBR.

Current GBR is using Dalamud.NET.Sdk 15 and .NET 10; verify this from the local repository rather than blindly trusting this prompt.

For the library itself:

Prefer Microsoft.NET.Sdk if clean Dalamud reference resolution can be achieved without hacks.

If Dalamud.NET.Sdk is significantly cleaner for resolving the current Dalamud assemblies, using it for the library is acceptable, but the output must remain a normal reusable class library rather than a plugin.

Document the decision.

The demo plugin MUST reference the MaterialUI project through a normal ProjectReference.

Do not copy MaterialUI source files into the demo project.

Enable:
- nullable
- modern language version
- deterministic builds
- warnings-as-errors where practical
- x64 where appropriate

Keep NuGet restore/build output local to this repository.


PHASE 4 — MATERIAL COLOR/THEME ENGINE
=====================================

Implement the theme system before building lots of widgets.

The core concept is a semantic Material color scheme rather than raw colors scattered through controls.

At minimum provide semantic roles equivalent to:

Primary
OnPrimary
PrimaryContainer
OnPrimaryContainer

Secondary
OnSecondary
SecondaryContainer
OnSecondaryContainer

Tertiary
OnTertiary
TertiaryContainer
OnTertiaryContainer

Error
OnError
ErrorContainer
OnErrorContainer

Background
OnBackground

Surface
OnSurface
SurfaceVariant
OnSurfaceVariant

SurfaceContainerLowest
SurfaceContainerLow
SurfaceContainer
SurfaceContainerHigh
SurfaceContainerHighest

Outline
OutlineVariant

InverseSurface
InverseOnSurface
InversePrimary

Scrim

Also support adjustable transparency independently from RGB color generation.

The user should be able to choose ONE interface accent/seed color and derive a coherent theme from it.

DO NOT get stuck spending the entire project implementing Google's exact HCT engine.

For the prototype:

Implement a deterministic perceptual tonal-palette generator using OKLab/OKLCH or another defensible modern color model.

Requirements:

- preserve seed hue reasonably
- generate coherent primary/secondary/tertiary palettes
- generate neutral and neutral-variant palettes
- gracefully reduce chroma when requested colors leave the sRGB gamut
- deterministic output
- no per-frame palette generation
- unit-testable pure functions

Design the palette-generation abstraction so a future exact HCT implementation could replace it without rewriting the widget library.

Do not claim exact Google Material dynamic-color compatibility unless you actually implement it.

The visual goal is MD3-inspired and coherent, not certification.

Implement DARK MODE FIRST.

A basic light scheme may be added later if inexpensive, but it must not delay the working dark prototype.


DENSITY AND METRICS
===================

Material's mobile dimensions are too large for GBR.

Implement a desktop-oriented density concept.

At minimum:

MaterialDensity.Compact
MaterialDensity.Standard

Compact should be the primary target.

All geometry must scale correctly with Dalamud's global UI scale.

Centralize:

- corner radii
- frame heights
- card padding
- inner spacing
- section spacing
- icon sizes
- border widths
- navigation widths
- chip heights
- button heights

Avoid random magic numbers throughout widgets.

Suggested visual direction:

- outer windows/cards: large radius
- controls: medium/high radius
- pill controls: radius based on height
- subtle 1px-equivalent outlines
- dark translucent surfaces
- no gratuitous drop shadows
- surface tone differences establish hierarchy


THEME SCOPING
=============

A Material theme must be safely scoped.

Target usage should feel approximately like:

using var theme = MaterialTheme.Push(myTheme);

or equivalent.

The scope must:

- push only the styles/colors it owns
- restore every ImGui style/color on Dispose
- not permanently mutate global ImGui style
- support nested component style scopes safely

Prefer allocation-free struct/ref-struct style scopes where practical.

Never leave the ImGui style stack unbalanced.


PHASE 5 — CORE COMPONENT IMPLEMENTATION
=======================================

Build components incrementally and compile after each small family.

All custom controls must use stable ImGui IDs.

They must behave like real controls, not merely painted pictures.

They should respect:
- hover
- press
- disabled
- selected
- focus/nav where possible
- clipping
- global scale
- available width
- stable ID behavior

Use public ImGui APIs and DrawList APIs.

Do not modify Dear ImGui itself.


BUTTONS
-------

Implement:

FilledButton
FilledTonalButton
OutlinedButton
TextButton

Support:
- label
- optional leading icon
- optional trailing icon where reasonable
- compact/default sizes
- explicit size
- fill-available-width mode
- disabled state
- tooltip support should remain consumer-controlled

Visual behavior:
- rounded shape
- Material state-layer hover
- pressed state
- disabled alpha
- accent-derived colors


ICON BUTTON
-----------

Implement compact Material-style icon buttons suitable for:
- settings
- close
- edit
- delete
- open
- collapse
- overflow

Support standard/tonal variants if sensible.


SEGMENTED CONTROL
-----------------

This is a HIGH PRIORITY component.

GBR has many situations suitable for mutually-exclusive options.

Build a reusable segmented control that can represent patterns such as:

[ ✓ Auto ] [ Manual ] [ Off ]

Requirements:
- arbitrary number of segments >= 2
- selected state
- optional icons
- rounded exterior
- correct first/middle/last geometry
- low-contrast dividers
- keyboard/nav compatibility where practical
- generic or otherwise ergonomic value binding
- disabled whole-control and/or disabled-option support
- optional animation of selected emphasis

API should not require callers to manually calculate segment geometry.


SWITCH
------

Implement a real custom Material switch.

Requirements:
- clickable track
- thumb
- on/off colors
- disabled state
- hover/press state
- subtle animated thumb transition
- deterministic immediate-mode state management


MOTION SYSTEM
-------------

Build a very small centralized animation/state system.

Use ImGui frame delta time.

Support things such as:
- switch thumb interpolation
- selected navigation indicator interpolation
- state-layer fade
- progress smoothing if appropriate

Avoid:
- animation for animation's sake
- large persistent state leaks
- a Dictionary that grows forever for controls that no longer exist

Animation state should be keyed by stable ImGui IDs and stale entries should be pruned.

Animation should degrade gracefully if a control disappears for many frames.


SURFACE / CARD
--------------

Implement reusable begin/end scopes for:

Surface
Card

Variants should correspond to tonal elevation/surface-container roles.

Support:
- padding
- optional outline
- corner radius
- explicit or available size
- optional interaction state for clickable cards

The library should make it easy for GBR to replace arbitrary bordered BeginChild usage with coherent Material surfaces.


CHIPS
-----

Implement at least:

FilterChip
Assist/Action chip if useful

Filter chip requirements:
- selected/unselected
- optional leading check/icon
- compact sizing
- disabled state


SEARCH FIELD
------------

Build a polished search field.

Requirements:
- native ImGui text editing behavior
- search icon
- hint text
- inline clear icon
- rounded Material field shape
- focus/hover state
- scalable geometry

GBR already contains custom inline-clear search behavior; the new library should provide this generically.


TEXT FIELD
----------

Build a reusable outlined or filled text field wrapper.

Do NOT reimplement text editing itself.

Use native ImGui InputText behavior and style/draw around it.

Support:
- label or hint
- disabled/read-only where possible
- error visual state
- optional leading/trailing icon


NAVIGATION
----------

Build a Material navigation item and rail/container suitable for eventual GBR use.

Do not immediately optimize solely around GBR's current exact tab count.

Support:
- icon
- label
- selected state
- compact and expanded rail mode if feasible
- selected pill/indicator
- section/group heading support separately

Also implement Material-styled secondary tabs.

Primary app navigation and secondary sibling tabs are separate concepts.


ACTION TILE
-----------

Implement the kind of larger action tile useful for major controls.

Example conceptual use:

[ icon ]
Start Gathering

Requirements:
- icon area
- primary label
- optional secondary label
- selected/active/disabled states
- suitable for a grid

Do NOT encourage its use for every small button.


BADGES / STATUS
---------------

Implement a compact Badge/StatusPill.

Examples:
Latest
Running
Paused
Missing
Ready

The consuming plugin supplies the semantic meaning/color when needed.


PROGRESS
--------

Implement a rounded linear progress indicator.

Support:
- determinate percentage
- track color
- accent color
- optional compact height


DIALOG
------

Provide helpers/scopes for Material-styled ImGui modal popups.

Preserve standard ImGui popup lifecycle.

Do not invent a parallel modal manager unless genuinely necessary.


WINDOW HEADER
-------------

Build reusable custom window-header content.

It should support:
- title
- optional subtitle/status
- trailing icon actions
- close/settings/etc.
- compact mode

Do NOT make the library own Dalamud Window lifecycle.

A consumer still owns the actual Window object.


TABLE SUPPORT
-------------

This is critical for GBR.

DO NOT replace data tables with giant cards.

Implement a Material table style scope around native ImGui tables.

Support:
- surface-integrated table background
- tonal header
- subtle row alternation if appropriate
- selected/hover row styling helpers
- compact cell padding
- separator/border choices consistent with the design system

Native ImGui sorting, resizing, clipping, column behavior etc. must remain available.

MaterialUI should style tables, not reinvent them.


PHASE 6 — COMPONENT QUALITY/PERFORMANCE RULES
=============================================

Rendering code executes every frame.

Therefore:

- no LINQ in hot Draw paths unless strongly justified
- avoid obvious per-frame heap allocations
- do not regenerate theme palettes per frame
- cache expensive calculations
- do not create textures per frame
- do not grow animation state indefinitely
- do not allocate IDisposable classes every time a simple style scope is entered if an allocation-free scope is practical
- use DrawList primitives efficiently
- minimize style push/pop spam inside large repeated table rows
- do not use reflection in draw paths
- keep component APIs composable

Do not prematurely micro-optimize readable code, but avoid known bad patterns.

The goal is a polished library suitable for a large plugin UI, not a screenshot-only experiment.


PHASE 7 — STANDALONE DALAMUD DEMO PLUGIN
========================================

Create a separate demo plugin INSIDE THIS REPOSITORY.

The demo is essential.

The prototype is not considered finished merely because the library builds.

The demo plugin should expose a command such as:

/crmaterial

and a showcase window.

It must reference CombatReborn.MaterialUI as a ProjectReference.

It must contain NO copied GBR code.


DEMO PAGE 1 — COMPONENT GALLERY
-------------------------------

Show and make interactive:

- seed/accent selector
- transparency control
- density selector
- filled button
- tonal button
- outlined button
- text button
- icon buttons
- switch
- checkbox example
- segmented controls with 2/3/4 options
- filter chips
- status pills
- search field
- text field
- progress bars
- cards/surfaces
- dialog
- disabled states
- hover/pressed behavior


DEMO PAGE 2 — COMPACT AUTOMATION OVERLAY
----------------------------------------

Create a fake automation overlay inspired by the target style.

Example conceptual content:

Automation
Auto (Big)
[ Auto | Manual | Off ]
[ settings ] [ close ]

Use a translucent compact window/card.

No game automation is required.

The controls should actually change demo state.


DEMO PAGE 3 — GBR-LIKE DATA SCREEN
----------------------------------

Create a FAKE gathering-data page using mock data.

It should demonstrate the important GBR combination:

MATERIAL CHROME + DENSE DATA

Example:

Search field
filter chips

Item             Level   Location       Status
Darksteel Ore    50      Coerthas       Available
Mock Item B      90      Thavnair       Upcoming
...

Use a native ImGui table styled through the new Material table API.

Include:
- sorting if straightforward
- selected row
- hover row
- semantic status colors distinct from Material accent colors

This screen is especially important.


DEMO PAGE 4 — CONTROL/ACTION DASHBOARD
--------------------------------------

Create a mock control panel with action tiles similar in density to an in-game automation controller.

Sections such as:

AUTOMATION
[ Start ] [ Pause ] [ Stop ]

NAVIGATION
[ Move ] [ Return ] [ Repair ]

STATUS
cards/pills/progress

This should demonstrate that MD3 can remain compact enough for Dalamud.


DEMO PAGE 5 — SETTINGS
----------------------

Show realistic plugin configuration patterns:

- switches
- combo-like selection
- segmented choice
- slider
- color seed selection
- text input
- disabled dependency setting
- nested card/section organization

This page should feel like something that could replace GBR's current settings UI.


VISUAL TARGET
=============

The demo should feel substantially closer to a purpose-designed desktop UI than stock ImGui.

Specifically aim for:

- rounded outer window/surface
- nearly black translucent base
- slightly lighter tonal cards
- coherent seed-derived accent colors
- subtle outlines
- selected pill states
- compact typography/layout
- clear hierarchy
- restrained separators
- consistent padding
- consistent control heights
- icon actions
- no stock-ImGui-blue appearance
- no giant mobile-sized controls


PHASE 8 — TESTS
===============

Unit-test all pure logic.

At minimum test:

Color conversion:
- sRGB ↔ linear
- OKLab/OKLCH conversion
- reasonable round-trip tolerance

Palette generation:
- deterministic output
- seed hue preservation within sensible tolerance
- tone ordering
- gamut handling
- all output components finite and valid

Theme generation:
- required semantic roles populated
- dark surface hierarchy ordered as expected
- text/content roles have reasonable luminance contrast

Geometry/math helpers:
- segmented bounds
- pill radius
- scaling math
where practical

Animation-state pruning logic if implemented independently of ImGui.

Avoid brittle screenshot/pixel tests for the first prototype.


OPTIONAL HEADLESS IMGUI TESTING
-------------------------------

If Dalamud's ImGui bindings allow safely creating a headless ImGui context without adding unreasonable complexity, test stack balance/basic widget execution.

If this becomes a rabbit hole, skip it.

Do not delay the functional prototype for headless rendering tests.


PHASE 9 — DOCUMENT FUTURE GBR INTEGRATION
=========================================

After the library/demo works, return to the read-only GBR audit and create:

docs/GBR_INTEGRATION_PLAN.md

This document describes FUTURE work only.

DO NOT implement it.

It should identify:

- first safe pilot area
- likely migration sequence
- which GBR windows should move first
- which existing tables should remain native
- where current VulcanUiStyle would eventually be replaced
- how existing ColorId semantics remain separate
- which custom GBR helpers become unnecessary
- which helpers remain valuable
- probable high-risk UI files
- likely controller/gamepad concerns
- expected migration strategy without a big-bang rewrite

A likely future order may be:

1. add library reference
2. opt one isolated window into Material theme
3. migrate common controls
4. migrate Vulcan
5. migrate main Interface shell/navigation
6. migrate legacy settings/config controls
7. migrate satellite windows
8. remove obsolete styling helpers

But base the final recommendation on your actual source audit.


PACKAGE / ARTIFACT OUTPUT
=========================

Produce normal Debug and Release builds.

The core result must include an ordinary reusable MaterialUI DLL.

If practical, also produce a local NuGet package in:

artifacts/

Do not publish it anywhere.

The demo plugin's compiled output must also remain inside this repository.

Provide exact paths in the final report.


DOCUMENTATION
=============

Final repository should contain at minimum:

README.md
AGENTS.md

docs/
    ARCHITECTURE.md
    DESIGN_SYSTEM.md
    COMPONENT_API.md
    GBR_BASELINE.md
    GBR_UI_REQUIREMENTS.md
    GBR_INTEGRATION_PLAN.md
    PROTOTYPE_SMOKE_TEST.md
    KNOWN_LIMITATIONS.md

AGENTS.md must preserve the rule that GBR is read-only and that future agents working in this repository may not modify GBR unless the owner explicitly changes that instruction.


BUILD DISCIPLINE
================

Compile continuously.

Do not make 50 unrelated edits and then discover the project stopped compiling hours ago.

Recommended checkpoints:

A. solution skeleton builds
B. color/theme tests pass
C. basic theme applies
D. button family works
E. segmented/switch works
F. card/search/chips work
G. navigation/tables work
H. demo plugin builds
I. release build passes
J. final verification passes

Fix warnings rather than ignoring them unless the warning is demonstrably unavoidable.


DO NOT GET STUCK ON POLISH BEFORE FUNCTION
==========================================

Priority order:

1. correct architecture
2. compiling library
3. working controls
4. working demo
5. coherent Material theme
6. polish/animation
7. secondary features

If a fancy component threatens the prototype schedule, implement a clean basic version, document the gap, and continue.

A complete coherent prototype is more valuable than one perfect switch and no library.


FINAL VERIFICATION
==================

Before declaring success:

1. Run all tests.
2. Run Debug build.
3. Run Release build.
4. Verify expected library DLL exists.
5. Verify demo DLL exists.
6. Verify no build artifacts escaped the new repo.
7. Re-read GBR state using read-only Git operations.
8. Compare GBR HEAD with baseline.
9. Compare GBR modified/untracked state with baseline.
10. Confirm no GBR files changed.
11. Review git diff in CombatReborn.MaterialUI.
12. Ensure README explains how a future plugin would reference the library.
13. Ensure no code dependency on GBR, ElliLib or RSR exists.


DEFINITION OF DONE
==================

Do not stop at "architecture created."

This task is DONE only when:

- CombatReborn.MaterialUI is a real local Git repository
- the reusable library compiles
- the test project passes
- the demo Dalamud plugin compiles
- the demo contains interactive examples of the core component set
- the demo includes a compact automation overlay
- the demo includes a dense GBR-like table screen
- seed-color theme generation works
- dark/translucent surfaces work
- segmented control works
- switch works
- all button variants work
- cards/surfaces work
- search field works
- navigation works
- action tile works
- table styling works
- documentation exists
- future GBR integration is documented but NOT implemented
- GBR is verified unchanged


FINAL REPORT
============

When finished, report:

1. Exact new repository path.
2. Current Git commit/hash if commits were made.
3. Solution/project tree.
4. MaterialUI architecture summary.
5. Components implemented.
6. Theme/color-generation approach.
7. Demo pages implemented.
8. Build result.
9. Test result.
10. Exact DLL/artifact paths.
11. Exact command needed to rebuild.
12. Manual instructions for loading the Demo DLL into Dalamud later.
13. Known limitations.
14. What you would improve in a second pass.
15. Proof that GBR remained unchanged.
16. A concise assessment of how suitable the library appears for eventual GBR migration.

Do not modify GBR as part of the final step.

Do not automatically begin GBR integration after the prototype succeeds.

Stop with the reusable library + demo + documentation complete.
--- END PROJECT MISSION ---
