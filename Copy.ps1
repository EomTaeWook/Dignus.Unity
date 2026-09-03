param()

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$candidateRoots = @(
    $scriptDir,
    (Resolve-Path (Join-Path $scriptDir '..')).Path,
    (Resolve-Path (Join-Path $scriptDir '..\..')).Path,
    (Resolve-Path (Join-Path $scriptDir '..\..\..\..')).Path
)

$workRoot = $null
foreach ($root in $candidateRoots) {
    if (Test-Path (Join-Path $root 'Dignus') -and Test-Path (Join-Path $root 'Dignus.Unity')) {
        $workRoot = $root
        break
    }
}

if (-not $workRoot) {
    Write-Error "[ERROR] Source root not found. Checked: $($candidateRoots -join ', ')"
    exit 1
}

$destRoot = Join-Path $scriptDir 'Lib'
if (-not (Test-Path $destRoot)) {
    $destRoot = Join-Path $workRoot 'UnityTest\Assets\Plugins\Lib'
}

if (-not (Test-Path $destRoot)) {
    Write-Error "[ERROR] Destination root not found. Expected: $destRoot"
    exit 1
}

function Copy-FolderFiltered {
    param(
        [string]$Source,
        [string]$Destination
    )

    Write-Host "---------------------------"
    Write-Host "[COPY] $([System.IO.Path]::GetFileName($Source)) → Unity Plugins (filtered)"
    Write-Host "Source: $Source"
    Write-Host "Target: $Destination"
    Write-Host "---------------------------"

    if (-not (Test-Path $Source)) {
        Write-Error "[ERROR] Source not found: $Source"
        exit 1
    }

    if (Test-Path $Destination) {
        Write-Host "Removing old copy..."
        Remove-Item -Recurse -Force $Destination
    }

    $robocopyArgs = @(
        $Source
        $Destination
        '/E'
        '/XD'
        'bin'
        'obj'
        'Properties'
        '.vs'
        '*.dll'
        '/XF'
        '*.csproj.user'
    )

    & robocopy @robocopyArgs
    $exitCode = $LASTEXITCODE

    if ($exitCode -lt 8) {
        Write-Host "[OK] $([System.IO.Path]::GetFileName($Source)) copied with filters."
    }
    else {
        Write-Error "[ERROR] Robocopy failed with code: $exitCode"
        exit 1
    }
}

Copy-FolderFiltered -Source (Join-Path $workRoot 'Dignus')     -Destination (Join-Path $destRoot 'Dignus')
Copy-FolderFiltered -Source (Join-Path $workRoot 'Dignus.Unity') -Destination (Join-Path $destRoot 'Dignus.Unity')
