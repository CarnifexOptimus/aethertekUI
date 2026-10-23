# DhogNav LCARS concepts

Design review only. No product implementation, build, deployment or client action is included.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `584790ae106a14545808676e64bd14f7f794097c`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

Existing Address Book main, not a duty planner. Saved location list, current/all-area browse selection, search, selected location name/territory/coordinates. Existing Settings and Commands actions, Compact/Expanded content choices distinct from global spacing. Use Sample location A/B and fictional coordinates; explicit travel/flag/edit actions only if represented by existing source affordances. No fake duty queue.

Settings tabs Safety filters and Window appearance; preserve All loaded players versus Players within radius, Blacklist and Whitelist editors. Separate Commands utility and location import/file dialog reference. Existing public Introduction/access loader is a separate locked state, not the private address book running without a module. Shared theme, no new licensing workflow.

## Existing UI source inventory

- `DhogNav/AddressBookWindow.cs`
- `DhogNav/CommandsWindow.cs`
- `public-shell/IntroductionWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).

