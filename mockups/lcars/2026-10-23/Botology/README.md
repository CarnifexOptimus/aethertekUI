# Botology LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `7f0017fb2e3b83c684e9a1643447199fd1cfb12d`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Actual plugin installer. Discover cards/table choice, Installed, Updates, repository management, search and category/tag filters. Selected catalog source visible. Three fabricated plugin cards Sample navigation / Sample utility / Sample planner with installed/update/available states, pin and View details actions. Retain explicit Install/Update actions where applicable. No automatic updater or invented update-all requirement.

Existing Settings and DTR manager reference. Installer plugin-detail view with Back to results, media, Project website and Discord, source identity and manual action. Repository management popup with Add repository/Remove repository/View plugins and existing confirm-removal state. Shared Appearance as third choice. No live package download claim.

## Existing UI source inventory

- `botology/Windows/CatalogEditorWindow.cs`
- `botology/Windows/ConfigWindow.cs`
- `botology/Windows/DtrManagerWindow.cs`
- `botology/Windows/MainWindow.Categories.cs`
- `botology/Windows/MainWindow.Installer.cs`
- `botology/Windows/MainWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

