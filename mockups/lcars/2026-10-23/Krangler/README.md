# Krangler LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `aee2c896a8f374f2745a58c4550caf17f325a4b6`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Existing screenshot privacy/appearance tool. Overview, Names, Appearance, Racism rules (fictional FFXIV race/clan/gender rules), Presets, Imaginary Fren, Soul Thief, Debug pages from source. Main Enable Krangler; Krangle Names/Chat, Do Not Krangle Self and Custom Self Display Name; appearance preset controls. Use anonymous game-character silhouettes only. No external identity service.

Source Setup Wizard and selected appearance/rule editor with existing Apply Rules and Discard Edits. Imaginary Fren reference has Enabled, Display Name, Preset, Test Spawn and Despawn; it is explicitly local-only. Shared colour/language/opacity settings and retained DTR. Krangle means obfuscating identifying text, NEVER 'show hidden settings'.

## Existing UI source inventory

- `Krangler/Windows/MainWindow.cs`
- `Krangler/Windows/SetupWizardWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

