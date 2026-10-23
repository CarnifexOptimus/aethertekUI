# Eureka Suite LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `3d162d5c00636644e7759fb64e95b22a83d0eeff`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Existing expedition assistant Main for Anemos/Pagos/Pyros/Hydatos/BA. Read-only zone, live threats/aggro reference and expedition/FATE module navigation. Keep existing module selector rather than invent a spaceship sidebar. Fabricated neutral tracker rows with named game monsters, state Idle; no claimed live danger or events. Do not alter native module actions or require private access for public features.

Settings/Appearance and real auxiliary map, cast-alert and bunny timer reference windows. Map is schematic reference, not a reconstructed game map. Private Bunny/access section remains a clearly locked or unavailable state with existing access controls; no invented unlock, license or package issuance. Preserve original English UI labels and use no Unicode emoji.

## Existing UI source inventory

- `Bunnies/BFE/Ui/DebugWindow/DebugWindow.cs`
- `Bunnies/BFE/Ui/MainWindow/MainWindow.cs`
- `UI/BunnyTimerWindow.cs`
- `UI/CastAlertWindow.cs`
- `UI/EurekaMapWindow.cs`
- `UI/FateShoutWindow.cs`
- `UI/MainWindow.About.cs`
- `UI/MainWindow.Aggro.cs`
- `UI/MainWindow.Fate.cs`
- `UI/MainWindow.Settings.cs`
- `UI/MainWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

