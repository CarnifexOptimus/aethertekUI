# Questionable Companion LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `ff61d3f3ef73efa1d7b3f6ae45ec8aece2f08d70`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Existing multi-character quest orchestration workspace. Character/account roster and planned actions, existing Start Rotation and Stop Rotation, per-character quest/job/status rows, with anonymous sample characters. Existing major workspaces (characters/roster, quest/helper settings, schedules/statistics) retain source navigation; no invented tabs. Current main uses its established compact layout, so preserve it rather than adding a new regular-layout preference.

Real QstComp Mini, selected Settings/Appearance and helper/combat setup reference. Retain AutoRetainer/Questionable dependency state, helper/chauffeur job and role planning, and explicit local/remote context. Main and Mini are different windows, not two density modes. No new character-creation automation or travel feature is introduced by the skin.

## Existing UI source inventory

- `QuestionableCompanion/Services/CompanionWindowLayout.cs`
- `QuestionableCompanion/Windows/AlliedSocietyPriorityWindow.cs`
- `QuestionableCompanion/Windows/ConfigWindow.cs`
- `QuestionableCompanion/Windows/DebugWindow.cs`
- `QuestionableCompanion/Windows/NewMainWindow.AlliedSociety.cs`
- `QuestionableCompanion/Windows/NewMainWindow.Appearance.cs`
- `QuestionableCompanion/Windows/NewMainWindow.AutoRetainerControls.cs`
- `QuestionableCompanion/Windows/NewMainWindow.BugReports.cs`
- `QuestionableCompanion/Windows/NewMainWindow.CharacterCreator.cs`
- `QuestionableCompanion/Windows/NewMainWindow.CharacterDetails.cs`
- `QuestionableCompanion/Windows/NewMainWindow.Characters.cs`
- `QuestionableCompanion/Windows/NewMainWindow.ClassUnlocks.cs`
- `QuestionableCompanion/Windows/NewMainWindow.Commands.cs`
- `QuestionableCompanion/Windows/NewMainWindow.DCTravel.cs`
- `QuestionableCompanion/Windows/NewMainWindow.Dependencies.cs`
- `QuestionableCompanion/Windows/NewMainWindow.DeveloperTools.cs`
- `QuestionableCompanion/Windows/NewMainWindow.DungeonPlugins.cs`
- `QuestionableCompanion/Windows/NewMainWindow.HealRider.cs`
- `QuestionableCompanion/Windows/NewMainWindow.HuntLogs.cs`
- `QuestionableCompanion/Windows/NewMainWindow.InitialSetup.cs`
- `QuestionableCompanion/Windows/NewMainWindow.MassGcSupply.cs`
- `QuestionableCompanion/Windows/NewMainWindow.Multiboxing.cs`
- `QuestionableCompanion/Windows/NewMainWindow.Retainers.cs`
- `QuestionableCompanion/Windows/NewMainWindow.RosterPlans.cs`
- `QuestionableCompanion/Windows/NewMainWindow.RosterRetainers.cs`
- `QuestionableCompanion/Windows/NewMainWindow.SelectedDCTravel.cs`
- `QuestionableCompanion/Windows/NewMainWindow.Settings.cs`
- `QuestionableCompanion/Windows/NewMainWindow.Statistics.cs`
- `QuestionableCompanion/Windows/NewMainWindow.SubmarineBuildPlanner.cs`
- `QuestionableCompanion/Windows/NewMainWindow.Tabs.cs`
- `QuestionableCompanion/Windows/NewMainWindow.TitleQuickControls.cs`
- `QuestionableCompanion/Windows/NewMainWindow.TravelingCharacters.cs`
- `QuestionableCompanion/Windows/NewMainWindow.XadbRefresh.cs`
- `QuestionableCompanion/Windows/NewMainWindow.cs`
- `QuestionableCompanion/Windows/QstCompMiniWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

