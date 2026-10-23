# VERMAXION LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `073c211110ec1e32fe9dabcd2251dba13f24a1a6`. Existing dirty paths were inspected by count (20); none are included or changed by this mockup task.

Existing multi-character task orchestrator main. Character list, next/current task order, availability and per-character state, existing Run All, Cancel, Settings, Crewcible, Verminion and Fish collection actions. Use two anonymous characters, idle fabricated task rows; don't invent progress/finish estimates. Preserve existing character/global scope and task-order editor semantics.

Settings pages Characters, Global, Task Order, Marketboard, Window appearance, About. Separate Beastmaster target/supply/status reference, Verminion and Island Sanctuary/Fish Collection auxiliary specimens as space permits; these are existing independent windows. Keep local run controls and supplies distinct from shared UI sounds. No new task engine, market automation or private IPC is implemented.

## Existing UI source inventory

- `VERMAXION/Models/DadTimeWindow.cs`
- `VERMAXION/Windows/BeastmasterMiniWindow.cs`
- `VERMAXION/Windows/BeastmasterWindow.Beasts.cs`
- `VERMAXION/Windows/BeastmasterWindow.Repair.cs`
- `VERMAXION/Windows/BeastmasterWindow.Run.cs`
- `VERMAXION/Windows/BeastmasterWindow.Schedule.cs`
- `VERMAXION/Windows/BeastmasterWindow.Status.cs`
- `VERMAXION/Windows/BeastmasterWindow.Supplies.cs`
- `VERMAXION/Windows/BeastmasterWindow.cs`
- `VERMAXION/Windows/ConfigWindow.cs`
- `VERMAXION/Windows/DebugWindow.cs`
- `VERMAXION/Windows/FishCollectionWindow.cs`
- `VERMAXION/Windows/IslandSanctuaryWindow.cs`
- `VERMAXION/Windows/MainWindow.cs`
- `VERMAXION/Windows/RegistrableConfigWindow.cs`
- `VERMAXION/Windows/VerminionWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

