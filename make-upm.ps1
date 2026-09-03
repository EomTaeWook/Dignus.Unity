param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$ProjectPath = (Join-Path $PSScriptRoot "Dignus.Unity\Dignus.Unity.csproj"),
    [string]$UpmRoot = (Join-Path $PSScriptRoot "publish\upm\com.dignus.unity"),
    [string]$BinRoot = (Join-Path $PSScriptRoot "Dignus.Unity\bin"),
    [string]$PublishLicensePath = (Join-Path $PSScriptRoot "LICENSE"),
    [string]$PublishReadmePath = (Join-Path $PSScriptRoot "publish\upm\com.dignus.unity\README.md"),
    [string]$PublishIconPath = (Join-Path $PSScriptRoot "publish\upm\com.dignus.unity\Icon.jpg"),
    [switch]$SkipBuild,
    [switch]$CreateZip,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

function Print-Usage {
    @"
Usage:
  .\make-upm.ps1
  .\make-upm.ps1 -Configuration Release
  .\make-upm.ps1 -SkipBuild
  .\make-upm.ps1 -CreateZip

Parameters:
  -Configuration        Build configuration (Debug|Release, default: Release)
  -ProjectPath          Path to Dignus.Unity.csproj
  -UpmRoot              Path to publish/upm/com.dignus.unity
  -SkipBuild            Skip dotnet build step
  -CreateZip            Create zip package as publish/upm/com.dignus.unity-v<version>.zip
  -Help                 Show this usage
"@
}

if ($Help) {
    Print-Usage
    exit 0
}

if (-not (Test-Path $ProjectPath)) {
    throw "Project file not found: $ProjectPath"
}

if (-not (Test-Path $UpmRoot)) {
    throw "UPM root not found: $UpmRoot"
}

$projectDir = Split-Path -Path $ProjectPath -Parent
$runtimeRoot = Join-Path $UpmRoot "Runtime"
if (-not (Test-Path $runtimeRoot)) {
    New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null
}

if (-not $SkipBuild) {
    Write-Host "Build: $ProjectPath ($Configuration)"
    & dotnet build $ProjectPath -c $Configuration -m:1 /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
}

[xml]$csproj = Get-Content -Raw $ProjectPath
$versionNode = $csproj.Project.PropertyGroup.Version | Select-Object -First 1
$projectVersion = if ($versionNode) { $versionNode.Trim() } else { "1.0.0" }

$packageJsonPath = Join-Path $UpmRoot "package.json"
if (Test-Path $packageJsonPath) {
    $json = Get-Content -Raw $packageJsonPath | ConvertFrom-Json
    if ($json.version -ne $projectVersion) {
        Write-Host "Sync package.json version: $($json.version) -> $projectVersion"
        $json.version = $projectVersion
        $json | ConvertTo-Json -Depth 20 | Set-Content -Path $packageJsonPath -Encoding UTF8
    }
}

$targetCandidates = @(
    (Join-Path $BinRoot "$Configuration\netstandard2.1"),
    (Join-Path $BinRoot "$Configuration\netstandard2.0"),
    (Join-Path $BinRoot "$Configuration\net481"),
    (Join-Path $BinRoot "$Configuration\net48")
)

$sourceDir = $null
foreach ($path in $targetCandidates) {
    if (Test-Path (Join-Path $path "Dignus.Unity.dll")) {
        $sourceDir = $path
        break
    }
}

if (-not $sourceDir) {
    throw "Cannot find Dignus.Unity.dll in build output. Check build result."
}

Write-Host "Copy runtime assemblies from: $sourceDir"
Copy-Item -Path (Join-Path $sourceDir "Dignus.Unity.dll") -Destination (Join-Path $runtimeRoot "Dignus.Unity.dll") -Force

$dignusDependency = Join-Path $sourceDir "Dignus.dll"
if (Test-Path $dignusDependency) {
    Copy-Item -Path $dignusDependency -Destination (Join-Path $runtimeRoot "Dignus.dll") -Force
} else {
    Write-Warning "Dependency Dignus.dll was not found in $sourceDir. Keeping existing one in $runtimeRoot if present."
}

if (Test-Path $PublishLicensePath) {
    Copy-Item -Path $PublishLicensePath -Destination (Join-Path $UpmRoot "LICENSE") -Force
}
if (Test-Path $PublishReadmePath) {
    Copy-Item -Path $PublishReadmePath -Destination (Join-Path $UpmRoot "README.md") -Force
}
if (Test-Path $PublishIconPath) {
    Copy-Item -Path $PublishIconPath -Destination (Join-Path $UpmRoot "Icon.jpg") -Force
}

if ($CreateZip) {
    $zipPath = Join-Path $PSScriptRoot "publish\upm\com.dignus.unity-v$projectVersion.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    Compress-Archive -Path (Join-Path $UpmRoot "*") -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "Created zip: $zipPath"
}

Write-Host "UPM generation completed. Version: $projectVersion"
