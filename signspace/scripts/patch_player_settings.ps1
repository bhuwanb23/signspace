#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Patches the Unity project settings that have no stable public API in Unity 6.

.DESCRIPTION
    Unity 6 moved the "Active Input Handling" setting out of the PlayerSettings
    API: there is no public property or field for it any more, but the
    serialized value still lives in ProjectSettings/ProjectSettings.asset as
    `activeInputHandler`.

        0 = Input Manager (Old)
        1 = Input System (New)
        2 = Both

    The Meta XR Interaction SDK drives hand and pinch input through the NEW
    input system. With the value at 0 the SDK is installed but its input
    backends are disabled, so nothing responds to a pinch on the headset. The
    project template ships 0.

    Unity 6 also ships `enableFrameTimingStats: 0`, which makes
    FrameTimingManager report zeros - so the perf probe's CPU/GPU split would
    come out blank, and a blank reads like a good number.

    Both are patched here rather than through the API, because there is no API
    for them, and the values are stable, documented, and verifiable.

    The patch runs BEFORE the editor opens the project, so Unity loads the new
    values and keeps them.

.PARAMETER ProjectPath
    The Unity project folder. Defaults to the repository's unity/ directory.

.EXAMPLE
    pwsh -File scripts/patch_player_settings.ps1
#>
[CmdletBinding()]
param(
    [string]$ProjectPath
)

$ErrorActionPreference = 'Stop'
if (-not $ProjectPath) {
    $here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
    $ProjectPath = Join-Path (Split-Path -Parent $here) 'unity'
}
$file = Join-Path $ProjectPath 'ProjectSettings\ProjectSettings.asset'

if (-not (Test-Path $file)) {
    Write-Host "no ProjectSettings.asset at $file - nothing to patch"
    exit 1
}

# The fields we care about. Top-level in the file, no indentation.
$patches = @(
    @{ name = 'activeInputHandler';    value = '2'; was = '0'; why = 'Both - the Meta XR SDK needs the new input system' },
    @{ name = 'enableFrameTimingStats'; value = '1'; was = '0'; why = 'so the perf probe can report a CPU/GPU split' },
    @{ name = 'metroPackageName';       value = 'com.signspace.signspace'; was = 'signspace-template-6000.3.28f1'; why = 'the template left its own name behind' },
    @{ name = 'metroApplicationDescription'; value = 'SIGNSPACE'; was = 'signspace-template-6000.3.28f1'; why = 'the template left its own name behind' },
    @{ name = 'AndroidPreferredInstallLocation'; value = '0'; was = '1'; why = 'auto - Meta requires auto, internalOnly is rejected' },
    @{ name = 'androidAutoRotationBehavior'; value = '0'; was = '1'; why = 'disabled - Meta requires locked landscape' },
    @{ name = 'defaultScreenOrientation'; value = '4'; was = '4'; why = 'LandscapeLeft - Meta requires landscape' }
)

$text = [System.IO.File]::ReadAllText($file)
$changed = $false

foreach ($patch in $patches) {
    $name = $patch.name
    $value = $patch.value
    # The PlayerSettings block is indented two spaces, so the leading whitespace
    # has to be captured and put back, or the YAML shape changes.
    $pattern = '(?m)^([ \t]*)(' + [regex]::Escape($name) + '):\s*.*$'

    $match = [regex]::Match($text, $pattern)
    if (-not $match.Success) {
        Write-Host ("  {0,-30} MISSING" -f $name)
        continue
    }

    $current = $match.Groups[2].Value + ': ' + $match.Value.Substring($match.Value.IndexOf(':') + 1).Trim()
    if ($current -eq ($name + ': ' + $value)) {
        Write-Host ("  {0,-30} already {1}" -f $name, $value)
        continue
    }

    $replacement = '${1}' + $name + ': ' + $value
    $text = [regex]::Replace($text, $pattern, $replacement)
    $changed = $true
    Write-Host ("  {0,-30} -> {1}   ({2})" -f $name, $value, $patch.why)
}

if ($changed) {
    [System.IO.File]::WriteAllText($file, $text)
    Write-Host "ProjectSettings.asset written"
} else {
    Write-Host "nothing to change"
}

# verify by reading it back
Write-Host "verify:"
$check = [System.IO.File]::ReadAllText($file)
foreach ($patch in $patches) {
    $m = [regex]::Match($check, '(?m)^[ \t]*' + [regex]::Escape($patch.name) + ':\s*(\S+)')
    Write-Host ("  {0,-30} = {1}" -f $patch.name, $(if ($m.Success) { $m.Groups[1].Value } else { 'MISSING' }))
}
