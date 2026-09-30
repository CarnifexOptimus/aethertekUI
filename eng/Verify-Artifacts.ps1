$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Enter-RepoEnv.ps1')
Push-Location $repoRoot
try {
    $version = ([xml](Get-Content Directory.Build.props)).Project.PropertyGroup.Version
    $required = @(
        'artifacts/lib/Release/CombatReborn.MaterialUI.dll',
        'artifacts/lib/Release/CombatReborn.MaterialUI.Dalamud.dll',
        'artifacts/demo/Release/CombatReborn.MaterialUI.Demo.dll',
        "artifacts/CombatReborn.MaterialUI.$version.nupkg",
        "artifacts/CombatReborn.MaterialUI.Dalamud.$version.nupkg",
        'artifacts/consumer-package/ConsumerContract.dll')
    $manifest = foreach ($relative in $required) {
        $file = Get-Item -LiteralPath (Join-Path $repoRoot $relative)
        [ordered]@{Path=$relative;Length=$file.Length;SHA256=(Get-FileHash -LiteralPath $file.FullName).Hash}
    }
    & dotnet run --project eng/ApiProbe -- --refs $required[0] $required[1] $required[2] *> artifacts/dependencies.txt
    if ($LASTEXITCODE -ne 0) { throw 'Assembly dependency inspection failed' }
    $dependencies = Get-Content artifacts/dependencies.txt
    if ($dependencies -match 'GatherBuddy|ElliLib|RSR') { throw 'Forbidden assembly dependency' }
    [ordered]@{Observed=(Get-Date -Format o);Version=$version;Artifacts=@($manifest);Dependencies=@($dependencies)} |
        ConvertTo-Json -Depth 6 | Set-Content -Encoding utf8 artifacts/ARTIFACT_MANIFEST.json
    Write-Output "Verified $($required.Count) release artifacts and assembly dependency boundaries."
}
finally { Pop-Location }
