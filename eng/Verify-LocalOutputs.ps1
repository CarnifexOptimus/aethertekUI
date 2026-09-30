$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$rootPrefix = $rootPath.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$outside = [Collections.Generic.List[string]]::new()
$outputs = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($list in Get-ChildItem (Join-Path $rootPath 'src'),(Join-Path $rootPath 'tests'),(Join-Path $rootPath 'eng'),(Join-Path $rootPath 'samples'),(Join-Path $rootPath '.cache/package-consumer-obj') -Recurse -Filter '*.FileListAbsolute.txt') {
    foreach ($line in Get-Content $list.FullName) {
        if (-not $line.Trim()) { continue }
        $fullPath = [IO.Path]::GetFullPath($line)
        if (-not $fullPath.StartsWith($rootPrefix,[StringComparison]::OrdinalIgnoreCase)) { $outside.Add($fullPath) }
        $null = $outputs.Add($fullPath)
    }
}
$environment = [ordered]@{}
foreach ($name in @('DOTNET_CLI_HOME','NUGET_PACKAGES','NUGET_HTTP_CACHE_PATH','NUGET_PLUGINS_CACHE_PATH','TEMP','TMP','APPDATA','LOCALAPPDATA')) {
    $value = [Environment]::GetEnvironmentVariable($name)
    $environment[$name] = $value
    if (-not $value -or -not [IO.Path]::GetFullPath($value).StartsWith($rootPrefix,[StringComparison]::OrdinalIgnoreCase)) {
        $outside.Add("$name=$value")
    }
}
if ($outside.Count) { throw ('Non-local output/cache path: '+($outside -join ', ')) }
$result = [ordered]@{ Observed=(Get-Date -Format o); GeneratedOutputPaths=$outputs.Count; OutsidePaths=@($outside); Environment=$environment; Scope='MSBuild FileListAbsolute manifests and configured process cache/temp paths; not a system-wide filesystem monitor.' }
$result | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 (Join-Path $rootPath 'artifacts/local-output-verification.json')
Write-Output ("Verified {0} generated output paths and all configured caches are repository-local." -f $outputs.Count)
