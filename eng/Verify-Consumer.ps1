$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Enter-RepoEnv.ps1')
Push-Location $repoRoot
try {
    $intermediate = (Join-Path $repoRoot '.cache/package-consumer-obj') + '/'
    $output = (Join-Path $repoRoot 'artifacts/consumer-package') + '/'
    $feed = Join-Path $repoRoot 'artifacts'
    $packageCache = [IO.Path]::GetFullPath((Join-Path $repoRoot '.cache/consumer-packages'))
    $allowedCachePrefix = [IO.Path]::GetFullPath((Join-Path $repoRoot '.cache')) + [IO.Path]::DirectorySeparatorChar
    if (-not $packageCache.StartsWith($allowedCachePrefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe package-cache path' }
    # This dedicated test cache must use the packages just produced, even when rebuilding the same version.
    if (Test-Path -LiteralPath $packageCache) { Remove-Item -LiteralPath $packageCache -Recurse -Force }
    $lockPath = Join-Path $intermediate 'packages.lock.json'
    if (Test-Path -LiteralPath $lockPath) { Remove-Item -LiteralPath $lockPath -Force }
    & dotnet build samples/ConsumerContract/ConsumerContract.csproj -c Release --disable-build-servers -m:1 -v:minimal --source $feed "-p:UseMaterialPackages=true" "-p:BaseIntermediateOutputPath=$intermediate" "-p:OutputPath=$output" "-p:RestorePackagesPath=$packageCache" "-p:NuGetLockFilePath=$lockPath" "-p:RestoreForce=true" "-p:RestoreForceEvaluate=true" *> artifacts/consumer-package.log
    if ($LASTEXITCODE -ne 0) { Get-Content artifacts/consumer-package.log; throw 'Package consumer failed' }
    foreach ($file in @('ConsumerContract.dll','CombatReborn.MaterialUI.dll','CombatReborn.MaterialUI.Dalamud.dll')) {
        if (-not (Test-Path (Join-Path $output $file))) { throw "Consumer dependency missing: $file" }
    }
    Get-Content artifacts/consumer-package.log -Tail 7
}
finally { Pop-Location }
