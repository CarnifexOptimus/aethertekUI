# MOM LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `62b5fc60364a83665358af57cf2210c28824fa50`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Existing PvP operator main, route/readiness and profile scope, existing run/stop controls, queue and current-match status. Fabricated idle/unmatched state. Preserve source General, Profiles, Targeting, Movement, Combat, Social, Debug/Developer and About organization; do not turn it into FrenRider or add new tactics. Existing Mini is separate from compact spacing.

Settings/Appearance plus real Mini and public Introduction/access-loader states. Source controls include Automation enabled, DTR, Krangle names, Allow queue automation, existing movement/burst and PvP role-action choices. Show only existing selected settings and retain manual LB option. No new match routing, game exploit or licensing flow; no code change or match action.

## Existing UI source inventory

- `mom/Windows/ConfigWindow.cs`
- `mom/Windows/MainWindow.cs`
- `mom/Windows/MiniWindow.cs`
- `public-shell/IntroductionWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

