Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$legacyArtifacts = Join-Path $repoRoot "artifacts\PanCrucian.Net10"

if (-not (Test-Path -LiteralPath $legacyArtifacts)) {
    Write-Host "Legacy artifact folder not found:"
    Write-Host "  $legacyArtifacts"
    exit 0
}

Remove-Item -LiteralPath $legacyArtifacts -Recurse -Force

Write-Host "Removed legacy artifact folder:"
Write-Host "  $legacyArtifacts"
