# Known limitations — v0.2

- Dark mode only; OKLCH-generated roles are MD3-inspired, not exact Google HCT or a certification claim.
- Core, optional Dalamud adapter, demo and independent consumer compile. The consumer also builds from freshly packed local packages. No plugin has been installed or loaded into FFXIV; GPU compositing, real fonts, unload/reload and GBR controller routing remain manual acceptance.
- All seven actual demo pages render through the native ImGui binding. Diagnostic CPU mesh previews use the default font; they are not in-game screenshots.
- Existing fish-timer charts, spearfishing addon coordinates, native item tooltips, domain formatting and controller bridge remain consumer code. The kit supplies scoped colors, native compatibility and borrowed texture/font helpers, not game services.
- Native navigation remains host-configured. NextEnabled supports explicit routed selection events; there is no global input manager or complete accessibility/screen-reader layer.
- Long labels clip within controls. Exhaustive localization and unusual fonts are not covered. Search suppresses decorations at very narrow widths; clear focus restoration requires an optional host-owned MaterialSearchState.
- Fixed segment lists can use index IDs. Reordered lists must provide stable unique Key values. Options require unique string keys. Repeated domain rows need caller PushID keys.
- Clipping requires uniformly spaced rows. Variable-height trees retain consumer traversal. Sorting/filter predicates and invalidation policy remain consumer-owned. The large catalog demonstrates single-column native sorting; existing native multi-sort specs remain available.
- Borrowed textures/fonts must stay alive through render submission. The library does not create or dispose host resources. Drag payloads support small unmanaged IDs only; type names must distinguish layouts of the same size.
- Split panes support mouse dragging; keyboard resize shortcuts are not implemented. Existing gamepad panel focus can stay in the consumer.
- ClickableCard is a single action; ordinary Card scopes can hold arbitrary interactive children without an overlapping card hit target.
- Scopes are UI-thread/context-bound and lexical LIFO. Do not copy scopes, mix contexts mid-scope, or reuse one theme across separate ImGui contexts.
- The bounded motion cache has 4096 entries and stale-frame pruning. No ripple physics or shared-element animation.
- Demo settings are memory-only. Production settings can serialize MaterialThemeSettings using the consumer's persistence service. No configuration files or game actions are owned by the libraries.
- API 15 / .NET 10 / Windows x64 is the validated combination. v0.2 is still a pre-1.0 API; rebuild consumers when upgrading.
- Filesystem evidence covers MSBuild manifests and configured cache/temp paths, plus GBR HEAD/status/index/content hashes. It is not a forensic system-wide filesystem monitor.

## Next pass

After explicitly authorized wiring: review actual in-game typography/translucency, controller and popup focus flow, retained overlay coordinates, resource lifetimes and performance with real data. Then refine long-label handling, keyboard panel resizing, accessibility and API stabilization. Light mode or exact HCT should follow demonstrated consumer needs.
