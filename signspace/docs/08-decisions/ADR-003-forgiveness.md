# ADR-003 — Forgiveness over accuracy in the matcher

**Status:** Accepted · 7 Nov 2026 · **Phase:** P2

## Context
Hand tracking degrades with occlusion, fast motion, self-occlusion, unusual lighting and unusual anatomy. A precise matcher produces *confident wrongness* — the worst possible user experience for a learner, and the worst possible demo for a judge with unfamiliar hands.

## Options
1. **Strict** — tight tolerance, binary pass/fail. Accurate-sounding, brittle in reality.
2. **Banded** — three states (warming / close / locked), never a failure state, 0.7 s hold, pause on low confidence.
3. **Trivial** — accept almost anything. Nobody fails, nobody learns.

## Decision
**Banded.** Compare relative joint angles only (never absolute position or scale), hold for 0.7 s before locking, and **pause rather than penalise** when tracking confidence drops.

## Consequences
- A judge in bad light with unusual hands still reaches a lock. The demo cannot fail on camera.
- The near-miss becomes *more* important, because the matcher is deliberately not fussy: the near-miss is where precision lives (it fires only above a confidence floor, and silence is the default).
- We must never show a red X. The refusal to fail is a design commitment, not a fallback.

## Revisit if
Playtesters report the lock feels unearned. Then tighten the *lock* band, never the failure behaviour.
