$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Enter-RepoEnv.ps1')
$baseline = Get-Content (Join-Path $repoRoot 'artifacts/GBR_UITEST_BASELINE.json') -Raw | ConvertFrom-Json
$gbrRoot = $baseline.Path
$safeDirectory = "safe.directory=$gbrRoot"
$branch = git -c $safeDirectory -C $gbrRoot branch --show-current
$head = git -c $safeDirectory -C $gbrRoot rev-parse HEAD
$indexHash = (Get-FileHash (Join-Path $gbrRoot '.git/index')).Hash
$headFileHash = (Get-FileHash (Join-Path $gbrRoot '.git/HEAD')).Hash
$branchRefHash = (Get-FileHash (Join-Path $gbrRoot '.git/refs/heads/UITest')).Hash
if ($branch -ne $baseline.Branch -or $head -ne $baseline.Head -or
    $indexHash -ne $baseline.IndexHash -or $headFileHash -ne $baseline.HeadFileHash -or
    $branchRefHash -ne $baseline.BranchRefHash) { throw 'GBR Git metadata differs from the authorized baseline.' }
# Existing GBR sources use CRLF, including blobs tracked with those endings.
& git -c $safeDirectory -c core.whitespace=blank-at-eol,blank-at-eof,space-before-tab,cr-at-eol -C $gbrRoot diff --check
if ($LASTEXITCODE -ne 0) { throw 'GBR diff whitespace check failed.' }
& git -c $safeDirectory -C $gbrRoot diff --cached --quiet
if ($LASTEXITCODE -ne 0) { throw 'Unexpected staged GBR changes.' }

$output = Join-Path $gbrRoot 'GatherBuddy/bin/Debug'
$artifacts = foreach ($name in @('GatherBuddyReborn.dll','GatherBuddyReborn.json','GatherBuddyReborn.deps.json','CombatReborn.MaterialUI.dll')) {
    $file = Get-Item (Join-Path $output $name)
    [ordered]@{ Path=$file.FullName; Length=$file.Length; SHA256=(Get-FileHash $file.FullName).Hash }
}
$builtCore = (Get-FileHash (Join-Path $repoRoot 'src/CombatReborn.MaterialUI/bin/x64/Debug/net10.0-windows/CombatReborn.MaterialUI.dll')).Hash
$copiedCore = (Get-FileHash (Join-Path $output 'CombatReborn.MaterialUI.dll')).Hash
if ($builtCore -ne $copiedCore) { throw 'GBR has a stale MaterialUI DLL.' }
$deps = Get-Content (Join-Path $output 'GatherBuddyReborn.deps.json') -Raw
if ($deps -notmatch '"CombatReborn.MaterialUI.dll"') { throw 'MaterialUI is missing from the GBR dependency manifest.' }

function Read-Warnings([string]$path) {
    @(Get-Content $path | ForEach-Object {
        if ($_ -match ': warning ([A-Z]+[0-9]+): (.*?) \[' -and $Matches[1] -ne 'NU1801') {
            $Matches[1] + ': ' + $Matches[2]
        }
    } | Sort-Object -Unique)
}
$before = Read-Warnings (Join-Path $repoRoot 'artifacts/gbr-baseline-build.log')
$after = Read-Warnings (Join-Path $repoRoot 'artifacts/gbr-material-build.log')
$newWarnings = @(Compare-Object $before $after | Where-Object SideIndicator -eq '=>')
if ($newWarnings.Count) { throw ('New GBR warnings: ' + ($newWarnings.InputObject -join '; ')) }

$allowed = @($repoRoot,$gbrRoot) | ForEach-Object {
    [IO.Path]::GetFullPath($_).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
}
$checkedOutputs = 0
foreach ($list in Get-ChildItem (Join-Path $repoRoot 'artifacts/gbr-current-outputs') -Filter '*.txt') {
        foreach ($line in Get-Content $list.FullName) {
            if (-not $line.Trim()) { continue }
            $path = [IO.Path]::GetFullPath($line)
            if (-not ($path.StartsWith($allowed[0],[StringComparison]::OrdinalIgnoreCase) -or
                      $path.StartsWith($allowed[1],[StringComparison]::OrdinalIgnoreCase))) {
                throw "Generated output outside authorized repositories: $path"
            }
            $checkedOutputs++
        }
}
if ($checkedOutputs -eq 0) { throw 'No current-build FileWrites records found. Run Build-GbrUITest.ps1 first.' }
$report = [ordered]@{
    Observed=(Get-Date -Format o); Branch=$branch; Head=$head
    IndexHash=$indexHash; HeadFileHash=$headFileHash; BranchRefHash=$branchRefHash
    GitMetadataMatchesBaseline=$true; NewWarnings=$newWarnings
    DebugOutputPathsChecked=$checkedOutputs; Artifacts=@($artifacts)
    WorkingTree=@(git -c $safeDirectory -C $gbrRoot status --porcelain=v1)
    Scope='Read-only Git checks, dependency hashes and current-build MSBuild FileWrites; no in-game rendering or system-wide filesystem monitor.'
}
$report | ConvertTo-Json -Depth 6 | Set-Content -Encoding utf8 (Join-Path $repoRoot 'artifacts/GBR_UITEST_VERIFICATION.json')
Write-Output "Verified UITest HEAD/index/ref unchanged; current DLL copied; no new warning messages; $checkedOutputs Debug output paths inside authorized repositories."
