# Known limitations
- Dark mode only; OKLCH-inspired roles, not exact Google HCT/Material dynamic color. There is no claim of Material certification.
- The plugin compiles and its actual pages render in a native headless ImGui context. It has not been installed or loaded into the game. Dalamud injection/lifecycle, gamepad bridge and visual compositing over real game scenes still need manual acceptance.
- Software previews use Dear ImGui's default font and a diagnostic CPU renderer. They validate layout for review; they are not screenshots from FFXIV or a production GPU backend.
- Compact is the primary target. Both scale and density are supported, but exhaustive localization, unusual host fonts and tiny window widths are not covered. Very long labels clip; no ellipsis/wrapping engine for controls. Give search fields practical width (at least about 120 logical units).
- Vector icon set is intentionally small and texture-free. Custom game textures and arbitrary glyph/font icons remain consumer rendering work.
- Navigation items use native button activation. Arrow/bumper group semantics and screen-reader metadata are not a complete accessibility layer. The library does not change host navigation flags.
- Segment IDs use stable indices inside the caller's ID. Keep the option order stable while a control is active; rebuild/re-ID deliberately when reordering. All scopes must remain lexical LIFO and must not be copied.
- ClickableCard is a separate single-action card. Surface/Card scopes can contain arbitrary native children, but do not intercept clicks across interactive descendants.
- Search clear does not automatically return keyboard focus to the edit after clearing. Native multiline editing remains a consumer call under the theme.
- Dense table APIs style native tables and selection; consumers still own sort/filter/virtualization logic. Demo sorting is single-column on 18 mock rows.
- Animation is a small exponential fade/position cache (4096-entry cap, 180 stale frames, sweep every 60 frames). No ripple, spring model, cross-window shared-element animation or animated sliding segment geometry.
- Theme scope is UI-thread/context bound. Use a separate cached theme per ImGui context and do not switch contexts while a scope is active. Library version 0.1 is not a frozen API.
- Demo settings are in memory only. No game actions, configuration files, textures or plugin installation are performed.
- Windows x64 / net10.0-windows / installed Dalamud API 15 is the validated configuration. The local package expects the consuming Dalamud SDK to provide its bindings. Native tests require that distribution's cimgui.dll and HexaGen.Runtime.dll.
- Output-boundary verification checks MSBuild output manifests and process cache/temp paths. It is not a forensic system-wide filesystem monitor. GBR is additionally verified with HEAD/status/index/content hashes.

## Second pass
In-game font/contrast review; real gamepad and context-menu acceptance; long-label/localization behavior; more flexible icon and content slots; keyboard focus return after clearing; table benchmarks with thousands of rows; optional light mode; refined motion and accessibility. Consider exact HCT only if a consumer needs it.
