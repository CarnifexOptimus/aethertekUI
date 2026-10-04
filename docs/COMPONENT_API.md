# Component API contract

## Adaptive sizing (v0.3)

See [Adaptive sizing and host compatibility](ADAPTIVE_SIZING.md) for units, ownership and upgrade behavior.

| API | Purpose |
|---|---|
| MaterialStyleMode.Compatible / Material / ColorsOnly | Preserve host layout by default; opt into full metrics or colors alone |
| MaterialControls.Push(context) | Match native and custom control heights using current font, density and scale |
| MaterialControlContext.Regular / Toolbar / Dense | Local sizing intent independent of theme density |
| MaterialControls.Push(metrics) | Explicit already-scaled pixel metrics; parameterless nested scopes inherit, explicit contexts remeasure |
| MaterialTheme(colors, density) | Exact immutable scheme with initialization-time role assignments; palette/seed constructors remain available |
| MaterialControlAppearance | Consumer fill, foreground, border, and nonnegative logical corner radius |
| MaterialAppearanceSelector.Draw | Swatch/presets/RGB picker plus keyed language dropdown; separate AccentChanged / LanguageChanged flags |
| MaterialTable.PushControls() | Native table styling plus Dense editor sizing |
| MaterialComboLayout | Independent popup width and visible row limit; optional native LeadingIcon in the field |
| MaterialActionTile.Measure / MaterialTileSizing | Content-aware wrapping and grid measurement; explicit fixed-height option |

All controls are immediate-mode; booleans return activation or value change. Consumers own state and lifecycle.

Appearance overloads of `MaterialButton.Draw`, `MaterialStatus.Badge`,
`MaterialTextField.SearchField`, and `MaterialCombo.Draw` retain native
interaction and existing IDs. Badge dimensions and the explicit
`MaterialSwitch.Draw(..., trackSize, ...)` overload use logical units. The new
search overload hides clear while empty; the old overload retains its slot.
The combo paints one outlined field/chevron and keeps native popup editing,
navigation and selection. `MaterialCanvas.Surface(min,max,top,bottom,radius)`
draws a rounded vertical gradient; coordinates and radius remain pixels.

`MaterialAppearanceSelector.Draw(id, ref accent, ref language, options, labels,
languageWidth)` borrows the active font. Preset selection reports immediately;
custom RGB reports on edit completion. Consumers own the draft, localized
labels/native language options, semantic colours, configuration/save path and
next-frame theme/font changes. Palette and Globe icons are appended to the
existing enum. Map, Clock, Link, Calculator, Person, Shield, Monitor and Location are also appended for area,
elapsed-time, dependency, calculation, identity, privacy, display and saved-position controls; all use native meshes without
font or texture ownership. Send appends a paper-plane icon for message composers
without changing existing enum values. Save, Copy, Heart and List append native
meshes for saving, copying, support and ordered-list affordances; consumers keep
the action, stable control ID and localized tooltip.
Sword (crossed blades) and Star append native combat/action and rule-priority artwork, preserving
all prior enum values. Their meshes need no font or texture resources.
Folder and Egg append native workspace/preset and breeding artwork for ChokeAbo
with the same ownership and stable-identity rules.
Wrench, DoorExit, Cube, Chest, Cart, Chat, Group, Warning and Plus supply ADS's
approved action/status artwork. Pulse, Utensils and Crystal supply LootGoblin's
summaries and Alexandrite action. Calendar, Crown, Book, Sparkle, Bell and Terminal
supply DAD's schedule, guide and command artwork. These append after Egg, preserving
every earlier ordinal, and retain the same font-free drawing and identity rules.
Power appends at ordinal 62 for FrenRider's account-wide enable action; consumers
retain their existing callbacks, state and native control identity.
Target appends at ordinal 63 for LootGoblin's Current Run crosshair; the existing
Location pin and every preceding mesh and ordinal remain available.
See the sizing showcase and independent consumer example.
For an established native language control, use
`MaterialAppearanceSelector.Draw(id, ref accent, labels, drawLanguage)`.
The callback runs after the selector's ID scope is restored and returns whether
the language changed. The standard selector preserves the measured selected-label,
arrow and padding minimum at narrow widths; consumers reflow the whole header group.
The consumer keeps its original native Combo label,
option ordering/index IDs, localization and disabled state. Accent selection
and change flags retain the same behavior.
The standard language field contains its Globe mesh. When an approved header
places existing actions between colour and language, call
`DrawAccent(id, ref accent, labels)` and
`DrawLanguage(id, ref language, options, languageWidth)` using the same ID.
Both retain the combined selector's native identities and change semantics;
the consumer still owns placement and saving. `MaterialComboLayout.LeadingIcon`
uses the current control metrics, and `MaterialIcons.Draw(icon, drawList, ...)`
borrows the field's draw list so its mesh stays in the field when a popup opens.
Use MaterialTheme.Push(theme, ImGuiHelpers.GlobalScale) around your window draw. Pass stable IDs separately from visible labels. Scope IDs further with ImGui.PushID for repeated domain objects.
Button variants share label, leading/trailing MaterialIcon, size and disabled support. Negative width fills available width; positive sizes are logical units; zero selects defaults.
`MaterialButton.Draw(id, label, cornerRadius, ...)` and `MaterialStatus.Badge(label, cornerRadius, ...)` accept an explicit nonnegative logical radius, clamped to the rendered bounds. Existing overloads keep their pill shapes. Document, Database, Refresh, Chart, ExternalLink and Download extend the texture-free icon set without changing prior enum values.
MaterialSegment<T> arrays are cached, passed as ReadOnlySpan, and bind ref T. Switch binds ref bool and animates by stable ID.
Surface/Card and Dialog scopes expose Visible. Always dispose child scopes even when Visible is false; only render child contents when visible. Dialog scopes call EndPopup only on a successful BeginPopupModal.
Tables remain native BeginTable/EndTable with a MaterialTable.PushStyle scope and optional row coloring. Native combo and slider behavior inherits the theme. Checkbox/radio helpers use stable native button IDs and custom visuals.

