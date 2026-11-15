#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Installs the SIGNSPACE XR toolchain onto the E: drive, unattended and idempotent.

.DESCRIPTION
    Everything heavy lands on E:. Unity Hub's editor install path defaults to
    C:\Program Files\Unity\Hub\Editor, and this machine only has ~28 GB free on C: --
    an editor plus the Android modules is ~14 GB, so the path is moved first.

    Steps
      1. Create the E:\unity\ layout.
      2. Point Unity's editor install path at E:\unity\Editors.
      3. Install Unity 6000.3.28f1 (Unity 6.3 LTS) with Android Build Support
         and all child modules (SDK, NDK r27c, OpenJDK, cmake).
      4. Redirect Unity's download cache and the UPM global cache off C:.
      5. Install the Meta XR Simulator 207.1 via the Meta VR CLI.
      6. Print a verification table.

    Re-running is safe: each step is skipped when it is already satisfied.

.PARAMETER EditorVersion
    Unity editor version to install. 6000.3.28f1 is Unity 6.3 LTS, supported to
    Dec 2027, and clears Meta XR All-in-One v207's floor of 6000.0.66f2.
    NOTE: 6000.0.85f2 (Unity 6.0 LTS) reached end of standard support on
    16 Oct 2026 -- do not use it. See docs/02-tech/TECH-STACK.md.

.PARAMETER SkipSimulator
    Skip the Meta XR Simulator install (415 MB). Only useful when re-running
    for the Unity editor alone.

.EXAMPLE
    pwsh -File scripts/setup_toolchain.ps1
#>
[CmdletBinding()]
param(
    [string]$EditorVersion = '6000.3.28f1',
    [switch]$SkipSimulator
)

$ErrorActionPreference = 'Continue'

$Root       = 'E:\unity'
$EditorsDir = Join-Path $Root 'Editors'
$CacheDir   = Join-Path $Root 'cache'
$MetaXRDir  = Join-Path $Root 'MetaXR'
$LogFile    = Join-Path $Root 'setup-log.txt'

$UnityCli = 'C:\Users\Bhuwan\AppData\Local\Unity\bin\unity.exe'
if (-not (Test-Path $UnityCli)) {
    $UnityCli = (Get-Command unity -ErrorAction SilentlyContinue).Source
}

function Write-Step {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format 'HH:mm:ss'), $Message
    Write-Host $line
    Add-Content -Path $LogFile -Value $line
}

function Invoke-Logged {
    param([string]$Label, [scriptblock]$Action)
    Write-Step "START  $Label"
    $out = & $Action 2>&1 | Out-String
    Add-Content -Path $LogFile -Value $out
    $code = $LASTEXITCODE
    Write-Step "DONE   $Label (exit $code)"
    return $out
}

# ---------------------------------------------------------------- 0. layout
New-Item -ItemType Directory -Force -Path $Root, $EditorsDir, $CacheDir, $MetaXRDir | Out-Null
Write-Step "SIGNSPACE toolchain setup -- log at $LogFile"

if (-not (Test-Path $UnityCli)) {
    Write-Step "FATAL: Unity CLI not found. Install it with: npx metavr@latest tools install unity-cli"
    exit 1
}

# ------------------------------------------- 1. editor install path -> E:
$current = (& $UnityCli install-path --get 2>&1 | Out-String).Trim()
Write-Step "editor install path currently: $current"
if ($current -notlike "$EditorsDir*") {
    Invoke-Logged "set editor install path -> $EditorsDir" { & $UnityCli install-path --set $EditorsDir }
    $current = (& $UnityCli install-path --get 2>&1 | Out-String).Trim()
    Write-Step "editor install path now: $current"
} else {
    Write-Step "editor install path already on E: -- skip"
}

# ------------------------------------------------- 2. install the editor
$installed = (& $UnityCli editors 2>&1 | Out-String)
if ($installed -match [regex]::Escape($EditorVersion) -and $installed -match 'true') {
    Write-Step "$EditorVersion already installed -- skip"
} else {
    Write-Step "installing $EditorVersion with android modules. This downloads ~14 GB. Do not close this window."
    Invoke-Logged "install $EditorVersion + android" {
        & $UnityCli install $EditorVersion -m android --cm --accept-eula -y --no-banner
    }
}

# ---------------------------------------- 3. verify + add modules if needed
$installed = (& $UnityCli editors 2>&1 | Out-String)
if ($installed -notmatch [regex]::Escape($EditorVersion)) {
    Write-Step "FATAL: $EditorVersion still not listed after install. Check $LogFile"
    exit 2
}
Write-Step "editor present."

# The editor install can succeed while the android child modules lag behind;
# this pass is a no-op when they are already present.
Invoke-Logged "ensure android modules" {
    & $UnityCli install-modules -e $EditorVersion -m android --cm --accept-eula -y --no-banner
}

# ------------------------------------------------- 4. caches off C: drive
foreach ($pair in @(
    @{ n = 'UPM_CACHE_PATH';        v = (Join-Path $CacheDir 'upm') },
    @{ n = 'UPM_GLOBAL_CACHE_PATH'; v = (Join-Path $CacheDir 'upm-global') })) {
    New-Item -ItemType Directory -Force -Path $pair.v | Out-Null
    [Environment]::SetEnvironmentVariable($pair.n, $pair.v, 'User')
    Write-Step "$($pair.n) = $($pair.v)"
}

# ------------------------------------------ 5. Meta XR Simulator 207.1
if ($SkipSimulator) {
    Write-Step "skipping Meta XR Simulator (-SkipSimulator)"
} else {
    $metavr = (Get-Command metavr -ErrorAction SilentlyContinue).Source
    if (-not $metavr) {
        Write-Step "metavr not on PATH -- trying npx"
        Invoke-Logged "install xrsim via npx metavr" { npx --yes metavr@latest tools install xrsim }
    } else {
        Invoke-Logged "install xrsim via metavr" { & $metavr tools install xrsim }
    }
}

# ------------------------------------------------------- 6. verification
Write-Step "================ VERIFICATION ================"
Write-Step "editor install path : $((& $UnityCli install-path --get 2>&1 | Out-String).Trim())"
Write-Step "installed editors   :"
(& $UnityCli editors 2>&1 | Out-String) -split "`n" | ForEach-Object { if ($_.Trim()) { Add-Content -Path $LogFile -Value $_.Trim(); Write-Host "  $_" } }

$androidPlayer = Join-Path $EditorsDir "$EditorVersion\Editor\Data\PlaybackEngines\AndroidPlayer"
if (Test-Path $androidPlayer) {
    Write-Step "android player present: $androidPlayer"
} else {
    Write-Step "WARNING: android player not found at $androidPlayer -- re-run with -EditorVersion $EditorVersion"
}

Write-Step "=============== SETUP COMPLETE ==============="
