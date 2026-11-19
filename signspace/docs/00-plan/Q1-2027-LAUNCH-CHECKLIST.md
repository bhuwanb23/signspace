# Q1 2027 pre-launch checklist

**Written:** 19 Nov 2026 · **Supersedes nothing** — it sequences the rows already in
[`HUMAN-TASKS.md`](HUMAN-TASKS.md) instead of adding new promises.

---

## Why this document exists

The competition build ran out of calendar before it ran out of work: every phase is complete *as work*, and
the rows that were never run are the ones that need a body, a headset, a signer or a stranger. A store
launch has the same rows plus a longer runway — which is exactly the advantage worth spending carefully.

**The order below is not arbitrary.** Each stage is gated on the one before it, because doing them out of
order produces evidence that looks like progress and is not:

- strangers tested on a build with unmeasured frame rate teach you about **that build**, not the product;
- a signer reviewing poses the app will not ship wastes the scarcest resource on the project;
- a video recorded before the fixes is a video you re-shoot;
- a store listing before a clean-account install is a refund request waiting to happen.

### Current position (measured, not assumed)

| | |
|---|---|
| Frozen artifact | `a1587624…`, code 14, tag `v1.0-competition` @ `3e533c` |
| Freeze guard | 9 PASS / 7 FAIL / 3 NOT RUN — all failures are submission paperwork or footage |
| Content | **6** runtime shapes (`Signs/index.json`), 6 deferred candidates, 20 probe variants measured offline |
| Device rows run | **0** |
| Stranger sessions | **0** |
| Signer review | **0**; every pose is `reference-authored` |

---

## Stage 0 — the long-lead items start today (week 0, 1–2 h)

Nothing here needs the app. All of it blocks a later stage, and two of them have the longest lead time on
the project.

| # | Row | Done when | Lead time | Why first |
|---|---|---|---|---|
| 1 | **H1 · Recruit and consent a Deaf signer** | signed consent, credit name chosen, rate agreed | **2–6 weeks** | The single longest dependency. Everything in Stage 1 waits on it, and Stage 1 gates the content claims. |
| 2 | **Device access decision** | a named Quest 3 or 3S (own, borrowed, rented) with a date, **or** the remote-tester route below | 1–3 weeks | Stages 2–3 cannot start without it. Decide now, not when the code is ready. |
| 3 | **H48 · shape count told correctly** | every public sentence says 6 shipped (20 measured), not 20 | done | A wrong number in the store copy is a takedown risk, not a typo. Fixed in `SUBMISSION-COPY.md`; the store listing inherits it. |
| 4 | **Launch-scope decision** | either 12 shapes ship, or the claim is restated as "N measured shapes at launch" | 1 h | `SIGNSET.md` designs 12; the build ships 6. Decide which sentence goes on the store page before the signer session, so H2 captures for the right set. |

**Remote-tester route (no owned headset).** Meta's release-channel invite is a URL: a tester with their own
Quest can install the build without you touching hardware. `PlaytestProbe` already writes a JSONL session
log, and `phases/p4-shelf`/`p8-playtest` name the export step. This does not replace H35's thermal numbers —
it does make H25–H29, H11, H12 and H23 reachable. It is the cheapest way out of "no device".

## Stage 1 — content truth (blocked on H1; 1 week once the signer is booked)

| # | Row | Done when | Gate |
|---|---|---|---|
| 5 | **H2 · Capture session** — 12 poses, two angles, neutral first | 12 takes per pose, named per the manifest | G5 |
| 6 | **H3 · Signer confirms the final set** | their own words on each shipped shape, in writing | G5 |
| 7 | **H4 · Signer approves credit and copy** | written approval, or the corrected wording | G5 |
| 8 | **H6 · Measured trackability** | the 70% cut rule can fire; the final-set decision is re-run | G3 content |
| 9 | **H5 · Deferred six** (optional) | 6 takes, `validate_poses.py` + `check_trackability.py` clean | G5 |

**Exit criteria:** `set-meta.json` no longer says `reference-authored` for any shipped shape, and the blind-spot
panel's number comes from a measurement. Until then the copy keeps the weaker, truthful claim.

## Stage 2 — device truth (needs the headset; 3–5 days, not 3–5 hours)

Ordered so that a failure early stops the expensive work late.

