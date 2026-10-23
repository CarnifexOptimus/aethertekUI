# DAD LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `9537d207ec0f8b6fc0d85faad2fbf9bea8ed9e22`. Existing dirty paths were inspected by count (58); none are included or changed by this mockup task.

Existing duty planner/coordinator main, selected Plan workspace. Navigation exactly Home, Plan, Trust, Schedules, Crew, Clients, Status, plus existing Settings action. Saved plan picker, activity/duty, crew/party slots and readiness preview, Save preset / Run now / Open schedule remain distinct. Anonymous four-member sample group. Main is NOT solely Trust. Do not add broad new navigation or remove the ordinary duty planner.

Trust section is separate from Plan. Show NPC leveling with simple Balance lowest / Priority order / Fixed party policies, NPC targets separate from player-job targets, per-role multi-select eligible player jobs and independent target settings. Ordinary Trust preset is a separate fixed specific group gated by compatible dungeons, not NPC leveling. Include Settings/Appearance and a distinct Mini status reference. No invented XADB Trust catalog or shared NPC/player target number.

## Existing UI source inventory

- `Models/DadTimeWindow.cs`
- `Windows/ConfigWindow.cs`
- `Windows/DadAutoPartyFleetMatrixWindow.cs`
- `Windows/DadAutoPartyWindow.cs`
- `Windows/DadClientReconnectWindow.cs`
- `Windows/DadDependenciesWindow.cs`
- `Windows/DadMiniStatusWindow.cs`
- `Windows/DadPresetBatchWizardWindow.cs`
- `Windows/DadQuickPanelWindow.cs`
- `Windows/DadShoppingWizardWindow.cs`
- `Windows/MainWindow.Accounts.cs`
- `Windows/MainWindow.GatherParty.cs`
- `Windows/MainWindow.MemberAssignments.cs`
- `Windows/MainWindow.Trust.cs`
- `Windows/MainWindow.cs`
- `Windows/SetupWizardWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

