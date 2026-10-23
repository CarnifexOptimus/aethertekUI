# DhogGPT LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `50755a4a07f1194290532381efa9fbd91770e29e`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Chat translation main, existing incoming/outgoing source/target language selectors, channel and input text, Preview translation, Translate and send, Clear. Guide, Settings, Status to chat, support links, Enabled/DTR. Fabricated neutral sample chat only, no actual messages or player identities. Retain incoming versus outgoing language distinction and the separate Ultra compact mode.

Settings sections Everyday, Channels, Providers, Advanced, Window appearance. Provider credentials masked placeholders only. Separate Ultra compact main specimen and first-use Guide offer; guide is NONMODAL with no dimmed/inert parent. The theme does not send a message or auto-enable translation. No invented AI chat transcript or system prompts.

## Existing UI source inventory

- `DhogGPT/Models/CharacterWindowState.cs`
- `DhogGPT/Services/Chat/VanillaChatWindowService.cs`
- `DhogGPT/Windows/ConfigWindow.cs`
- `DhogGPT/Windows/FirstUseGuideWindow.cs`
- `DhogGPT/Windows/MainWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

