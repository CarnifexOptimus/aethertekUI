# Coppelia LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `b5a59e3e930473e2d5a3664a38eef8f45afa7c84`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Existing healing/helper main. Quick Setup, Watch, Settings, Mini, Status to chat and Ko-fi actions. Current role/behavior and read-only health/readiness status; existing powerlevel job choice. Fabricated Sample character and Sample companion only. Retain existing details disclosure; do not invent combat automation controls.

Settings tabs Quick Setup, General, HealBot Actions, Window appearance, Requirements / Help. Show Appearance and a separate real Watch window with saved heal-target rows; Mini is a distinct utility window. Quick Setup example preserves Stand-alone/Helper/Newb roles and target-kind choices Players, Companion chocobos, NPC party members, Friendly battle NPCs. No new role or target type.

## Existing UI source inventory

- `Coppelia/Windows/ConfigWindow.cs`
- `Coppelia/Windows/MainWindow.cs`
- `Coppelia/Windows/MiniWindow.cs`
- `Coppelia/Windows/WatchWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

