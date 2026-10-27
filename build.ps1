<#
.SYNOPSIS
    Build script for Pigsty - Android Storage Manager (Freeware)
.DESCRIPTION
    Compiles Pigsty using the built-in Microsoft .NET Framework MSBuild engine
    and creates a clean standalone release package in dist/
#>

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Pigsty - Android Storage Manager Build" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Locate MSBuild
$msbuildCandidates = @(
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
)

$msbuild = $null
foreach ($path in $msbuildCandidates) {
    if (Test-Path $path) {
        $msbuild = $path
        break
    }
}

if (-not $msbuild) {
    $cmd = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($cmd) {
        $msbuild = $cmd.Source
    }
}

if (-not $msbuild) {
    Write-Error "MSBuild.exe was not found on this system. Please ensure .NET Framework is installed."
    exit 1
}

Write-Host "Using MSBuild: $msbuild" -ForegroundColor Gray

# Project file
$projectPath = Join-Path $PSScriptRoot "src\Pigsty.csproj"
if (-not (Test-Path $projectPath)) {
    Write-Error "Project file not found at $projectPath"
    exit 1
}

# Run build
Write-Host "Building Pigsty (Release)..." -ForegroundColor Yellow
& $msbuild $projectPath /p:Configuration=Release /v:minimal /nologo

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Create dist and release directories
$distDir = Join-Path $PSScriptRoot "dist"
$releaseDir = Join-Path $PSScriptRoot "release"

if (Test-Path $distDir) { Remove-Item -Recurse -Force $distDir }
if (Test-Path $releaseDir) { Remove-Item -Recurse -Force $releaseDir }

New-Item -ItemType Directory -Force -Path $distDir | Out-Null
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

# Copy standalone Pigsty.exe to dist and release
$sourceExe = Join-Path $PSScriptRoot "bin\Release\Pigsty.exe"
if (Test-Path $sourceExe) {
    Copy-Item $sourceExe (Join-Path $distDir "Pigsty.exe")
    Copy-Item $sourceExe (Join-Path $releaseDir "Pigsty.exe")

    $hash = (Get-FileHash "$releaseDir\Pigsty.exe" -Algorithm SHA256).Hash
    Set-Content -Path (Join-Path $releaseDir "SHA256SUMS.txt") -Value "$hash *Pigsty.exe"

    Write-Host "`n[SUCCESS] Pigsty build and release packaging complete!" -ForegroundColor Green
    Write-Host "Portable Single Binary: $releaseDir\Pigsty.exe" -ForegroundColor Green
    Write-Host "SHA-256 Checksum: $hash" -ForegroundColor Yellow
    Write-Host "Size: $((Get-Item "$releaseDir\Pigsty.exe").Length / 1KB) KB" -ForegroundColor Green
} else {
    Write-Error "Compiled executable not found at $sourceExe"
    exit 1
}
