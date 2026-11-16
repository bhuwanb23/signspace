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
    .PARAMETER Template
#>
[CmdletBinding()]
param(
    [string]$EditorVersion = '6000.3.28f1',
    [string]$Template = 'com.unity.template.urp-blank',
    [switch]$SkipCompile
)

$ErrorActionPreference = 'Continue'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$UnityDir = Join-Path $RepoRoot 'unity'
$Scratch  = Join-Path ([System.IO.Path]::GetTempPath()) "signspace-template-$EditorVersion"
$EditorExe = "E:\unity\Editors\$EditorVersion\Editor\Unity.exe"
$Manifest  = Join-Path $UnityDir 'Packages\manifest.json'

function Say($m) { Write-Host $m; Write-Host $m }

if (-not (Test-Path $EditorExe)) {
    Say "FATAL: Unity $EditorVersion not found at $EditorExe"
    Say "Run scripts/setup_toolchain.ps1 first."
    exit 1
}

# ---------------------------------------------------------------- 1. scratch
if (Test-Path $Scratch) { Remove-Item -Recurse -Force $Scratch }
New-Item -ItemType Directory -Force -Path $Scratch | Out-Null
Say "creating scratch project from template $Template ..."
& $EditorExe -createProject $Scratch -cloneFromTemplate $Template -quit -batchmode -nographics -logFile "$Scratch\create.log"
if (-not (Test-Path "$Scratch\ProjectSettings")) {
    Say "FATAL: template project was not created. Check $Scratch\create.log"
    Say "If the log mentions com.unity.editor.headless, your Unity licence is not activated."
    Get-Content "$Scratch\create.log" -Tail 20 | ForEach-Object { Say "  $_" }
    exit 2
}

# -------------------------------------------------- 2. ProjectSettings only
Say "copying ProjectSettings into unity\ ..."
$src = Join-Path $Scratch 'ProjectSettings'
$dst = Join-Path $UnityDir 'ProjectSettings'
if (Test-Path $dst) { Remove-Item -Recurse -Force $dst }
Copy-Item -Recurse $src $dst

# the project path is machine specific and must not be committed
$vs = Join-Path $dst 'ProjectSettings.asset'
if (Test-Path $vs) {
    (Get-Content $vs -Raw) -replace 'm_EditorVersionWithRevision: .*', 'm_EditorVersionWithRevision: ' | Set-Content $vs -NoNewline
}

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
if ($SkipCompile) { Say "skipping compile (-SkipCompile)"; exit 0 }

$compileLog = Join-Path $UnityDir 'compile.log'
Say "compiling headlessly (this resolves packages and can take several minutes) ..."
& $EditorExe -projectPath $UnityDir -quit -batchmode -nographics -logFile $compileLog
$code = $LASTEXITCODE

$errors = Select-String -Path $compileLog -Pattern '^.*error CS\d+:' -ErrorAction SilentlyContinue
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
exit 0
