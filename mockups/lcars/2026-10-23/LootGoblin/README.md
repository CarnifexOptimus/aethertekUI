# Loot Goblin LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `c995667cf3947e56197159ef01ad82169e0a3fc0`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Treasure-map automation main, existing Status, Map Queue, Location Data, Current Run, Navigation, Party Status, Dependencies/Integrations sections. Existing Start, Resume, Pause, Stop, Alexandrite and Settings. Queue table uses configured map type, count and current state with three fabricated entries; idle run. Retain Auto Start next map, Refresh Maps and distinct manual map/location actions. No new map purchase scheduler.

Existing Config/Appearance, Alexandrite utility window and map-location editor reference. Config source choices include Combat job, Gather job, Max map allowance wait, Return when done/destination, Repair mode, Fetch maps from retainers/saddlebags. Show current-page controls only; no fake duty navigation or success totals. Theme changes do not start a map run.

## Existing UI source inventory

- `LootGoblin/Windows/AlexandriteMapWindow.cs`
- `LootGoblin/Windows/ConfigWindow.cs`
- `LootGoblin/Windows/MainWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