| # | Row | Done when | Why here |
|---|---|---|---|
| 10 | **H30 · Install smoke** | `adb install` succeeds; app launches with hands + passthrough | If this fails nothing below is worth doing |
| 11 | **H39 · Cold start to first interaction** | under 15 s, stopwatch, fresh install | The first impression; cheap to measure while the device is fresh |
| 12 | **H23 · First mastered shape under 75 s** | stopwatch from launch to first lock | Defines whether the loop is teachable *before* strangers see it |
| 13 | **H8 · Near-miss on device** | 8/8 deliberate near misses named; 0 false names in 2000 s | The product's only claim no 2D app can make |
| 14 | **H11 · Shelf survives relaunch** + **H12 · leave and return** | 5/5 restore in place; no re-placement prompt | MRUK is the part most likely to differ from the simulator |
| 15 | **H24 · Frame rate** + **H35 · 15 minutes at the rate** | ≥72 fps sustained, 1% low, thermal onset noted | Blocks the fps claim in every sentence and the video overlay |
| 16 | **H37 · 20 launches + hostile conditions** | 0 crashes; every misbehaviour written down | Crash data before strangers, or the strangers find them for you |
| 17 | **H14–H18, H20–H22 · gaze, eyes-only, switches, reduced motion, one hand** | each path recorded | Accessibility claims are rubric-visible and easy to overclaim |
| 18 | **H19 · Restricted-hand calibration** | a shape locks that could not before; persists | The differentiator for non-standard hands |
| 19 | **H34 · FoV profile** + **H38 · the chime by ear** | reflow reverses cleanly; you are not tired of the sound | Both are cheap and both change the video |

**Exit criteria:** `PERFORMANCE.md` has a device row, `boot-times.csv` has five runs, `crash-sweep.md` has 20,
and `TRACKABILITY.md` has a measured row. Any failure here goes back to code **before** Stage 3.

## Stage 3 — strangers (needs Stage 2 green; 1 week including recruiting)

| # | Row | Done when | Note |
|---|---|---|---|
| 20 | **H25 · Recruit three testers**, at least one non-standard hand | dates booked, consent signed | Remote route is fine here |
| 21 | **H26 · Run sessions without helping** | you only observe; nobody explains | If you help, the number is yours, not theirs |
| 22 | **H27 · First mastered shape under 75 s** | measured per tester, per session | The only row that converts "teachable" into a figure |
| 23 | **H28 · Fix only the top three frictions** | three fixes, each with a before/after note | Resist fixing five; three is what one week buys |
| 24 | **H29 · Re-test with one fresh stranger** | the three fixes hold | Closes G8 |

**Exit criteria:** G8 is green with three sessions logged, not three sessions remembered.

## Stage 4 — evidence pack (needs Stages 1–3; 3–4 days)

| # | Item | Done when |
|---|---|---|
| 25 | **H31 · Record the video** | the 14 shots in `VIDEO-SCRIPT.md`, real device capture, fps overlay from the device |
| 26 | **Cut and caption** | under 3:00, legible muted, `archive/edit-notes.md` updated with what was cut |
| 27 | **Screenshots and store copy** | 6 shapes, measured numbers only, `count.py` green |
| 28 | **Refresh the archive** | `BUILD.md` (new sha, new commit), `shot-list.csv` capture column filled, video master stored |

**Exit criteria:** every claim on the store page appears visibly in the video or in a measured row. If a claim
has neither, it comes out of the copy.

## Stage 5 — submission (needs Stage 4; 2 days)

| # | Item | Done when |
|---|---|---|
| 29 | **Channel + listing** | `Competition`→`Beta`/store channel lists the frozen build; screenshot archived |
| 30 | **H46 · Clean-account install** | a second Meta account with no developer login installs and launches |
| 31 | **H44 · Fill `submitted.md`** | submission ID, invite URL, confirmation email; `freeze_check.py` submission rows PASS |
| 32 | **Tag and push** | annotated tag, pushed; record names the same commit |
| 33 | **Submit** | confirmation on file; `reverify-log.md` gets a row |

## Stage 6 — first 72 hours after launch

| | |
|---|---|
| Watch | crash-free sessions, first-lock time, drop-off point, near-miss off-switch use |
| Have ready | a hotfix branch **off the tag**, not on it — and a version-code bump |
| Do not | rebuild the tagged commit to "clean it up"; that is how a submission becomes unverifiable |

---

## The critical path, in one line

**Signer consent (2–6 weeks) → capture + approval (1 week) → device sweep (1 week) → strangers (1 week) →
video (4 days) → store.** Device access is the only item that can start in parallel with the signer and must
not wait for it.

## What would make this checklist wrong

- **No signer.** Then the copy stays `reference-authored` and the launch is a prototype release, not an ASL
  product. Say so on the store page, or postpone.
- **No headset and no remote testers.** Then stages 2–3 do not happen, G7/G8 stay red, and the honest launch
  is a TestFlight-style beta with no performance claims at all.
- **A frame rate below 72.** Then the budget claim changes, the video overlay shows the real number, and the
  store copy says what the device says.
