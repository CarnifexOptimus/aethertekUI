# Dheacon LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `b17afc4b84ec4a23fb1b845286cdbe8de1f8bf7f`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Local TTS/commentary and classic transition alerts main. Enabled and DTR controls; Settings, Status to chat, Test speech, Clear cache. Show existing mode choices Classic transition alerts and Spoken commentary, commentary preset and read-only backend/status. Idle fabricated example. LCARS UI feedback mute is separate from the product's speech mode; do not replace speech controls with theme sounds.

Settings tabs Quick Setup, General, Speech, Piper Voices, Window appearance, Diagnostics. Existing voice/search/preset controls, not a cloud service. Separate Mini speech status and an explicit test/wizard reference with Recommended local Piper or Windows default; no installation or test is shown as performed. Shared UI-sounds preference default off, no invented volume control.

## Existing UI source inventory

- `dheacon/Windows/ConfigWindow.cs`
- `dheacon/Windows/MainWindow.cs`
- `dheacon/Windows/MiniWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

