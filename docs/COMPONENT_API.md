# Component API contract
All controls are immediate-mode; booleans return activation or value change. Consumers own state and lifecycle.
Use MaterialTheme.Push(theme, ImGuiHelpers.GlobalScale) around your window draw. Pass stable IDs separately from visible labels. Scope IDs further with ImGui.PushID for repeated domain objects.
Button variants share label, leading/trailing MaterialIcon, size and disabled support. Negative width fills available width; positive sizes are logical units; zero selects defaults.
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
Use stable domain IDs for repeated items. Keep segment order stable while interacting; segment indices form part of IDs. Labels in custom controls are display text and can change without changing the hit ID. Tabs follow native ImGui label/### conventions.
Standard keyboard/gamepad navigation requires the host's native navigation configuration. The library does not set it. Segments are separately focusable native button items; a full radio-group arrow/bumper model is future work.
Native inputs own text editing and flags, including ReadOnly. Search reserves a trailing clear slot rather than drawing over editable text. Practical minimum width is about 120 logical units. After SearchField, the last item is the field group; tooltips can be attached to it. Button/switch/tile calls preserve their hit item for native consumer tooltips/context menus.

## Lifetime and performance
Create themes and segment/action arrays on configuration/model changes, not each frame. A theme caches all palette colors; modifying SurfaceOpacity does not regenerate RGB. Use theme.Motion.Enabled=false for reduced motion.
Every style/child/tab/dialog scope is a ref struct. Do not copy, box, store or dispose them out of order. All calls belong on the owning ImGui UI thread. Native draw-list primitives are clipped by the host.
