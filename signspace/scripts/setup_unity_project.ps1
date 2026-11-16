#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates the Unity project for SIGNSPACE and compiles it headlessly.

.DESCRIPTION
    The repository deliberately does NOT commit ProjectSettings/ -- it is
    machine-generated and churns. This script produces it.

    Steps
      1. Create a scratch project from the URP 3D template (Unity 6.3 LTS).
      2. Copy ProjectSettings/ into unity/, leaving the existing Assets/ alone.
      3. Merge the template's package list into unity/Packages/manifest.json,
         keeping the pinned XR packages this repo owns.
      4. Run a headless compile and report every error.

    REQUIREMENTS
      - Unity 6000.3.28f1 installed (scripts/setup_toolchain.ps1 does this)
      - an ACTIVATED Unity licence. Batch mode refuses to run without one:
        "[Licensing::Module] Error: 'com.unity.editor.headless' was not found."
        Activate with:  unity auth login   (or sign in to Unity Hub)

    .PARAMETER EditorVersion
    .PARAMETER SkipCompile
    .PARAMETER SkipRenderPipeline
#>
[CmdletBinding()]
param(
    [string]$EditorVersion = '6000.3.28f1',
    [switch]$SkipCompile,
    [switch]$SkipRenderPipeline
)

$ErrorActionPreference = 'Continue'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$UnityDir = Join-Path $RepoRoot 'unity'
$Scratch  = Join-Path ([System.IO.Path]::GetTempPath()) "signspace-template-$EditorVersion"
$EditorExe = "E:\unity\Editors\$EditorVersion\Editor\Unity.exe"
$Manifest  = Join-Path $UnityDir 'Packages\manifest.json'

function Say($m) { Write-Host $m }

if (-not (Test-Path $EditorExe)) {
    Say "FATAL: Unity $EditorVersion not found at $EditorExe"
    Say "Run scripts/setup_toolchain.ps1 first."
    exit 1
}

# ---------------------------------------------------------------- 1. scratch
if (Test-Path $Scratch) { Remove-Item -Recurse -Force $Scratch }
New-Item -ItemType Directory -Force -Path $Scratch | Out-Null
Say "creating scratch project ..."
# No -cloneFromTemplate: the URP template is a Package Manager package, not a built-in
# one, and the editor's Package Manager cannot resolve it in a -createProject call
# ("Package [com.unity.template.urp-blank] cannot be found"). The default 3D project
# is created instead and the render pipeline is applied by SetupRenderPipeline.cs.
& $EditorExe -createProject $Scratch -quit -batchmode -nographics -logFile "$Scratch\create.log"

# Unity.exe returns as soon as the editor has been launched; the project is still
# being written. Poll for it rather than checking once and declaring failure.
$created = $false
for ($i = 0; $i -lt 120; $i++) {
    if (Test-Path "$Scratch\ProjectSettings") { $created = $true; break }
    Start-Sleep -Seconds 2
}
if (-not $created) {
    Say "FATAL: the scratch project was not created. Check $Scratch\create.log"
    Get-Content "$Scratch\create.log" -Tail 25 | ForEach-Object { Say "  $_" }
    exit 2
}
Say "scratch project created."

# -------------------------------------------------- 2. ProjectSettings only
Say "copying ProjectSettings into unity\ ..."
$src = Join-Path $Scratch 'ProjectSettings'
$dst = Join-Path $UnityDir 'ProjectSettings'
if (Test-Path $dst) { Remove-Item -Recurse -Force $dst }
Copy-Item -Recurse $src $dst

# NOTE: ProjectSettings is deliberately NOT stripped of the editor version.
# Unity uses m_EditorVersionWithRevision to know which editor the project belongs to;
# blanking it makes the project open with "created with a newer version" prompts and
# is worse than a line of churn in a diff. ProjectSettings IS committed - it pins the
# render pipeline asset and the graphics settings, and a build that depends on an
# uncommitted setting is not reproducible.

