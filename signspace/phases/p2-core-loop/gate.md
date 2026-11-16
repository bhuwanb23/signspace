# GATE G2 — Can a stranger lock one handshape, uncoached, without anyone telling them what to do?

**Phase:** P2 · **Verdict:** ⬜ not yet evaluated — nothing here can be passed from the sandbox
**Status:** P2.1–P2.7 all built and measured in simulation; the device queue is in
[`phase-2-complete.md`](phase-2-complete.md) §6. A `[x]` below means the criterion is satisfied *by
measurement in this repository* and names the device check still owed; `[ ]` means it cannot be true
without hardware.

## Criteria — all must be true
- [x] Pose format frozen, documented, validated by script, with three committed fixtures — **schema 2**, `--self-test` passes 29 cases, 3 broken fixtures in `scripts/fixtures/broken/` ([POSE-FORMAT.md](../../docs/02-tech/POSE-FORMAT.md))
- [x] A live per-joint distance computed in degrees, weights data-driven — v3 weighted metric, six
      weight bands in `Matching/Weights.asset`, no percentage anywhere; nearest pair 26.0°, noise p95
      6.7°. *Device:* P2.2 step 5 (one hand-captured distance per shape)
- [x] Three bands with tuned thresholds and no hysteresis flicker — warming >24 / close 12–24 (exit 26)
      / locked <12 (exit 14), 150 ms hysteresis, 0.0% false close, 100% survival ≤12° and 0% ≥12.5°.
      *Device:* the 30 s edge flicker test
- [ ] 20/20 intentional locks and 0/20 false locks — **20/20 and 0/20 simulated** (`hold-log`), not on
      hardware. The criterion is the hardware run
- [ ] Lock payoff completes in under 1.2 s and has been filmed — **0.99 s measured of a 1.20 s budget**
      (82%), but `lock-payoff-take01.mp4` does not exist. Half the criterion
- [x] Palm orientation contributes independently and a mis-facing hand fails — basis comparison (no
      Euler), `D_total = D_fingers + k·max(0, O − 15)`, k = 1.0; 0.0% lock from 30° mis-faced, 100% up
      to 20°. *Device:* the wrist-roll test
- [x] Left hand works, or is disabled with a documented reason — **disabled**, in one place, with the
      evidence and the enable path ([`left-hand-decision.md`](left-hand-decision.md)). Fully satisfiable
      without hardware, and satisfied
- [ ] **A person who has never seen the app locks one shape without instruction** — needs one stranger,
      one headset, one session. Not evaluable here

## If red
A red G2 is a *feel* problem, not a feature problem. Do not add features to fix it. In order: widen the locked band, increase `O_slack`, lengthen the hold to 0.9 s. If it still fails after those three, the disambiguation between 'near' and 'locked' is wrong and is redesigned on Day 4 before P3 begins.

## Decision record

| Field | Value |
|---|---|
| Verdict | ⬜ PASS / ⬜ FAIL |
| Date evaluated | |
| Evaluated by | |
| Evidence (screenshot / log / recording) | |
| Notes | |
