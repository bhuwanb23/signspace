# ADR-002 — ASL for v1, ISL named as the expansion

**Status:** Accepted · 7 Nov 2026 · **Phase:** P5

## Context
The team is in Chennai. Indian Sign Language is materially under-resourced and would be the more *needed* product. The judging panel is likely US-centric and can verify ASL correctness; almost none of them can verify ISL.

## Options
1. **ASL** — verifiable by the panel, largest learning market, richest signer availability documentation.
2. **ISL** — higher need, higher distinctiveness, low verifiability; local signer sourcing easier for us.
3. **Both** — doubles capture, doubles correctness risk, fits in neither 11 days nor 12 shapes.

## Decision
**ASL in v1. ISL named in the submission as the first planned expansion.** The matching engine is language-agnostic by construction — it stores relative joint angles, not English words.

## Consequences
- We trade distinctiveness for verifiability. That trade is correct *for this competition*.
- The expansion story must be real, not decorative: the submission says 60 signs at launch and ISL after, and the engine must genuinely support it (it does — poses are data).
- If we later build the ISL product, the same codebase works. Nothing here is wasted.

## Revisit if
A judge or mentor signals that ISL would be received well, or a local Deaf organisation offers to partner. Then the v2 roadmap moves forward, not v1.
