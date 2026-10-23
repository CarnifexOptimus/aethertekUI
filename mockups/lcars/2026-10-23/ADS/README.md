# AI Duty Solver LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `1d5ad22a9c09fcccd628e1044560c371df6e1f67`. Existing dirty paths were inspected by count (6); none are included or changed by this mockup task.

Main Overview. Existing actions Start Outside, Start Inside, Resume, Stop; Settings and ADS Controls. Read-only Current Duty card (Duty, Family, Catalog, Maturity, Instanced/Catalog, MSQ, Unsafe Transition), current objective/planner and execution state cards. Sample matched 4-Man duty, stopped observer state; no ownership granted merely by entering. Retain current tab structure, show Overview selected; use source-derived labels rather than invented queue tools.

Settings with existing plugin-enabled, DTR, start-window, AutoDuty-conflict, loot/coffer and debug controls; shared Appearance. Separate small ADS Controls utility window with the four existing ownership actions. Small reference vignettes for Loot/Inventory utilities and object/dialog rule editors, with list/editor selection and explicit actions; no new features.

## Existing UI source inventory

- `ADS/Windows/ConfigWindow.cs`
- `ADS/Windows/DesynthesisWindow.cs`
- `ADS/Windows/DialogRuleEditorWindow.cs`
- `ADS/Windows/DutyMaturityEditorWindow.cs`
- `ADS/Windows/FrontierLabelWindow.cs`
- `ADS/Windows/GearSalePreviewWindow.cs`
- `ADS/Windows/GhostListWindow.cs`
- `ADS/Windows/HigherLowerWindow.cs`
- `ADS/Windows/InventoryUtilityWindow.cs`
- `ADS/Windows/LazyLootWarningWindow.cs`
- `ADS/Windows/LootWindow.cs`
- `ADS/Windows/MainWindow.cs`
- `ADS/Windows/ObjectExplorerWindow.cs`
- `ADS/Windows/ObjectRuleEditorWindow.cs`
- `ADS/Windows/QuickControlWindow.cs`
- `ADS/Windows/ReflectionWindow.cs`
- `ADS/Windows/RuleGuideWindow.cs`
- `ADS/Windows/ServerEventExplorerWindow.cs`
- `ADS/Windows/ShopListsWindow.cs`
- `ADS/Windows/TreasureRouteEditorWindow.cs`
- `ADS/Windows/VfxExplorerWindow.cs`
- `ADS/Windows/WizardWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

