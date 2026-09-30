param([switch]$Previews)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Enter-RepoEnv.ps1')
Push-Location $repoRoot
try {
    if ($Previews) { $env:MATERIALUI_PREVIEW_DIR = Join-Path $repoRoot 'artifacts/previews' }
    foreach ($configuration in @('Debug','Release')) {
        $buildLog = Join-Path $repoRoot "artifacts/build-$configuration.log"
        & dotnet build CombatReborn.MaterialUI.sln -c $configuration --disable-build-servers -m:1 -v:minimal *> $buildLog
        if ($LASTEXITCODE -ne 0) { Get-Content $buildLog; throw "Build failed: $configuration" }
        Get-Content $buildLog -Tail 6
        $testLog = Join-Path $repoRoot "artifacts/test-$configuration.log"
        & dotnet test tests/CombatReborn.MaterialUI.Tests -c $configuration --no-build --no-restore --logger "trx;LogFileName=tests-$configuration.trx" --results-directory artifacts/TestResults -v:minimal *> $testLog
        if ($LASTEXITCODE -ne 0) { Get-Content $testLog; throw "Tests failed: $configuration" }
        Get-Content $testLog -Tail 3
        $libraryOut = Join-Path $repoRoot "artifacts/lib/$configuration"
        $demoOut = Join-Path $repoRoot "artifacts/demo/$configuration"
        New-Item -ItemType Directory -Force -Path $libraryOut,$demoOut | Out-Null
        Copy-Item "src/CombatReborn.MaterialUI/bin/$configuration/net10.0-windows/CombatReborn.MaterialUI.dll" $libraryOut
        Copy-Item "src/CombatReborn.MaterialUI/bin/$configuration/net10.0-windows/CombatReborn.MaterialUI.pdb" $libraryOut
        Get-ChildItem "src/CombatReborn.MaterialUI.Demo/bin/$configuration" -File |
            Where-Object { $_.Extension -in @('.dll','.pdb','.json','.zip') } |
            Copy-Item -Destination $demoOut
    }
    & dotnet pack src/CombatReborn.MaterialUI -c Release --no-build --no-restore -o artifacts -v:minimal *> artifacts/pack.log
    if ($LASTEXITCODE -ne 0) { Get-Content artifacts/pack.log; throw 'Pack failed' }
    Get-Content artifacts/pack.log
    & (Join-Path $PSScriptRoot 'Verify-LocalOutputs.ps1')
}
finally { Pop-Location }
