param([switch]$NoRestore, [switch]$Rebuild)
$ErrorActionPreference='Stop'
$env:DalamudLibPath='C:/Users/pscho/AppData/Roaming/XIVLauncher3/addon/Hooks/dev'
. (Join-Path $PSScriptRoot 'Enter-RepoEnv.ps1')
$gbrRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../GatherBuddyReborn'))
$env:GIT_CONFIG_COUNT='2'
$env:GIT_CONFIG_KEY_0='safe.directory'
$env:GIT_CONFIG_VALUE_0=$gbrRoot.Replace('\','/')
$env:GIT_CONFIG_KEY_1='safe.directory'
$env:GIT_CONFIG_VALUE_1=$repoRoot.Replace('\','/')
$branch=git -C $gbrRoot branch --show-current
if($branch -ne 'UITest') { throw "Expected UITest; found $branch. No Git mutations are permitted." }
Push-Location $repoRoot
try {
    $project=Join-Path $gbrRoot 'GatherBuddy/GatherBuddy.csproj'
    if(-not $NoRestore) {
        # Read already-installed packages only; all new restore writes use the prototype-local cache.
        & dotnet restore $project --source 'C:/Users/pscho/.nuget/packages' --source $env:NUGET_PACKAGES --ignore-failed-sources -p:NuGetAudit=false -p:Platform=x64 --disable-build-servers -v:minimal *> artifacts/gbr-material-restore.log
        if($LASTEXITCODE -ne 0) { Get-Content artifacts/gbr-material-restore.log -Tail 40; throw 'GBR restore failed' }
    }
    $buildOptions = @()
    if($Rebuild) { $buildOptions += '-t:Rebuild' }
    $buildOptions += "-p:CustomAfterMicrosoftCommonTargets=$(Join-Path $PSScriptRoot 'Record-GbrOutputs.targets')"
    $buildOptions += "-p:MaterialPreviewOutputDirectory=$(Join-Path $repoRoot 'artifacts/gbr-current-outputs')"
    & dotnet build $project -c Debug --no-restore --disable-build-servers -m:1 -p:Platform=x64 -p:UseSharedCompilation=false -v:minimal @buildOptions *> artifacts/gbr-material-build.log
    Get-Content artifacts/gbr-material-build.log -Tail 8
    if($LASTEXITCODE -ne 0) { Select-String -Path artifacts/gbr-material-build.log -Pattern ': error '; throw 'GBR build failed' }
}
finally { Pop-Location }
