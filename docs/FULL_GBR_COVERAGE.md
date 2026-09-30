# Whole-plugin integration contract

The continuation expands the library to support GBR's entire UI, without editing or wiring GBR. A reference alone cannot migrate custom layouts. Readiness means compiled reusable APIs, a compiled host example, native behavior tests, and explicit retained responsibilities.

## Audited surfaces
The original 53-file Gui survey remains applicable. Additional UI exists in AutoGather/AutoGather.Ui.cs, GatherHelper/GatherWindow.Ui.cs, FishTimer/FishTimerWindow.cs and FishTimerWindow.FishCache.cs, and Spearfishing/SpearfishingHelper.Ui.cs. GatherBuddy.cs owns WindowSystem, draw subscription and controller registrations. These stay consumer-owned.

| Consumer patterns | Integration support | Retained behavior |
|---|---|---|
| Main Interface shell and 13 tabs; Vulcan shell and 10 tabs | Theme host, navigation, secondary tabs, surfaces, scoped native styling | Window IDs, visibility, actions and controller registrations |
| Config tabs, presets and recipe overrides | Native combo/numeric/multiline wrappers; nullable/inherited choice through typed options | Configuration types, validation, persistence and inheritance rules |
| Gathering/crafting/vendor/collectable tables | Native table styling, bounded clipper, cached filter/sort view, stable row IDs | Domain columns, item resolution, native sorting specs, actions and texture lifetimes |
| Lists/folders and recipe tree | Native tree, context menu and drag/drop scopes | Hierarchy, operation permissions and drop interpretation |
| Destructive actions/import dialogs | Native popup/modal scopes, disabled controls, multiline editing, feedback | Confirmation policy, modifier requirements and parsing |
| Fish timer | Color-only theme scope, public drawing helpers, semantic overrides | Time chart, bait/tug colors, hatching, timing and game data |
| Gather overlay | Theme scope or colors-only profile; native table support | Anchoring, uptime colors, hotkeys, draw conditions |
| Spearfishing overlay | Optional colors-only styling and borrowed texture support | Game-addon coordinates/scaling, hit geometry, speed/size meaning |
| FontAwesome and item images | Borrowed font glyph and texture helpers | Font/texture acquisition, disposal and frame validity |
| Controller routes and blocking windows | Stable IDs, native navigation, explicit selection traversal helper | Existing controller bridge, focus routing, block registration and game input ownership |

## Boundaries
No dependency on GBR, ElliLib or RSR. Domain ColorId values must be supplied unchanged by the consumer. The host adapter never subscribes to events, changes ImGui IO configuration, owns windows or writes configuration. Use the colors-only profile for code that already computes game-anchored geometry. Native ImGui remains supported under the theme for specialized existing widgets.
