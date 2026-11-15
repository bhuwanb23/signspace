# ADR-004 — 12 handshapes, 3 families, no motion

**Status:** Accepted · 7 Nov 2026 · **Phase:** P5

## Context
The temptation is to ship fingerspelling plus a few "real" signs plus maybe simple two-handed shapes. Each addition multiplies capture, correctness risk and test surface, and every one of them is something a Deaf judge can catch us getting wrong.

## Options
1. **Full alphabet + vocabulary** — impressive on a feature list, unshippable in 11 days, and maximally exposed to correctness error.
2. **12 static handshapes in 3 families** — deep, verifiable, and enough to demonstrate every technical claim.
3. **6 handshapes** — the fallback if P1.5 goes badly.

## Decision
**12 static handshapes, 3 families, zero motion.** Selected for articulatory contrast and tracking robustness (`SIGNSET.md`).

## Consequences
- Every claim in the submission is checkable in under five minutes of play.
- We must be explicit that motion and facial grammar are *out of scope*, not *coming later* — honesty is cheaper than a promise.
- The cut ladder can shrink the set to 6 without changing the product's argument.

## Revisit if
P1.5 kills more than 4 shapes. Then the set becomes 6, one family, and we say so in the description.
