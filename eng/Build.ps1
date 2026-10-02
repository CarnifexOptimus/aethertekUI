param([switch]$Previews)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Enter-RepoEnv.ps1')
Push-Location $repoRoot
try {
    if ($Previews) { $env:AETHERTEKUI_PREVIEW_DIR = Join-Path $repoRoot 'artifacts/previews' }
    & dotnet build AethertekUI.sln -c Debug -p:Platform=x64 --disable-build-servers -m:1 -v:minimal *> artifacts/build-Debug.log
    if ($LASTEXITCODE -ne 0) { Get-Content artifacts/build-Debug.log; throw 'Debug/x64 build failed' }
    Get-Content artifacts/build-Debug.log -Tail 6
    & dotnet test tests/AethertekUI.Tests -c Debug --no-build --no-restore --logger 'trx;LogFileName=tests-Debug.trx' --results-directory artifacts/TestResults -v:minimal *> artifacts/test-Debug.log
    if ($LASTEXITCODE -ne 0) { Get-Content artifacts/test-Debug.log; throw 'Debug tests failed' }
    Get-Content artifacts/test-Debug.log -Tail 4
    & (Join-Path $PSScriptRoot 'Verify-Consumer.ps1')
}
finally { Pop-Location }
