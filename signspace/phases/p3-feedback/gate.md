# GATE G3 — Does the near-miss land in under 30 seconds of play — and does it never once lie?

**Phase:** P3 · **Verdict:** ⬜ not yet evaluated

## Criteria — all must be true
*Status is `[x]` only where the evidence would survive someone checking it; `[~]` where the work is done
but the check needs a headset or a second person.*

- [~] All 8 documented near-miss pairs fire correctly — 8/8 in simulation (`nearmiss_sim.py`), correct
      shape every time, `D(made)` 2.2–2.7°; **not fired on a real hand** (P3.2 rerun owed)
- [x] Zero false positives across 20 sloppy attempts — 0 fires on 18 non-qualifying attempts, 1 on the
      qualifying one, and 0 across 2000 sloppy samples (`false_positive_sim.py`)
- [x] Confidence gate, session rate cap, and an off switch all working — floor 0.70, 0.40 s decision
      window, 1 per 20 s, `SetNearMissEnabled` persisted; removing the window produces 9 fires instead of 1
- [~] Near-miss copy is ≤12 words and understood by a non-signer in one read — 9 words shipped, worst
      composed case 11; the non-signer read is owed (one reader, and that reader is on the project)
- [x] Honest-mirror panel ships, reachable from inside the space, with correct (not shortcut) shape
      renders — `HonestMirror.prefab`, opened by pinching a plinth, four excluded shapes drawn from their
      own captures and passing the same validator as the taught set. **Correctness of the handshape wants
      a signer's eye** (format ≠ handshape); that check is owed, not assumed
- [~] The published count matches measured data and carries a measurement date — the code carries the date
      the moment there is one, and `scripts/check_trackability.py` now fails if the panel's sentence is
      absent from the doc, if `measured` is true without an ISO date, or if a half-filled file carries a
      status. **The measurement itself is owed** (P3.6); until then the panel prints four, not a fraction
- [x] No lock can be awarded while confidence is below the floor — asserted in the shipped code
      (`HoldTimer.SelfTest` case 8: perfect distance, no tracking, 200 frames, no lock) and measured
      against a control (`pause_sim.py`: 0 locks below the floor in three degradations, **1 each with the
      floor removed**, all three resume). The visible half is `BandFeedback.pauseDim` + the lamp.
      **Hardware confirmation folds into P3.6**
- [ ] **A stranger playing for 30 seconds experiences the near-miss and can explain what it told them** —
      needs a stranger, and the only stranger available is the person who built it

## If red
If the near-miss lies even once, disable it rather than tune it live — a silent product is acceptable, a wrong one is not. If the honest mirror reads as an apology rather than a flex, rewrite the framing before moving on: the panel is a never-cut item, so a G3 red blocks P4.

## Decision record

| Field | Value |
|---|---|
| Verdict | ⬜ PASS / ⬜ FAIL |
| Date evaluated | |
| Evaluated by | |
| Evidence (screenshot / log / recording) | |
| Notes | |
