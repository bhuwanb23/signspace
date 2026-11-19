# Frozen build record

**Written:** 19 Nov 2026 · **Phase:** P9 · **Task:** P9.8 · **Gate:** G9
**Source of truth:** `phases/p9-submission/apk-hash.txt` (this file explains it; that one is hashed)

---

## 1. The artifact

| | |
|---|---|
| Path | `unity/Builds/Android/signspace.apk` |
| Size | 63,752,960 bytes |
| SHA-256 | `a1587624f223cbe7ac11ba9d72a16c1f4022e850decb31dfd1f14d43ea82dc7c` |
| Built | 2026-11-18 17:09:49 UTC |
| Package | `com.signspace.signspace` |
| Version name | `1.0.0-competition` |
| Version code | **14** (`BuildScript.BundleVersionCode`; `aapt2 dump badging` agrees) |
| Commit | `3e5333cda0a45af7166cc06f86e4f35f6e41a51c` |
| Tag | `v1.0-competition` → that commit |
| Min SDK / target SDK | 32 / 34 |
| ABI | `arm64-v8a` only (Quest is ARM64-only; the build always set this, and the 32-bit Unity libraries were stripped on 18 Nov) |
| Scripting backend | IL2CPP, Release |
| Manifest facts | `screenOrientation` landscape, `installLocation=auto`, `horizonos.permission.HEADSET_CAMERA` requested |

## 2. How it was built

```powershell
Unity.exe -batchmode -quit -projectPath unity `
  -executeMethod Signspace.Editor.BuildScript.BuildAndroid
```

Toolchain, as pinned in the project:

| Component | Version | Where it is pinned |
|---|---|---|
| Unity | 6000.3.28f1 (`8f082d2fe476`) | `ProjectSettings/ProjectVersion.txt` |
| Meta XR All-in-One | 207.0.0 | `Packages/manifest.json` |
| OpenXR plugin | 1.18.0 | `Packages/manifest.json` |
| XR Management | 4.6.1 | `Packages/manifest.json` |
| Android SDK | 36.0.0 build-tools | `docs/02-tech/TECH-STACK.md` |

## 3. What this record does and does not certify

**Certified by a command** (`scripts/freeze_check.py`, 10 PASS / 6 FAIL / 3 NOT RUN on 19 Nov):

- the bytes on disk hash to the number above, and have not changed since the record was written;
- the packaged manifest reports version code 14 and version name `1.0.0-competition`;
- the build constants, `version-code.txt` and the record all agree;
- the tag exists and points at the commit the record names.

**Not certified — do not read this file as claiming it:**

- that the APK installs on a Quest. It has never been side-loaded in this repository's record.
- that it reaches 72 fps, survives 20 cold launches, or holds tracking under adverse light.
- that a stranger can open it from an invite. The invite URL was never recorded.
- that any of the six shipped shapes is signer-approved. Both files say `reference-authored`.

The distinction matters: this is a record of a **build**, not a record of a **working entry**. The rows
that would make it the second are H34–H40 and H43–H46 in `docs/00-plan/HUMAN-TASKS.md`, and they remain
unticked.

## 4. If the artifact is ever rebuilt

The hash above stops being true the moment the APK is rebuilt, even with the same source. A rebuild must:

1. bump `BundleVersionCode` (a judge may compare it against the channel listing);
2. re-run `scripts/freeze_check.py` and append a row to `../reverify-log.md` — never edit an old row;
3. move the tag, or create a new one. Two builds, one tag is how a submission becomes unverifiable.
