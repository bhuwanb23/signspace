#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Resolves packages, configures the XR project, and compiles it. Headless.

.DESCRIPTION
    This script is the single entry point for "make the Unity project correct".
    It is idempotent: run it as often as you like, it converges on the same state.

    What it does, in order, and why each step exists:

      1. GUARD THE MANIFEST. Unity engine MODULES are easy to lose by hand-editing
         Packages/manifest.json, and losing them is not a warning - it is 144
         compile errors in the Meta SDK's own code, because the SDK uses
         UnityEngine.AnimationModule (HumanBodyBones) and UnityEngine.TerrainModule.
         This step puts back anything missing and says so.

      2. RESOLVE PACKAGES AND COMPILE. The Meta XR SDK is fetched and every
         script in the project is compiled. This is the step that proves anything.

      3. PATCH THE SETTINGS UNITY 6 HAS NO API FOR. The active input handling
         lives in ProjectSettings.asset as activeInputHandler and there is no
         public property for it. The template ships 0 (the old input manager)
         and the Meta XR Interaction SDK drives pinch through the new one, so
         nothing would respond to a pinch.

      4. CREATE AND ASSIGN THE RENDER PIPELINE. Unity 6's URP template is a
         Package Manager package that -createProject cannot resolve, so the
         default project is Built-in RP. An unassigned pipeline renders magenta
         on the headset.

      5. CONFIGURE XR FOR ANDROID. The XR Plug-in Management window does not
         exist in batch mode, so a build would ship with no XR loader at all:
         it installs, it launches, and it shows a black screen.

      6. COMPILE AGAIN AND REPORT.

    ProjectSettings/ IS COMMITTED, so a clean rebuild is:
        git checkout -- unity/ProjectSettings
    and there is no scratch template involved any more.

.PARAMETER EditorVersion
.PARAMETER SkipCompile
.PARAMETER SkipRenderPipeline
.PARAMETER SkipXR

.EXAMPLE
    pwsh -File scripts/setup_unity_project.ps1
#>
[CmdletBinding()]
param(
    [string]$EditorVersion = '6000.3.28f1',
    [switch]$SkipCompile,
    [switch]$SkipRenderPipeline,
    [switch]$SkipXR
)

$ErrorActionPreference = 'Continue'
$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot = Split-Path -Parent $here
$UnityDir = Join-Path $RepoRoot 'unity'
$EditorExe = "E:\unity\Editors\$EditorVersion\Editor\Unity.exe"
$Manifest = Join-Path $UnityDir 'Packages\manifest.json'
$compileLog = Join-Path $UnityDir 'compile.log'

function Say($m) { Write-Host $m }

if (-not (Test-Path $EditorExe)) {
    Say "FATAL: Unity $EditorVersion not found at $EditorExe"
    Say "Run scripts/setup_toolchain.ps1 first."
    exit 1
}

function Invoke-Unity([string]$label, [string[]]$extra) {
    Say "$label ..."
    $unityArgs = @("-batchmode", "-quit", "-projectPath", $UnityDir, "-logFile", $compileLog) + $extra
    # Start-Process -Wait, not the call operator: Unity.exe returns as soon as the
    # editor has launched, and reading the log before the compile has finished is
    # how you end up believing a build succeeded because the log was empty.
    $proc = Start-Process -FilePath $EditorExe -ArgumentList $unityArgs -Wait -PassThru -WindowStyle Hidden
    return $proc.ExitCode
}

# ------------------------------------------------------- 1. guard the manifest
# These four are load-bearing. The engine modules are needed by Meta's own code;
# URP and ugui are needed by this project.
$required = @{
    'com.unity.render-pipelines.universal' = '17.3.0'
    'com.unity.ugui'                      = '2.0.0'
    'com.unity.modules.animation'          = '1.0.0'
    'com.unity.modules.terrain'            = '1.0.0'
    'com.unity.modules.physics'            = '1.0.0'
    'com.unity.modules.ui'                 = '1.0.0'
    'com.unity.modules.unitywebrequest'    = '1.0.0'
    'com.unity.modules.xr'                 = '1.0.0'
    'com.unity.modules.jsonserialize'      = '1.0.0'
}

Say "1. guarding Packages/manifest.json ..."
$manifestText = [System.IO.File]::ReadAllText($Manifest)
$manifest = $manifestText | ConvertFrom-Json
$added = 0
foreach ($name in $required.Keys) {
    if (-not $manifest.dependencies.PSObject.Properties[$name]) {
        $manifest.dependencies | Add-Member -NotePropertyName $name -NotePropertyValue $required[$name]
        Say "   + $name = $($required[$name])"
        $added++
    }
}
if ($added -gt 0) {
    $manifest | ConvertTo-Json -Depth 10 | Set-Content $Manifest
    Say "   ($added restored. Losing these is not a warning - it is 144 compile errors in Meta's own code.)"
} else {
    Say "   manifest is complete."
}

# ------------------------------------------------ 2. resolve and compile once
Say "2. resolving packages and compiling ..."
$code = Invoke-Unity "resolve + compile" @()
$errors = @(Select-String -Path $compileLog -Pattern 'error CS\d+:' -ErrorAction SilentlyContinue)
Say "   exit code : $code"
Say "   errors    : $($errors.Count)"
if ($errors.Count -gt 0) {
    Say ""
    Say "--- errors ---"
    $errors | ForEach-Object { Say "  $(($_.Line -replace '^.*?Assets','Assets') -replace '\s+',' ')" } |
        Select-Object -First 30 | ForEach-Object { Say $_ }
    exit 3
}

# ------------------------------ 3. the settings Unity 6 has no public API for
Say "3. patching the player settings that have no public API ..."
$patchScript = Join-Path $here 'patch_player_settings.ps1'
if (Test-Path $patchScript) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $patchScript | ForEach-Object { Say "  $_" }
}

# ------------------------------------------------- 4 and 5. pipeline and XR
if (-not $SkipRenderPipeline) {
    Say "4. creating and assigning the render pipeline ..."
    Invoke-Unity "render pipeline" @("-executeMethod", "Signspace.Editor.SetupRenderPipeline.Run") | Out-Null
}

if (-not $SkipXR) {
    Say "5. configuring XR for Android ..."
    Invoke-Unity "XR" @("-executeMethod", "Signspace.Editor.SetupXR.Run") | Out-Null
}

# ------------------------------------------------------------ 6. final compile
Say "6. final compile ..."
$code = Invoke-Unity "final compile" @()

$errors = @(Select-String -Path $compileLog -Pattern 'error CS\d+:' -ErrorAction SilentlyContinue)
$warnings = @(Select-String -Path $compileLog -Pattern 'warning CS\d+:' -ErrorAction SilentlyContinue)

Say ""
Say "================ COMPILE RESULT ================"
Say "exit code : $code"
Say "errors    : $($errors.Count)"
Say "warnings  : $($warnings.Count)"
if ($errors.Count -gt 0) {
    Say ""
    Say "--- errors ---"
    $errors | ForEach-Object { Say "  $(($_.Line -replace '^.*?Assets','Assets') -replace '\s+',' ')" } |
        Select-Object -First 30 | ForEach-Object { Say $_ }
    exit 3
}
Say ""
Say "no compile errors."
exit 0
