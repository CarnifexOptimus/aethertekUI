# Future GBR integration — not implemented
This is a proposal based on the recorded local audit, not authorization to modify GBR. AGENTS.md and PROJECT_MISSION.md keep GBR read-only until the owner explicitly changes the instruction.

## First pilot
Start with CraftingStatusWindow. Its roughly 341 lines contain a small visible UI: current state, queue progress/estimate, pause/resume/stop, action-delay slider and navigation to Vulcan. Domain operations remain untouched. Keep queue state colors and pause reasons explicit. Preserve ControllerSupport.UpdateFocusedWindow on focus changes.

This is lower risk than replacing Interface's tab shell or the 2,239-line CraftingListEditor. CollectablesWindow is a good second pilot because its 450 lines exercise disabled dependencies, settings and route combos without replacing a major data browser.

## Incremental sequence
1. Add the library reference in a separately authorized GBR change; keep the assembly next to the consuming plugin. Cache one theme from seed/density settings.
2. Opt the status pilot into scoped theme styling. To style outer native windows, wrap the owning WindowSystem.Draw callback or use an explicitly paired consumer window-style adapter; calling Push only inside Window.Draw styles content after the outer Begin has already happened.
3. Migrate the pilot's common buttons, switch, badges and progress. Add an opt-out while validating real fonts, scaling, focus and gamepad activation.
4. Migrate CollectablesWindow and a small subset of Vulcan settings. Use native searchable combos for routes, large food/macro selections and settings with nullable inheritance.
5. Apply Material table styling to one CraftingMaterialsWindow panel while preserving its native table/clipper, frozen header, category accents and currency actions. Do not remove the ElliLib table infrastructure merely to add Material.
6. Migrate Vulcan's container, toolbar and secondary tab styles. Replace VulcanUiStyle at the owning scope; keep existing controller TabNavigation bindings until an explicit navigation redesign is tested. Migrate recipe filters and list selectors gradually.
7. Migrate Interface.Header and app-level navigation only after documenting existing 13-tab order, focus requests, alarms and weather/time semantics. Rail navigation may need an overflow group in GBR; the prototype is not optimized for its exact tab count.
8. Migrate legacy ConfigTab/ConfigPresetsTab settings in sections. Keep multi-bit filters as checkboxes/chips, not segmented exclusive controls.
9. Migrate remaining satellite windows: VendorBuyListWindow, RecipeCraftSettingsPopup, CraftingListConsumablesPopup. Preserve their independent window/popup lifecycles and save/cancel semantics.
10. Remove only unused generic styling/search helpers. Keep specialized rendering and domain adapters.

## Native tables to preserve
Item, fish, location, weather, record, recipe, vendor, marketboard, material inventory, preset checks and debug tables remain native/consumer-owned. Preserve column widths/order/hiding/sort persistence, multi-row headers, clipper ownership and context menus. MaterialTable.PushStyle can wrap an existing table draw call. RowState is optional and must not overwrite semantic warning/current-weather backgrounds; pass or retain the semantic background explicitly.

## Color migration
Never map ColorId by apparent hue. Availability green, upcoming yellow, dependent-fish colors, tug strength, custom locations, weather/current-previous distinctions, bait and warning colors retain their stored values. Material roles style chrome. Mixed legacy roles (HeaderEorzeaTime, HeaderWeather, DisabledText, HighlightText, overlay backgrounds) need use-site decisions and backwards-compatible config handling. Retain textual status in addition to color.

## Replaceable versus retained helpers
Replace candidates: VulcanUiStyle's fixed palette and allocated scope; generic inline-clear search from VulcanWindow; repeated icon-button centering; common panel border/padding constants; generic button hover colors. VulcanUiScaling can eventually delegate to shared metrics but must retain zero/negative dimension conventions during migration.
Retain: NativeItemTooltipBridge, CraftingRowIcons' game texture/meaning mapping, UiHelpers uptime/location/domain formatting, data/presentation caches, ElliLib selectors/table models, clipboard import parsers, config persistence and all controller/game-input blocking.

## High risk
CraftingListEditor (queue planning intertwined with draw state); RecipesTab.Content/Models (filtering, context menus and focus); Interface.ItemTab/FishTab (custom columns); CraftingTreeWindow (recursive hit regions/connectors); MaterialsWindow (dynamic retainer columns and domain categories); RecipeCraftSettingsPopup (nullable inherited settings and instance arbitration).

## Controller acceptance criteria
Keep bumper tab order and programmatic focus, context-menu activation and root/child focused-window reporting. Test disabled controls, popup focus return, search clear focus, long scrolling tables and D-pad traversal. Standard ImGui button navigation is covered by the prototype; GBR's custom controller bridge is not. Do not enable global ImGui navigation flags from the library.

The prototype is suitable for a limited opt-in pilot. Broad migration depends on in-game visual/input validation and retaining the existing domain/table behaviors.
