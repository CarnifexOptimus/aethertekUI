# AutoParty LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `eece798bd66bdc694b77660d3e9f4f5e9711a5fb`. Existing dirty paths were inspected by count (3); none are included or changed by this mockup task.

BROWSER companion, not a Dalamud window. Existing loopback AutoParty administration page. Header appearance/language controls. Health rows Database and Gateway; read-only tables Registrations (Island/User/Guild/State/Alias), Routes (Route/Initiator/Peer/Active), Blocks (Type/Subject/Reason), Redacted audit (Event/Outcome/Time). All values fictional/redacted and idle. Existing separate subject actions may appear below; no operational action is shown as executed.

Separate browser Lifecycle Lab with badge Virtual lab, Scenario and controls, virtual nodes/results/coverage, Start/Pause/Resume/Stop and explicit time-advance controls. Clearly NOT the live administration page. Show shared browser Appearance controls Auto/Picker/LCARS, language, compact, opacity/fade in existing header/settings, retaining script-free administration posture. No ImGui titlebar, Discord secrets, remote PC controls or invented updater.

## Existing UI source inventory

- `src/AutoParty.DiscordHost/LoopbackAdminServer.cs`
- `src/AutoParty.LifecycleLab/Ui.html`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