# ------------------------------------------------------ 3. merge the manifest
Say "merging template packages into the pinned manifest ..."
$templateManifest = Join-Path $Scratch 'Packages\manifest.json'
if (Test-Path $templateManifest) {
    $template = Get-Content $templateManifest -Raw | ConvertFrom-Json
    $mine = Get-Content $Manifest -Raw | ConvertFrom-Json

    foreach ($p in $template.dependencies.PSObject.Properties) {
        if (-not $mine.dependencies.PSObject.Properties[$p.Name]) {
            Say "  + $($p.Name) = $($p.Value)"
            $mine.dependencies | Add-Member -NotePropertyName $p.Name -NotePropertyValue $p.Value
        }
    }
    # URP is required by TECH-STACK.md; make sure it is present
    if (-not $mine.dependencies.PSObject.Properties['com.unity.render-pipelines.universal']) {
        Say "  + com.unity.render-pipelines.universal = 17.1.0  (from the template's own pin)"
        $mine.dependencies | Add-Member -NotePropertyName 'com.unity.render-pipelines.universal' -NotePropertyValue '17.1.0'
    }
    $mine | ConvertTo-Json -Depth 10 | Set-Content $Manifest
}
Say "manifest now contains:"
(Get-Content $Manifest -Raw | ConvertFrom-Json).dependencies.PSObject.Properties |
    ForEach-Object { Say ("  {0} = {1}" -f $_.Name, $_.Value) }

# ------------------------------------------------------------- 4. compile
# Two passes, because the render pipeline assets can only be created once the URP
# package has been resolved and compiled. Pass 1 resolves and compiles; pass 2
# creates the pipeline assets; pass 3 compiles again with everything in place.

$compileLog = Join-Path $UnityDir 'compile.log'
function Invoke-Unity([string]$label, [string[]]$extra) {
    Say "$label ..."
    $unityArgs = @("-batchmode", "-quit", "-projectPath", $UnityDir, "-logFile", $compileLog) + $extra
    # Start-Process -Wait, not the call operator: Unity.exe returns as soon as the
    # editor has launched, and reading the log before the compile has finished is
    # how you end up believing a build succeeded because the log was empty.
    $proc = Start-Process -FilePath $EditorExe -ArgumentList $unityArgs -Wait -PassThru -WindowStyle Hidden
    return $proc.ExitCode
}

Say "pass 1: resolve packages and compile (this downloads the Meta XR SDK) ..."
$code = Invoke-Unity "pass 1" @()

$errors = Select-String -Path $compileLog -Pattern 'error CS\d+:' -ErrorAction SilentlyContinue
Say "pass 1 exit code : $code"
Say "pass 1 errors    : $(@($errors).Count)"

# ---- the settings that have no public API in Unity 6 ----------------------
# Must run while the editor is closed: the editor loads these values at
# startup, so patching the file after it has closed is what makes the change
# stick.
Say "patching the player settings that have no public API ..."
$patchScript = Join-Path $PSScriptRoot 'patch_player_settings.ps1'
if (Test-Path $patchScript) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $patchScript | ForEach-Object { Say "  $_" }
}

if (-not $SkipRenderPipeline) {
    Say "pass 2: create and assign the render pipeline assets ..."
    Invoke-Unity "pass 2" @("-executeMethod", "Signspace.Editor.SetupRenderPipeline.Run") | Out-Null

    Say "pass 3: configure XR for Android (the OpenXR loader and Meta's feature set) ..."
    Invoke-Unity "pass 3" @("-executeMethod", "Signspace.Editor.SetupXR.Run") | Out-Null
}

Say "pass 4: final compile ..."
$code = Invoke-Unity "pass 4" @()

$errors = Select-String -Path $compileLog -Pattern 'error CS\d+:' -ErrorAction SilentlyContinue
$warnings = Select-String -Path $compileLog -Pattern 'warning CS\d+:' -ErrorAction SilentlyContinue
$licenseErrors = Select-String -Path $compileLog -Pattern "com.unity.editor.headless|License.*not found|No valid Unity Editor license" -ErrorAction SilentlyContinue

Say ""
Say "================ COMPILE RESULT ================"
Say "exit code : $code"
Say "errors    : $(@($errors).Count)"
Say "warnings  : $(@($warnings).Count)"
if ($licenseErrors) {
    Say ""
    Say "LICENCE PROBLEM -- Unity batch mode needs an activated licence:"
    Say "    unity auth login"
    Say "    (or sign in to Unity Hub; the Personal licence activates automatically)"
}
if ($errors) {
    Say ""
    Say "--- errors ---"
    $errors | Select-Object -First 40 | ForEach-Object { Say "  $($_.Line)" }
    exit 3
}
Say ""
Say "no compile errors."
if ($SkipCompile) { Say "skipping compile (-SkipCompile)"; exit 0 }
exit 0
