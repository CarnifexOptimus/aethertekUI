# Hello Fellow Human LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `53c854fb0656c703e1d55e0bb3d1e646d80599d6`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Actual proximity/emote reaction editor. Presets list and Configuration pane; existing Enabled, DTR and Krangle controls, Guided Setup, New/Create/Delete/Export/Import/Reset Default. Selected fictional 'Friendly greeting' preset with one existing rule: nearby player enters range, response command /wave; condition and response fields remain explicit. No new social network, player surveillance or chat history.

Guided Setup wizard with destination preset, trigger choices A player enters range / A selected emote is performed / COPYCAT any incoming emote; target Any nearby player or One specific player, response command and optional weather. Back/Next/finish as in source, not a modal blocking the parent. Shared Appearance in existing settings; no new trigger kind.

## Existing UI source inventory

- `HelloFellowHuman/Windows/ConfigWindow.cs`
- `HelloFellowHuman/Windows/SetupWizardWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

