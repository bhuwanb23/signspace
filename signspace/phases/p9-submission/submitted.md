# P9.1 — submitted build record

**Phase:** P9 · **Task:** P9.1 · **Gate:** G9 · **Written:** 17 Nov 2026 · **Updated:** 19 Nov 2026

> Two different things live in this file, and they have different owners.
>
> **The build** is settled: it exists, it is hashed, it is tagged, and every fact below was read from the
> artifact or the repository. **The submission** is not: a submission ID, an invite URL and a confirmation
> email can only come from the dashboards, and none of them is recorded here. The blanks below start with
> `(` so that `scripts/freeze_check.py` keeps failing those rows instead of passing on a placeholder.

---

## 1. Frozen build — verified

| Field | Value | Verified by |
|---|---|---|
| APK path | `unity/Builds/Android/signspace.apk` | `freeze_check.py` row 1 |
| Size | 63,752,960 bytes | filesystem |
| Package name | `com.signspace.signspace` | `aapt2 dump badging` |
| Version name | `1.0.0-competition` | `aapt2`, `BuildScript.Version`, record |
| Bundle version code | **14** | `aapt2` = 14 · `BuildScript.BundleVersionCode` = 14 · `version-code.txt` = 14 |
| SHA-256 | `a1587624f223cbe7ac11ba9d72a16c1f4022e850decb31dfd1f14d43ea82dc7c` | `freeze_check.py` row 2 |
| Commit hash | `3e5333cda0a45af7166cc06f86e4f35f6e41a51c` | `git rev-parse`, ancestor of HEAD |
| Tag | `v1.0-competition` → that commit | `freeze_check.py` row 9 |
| Frozen at | 2026-11-18T17:09:49Z | APK mtime; matches the record |
| Build command | `Unity.exe -batchmode -quit -projectPath unity -executeMethod Signspace.Editor.BuildScript.BuildAndroid` | `BuildScript.cs` |
| Manifest facts | `screenOrientation` landscape · `installLocation=auto` · `horizonos.permission.HEADSET_CAMERA` | `aapt2 dump xmltree` / `dump permissions` |
| ABI | `arm64-v8a` only | `unzip -l` on the APK |

The canonical copy of the hash is `apk-hash.txt`; this section is a reader for it, and `BUILD.md` in
`archive/` explains the build. If the two ever disagree, `apk-hash.txt` wins.

## 2. Submission — **OWNER ONLY, not recorded**

These are the three fields a judge's path depends on. They cannot be derived from the repository, and this
file must not pretend otherwise.

| Field | Value | Where it comes from |
|---|---|---|
| Submission ID | (owner — paste from the Devpost entry) | Devpost entry page, after submit |
| Invite URL | (owner — paste the Meta invite link) | Meta developer dashboard → the app's invite/distribution page |
| Confirmation email | (owner — paste the sender and the UTC time it arrived) | the submission confirmation in the owner's inbox |
| Channel | (owner — confirm `Competition` lists `1.0.0-competition`, code 14) | Meta dashboard → release channels |
| Video URL | (owner — the public video link) | Devpost entry / YouTube |

**Status of the submission itself: UNRESOLVED as of 19 Nov 2026, 08:40 IST.** `phases/p0-foundation/README.md`
states that the draft, app, channel upload and invite URL were "confirmed 15 Nov"; no identifier for any of
them is recorded anywhere in this repository, and the competition deadline (19 Nov, 01:30 IST) has passed.
Either the entry exists and was never written down, or it was never submitted. Only the dashboards answer
that, and until they do, this section stays blank on purpose.

### How to fill it

1. Paste the three values into the table above, replacing the whole `(owner …)` cell. A cell that starts
   with `(` is treated as empty **by design**.
2. Re-run `python signspace/scripts/freeze_check.py`. The three `submission record:` rows should turn PASS.
3. Append a row to `../p10-post/reverify-log.md` with the date and what you pasted — never edit an old row.
4. If the entry does not exist, do not fill these with anything. Write that in `POSTMORTEM.md` instead; a
   truthful "not submitted" is worth more than a record that cannot be checked.

## 3. Verification checklist

Ticked items are verified in this repository. Everything else needs hardware or an account.

- [x] `SIGNSPACE/P9 Check all` ran and produced a record — **note:** `CheckAll` rewrites `apk-hash.txt`, so
      it was run *before* the record was corrected by hand; the hash and tag have been verified since
- [ ] APK installs on Quest 3 (hardware) — **never side-loaded in this record**
- [ ] APK installs on Quest 3S (hardware) — **never side-loaded in this record**
- [ ] App launches with hands + passthrough — **not run on a device**
- [ ] App name reads `SIGNSPACE`, not "Untitled" — **not observed**
- [x] `aapt2 dump permissions` shows `horizonos.permission.HEADSET_CAMERA` — verified against the APK
- [x] SHA-256 matches the freeze record — `freeze_check.py` row 2
- [x] `git tag v1.0-competition` created — **local only; not pushed**

## 4. How the freeze is reproduced

```powershell
# 1. bump the version (BuildScript.cs)
#    const string Version = "1.0.0-competition";
#    const int BundleVersionCode = 14;

# 2. build
"E:\unity\Editors\6000.3.28f1\Editor\Unity.exe" -batchmode -quit -projectPath unity `
  -executeMethod Signspace.Editor.BuildScript.BuildAndroid

# 3. verify — this reads the APK, so a stale text file cannot certify itself
python signspace/scripts/freeze_check.py

# 4. tag and push
git tag -a v1.0-competition <commit that produced the APK> -m "SIGNSPACE 1.0.0-competition"
git push origin v1.0-competition

# 5. do not touch the build after this
```

`P9Checks.CheckAll` is the older path and it **overwrites** `apk-hash.txt` (new `frozen at`, and the commit
field is read from HEAD, not from the build). Prefer `freeze_check.py`: it verifies, it does not write.

## 5. Change log

| Date | Change | By |
|---|---|---|
| 17 Nov 2026 | File created. Template for the freeze record. | agent |
| 19 Nov 2026 | Filled the frozen facts from the artifact; marked the dashboard fields as owner-only blanks; replaced the misleading "run `P9 Check all`" advice with the verify-only path; recorded that the APK has never been side-loaded and the tag is not pushed. | agent |
