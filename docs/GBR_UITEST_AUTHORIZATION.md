# Temporary UITest integration authorization

The owner explicitly superseded the earlier read-only GBR boundary for this task on 2026-09-30:

> Ok I am opening up GBR for you on a fresh branch/worktree so you can wire in all of the material UI work into GBR. You have temporary write permission to the GBR repo on my local machine. DO NOT mutate git, no commits, no PRs, merges, or staging of files. Just wire it up properly(and give it the library to work with) just so I can look at how it looks within the game as a debug build of GBR. Current branch is UITest.

Verified checkout: C:/Users/pscho/OneDrive/Documents/GitRepos/GatherBuddyReborn, branch UITest, HEAD a204f2f45cb98302dbe938921255c3a6d617c775, initially clean.
Source edits and Debug build/restore in GBR are authorized. Tool caches/logs remain local to MaterialUI. No Git mutation, staging, commit, branch operation or publication is authorized in either repository. Installed Dalamud remains read-only. The original PROJECT_MISSION.md remains untouched.
Git baseline hashes are recorded in artifacts/GBR_UITEST_BASELINE.json for a final HEAD/index/ref comparison.

## v0.3 integration pass — October 2, 2026

The owner renewed GBR write/build authorization after the library-only pass:

> Ok, you have read/write access to GBR to make the necessary changes. DO NOT mutate git status during the process. No commit, PR, staging of files. Perform Debug build when you are done with the changes.

Only source/documentation working-tree edits and the requested Debug build/restore are performed. Caches/logs remain local to MaterialUI. Current-pass baseline: artifacts/gbr-v03-integration-baseline.json; final report: artifacts/GBR_V03_VERIFICATION.json. No Git mutation commands are authorized or performed. The original PROJECT_MISSION.md remains unchanged. This permission is for this integration pass and does not permanently remove the read-only default.

An opt-in eng/GbrUiProbe checks the already-built GBR adapters against native ImGui without initializing a plugin or game services. It is not referenced by the standalone library solution/build. Run it only after an authorized GBR build; enter eng/Enter-RepoEnv.ps1 first and then `dotnet run --project eng/GbrUiProbe --disable-build-servers`.
