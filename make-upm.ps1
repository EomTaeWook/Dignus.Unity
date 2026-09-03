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
    [bool]$UpdateMarkdownVersions = $true,
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
  -UpdateMarkdownVersions
                       Sync markdown version tokens from <Version> in csproj (default: $true)
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

function Update-MarkdownVersions {
    param(
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][string[]]$Paths
    )

    foreach ($mdPath in $Paths) {
        if (-not (Test-Path $mdPath)) {
            Write-Host "Skip missing markdown file: $mdPath"
            continue
        }

        $content = Get-Content -Raw $mdPath

        $updatedContent = [regex]::Replace(
            $content,
            'https://github\.com/EomTaeWook/Dignus\.Unity\.git(\?path=publish/upm/com\.dignus\.unity)?#(v?)(\d+\.\d+\.\d+)',
            [System.Text.RegularExpressions.MatchEvaluator]{ param($match)
                $path = $match.Groups[1].Value
                if ($match.Groups[2].Value -ieq 'v') {
                    "https://github.com/EomTaeWook/Dignus.Unity.git$path#v$Version"
                } else {
                    "https://github.com/EomTaeWook/Dignus.Unity.git$path#$Version"
                }
            }
        )

        $updatedContent = [regex]::Replace(
            $updatedContent,
            '(?<![A-Za-z0-9])#(v?)(\d+\.\d+\.\d+)(?![A-Za-z0-9])',
            [System.Text.RegularExpressions.MatchEvaluator]{ param($match)
                if ($match.Groups[1].Value -ieq 'v') {
                    "#v$Version"
                } else {
                    "#$Version"
                }
            }
        )

        if ($updatedContent -ne $content) {
            Set-Content -Path $mdPath -Value $updatedContent -Encoding UTF8
            Write-Host "Updated markdown versioning: $mdPath"
        }
    }
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
$versionNode = $csproj.SelectSingleNode("//Project/PropertyGroup/Version")
if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.'#text')) {
    throw "Version node not found in csproj: $ProjectPath"
}
$projectVersion = $versionNode.'#text'.Trim()

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
    $upmReadmePath = Join-Path $UpmRoot "README.md"
    if ((Resolve-Path $PublishReadmePath).Path -ne (Resolve-Path $upmReadmePath).Path) {
        Copy-Item -Path $PublishReadmePath -Destination $upmReadmePath -Force
    }
}
if (Test-Path $PublishIconPath) {
    $upmIconPath = Join-Path $UpmRoot "Icon.jpg"
    if ((Resolve-Path $PublishIconPath).Path -ne (Resolve-Path $upmIconPath).Path) {
        Copy-Item -Path $PublishIconPath -Destination $upmIconPath -Force
    }
}

if ($CreateZip) {
    $zipPath = Join-Path $PSScriptRoot "publish\upm\com.dignus.unity-v$projectVersion.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    Compress-Archive -Path (Join-Path $UpmRoot "*") -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "Created zip: $zipPath"
}

if ($UpdateMarkdownVersions) {
    $markdownPaths = @(
        (Join-Path $PSScriptRoot "README.md"),
        (Join-Path $PSScriptRoot "publish\Dignus.Unity.md"),
        (Join-Path $PSScriptRoot "publish\upm\com.dignus.unity\README.md")
    )
    Update-MarkdownVersions -Version $projectVersion -Paths $markdownPaths
}

Write-Host "UPM generation completed. Version: $projectVersion"
