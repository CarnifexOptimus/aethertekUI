# Prototype smoke test
## Automated validation
Run from the repository:
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\Build.ps1 -Previews
```
Build.ps1 enters the existing repository-local environment, builds the renamed
solution once in Debug/x64, runs the existing test project, and checks the
independent project-reference consumer. Logs and TRX results are in artifacts/.
Release/package generation and legacy GBR tooling are outside this route.

Tests cover color/theme/control behavior, disabled controls, stable IDs, scope
restoration, native input, clipping, data and adaptive sizing across density,
control context, geometry and font scales. All eight showcase pages are submitted
through the native binding. The warmed catalog check measures CPU submission and
managed allocations; native allocations and GPU costs are excluded. Use fresh
local output for pass counts; preserved historical reports describe prior runs.

With -Previews, artifacts/previews contains page-0.png through page-7.png,
settings-150-percent.png, narrow-field-wide-popup.png and integration density/scale
previews. These are diagnostic CPU renders of actual native ImGui meshes with a
default font and fixed background, not in-game screenshots or pixel assertions.
The preview switch sets AETHERTEKUI_PREVIEW_DIR for the test process.

## Later in-game manual acceptance (not performed)
1. After separately authorizing client installation, load src/AethertekUI.Demo/bin/Debug/AethertekUI.Demo.dll as a developer plugin with its manifest and AethertekUI.dll beside it; run /aethertekui. Confirm command works and no log errors occur. Close/reopen through plugin UI.
2. Gallery: change green seed to violet/orange. All chrome changes coherently; change opacity independently; switch Compact/Standard. Tab through controls, activate with keyboard/gamepad where host supports it.
3. Activate all four button variants and each icon. Toggle switch/checkbox/chips, 2/3/4 segments. In Disabled & focus, confirm entire and per-option disabled states do not activate. Test read-only/error fields, typing, paste, selection and inline clear.
4. Scroll to cards; select the interactive card. Open/confirm/cancel the modal. Confirm popup focus and no style leakage into another plugin.
5. Automation: open the separate floating overlay, change Auto/Manual/Off, open settings, close and reopen. Move the overlay across bright/dark game scenes.
6. Data: filter by text/location and status, clear, sort each header both directions, resize columns, hover/select rows. Change seed and confirm availability/upcoming/missing colors do not change. Check scrolling at smaller height.
7. Dashboard: Start, Pause, resume Start and Stop. Confirm progress changes, pauses and resets; Move/Return/Repair only update mock selection.
8. Settings: nested cards, route combo, delay slider, preset text, disabled dependency and reduced motion. Repeat at 100%, 150% and 200% global scale.
9. Reload/unload the plugin; confirm command/events are removed and no global ImGui state remains changed.
10. List editor: search the 500-option combo; disabled recipes must not select. Change quantity/delay/inheritance and job bits. Resize panels, drag the mock item between folders, right-click it, hold Ctrl to enable Remove. Edit/paste multiline text.
11. Large catalog: scroll to both ends, sort all columns, hide/reorder/resize columns, Ctrl-select rows, then filter and clear. Selection must remain tied to IDs. Type immediately after clearing search. Change seed and confirm semantic statuses retain their color.
12. Sizing lab: compare all three row contexts at both densities and different font/global scales. Narrow quantities must remain editable, wider ones retain steppers, long choices open wider searchable menus, and tile labels wrap without overlap. Compare another plugin's styles before/after closing this window.

No actual FFXIV rendering, gamepad integration, or plugin loading is claimed by the automated results.
