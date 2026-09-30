$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.cache/dotnet'
$env:NUGET_PACKAGES = Join-Path $repoRoot '.cache/packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $repoRoot '.cache/nuget-http'
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $repoRoot '.cache/nuget-plugins'
$env:TEMP = Join-Path $repoRoot '.cache/temp'
$env:TMP = $env:TEMP
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = '1'
$env:DOTNET_NOLOGO = '1'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:GIT_OPTIONAL_LOCKS = '0'
$env:APPDATA = Join-Path $repoRoot '.cache/appdata'
$env:LOCALAPPDATA = Join-Path $repoRoot '.cache/localappdata'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
foreach ($directory in @($env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES, $env:NUGET_HTTP_CACHE_PATH, $env:NUGET_PLUGINS_CACHE_PATH, $env:TEMP, $env:APPDATA, $env:LOCALAPPDATA, (Join-Path $repoRoot 'artifacts'))) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}
# Resolve the installed Dalamud distribution read-only; override for other machines.
if (-not $env:DalamudLibPath) {
    if ($env:DALAMUD_HOME) { $env:DalamudLibPath = $env:DALAMUD_HOME }
    else { $env:DalamudLibPath = Join-Path $env:USERPROFILE 'AppData/Roaming/XIVLauncher/addon/Hooks/dev' }
}
$env:DALAMUD_HOME = $env:DalamudLibPath
