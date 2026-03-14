param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$ReleaseName = "ExileApi-by-PanCrucian",
    [switch]$NoZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function New-CleanDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path | Out-Null
}

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishRoot = Join-Path $artifactsRoot (Join-Path "publish" (Join-Path $Configuration $RuntimeIdentifier))
$releaseFolderName = "$ReleaseName-$Configuration-$RuntimeIdentifier"
$releaseRoot = Join-Path $artifactsRoot (Join-Path "release" $releaseFolderName)
$zipPath = Join-Path (Join-Path $artifactsRoot "release") "$releaseFolderName.zip"
$loaderProject = Join-Path $repoRoot "Loader\Loader.csproj"
$releaseReadme = Join-Path $repoRoot "packaging\README.release.md"

Ensure-Directory -Path $artifactsRoot
Ensure-Directory -Path (Split-Path -Parent $publishRoot)
Ensure-Directory -Path (Split-Path -Parent $releaseRoot)

New-CleanDirectory -Path $publishRoot

Write-Host "Publishing Loader for $RuntimeIdentifier ($Configuration)..."
dotnet publish $loaderProject `
    -nologo `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained false `
    /p:UseAppHost=true `
    --output $publishRoot

New-CleanDirectory -Path $releaseRoot
Copy-Item -Path (Join-Path $publishRoot "*") -Destination $releaseRoot -Recurse -Force

$runtimeDirectories = @(
    "config",
    "config\global",
    "config\themes",
    "Data",
    "fonts",
    "Logs",
    "Plugins",
    "Plugins\Compiled",
    "Plugins\Source",
    "Sounds"
)

foreach ($relativePath in $runtimeDirectories) {
    Ensure-Directory -Path (Join-Path $releaseRoot $relativePath)
}

Copy-Item -Path $releaseReadme -Destination (Join-Path $releaseRoot "README.md") -Force

Set-Content -Path (Join-Path $releaseRoot "Plugins\Compiled\README.txt") -Value @(
    "Place compiled plugin folders here."
    "Expected layout: Plugins\Compiled\<PluginName>\<PluginName>.dll"
) -Encoding ASCII

Set-Content -Path (Join-Path $releaseRoot "Plugins\Source\README.txt") -Value @(
    "Source-plugin hot-compilation is disabled on the PanCrucian .NET 10 branch."
    "Keep source plugins in the source repository, not in this runtime package."
) -Encoding ASCII

$gitCommit = ""

try {
    $gitCommit = (git -C $repoRoot rev-parse HEAD).Trim()
}
catch {
    $gitCommit = ""
}

$releaseInfo = [ordered]@{
    name = $ReleaseName
    configuration = $Configuration
    runtimeIdentifier = $RuntimeIdentifier
    commit = $gitCommit
    builtAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    sourceRepository = "https://github.com/PanCrucian/ExileApi-by-PanCrucian"
}

$releaseInfo | ConvertTo-Json -Depth 3 | Set-Content -Path (Join-Path $releaseRoot "release-info.json") -Encoding ASCII

if (-not $NoZip) {
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $releaseRoot "*") -DestinationPath $zipPath -CompressionLevel Optimal
}

Write-Host ""
Write-Host "Release package ready:"
Write-Host "  $releaseRoot"

if (-not $NoZip) {
    Write-Host "Zip archive ready:"
    Write-Host "  $zipPath"
}
