# Chocobo Colourized LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `8876034f7529d64170b6d16abd27753d3715092c`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Main tabs exactly Calculator, Timers, Automation. Calculator selected: current colour and desired colour selectors, fruit/feed plan result table, Save Plan & Switch to Automation and Copy Results to Clipboard. Use a fabricated valid plan with fruit counts and no actual feeding completion. Keep clear-plan action separate. No new food shop or racing controls.

Automation selected alternate window with Check stable condition before feeding, existing Start Automated Feeding / Stop Automation, existing insufficient-fruits disabled-start state as a separate OUTSIDE-app inset. Timers view and native Settings/Appearance reference. Existing OK/Dismiss messages remain distinct. No invented breeding scheduler.

## Existing UI source inventory

- `ChocoboColourized/Windows/ConfigWindow.cs`
- `ChocoboColourized/Windows/MainWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

