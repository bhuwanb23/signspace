# P9.1 — submitted build record

**Phase:** P9 · **Task:** P9.1 · **Gate:** G9 · **Written:** 17 Nov 2026

## Freeze record

| Field | Value |
|---|---|
| APK path | `unity/Builds/Android/signspace.apk` |
| Package name | `com.signspace.signspace` |
| Version name | `1.0.0-competition` |
| Bundle version code | (from `version-code.txt` at freeze time) |
| SHA-256 | (from `SIGNSPACE/P9 Check all`) |
| Build command | `Unity.exe -batchmode -quit -projectPath unity -executeMethod Signspace.Editor.BuildScript.BuildAndroid` |
| Commit hash | (tag `v1.0-competition`) |
| Frozen at | (UTC timestamp from `FreezeBuild()`) |

## Verification checklist

- [ ] `SIGNSPACE/P9 Check all` exits 0
- [ ] APK installs on Quest 3 (hardware)
- [ ] APK installs on Quest 3S (hardware)
- [ ] App launches with hands + passthrough
- [ ] App name reads `SIGNSPACE`, not "Untitled"
- [ ] `aapt2 dump permissions` shows `horizonos.permission.HEADSET_CAMERA`
- [ ] SHA-256 matches the freeze record
- [ ] `git tag v1.0-competition` created and pushed

## How to freeze

```powershell
# 1. bump the version
#    BuildScript.cs: const string Version = "1.0.0-competition";

# 2. run the freeze check
"E:\unity\Editors\6000.3.28f1\Editor\Unity.exe" -batchmode -quit -projectPath unity `
  -executeMethod Signspace.Editor.P9Checks.CheckAll

# 3. tag and push
git tag v1.0-competition
git push origin v1.0-competition

# 4. do not touch anything after this
```

## Change log

| Date | Change | By |
|---|---|---|
| 17 Nov 2026 | File created. Template for the freeze record. | agent |
