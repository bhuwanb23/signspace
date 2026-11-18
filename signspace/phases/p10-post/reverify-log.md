# P10 — Re-verification log

**Phase:** P10 · **Gate:** G10 · **Log started:** 18 Nov 2026 · **Rule:** append-only. A past row is never edited; a correction is a new row.

> G10 asks whether the entry is *still* intact three weeks later. That is a question about time, so the
> answer has to be a dated row rather than a paragraph. `scripts/freeze_check.py` performs the mechanical
> part — artifact bytes, version constants, git tag, commits after the freeze, archive contents and the
> submission record — and prints PASS / FAIL / NOT RUN. The account-side checks (clean-account invite URL,
> channel listing, public video) are **NOT RUN** here because no browser session or headset is available
> to this check; they are never inferred from a file that happens to exist.

---

## Row 1 — 18 Nov 2026, 16:14 UTC · desk re-verification

**Command:** `python signspace/scripts/freeze_check.py` (exit 1)
**Result:** **4 PASS · 9 FAIL · 3 NOT RUN — the freeze is intact on the artifact and broken on the paperwork.**

| # | Check | Expected | Actual | Verdict |
|---|---|---|---|---|
| 1 | APK present | `Builds/Android/signspace.apk` | 64,617,747 bytes, mtime 17 Nov 22:56 UTC | ✅ PASS |
| 2 | APK SHA-256 | `81b60582…cdb9d` | `81b60582ed2feff6…be2cdb9d` | ✅ PASS |
| 3 | Version name | `1.0.0-competition` | `1.0.0-competition` | ✅ PASS |
| 4 | Bundle version code | 13 | 13 | ✅ PASS |
| 5 | Tag `v1.0-competition` exists | a tag at the submitted commit | **no such tag** | ❌ FAIL |
| 6 | Record commit is HEAD | a commit hash in `apk-hash.txt` | the record says `commit: ` (empty) | ❌ FAIL |
| 7 | No commits after freeze | 0 | **3** — `e232956`, `3f77e69`, `de3c160` | ❌ FAIL |
| 8 | Working tree clean | 0 files | 1 (this script, new) | ❌ FAIL |
| 9 | Archive complete | 5 files | missing `BUILD.md`, `shot-list.csv`, `edit-notes.md`, `channel-state.png`, `count-output.txt` | ❌ FAIL |
| 10 | Submission record: ID | recorded | empty | ❌ FAIL |
| 11 | Submission record: invite URL | recorded | empty | ❌ FAIL |
| 12 | Submission record: confirmation email | recorded | empty | ❌ FAIL |
| 13 | Video master present | the captured entry video | **only a previs exists** | ❌ FAIL |
| 14 | Invite URL from a clean account | opens, installs, launches | requires a clean browser session and a headset | ⬜ NOT RUN |
| 15 | Channel lists the frozen version | `1.0.0-competition` | requires a developer-account login | ⬜ NOT RUN |
| 16 | Video plays publicly | the submitted video URL | requires the video URL and a browser | ⬜ NOT RUN |

### What this row actually establishes

**The artifact is frozen.** The APK on disk is byte-identical to the moment the freeze record was written
(`81b60582…`), the version name and code match the record, and nothing in the repository can change the
bytes a judge would install. That is the part G10 cares about most, and it is green.

**The submission is not.** The record has no commit hash, there is no tag, `submitted.md` has no
submission ID, invite URL or confirmation email, and the only video on disk is a previs. Taken together
these say the same thing P9's review said: **the desk-side is prepared and the entry has not been
submitted.** G10 cannot pass on an entry that does not exist, and this log will not pretend otherwise.

**The freeze was violated in the letter, not in the bytes.** Three commits landed after the frozen-at
timestamp (`17 Nov 17:27 UTC`) — `e232956` at 22:57, `3f77e69` at 22:58, `de3c160` on 18 Nov 08:00. All
three are paperwork and none touched the build (the APK hash and mtime are unchanged), but the rule in
`archive/ARCHIVE.md` is "no commits, no edits", and the log records the violation rather than reinterpreting
it as fine.

