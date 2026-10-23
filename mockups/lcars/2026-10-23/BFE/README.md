# BFE LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `3a7fe1ad6cd7776879302dd648200fde9c048387`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Main tabs exactly Start Bunnies, Stats, About. Start Bunnies selected: Area Selection with existing Eureka choices (sample Pyros); Task 'Idle. Select an area and press Start to begin.'; Time elapsed 00:00; Dependencies with ready/missing semantic text. Existing single Start/Stop action at bottom, not both running states at once. Settings, Ko-fi and Discord existing links.

Settings tabs General Settings, AutoRetainer Settings, Window appearance. Show Appearance with Auto/Picker/LCARS and existing language, compact, opacity, main-window-control preferences. Small Stats reference with Lifetime/Session rows and RESET STATS; no fabricated progress bars or automation schedule.

## Existing UI source inventory

- `BFE/Ui/DebugWindow/DebugWindow.cs`
- `BFE/Ui/MainWindow/MainWindow.cs`
- `BFE/Ui/SettingsWindow/SettingsWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

