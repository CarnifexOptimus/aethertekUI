# Footballer LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `195b2e6c63605d2c4d02134fa0a8a3da30d4a51a`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Existing party foot/portrait showcase main. Party member cards with anonymous Sample character/Companion labels and neutral avatar/boot placeholder art, no actual player pictures. Existing Foot showcase, Without footwear, Settings, Refresh party, Refresh Lodestone controls. Job/details and source-availability display; if face data unavailable hide associated foot image rather than fabricate it. Preserve current card layout and privacy choices.

Existing Settings/Appearance with saved showcase/own-feet/face/privacy/portrait replacement choices, not a new skin scheduler. Separate unavailable-image card/reference and capture-preview controls where the source already provides them. No real identifying images, sexualized framing or invented Lodestone data. Keep original actions distinct from preview-only states.

## Existing UI source inventory

- `footballer/Windows/ConfigWindow.cs`
- `footballer/Windows/MainWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

