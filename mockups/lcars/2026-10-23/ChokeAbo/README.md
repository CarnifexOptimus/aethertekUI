# ChokeAbo LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `4724a8545c4b755737981726f7cf397c1604af8e`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Chocobo breeding/progression main view. Read-only Pedigree, racing rank, inherited ability and learned ability; current progression status. Existing Resume/Pause/Stop progression, Refresh Chocobo Data, Status to chat, Settings and support action. Existing manual training/feed-purchase area: Clear Plan, Buy Needed Feed, Feed Planned Sessions, Run Full Cycle. Show idle fabricated bird data, not a claimed completed breeding cycle.

Existing Settings with Plugin enabled, DTR and automation feature controls. Include small progression/covering/retirement selection popup capture reference (existing selector states), not a new page. Shared Window appearance. Keep Start / Resume Breeding and Stop actions distinct; cleanup preview/confirmation is separate from immediate discard.

## Existing UI source inventory

- `ChokeAbo/Windows/ConfigWindow.cs`
- `ChokeAbo/Windows/MainWindow.cs`
- `ChokeAbo/Windows/PopupCaptureWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

