# GBR UI requirements audit
Inspected read-only at baseline a204f2f45cb98302dbe938921255c3a6d617c775. All 53 Gui C# files were surveyed; see GBR_GUI_SURVEY.txt for per-file line counts and pattern frequencies (syntactic call-site estimates, not runtime frequency; wrappers and definitions can affect counts). Config files and the project were also inspected. No implementation uses GBR source.

## Migration matrix
| Existing pattern | Frequency / source locations | MaterialUI component | Difficulty | Behavior to retain |
|---|---|---|---|---|
| Primary start/stop/save/import buttons | Buttons across 40+ files; CollectablesWindow 10, CraftingListEditor 16, VendorsTab 32 matches | Filled / tonal / outlined / text buttons | Low–medium | State-dependent labels must keep stable IDs; command side effects stay in consumer |
| Small toolbar / icon / destructive / full-width buttons | Interface.Header:124/144; LocationsTab:358/366; MaterialsWindow:1283; VendorsTab:390; VendorBuyListWindow:191 | IconButton / WindowHeader | Medium | FontAwesome, disabled tooltips, modifier-gated deletion, map/teleport behavior |
| Boolean settings / flag filters | ConfigTab 112, ConfigPresetsTab 22, RecipesTab.Content 11 toggle matches | Switch for settings; Checkbox / FilterChip for dense lists | Low | Save only on change; retain config flags and dependent disables |
| Exclusive options | VendorBuyListWindow.TeamCraftImport:88/95; DebugTab:161/168; SettingsTab solver mode; RecipeCraftSettingsPopup override modes | SegmentedControl<T> / Radio | Medium | Distinguish Inherit, Specific, Off; disabled option support; do not turn multi-select flags into exclusive controls |
| Large / searchable combos | RecipeCraftSettingsPopup 29 combo matches, ConsumablesPopup 10; ConfigTab 10 | Native BeginCombo styled by theme + SearchField | Medium | Native scrolling, focus, preview labels, lazy data and localization |
| Search / text / multiline imports | VulcanWindow:200 inline-clear helper; Recipes, Solutions, Macros, Marketboard; TeamCraft imports up to 500000 chars | SearchField / TextField; native multiline remains | Low–medium | Native editing, buffers, hint, clear hit area without text overlap, copy/paste |
| Filter groups | ItemTab:201/215, FishFilter/ItemFilter flags; Recipes job icons and equipment filters | Chips + native combo / checkbox | Medium | Enable-all semantics, persisted filter bits, optional game icons |
| Data tables and clipping | ItemTable, FishTable, LocationTable, RecordTable, WeatherTable via ElliLib; MaterialsWindow:504; Vendors and buy list tables | MaterialTableStyle around existing tables | High | Sort/resizing/hiding columns, native clipper, frozen headers, dense number columns, per-cell colors |
| Split panes / children | CraftingListEditor:283 40/60 split; Recipes 3, Workshops 6 child matches; Vendor sidebar | Surface / Card scopes | Medium | BeginChild/EndChild even if clipped; positive vs available/negative dimensions; nested scrolling |
| Folder / list selectors | AutoGatherListSelector, CraftingListsTab, GatherGroupTab, AlarmTab | ListItem / navigation item, native drag/drop | High | Stable per-domain IDs, selection, folder expansion, drag/drop payloads, context actions |
| Popups / modals / editing windows | VendorBuyListWindow 18, CraftingListsTab.Popups 15 popup matches; RecipeCraftSettingsPopup uses a standalone Begin window | Dialog scope for real modals; native windows remain | Medium | OpenPopup ID stack and focus; do not force every editor into modal lifecycle |
| Tooltips / native item tooltips | UiHelpers, ImGuiEx, NativeItemTooltipBridge, CraftingRowIcons; widespread hover checks | Consumer-controlled after component | Medium | AllowWhenDisabled, rich game tooltip bridge, hover delays and Ctrl gating stay outside library |
| Progress / compact status | MaterialsWindow: percentage column; CraftingStatusWindow state/estimate; Record counts | LinearProgress / Badge | Low | Numeric data and semantic status, tiny table rows |
| Navigation / tabs | Interface.cs 13 Draw tabs; VulcanWindow.cs 10 tabs and controller bridge | Rail/item for app navigation; secondary tabs | High | Focus requests, tab ordering, custom bumper navigation, window input blocking |
| Window/header actions | Interface.Header alarm, time/weather; VulcanWindow.DrawHeader plugin toggles; status window pause/resume | WindowHeader + trailing actions | Medium | Current title/subtitle, dynamic availability; library owns no Window |
| Game item icons / action areas | Recipes job ImageButton, CraftingRowIcons, Workshops actions | ActionTile for major actions; native images in dense rows | Medium | Texture ownership remains plugin; no texture loads per frame |
| DrawList rendering | CraftingTreeWindow connectors/disclosures; StatsPage charts; search clear; Materials icons | Draw helpers / theme tokens only | High | Keep specialized trees/charts and hit testing; do not replace with generic cards |
| Scaling | Interface.Values, VulcanUiScaling, ImGuiHelpers.GlobalScale | MaterialMetrics scaled once per scope | Medium | Preserve zero/negative size sentinels; never double-scale |
| Disabled / click helpers | ImGuiEx.HoveredAndClicked release/drag/Ctrl; ImGuiUtil.DrawDisabledButton; 9 disables in VendorBuyListWindow | Native button behavior + disabled scopes | Medium | Disabled hover tooltips; do not inadvertently change release vs press behavior |
| Config persistence | Configuration, Defaults, CollectableConfig, all filter enums, TabSortOrder | Consumer responsibility | Low | Keep Save/SaveIfDirty, migrations and IDs in GBR |

