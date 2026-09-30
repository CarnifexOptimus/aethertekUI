# Architecture
A normal net10.0-windows x64 assembly built with Microsoft.NET.Sdk. The only direct runtime reference is Dalamud.Bindings.ImGui, resolved from the installed distribution via DalamudLibPath; it is not copied into consumer builds. No plugin lifecycle or game data is inside the library.
The demo uses Dalamud.NET.Sdk/15.0.0 (verified in GBR) and a normal ProjectReference. Its lifecycle owns command registration and UI callbacks.

Core contains pure OKLab conversion, replaceable IPaletteGenerator, cached immutable semantic color schemes, density metrics and bounded motion state. Drawing contains public-API geometry and drawing utilities. Components are small static families plus stack-only scopes.

MaterialTheme.Push(theme, globalScale) installs a thread-local current theme and scaled metrics and pushes owned ImGui styles. It must surround Begin/End to style outer windows. It restores the prior theme and ImGui stacks in reverse order. Consumer supplies ImGuiHelpers.GlobalScale, avoiding a library dependency on Dalamud services. Nest scopes in lexical LIFO order; never copy/dispose scopes twice.

Controls use public native ImGui buttons for IDs, nav, clipping and disabled behavior; custom visuals are drawn inside the item bounds. Native inputs handle all editing. Native table/popups/child lifecycle is retained. Strings/segment arrays should be cached by consumers. No textures, reflection or LINQ in library draw paths.

Each theme owns a bounded MaterialMotion cache. Frame time and frame number come from ImGui. Stale state is swept periodically; capacity has a hard bound. UI thread only; no global lifecycle subscription. Changing the seed creates a theme only on configuration changes; changing opacity does not regenerate RGB palettes.