## Public families
| Type | Entry points | State / result |
|---|---|---|
| MaterialTheme | constructor(seed,density,generator), Push(theme,globalScale), Colors, SurfaceOpacity, Density, Motion | Cached immutable RGB scheme; opacity/density mutable |
| MaterialButton | FilledButton, FilledTonalButton, OutlinedButton, TextButton, Draw, IconButton | Activation bool; stable id separate from label |
| MaterialSegmented | Draw<T>(id, segments, ref value, width, disabled) | Value changed bool; options include Disabled and Icon |
| MaterialSwitch | Draw(id,label,ref value,disabled) | Changed bool; thumb interpolation |
| MaterialSelection | Checkbox; Radio<T> | Changed bool; caller owns setting |
| MaterialSurface | Begin; Card; ClickableCard; Color(elevation) | Ref-struct child scope / activation bool |
| MaterialChip | FilterChip; ActionChip | Ref bool selection / activation |
| MaterialTextField | SearchField; TextField | Changed bool; native string editing, flags, maxLength |
| MaterialNavigation | Rail; Item; ListItem | Child scope / activation |
| MaterialTabs | Begin; Item | Native tab scopes with Visible |
| MaterialActionTile | Draw | Activation, active/disabled flags, optional subtitle |
| MaterialStatus | Badge; LinearProgress; SectionHeader | Display only; optional consumer colors |
| MaterialDialog | Begin(id,ref open,flags) | Modal scope with Visible |
| MaterialWindowHeader | Draw(title,subtitle,actions,compact) | Activated action index or -1 |
| MaterialTable | PushStyle(alternateRows); RowState(selected,hovered,semanticBackground) | Style-only scope; native tables remain caller-owned |
| MaterialIcons | Draw | Texture-free vector drawing; caller can use native images separately |

## Child scopes and composition
```csharp
using (var card = MaterialSurface.Card("card", new Vector2(-1, 120)))
{
    if (card.Visible)
    {
        MaterialStatus.SectionHeader("QUEUE");
        MaterialStatus.Badge("Running", domainRunningColor);
        MaterialStatus.LinearProgress(completion);
    }
}
```
Child scopes always call EndChild, even when BeginChild returns false. A clickable card is a separate single-action control; use an ordinary Card for interactive child contents. Surface padding and radius overrides are logical units. All zero/negative child sizes retain native semantics.

## Native table preservation
```csharp
using var tableStyle = MaterialTable.PushStyle();
if (ImGui.BeginTable("items", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
{
    // Column setup, sorting and clipping stay in the consumer.
    ImGui.TableNextRow();
    ImGui.TableNextColumn();
    if (ImGui.Selectable(itemName, selected, ImGuiSelectableFlags.SpanAllColumns))
        selected = true;
    MaterialTable.RowState(selected, ImGui.IsItemHovered());
    // Render remaining cells and consumer-owned semantic colors.
    ImGui.EndTable();
}
```
Apply the table scope once around the table, not per row. RowState may take a semantic background; otherwise do not call it on rows with domain-owned background colors.

