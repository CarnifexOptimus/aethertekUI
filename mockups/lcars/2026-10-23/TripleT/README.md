# Triple-T LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `031dd28dd22a79967cb58c03aaca2726e8bd7d78`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Triple Triad main. Existing navigation in order Overview, NPCs & cards, Farm queue, Tournament, Scheduler, Unlock quests, Integrations, Activity, Settings, Developer tools. Selected NPCs & cards: NPC search, Only NPCs with missing reward cards, Show restricted NPCs (inspection only), fictional sample unlock/card rows and farm goal. Retain STOP ALL. No estimate of match wins or guaranteed card rewards.

Settings/Appearance plus Scheduler and Tournament reference vignettes. Scheduler source fields name, schedule type/time/interval, minutes per NPC, safety cap games/gil and ordered NPC rotation. Tournament is existing two-client designated-winner workflow; no fabricated result. Keep unavailable NPCs visibly unavailable and saving distinct from enabling a plan.

## Existing UI source inventory

- `src/TripleT/Windows/MainWindow.Appearance.cs`
- `src/TripleT/Windows/MainWindow.Scheduler.cs`
- `src/TripleT/Windows/MainWindow.Shell.cs`
- `src/TripleT/Windows/MainWindow.Tournament.cs`
- `src/TripleT/Windows/MainWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

