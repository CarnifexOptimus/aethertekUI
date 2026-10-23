# DDuck LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `d59a73450fcfa46d670e694dadf6eb44026471ec`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Deep-dungeon run workspace for Palace of the Dead, Heaven-on-High, Eureka Orthos and Pilgrim's Traverse. Selected dungeon/save/floor plan and preparation/readiness; existing run actions and FULL STOP. Source sections Run setup, Next run settings (current run keeps its plan), Current activity, Current run details, Saves, Previous run. Fabricated idle plan; do not claim a live run, checkpoint or party-ready pass.

Separate Mini status window and existing Appearance/settings. Show one existing pomander/consumable-rule page reference with POTD or Orthos items, not new rules. Existing public introduction/access loader as a separate locked state; it never unlocks private functionality in the mockup. Retain existing save/leave confirmations and dungeon-specific configuration.

## Existing UI source inventory

- `DDuck/Windows/MainWindow.Consumables.cs`
- `DDuck/Windows/MainWindow.Help.cs`
- `DDuck/Windows/MainWindow.Layout.cs`
- `DDuck/Windows/MainWindow.Leveling.cs`
- `DDuck/Windows/MainWindow.Pomanders.cs`
- `DDuck/Windows/MainWindow.Presets.cs`
- `DDuck/Windows/MainWindow.TrapData.cs`
- `DDuck/Windows/MainWindow.Vercure.cs`
- `DDuck/Windows/MainWindow.cs`
- `DDuck/Windows/MiniWindow.cs`
- `ref/HoardFarm/HoardFarm/Windows/ConfigWindow.cs`
- `ref/HoardFarm/HoardFarm/Windows/MainWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