## Modal lifecycle
```csharp
if (MaterialButton.FilledButton("open", "Open"))
{
    open = true;
    ImGui.OpenPopup("Confirm");
}
using var dialog = MaterialDialog.Begin("Confirm", ref open);
if (dialog.Visible)
{
    if (MaterialButton.FilledButton("confirm", "Confirm"))
    {
        ImGui.CloseCurrentPopup();
        open = false;
    }
}
```
OpenPopup and Begin must run at the same ImGui ID-stack level. Scope disposal handles only EndPopup; the consumer controls close/save/cancel.

## IDs, input and clipping
Use stable domain IDs for repeated items. MaterialSegment.Key supplies identity across reordering; omitted keys retain positional IDs for fixed lists. Labels in custom controls are display text and can change without changing the hit ID. Tabs follow native ImGui label/### conventions.
Standard keyboard/gamepad navigation requires the host's native navigation configuration. The library does not set it. Segments are separately focusable native button items; a full radio-group arrow/bumper model is future work.
Native inputs own text editing and flags, including ReadOnly. Search reserves a trailing clear slot rather than drawing over editable text. Practical minimum width is about 120 logical units. After SearchField, the last item is the field group; tooltips can be attached to it. Button/switch/tile calls preserve their hit item for native consumer tooltips/context menus.

## Lifetime and performance
Create themes and segment/action arrays on configuration/model changes, not each frame. A theme caches all palette colors; modifying SurfaceOpacity does not regenerate RGB. Use theme.Motion.Enabled=false for reduced motion.
Every style/child/tab/dialog scope is a ref struct. Do not copy, box, store or dispose them out of order. All calls belong on the owning ImGui UI thread. Native draw-list primitives are clipped by the host.

## Integration additions (v0.2)
| Type | Contract |
|---|---|
| MaterialThemeSettings / Controller | Serializable immutable settings; Apply validates, skips unchanged settings, regenerates only for RGB changes |
| MaterialWindowHost (optional adapter assembly) | Draw(WindowSystem,applyMetrics) or BeginFrame(applyMetrics); no subscriptions or config writes |
| MaterialTheme.Push(...,applyMetrics:false) | Native colors only; preserves existing ImGui geometry styles |
| MaterialOptions<T> / MaterialCombo.Draw | Cached keyed options, filtered on SetQuery/Replace; disabled options, native editing/selection |
| MaterialInput | Integer, Number, Slider, Multiline; stable IDs, native flags, logical size, disabled and bounds |
| MaterialLayout.FitNextItemWidth | Native pixel widths; moves an exhausted row and preserves the measured readable minimum. Negative fills; zero uses the minimum; consumer owns narrow-pane scrolling. |
| MaterialOverride.Switch | Nullable bool with Inherit/On/Off |
| MaterialFlags | Explicit ulong masks; preserves unrelated bits; Draw, IsSet and Set |
| MaterialTree.Node / MaterialContextMenu.Begin | Native scopes; Visible gates contents; lexical disposal |
| MaterialDragDrop.Source / TryAccept<T> | Copies unmanaged ID payload, validates type/size and waits for delivery |
| MaterialSplitPane.Divider | Host-owned first panel width, logical dimensions, mouse drag and clamps |
| MaterialDataView<T> | Explicit Rebuild(source,filter,comparer); old view retained if callback throws |
| MaterialSelectionSet<TKey> | Selection survives filtering and sorting; explicit prune on object deletion |
| MaterialClipper | Native uniform-row clipper; Step then Start..End, optional Include before first Step |
| MaterialSearchState | Optional FocusRequested, restores text focus following clear |
| MaterialSemantic | Text/Colors scopes preserve supplied colors; Feedback and Tooltip |
| MaterialCanvas | Public pixel-space Color/Surface; borrowed Image/ImageButton/GlyphButton with logical control sizes |
| MaterialNavigationMath.NextEnabled | Pure host-routed traversal; skips disabled options, optional wrap; no global input |

Options/models must be owned per control or deliberately shared; sharing a MaterialOptions instance also shares its query. Payload type names must distinguish incompatible layouts even when their unmanaged sizes match. Keep native table ID/column settings and host texture lifetimes. Full usage is compiled in samples/ConsumerContract; see INTEGRATION_GUIDE.md.