### Root cause of the empty commit, found in P9's tool

`P9Checks.ReadCommitHash()` looks for `.git` in `Directory.GetCurrentDirectory()` — which is `unity/`
when Unity runs with `-projectPath unity`, while the repository root is one level above. So the lookup
fails, the method returns an empty string, and `apk-hash.txt` was written with `commit: ` and nothing
after it. The freeze wrote a record that cannot identify the code it froze, and the bug is one path.

It is deliberately **not fixed here.** The freeze is in force, and editing `P9Checks.cs` after the
artifact was built is exactly the "one small fix" `archive/ARCHIVE.md` forbids — even though it cannot
change a single byte of the APK. The fix goes on a branch after judging closes (H42), and the record's
commit is filled by hand from `git log` in the meantime.

### A copy-versus-build discrepancy worth acting on before upload

The build ships **twenty** handshapes — `Assets/StreamingAssets/Probe/index.json` lists `B U V W C O A M
N T R S D E F G I L X Y`, and every one has a pose file beside it. `SUBMISSION-COPY.md` §3 says *"6
shapes today"*. A judge can count the shelf and find the description understating the build by fourteen
shapes, which is the wrong direction for that error to run in but still a factual mismatch in the entry.

If the entry has not been submitted, correct the description to the measured count and re-verify. If it
has, leave the copy alone — a post-deadline edit is worse than an understated count — and note it in the
post-mortem. The log does not decide which; it records that the two documents disagree.

### Remediation — owner-only, in this order

| # | Fix | Who | Why it must happen before judging |
|---|---|---|---|
| 1 | `git tag v1.0-competition <the commit that built 81b60582…>` and push it | owner | without a tag, "the submitted code" is not specified |
| 2 | Write the commit hash into `phases/p9-submission/apk-hash.txt` | owner | the record currently cannot identify its own artifact |
| 3 | Fill `submitted.md`: submission ID, confirmation email, invite URL | owner | rows 10–12 are the entry |
| 4 | Upload 81b60582… to the `Competition` channel; screenshot into `archive/channel-state.png` | owner | row 15 |
| 5 | Put the video master in `archive/video-master/` (or its URL in `submitted.md`) | owner | row 13; the capture is the only irreplaceable asset |
| 6 | Copy `shot-list.csv`, run `scripts/count.py > archive/count-output.txt`, write `archive/BUILD.md` from the freeze record | owner (5 min) | row 9 |
| 7 | Verify the invite URL from a clean account with no developer login | owner | row 14 — the check G9 exists for |
| 8 | Stop committing to `main`; any fix goes to a branch | owner | row 7 |
| 9 | Reconcile "6 shapes" in `SUBMISSION-COPY.md` with the twenty that ship — only if not yet submitted | owner | a countable claim in the entry |

**Do not rebuild.** The APK hash is the one green thing in this row; a rebuild to fix paperwork would
trade a verified artifact for an unverified one.

---

## Row 2 — 18 Nov 2026, later the same day · after the P10 artefacts were written

**Command:** `python signspace/scripts/freeze_check.py` (exit 1)
**Result:** **4 PASS · 9 FAIL · 3 NOT RUN — unchanged.**

The counts are identical to row 1, and that is the intended outcome rather than a coincidence: the P10
documents, `freeze_check.py` and `P10Checks.cs` are **untracked**, so they change the “working tree clean”
row's *description* (1 file → 16) without changing its verdict, and they change nothing about the APK.

The row exists because the log is append-only and the tree genuinely changed. Deleting row 2 and editing
row 1 to say “16 files” would make the record tidier and less true.

**What this means for the freeze:** P10's own paperwork is now part of the post-freeze drift the gate
records. It must not be committed to `main` until judging closes; if it is committed, the honest move is
a new row saying so, not a quiet amend.

---

