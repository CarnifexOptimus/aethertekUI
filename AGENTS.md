# Project constraints
Read PROJECT_MISSION.md at every major phase and after context loss. It is authoritative and immutable.
All writes, caches, temporary files, logs and artifacts must stay in this repository.
Dot-source eng/Enter-RepoEnv.ps1 before invoking toolchains.
GatherBuddyReborn is a read-only reference. Never edit, build, restore, fetch, update submodules or alter its index/.git. Use GIT_OPTIONAL_LOCKS=0 for read-only Git commands. Only an explicit owner instruction changing this boundary can permit future changes.
Do not inspect RSR implementation or install this demo into Dalamud. No remote publication.
Use native ImGui behavior, semantic theme roles, stable IDs, balanced value-type scopes and bounded motion state. Keep domain-semantic colors consumer-owned.
