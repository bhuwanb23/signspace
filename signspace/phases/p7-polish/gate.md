# GATE G7 — Does someone who has never touched a headset get from launch to their first mastered shape without you saying a word?

**Phase:** P7 · **Verdict:** ⬜ not yet evaluated — one criterion is desk-verified, six are device rows

## Criteria — all must be true

| # | Criterion | Desk | Device | Evidence |
|---|---|---|---|---|
| 1 | Every state change has a sound or a documented silence | ✅ | — | 16 clips, 12 cues, 5 silences named in code; `P7.1 Check sound` asserts every cue has a clip and the list of silences is not empty |
| 2 | ≤3 words of text in the first five minutes; cold start to first interaction <15 s | 🟡 words ✅ / time ⬜ | ⬜ | **0 instructional words** by `P7.2 Check text budget`; the <15 s row is a cold-boot measurement and is **NOT RUN** (`boot-times.csv`, H24) |
| 3 | Full loop playable at the 70°×66° profile with nothing outside the safe cone | ✅ geometry | ⬜ | worst slot edge 19.0°, grid 30.9°, tray 26.0°, HUD 19.2° — `P7.3`; **legibility and edge-gaze at 70° are NOT RUN** |
| 4 | ≥72 fps sustained for 15 minutes on Quest 3S, recorded | — | ⬜ **NOT RUN** | the build targets 72, has 0 lights, 0 shadow casters and MSAA 2×, but no frame has been measured. `PERFORMANCE.md` §2.2 |
| 5 | Median cold boot <15 s; 5/5 background cycles clean | 🟡 instrument | ⬜ **NOT RUN** | `BootProbe` writes `startup/hands/interactive_ms`; `boot-times.csv` is empty of numbers |
| 6 | 0 crashes across 20 launches and 4 hostile conditions | ✅ 5 conditions answered | ⬜ **NOT RUN** | `HostileGuard.Table`, `crash-sweep.md`; no launch has been run |
| 7 | All eight accessibility paths pass after the polish pass | ✅ | ⬜ | the whole P6 suite runs inline inside `P7.7` and passes; the eight device recordings remain NOT RUN |

**A criterion is ✅ only when the row above it is evidence. Nothing here promotes a device row on the
strength of a passing check**, and six of the seven criteria above are device rows.

## If red

If G7 is red on device, the first-five-minutes problem is fixed before the stranger test: a friction
log taken on broken onboarding teaches nothing that is not already known. The two most likely
failures, and what each means:

| Failure | What it would mean | First move |
|---|---|---|
| Sustained fps under 72 | the passthrough + hand pipeline is heavier than the budget assumed | measure at MSAA 1× and FFR high before touching the design; `RISKS.md` R10 |
| A first-time user does not grab the hand | the spatial cue is not doing its job | lengthen the pulse and move it with the hand; add nothing textual |
| Cold boot over 15 s | MRUK or the XR plugin is blocking the first frame | the plinth path must resolve first — it already can; check the launch log for what waits |

## Decision record

| Field | Value |
|---|---|
| Verdict | ⬜ PASS / ⬜ FAIL / ⬜ NOT EVALUATED |
| Date evaluated | — |
| Evaluated by | — |
| Evidence (screenshot / log / recording) | headless: `phases/p7-polish/*`, `P7 Check all` 128/128; device: none |
| Notes | Criterion 1 is the only fully closed one. Criteria 2–7 need the headset, one stranger and a clock. |