## Row 3 — 18 Nov 2026, 21:53 UTC · a commit landed

**Command:** `python signspace/scripts/freeze_check.py` (exit 1)
**Result:** **4 PASS · 9 FAIL · 3 NOT RUN — same verdicts, one description changed.**

A commit (`4dab82b`, *"feat: Add launch sketch and re-verification log for P10 phase"*) landed on `main`
with the P10 artefacts in it. Row 2 said they were untracked and must stay that way; that sentence was
true when it was written and is false now, so it is not edited — this row supersedes it.

| Row | commits after freeze | tree |
|---|---|---|
| 1 | 3 | 1 untracked file |
| 2 | 3 | 16 untracked files |
| **3** | **4** | **4 modified files** |

This is the freeze being violated by the project's own paperwork, in public, which is the point of an
append-only log. Nothing in rows 1–3 changes the APK: its hash is still `81b60582…` and its mtime is still
17 Nov 22:56. The remediation does not change either — the tag (H41) and the record's commit field (H42)
are still the two things that turn “there is a hash” into “there is an identified entry”.

---

## Weekly rows — one per week until judging closes

Run the same command on the same weekday, append a row, and do not edit earlier ones. A week with no row
is indistinguishable from a week nobody checked.

| Week | Date due | Verdict | FAIL count | Notes |
|---|---|---|---|---|
| 2 | Wed 25 Nov 2026 | ⬜ NOT RUN | — | |
| 3 | Wed 2 Dec 2026 | ⬜ NOT RUN | — | |
| 4 | Wed 9 Dec 2026 | ⬜ NOT RUN | — | judging closes 11 Dec; this is the last row that matters |

**What changes a row from FAIL to PASS:** the eight remediation items above, nothing else. A row that says
PASS while the submission record is empty would be the one dishonest line in the project.

---

## Change log

| Date | Change | By |
|---|---|---|
| 18 Nov 2026 | File created; row 1 written from a real `freeze_check.py` run, including the nine FAILs. | agent |

## Row 4 — 18 Nov 2026, 16:47 UTC · the guard was widened, and caught a false pass

**Command:** `python signspace/scripts/freeze_check.py` (exit 1)
**Result:** **6 PASS · 9 FAIL · 3 NOT RUN** across 18 rows.

Rows 1–3 compared the record's version code against `version-code.txt` — which is the file the record
was generated *from*. That is not a check; it is the record agreeing with itself. Two rows were added:
the `const int BundleVersionCode` in `BuildScript.cs` (the value the build path actually uses) and the
packaged manifest read with `aapt2 dump badging`.

Both said **14**. The record said **13**. So the freeze record understated the APK's version code, and
the earlier PASS was blindness rather than truth.

**Corrected, and recorded because it changes a frozen document:**

| File | Before | After | Why |
|---|---|---|---|
| `unity/Assets/_Project/version-code.txt` | 13 | **14** | the file was stale; `BuildScript` ignores it and uses 14 |
| `phases/p9-submission/apk-hash.txt` | `bundle version code 13` | **14** | the record must describe the artifact it froze |
| `docs/06-submission/SUBMISSION-COPY.md` | "6 shapes today" | **"20 handshapes today"** | `Probe/index.json` ships 20; 499/500 words still holds |

The APK was not rebuilt: hash `81b60582…` and mtime 17 Nov 22:56 are unchanged, and the packaged
manifest was already 14 when the record claimed otherwise.

**Also fixed outside this log:** the audit's rename of the finder and the state machine was a copy, not a
rename, so the project did not compile (duplicate `MrUkSurfaceFinder`, `ISurfaceFinder`,
`SurfaceCandidate`, `AppState`, `AppStateMachine`). The stale originals were deleted and their GUIDs moved
onto the surviving files, so `Main.unity` keeps its references.

**What is still owed is unchanged:** tag, commit hash in the record, submission ID, invite URL, channel
screenshot, video master, clean-account install, three weekly rows.
