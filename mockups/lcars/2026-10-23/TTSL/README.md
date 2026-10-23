# Thick Thighs Save Lives LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `96f8dde8194b24d1b2e623d24c675818efecf8e7`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Existing HUD operator main, not a new game dashboard. Player, Condition, Repair, Party and Radar panels, anonymous fixture rows and schematic radar points only. Existing Setup, Settings, Open Web HUD, Ko-fi/Discord actions. Remote HUD connection/state shown separately from local panels. Same readable text in regular/compact; no fake latency/FPS charts or streamed game image.

Settings/Appearance and Setup Wizard with existing panel selection, radar dimensions, Krangle displayed names and optional remote-publishing/server URL (fictional localhost only). Separate existing browser Web HUD specimen preserving its own layout and public data structure; no new server installation, credentials or sharing action. Browser appearance controls remain in browser header, not ImGui titlebar.

## Existing UI source inventory

- `TTSL/Windows/ConfigWindow.cs`
- `TTSL/Windows/MainWindow.cs`
- `TTSL/Windows/SetupWizardWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

