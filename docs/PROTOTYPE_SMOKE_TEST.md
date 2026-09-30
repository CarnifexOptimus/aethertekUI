# Prototype smoke test
## Automated validation
Run from the repository:
```powershell
pwsh -NoProfile -File .\eng\Build.ps1 -Previews
pwsh -NoProfile -File .\eng\Verify-Gbr.ps1
```
Build.ps1 sets local caches, builds Debug/Release, runs all tests for both configurations, creates a local NuGet package, copies convenient DLL artifacts and verifies output paths. Test logs and TRX files are in artifacts/.

The v0.2 suite has 63 tests: original color/theme/control coverage plus settings serialization and regeneration caching, keyed filtering/sorting/selection, navigation math, flag preservation, native searchable combo editing, drag payload delivery, numeric clamps/disabled state, search focus return, colors-only/semantic scope preservation, tree/context/multiline scopes, borrowed icon rendering, splitter dragging, native 10,000-row clipping, and all seven demo pages. Integration pages are exercised at Compact/100% and Standard/150%. A warmed 300-frame catalog test records managed allocation and CPU submission time; native allocations and GPU cost are excluded.

With -Previews, artifacts/previews contains page-0.png through page-6.png, settings-150-percent.png and integration density/scale previews. These are generated from the real ImGui draw meshes with a small CPU renderer, default font and fixed background. They are for manual review, not brittle pixel assertions. Build.ps1 also validates both packages with an independent consumer and records artifact hashes/dependencies.

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

No actual FFXIV rendering, gamepad integration, or plugin loading is claimed by the automated results.
