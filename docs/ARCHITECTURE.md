# Architecture
Version 0.3 separates theme ownership from local control geometry. Compatible theme scopes preserve host layout by default; Material opts into complete layout metrics. MaterialControls measures the current font and exposes Regular/Toolbar/Dense contexts through balanced value-type scopes. Native and custom controls share row heights without a plugin-specific layout engine. See [adaptive sizing](ADAPTIVE_SIZING.md).

Version 0.2 adds a separately packaged CombatReborn.MaterialUI.Dalamud adapter and a compiled independent ConsumerContract sample. Core still has no game/plugin dependency. The adapter wraps WindowSystem.Draw or returns a frame scope without subscribing to events. ThemeController owns validated settings and regenerates only on seed changes; persistence remains in the consumer. See INTEGRATION_GUIDE.md.

Data contains explicit-invalidation view/filter/sort caches, keyed options and keyed selection. There is no background task or global model cache. Native wrapper components add multiline/numeric editors, combos, flags/inheritance, tree/context/drag-drop and clipping. Borrowed texture/font helpers never own resources. Colors-only scoping preserves native geometry styles for game-anchored overlays.

A normal net10.0-windows x64 assembly built with Microsoft.NET.Sdk. The only direct runtime reference is Dalamud.Bindings.ImGui, resolved from the installed distribution via DalamudLibPath; it is not copied into consumer builds. No plugin lifecycle or game data is inside the library.
The demo uses Dalamud.NET.Sdk/15.0.0 (verified in GBR) and a normal ProjectReference. Its lifecycle owns command registration and UI callbacks.

Core contains pure OKLab conversion, replaceable IPaletteGenerator, cached immutable semantic color schemes, density metrics and bounded motion state. Drawing contains public-API geometry and drawing utilities. Components are small static families plus stack-only scopes.

MaterialTheme.Push(theme, globalScale) installs a thread-local current theme and scaled metrics and pushes owned ImGui styles. It must surround Begin/End to style outer windows. It restores the prior theme and ImGui stacks in reverse order. Consumer supplies ImGuiHelpers.GlobalScale, avoiding a library dependency on Dalamud services. Nest scopes in lexical LIFO order; never copy/dispose scopes twice.

Controls use public native ImGui buttons for IDs, nav, clipping and disabled behavior; custom visuals are drawn inside the item bounds. Native inputs handle all editing. Native table/popups/child lifecycle is retained. Strings/segment arrays should be cached by consumers. No textures, reflection or LINQ in library draw paths.

Each theme owns a bounded MaterialMotion cache. Frame time and frame number come from ImGui. Stale state is swept periodically; capacity has a hard bound. UI thread only; no global lifecycle subscription. Changing the seed creates a theme only on configuration changes; changing opacity does not regenerate RGB palettes.
