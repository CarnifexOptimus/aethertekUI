$ErrorActionPreference = 'Stop'
$rootPath = Split-Path $PSScriptRoot -Parent
$env:GIT_OPTIONAL_LOCKS='0'
$baseline = Get-Content (Join-Path $rootPath 'docs/GBR_BASELINE.json') -Raw | ConvertFrom-Json
$files = Get-Content (Join-Path $rootPath 'docs/GBR_FILE_BASELINE.json') -Raw | ConvertFrom-Json
$head = git -C $baseline.Path rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'GBR HEAD read failed' }
$state = @(git -C $baseline.Path status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0) { throw 'GBR status read failed' }
$differences = [Collections.Generic.List[string]]::new()
if ($head -ne $baseline.Head) { $differences.Add('HEAD differs') }
if (($state -join "`n") -cne ($baseline.State -join "`n")) { $differences.Add('Working-tree state differs') }
$indexHash = (Get-FileHash (Join-Path $baseline.Path '.git/index')).Hash
if ($indexHash -ne $baseline.IndexHash) { $differences.Add('Index hash differs') }
foreach ($file in $files) {
    $path = Join-Path $baseline.Path $file.Path
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $differences.Add('Missing: '+$file.Path); continue }
    if ((Get-FileHash -LiteralPath $path).Hash -ne $file.Hash) { $differences.Add('Changed: '+$file.Path) }
}
$missionHash=(Get-FileHash (Join-Path $rootPath 'PROJECT_MISSION.md')).Hash
if ($missionHash -ne $baseline.MissionHash) { $differences.Add('Mission hash differs') }
$result=[ordered]@{Observed=(Get-Date -Format o);Head=$head;WorkingTreeMatches=($state -join "`n") -ceq ($baseline.State -join "`n");IndexHash=$indexHash;FilesVerified=$files.Count;MissionHash=$missionHash;Differences=@($differences)}
$result | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 (Join-Path $rootPath 'docs/GBR_FINAL_VERIFICATION.json')
$result | ConvertTo-Json -Depth 5
if ($differences.Count) { throw 'GBR or mission verification failed. Do not change GBR to fix this.' }
