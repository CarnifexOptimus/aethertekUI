# Prototype smoke test
## Automated validation
Run from the repository:
```powershell
pwsh -NoProfile -File .\eng\Build.ps1 -Previews
pwsh -NoProfile -File .\eng\Verify-Gbr.ps1
```
Build.ps1 sets local caches, builds Debug/Release, runs all tests for both configurations, creates a local NuGet package, copies convenient DLL artifacts and verifies output paths. Test logs and TRX files are in artifacts/.

The suite has 39 tests: sRGB/OKLab/OKLCH round trips, known reference coordinates, deterministic palette hue/gamut/tone ordering, all 33 semantic roles, contrast, independent alpha, geometry/scaling and bounded motion; native context rendering, every theme color restoration, mouse/keyboard activation, disabled switch/control/options, input typing and clear, button families, chips, checkbox/radio, navigation/action tiles, popup/table/child scopes, five actual demo pages and a 150% layout.

With -Previews, artifacts/previews contains page-0.png through page-4.png and settings-150-percent.png. These are generated from the real ImGui draw meshes with a small CPU renderer, default font and fixed background. They are for manual review, not brittle pixel assertions.

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

No actual FFXIV rendering, gamepad integration, or plugin loading is claimed by the automated results.
