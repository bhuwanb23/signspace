# GATE G10 — Is the submitted entry still intact, installable and unchanged three weeks later?

**Phase:** P10 · **Verdict:** ⬜ **NOT EVALUABLE** — the post-submission artefacts are written, but there is no submitted entry for them to protect

## Criteria — all must be true
- [ ] Morning-after verification passed — **run, and it is red**: 6 PASS / 9 FAIL / 3 NOT RUN (`reverify-log.md`)
- [ ] Weekly re-verification logged until judging closes — **0 of 3 rows written** (25 Nov, 2 Dec, 9 Dec)
- [ ] No commits landed on `main` after the freeze — **commits did**; the APK bytes never changed
- [x] Judging note ready — `judging-note.md`
- [x] Launch sketch written — `launch-sketch.md`
- [ ] Post-mortem written with real hour counts — **written, but hours are NOT RECORDED**; it uses measured commit data instead

## If red
There is no red here — only loss. If the channel breaks, fix it on a branch, restore the frozen version string, and record what happened.

## Decision record

| Field | Value |
|---|---|
| Verdict | ⬜ NOT EVALUABLE (neither PASS nor FAIL — there is nothing submitted to judge) |
| Date evaluated | 18 Nov 2026 (desk rows 1–4) |
| Evaluated by | `scripts/freeze_check.py` (mechanical) + P10.2–P10.5 documents |
| Evidence (screenshot / log / recording) | `phases/p10-post/reverify-log.md` — latest run 6 PASS / 9 FAIL / 3 NOT RUN |
| Notes | The artifact is intact and truthful about its version code; the entry is not. G10's criteria cannot all be true until the submission record, tag, video and channel listing exist. |

## A note on what "red" means here

Nothing about the build got worse: the APK still hashes to `81b60582…`, no post-freeze commit touched
it, and the packaged manifest agrees with the corrected record. What is missing is the entry itself — a
submission ID, an invite URL, a video, a channel listing — and no amount of repository work produces
those. The gate stays **not evaluable** rather than fail, because "fail" would imply the thing was
measured and found wanting; it was never measured, because it does not exist.
