# MOGTOME LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `634285b9eab0c7d0529480d4fb6ad9b55c15f334`. Existing dirty paths were inspected by count (1); none are included or changed by this mockup task.

Existing duty-farming/tomestone operator window. Current duty/activity selection, team-leader and party status, dependency checks, Start/Stop and stop-after-next-success controls. Fabricated idle Praetorium example, run counter and tomestone target as existing editable/read-only values. Retain real activity navigation for duty farming, Blunderville and Keybound Brawler, not a new task dashboard.

Existing Config/Appearance and separate Stats/Moogle shop target editor reference; current counter, return-to-entrance delay and farming thresholds are retained. Small auxiliary warning/action-blocked specimen, NOT a new dismiss flow. Blunderville/Keybound Brawler remain separate existing activities; no fake completion rates, live event outcome or new purchase command.

## Existing UI source inventory

- `MOGTOME/Windows/ActionWarningWindow.cs`
- `MOGTOME/Windows/BlundervilleWindow.cs`
- `MOGTOME/Windows/ConfigWindow.cs`
- `MOGTOME/Windows/KeyboundBrawlerWindow.cs`
- `MOGTOME/Windows/MainWindow.cs`
- `MOGTOME/Windows/MoogleShopWindow.cs`
- `MOGTOME/Windows/StatsWindow.cs`
- `MOGTOME/Windows/WarningTextWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

