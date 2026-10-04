# Adaptive sizing and host compatibility — v0.3

The library supplies a common sizing policy for any plugin. It cannot infer a plugin's column priorities, pane widths or application semantics. Choose a local control context instead of forcing one physical control size onto every screen.

## Theme ownership

`MaterialTheme.Push(theme, globalScale)` now defaults to `MaterialStyleMode.Compatible`: theme colors and conservative rounding, while preserving the host's padding, spacing and border sizes. The existing boolean overload remains available; `applyMetrics: false` selects `ColorsOnly`. The optional Dalamud adapter exposes the same choice through `MaterialWindowHost.StyleMode`.

New windows that want the complete Material layout can explicitly use:

```csharp
using var themeScope = MaterialTheme.Push(theme, globalScale, MaterialStyleMode.Material);
```

Apply the scope around the window's Begin/End when changing window appearance. All scopes are lexical, UI-thread/context-bound and disposed in reverse order. Do not copy them or switch ImGui contexts inside them.

## Local control contexts

Density (Compact/Standard) is a theme preference. Context (Regular/Toolbar/Dense) describes the space available for a particular group of controls. Heights account for the current font as well as global scale, so large fonts do not get forced into smaller fixed frames.

```csharp
using (MaterialControls.Push(MaterialControlContext.Toolbar))
{
    MaterialButton.FilledTonalButton("run", "Run");
    ImGui.SameLine();
    ImGui.Button("Native action");
}
```

The scope aligns native frame padding with Material controls. Native field padding is intentionally smaller horizontally than text-button padding. Enter a new scope after changing the font; do not expect a previously entered native style scope to remeasure itself. Control metrics are measured without regenerating color palettes.

For an approved measured design, `MaterialControls.Push(MaterialControlMetrics)`
accepts explicit pixel values already scaled once. Parameterless nested control
scopes inherit those values. An explicit Regular/Toolbar/Dense scope remeasures
for that context; nested theme scopes use their own metrics. Disposal restores
the previous values and native styles. Logical component sizes/radii remain
separate from these pixel metrics.

For editable tables use `MaterialTable.PushControls()` once around the table. It combines the table colors with Dense control metrics. `PushStyle()` remains available for existing tables that only want table colors/cell padding. Both preserve native table sorting, resizing, clipping and column behavior.

## Fields and dropdowns

Input widths fit the available region, including positive requested widths. Negative width fills the region; zero uses the component's natural width. Positive sizes are logical units, scaled once. Measurement results such as `MaterialActionTile.Measure` and native ImGui coordinates are pixels. Explicit positive button dimensions retain caller sizing.

Natural and fill-width buttons preserve their measured single-line labels and move off an exhausted row before sizing. Native consumer wrappers can call `MaterialLayout.FitNextItemWidth(requestedPixels, minimumPixels)` with a measured readable minimum before `SetNextItemWidth` or `Button`. Both arguments and the result are pixels. This opt-in helper preserves that minimum even when the pane is narrower; the consumer then supplies horizontal scrolling or a wider pane. It does not change native IDs or configuration.

Dropdown field width and popup width are independent:

```csharp
MaterialCombo.Draw("destination", options, ref selected,
    new MaterialComboLayout(PopupWidth: 340, VisibleRows: 8), width: -1);
```

Use `new MaterialComboLayout()` or `MaterialComboLayout.Default` for defaults. A zero popup width chooses at least 200 logical units, while respecting viewport width. VisibleRows limits popup height. `ConstrainFieldWidth: false` explicitly permits the field to exceed the region. Search and native option navigation remain available.

Integer fields suppress their minus/plus buttons when those buttons would leave insufficient room for the current value. Text editing remains available; the buttons return when there is enough width. A zero step always suppresses them. The value measurement uses a stack buffer, without allocating a formatted string every frame.

## Icons and tiles

Vector and borrowed-font icon buttons fit their artwork inside the hit rectangle, including very small explicit buttons. The consumer owns borrowed font/texture lifetimes. `MaterialCanvas.Surface` accepts pixel coordinates; Image, ImageButton and GlyphButton accept logical sizes.

Action tiles wrap their primary/secondary labels and treat requested height as a minimum. `MaterialActionTile.Measure` returns their content-aware pixel size for grid layout. Use the overload with `MaterialTileSizing.Fixed` when a fixed height and clipped content are intentional. Compact button/navigation labels still clip rather than wrap.

## Upgrading a consumer

Existing entry-point signatures remain available, but default theme padding and tile sizing behavior change. Rebuild consumers against v0.3. Use Compatible for incremental adoption, local control scopes for mixed native/Material rows, and Material for newly designed windows. Plugins retain responsibility for column widths, pane proportions, label/tooltips, semantic colors and explicit local style overrides.

The Sizing lab demo page compares all three contexts, native and Material controls, narrow editors, wide popup menus and wrapping tiles. Tests cover Compact/Standard, 100/125/150 percent geometry scales and two font scales. CPU previews are diagnostic native ImGui renders, not in-game verification. This pass does not modify, build, or deploy to GBR.
