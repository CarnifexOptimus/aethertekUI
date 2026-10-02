# Prototype smoke test
## Automated validation
Run from the repository:
```powershell
pwsh -NoProfile -File .\eng\Build.ps1 -Previews
```
Build.ps1 sets local caches, builds Debug/Release, runs all tests for both configurations, creates a local NuGet package, copies convenient DLL artifacts and verifies output paths. Test logs and TRX files are in artifacts/.

The v0.3 suite has 111 passing tests in Debug and Release. It retains color/theme/control, data, native input, clipping and performance coverage, and adds sizing regression tests across both densities, three local contexts, 100/125/150 percent geometry scales and two font scales. Checks cover shared native/custom heights, host-style restoration, constrained fields with wider searchable popups, small icon activation, narrow numeric stepper fallback and tile wrapping. All eight demo pages render through the native binding. A warmed 300-frame catalog test records managed allocation and CPU submission time; native allocations and GPU cost are excluded.

The original Verify-Gbr.ps1 compares against the initial prototype baseline. Do not use that old baseline as evidence for a newer pass with separately authorized intervening GBR work. The current pass compares HEAD, index, status and 410 file hashes against artifacts/gbr-library-sizing-baseline.json; results are in artifacts/gbr-library-sizing-verification.json and [the validation report](LIBRARY_SIZING_VALIDATION.md).

With -Previews, artifacts/previews contains page-0.png through page-7.png, settings-150-percent.png, narrow-field-wide-popup.png and integration density/scale previews. These are generated from the real ImGui draw meshes with a small CPU renderer, default font and fixed background. They are for manual review, not brittle pixel assertions. Build.ps1 also validates both packages with an independent consumer and records artifact hashes/dependencies.

## Later in-game manual acceptance (not performed)
1. Follow README's dev-plugin loading steps; run /crmaterial. Confirm command works and no log errors occur. Close/reopen through plugin UI.
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