## Chrome versus domain meaning
**A — design-system chrome:** VulcanUiStyle's PanelBackground, SurfaceBackground, hover/active/popup/header/border/table colors; hard-coded purple/blue launch buttons in Interface.Header:121–143; dark child fills in CraftingListEditor:293/303; selected folder/list treatment; separator and generic disabled appearance. These can become Material roles.

**B — domain colors:** preserve AvailableItem, UpcomingItem, DependentAvailableFish, DependentUpcomingFish, AvailableBait, CustomFishData, ChangedLocationBg, WarningBg; every FishTimer tug/uncaught/lure/marker distinction; WeatherTab current/previous cell/header colors; GatherWindowAvailable/Upcoming; warning/validation and owned/missing material states. UiHelpers.DrawTimeInterval selects colors from availability and dependencies. These are not accent roles.
HeaderEorzeaTime/HeaderNextHour/HeaderWeather, HighlightText, DisabledText and specialized overlay backgrounds are mixed/legacy configurable roles: audit individual uses and keep user overrides until explicitly migrated. Do not globally overwrite ColorId or map every green to Primary.

## High density and controller constraints
Retain Materials' 4/5/6-column Have/Ret/RNQ/RHQ/Need/% layout, native clipping and 36/46-unit numerical columns. Retain recipe/item/fish/location/weather/record/vendor/marketboard tables and queue lists. Group chrome around them, not around each row.

Vulcan tabs explicitly call ControllerSupport.TabNavigation.TabItem with indices and Update(GamepadState,10). Several windows report focused root/child windows to suppress game inputs; context menus have gamepad entry paths. MaterialUI must not own that bridge. Standard ImGui nav-capable items provide a baseline, but bumper selection and controller context menus need consumer integration and in-game acceptance testing.

## Detailed source observations
- CraftingListEditor contains presentation caches, queue planning and editing as well as split-pane UI; migrate draw seams without moving domain logic.
- CraftingMaterialsWindow renders category panels, retainer columns, currency totals and vendor/mob-drop actions. Palette cannot erase category and validation meaning.
- CraftingTreeWindow's recursive connectors, disclosure triangles and icon alignment remain specialized; surface tokens can improve the frame without rewriting the tree.
- CraftingStatusWindow has the smallest useful pilot: state text, pause/resume/stop and delay slider, plus focus registration.
- CollectablesWindow combines plugin availability, disabled dependencies, routes and buy-list preferences; strong second pilot for realistic settings.
- RecipeCraftSettingsPopup is a separate editor window with instance arbitration and nullable override semantics, not simply an OpenPopup wrapper.
- Interface's ElliLib table subclasses are a major migration seam. The Material library must never reference ElliLib; GBR may keep using it alongside scoped table styling.
- NativeItemTooltipBridge and CraftingRowIcons remain valuable adapters; generic style/search helpers are replacement candidates.
