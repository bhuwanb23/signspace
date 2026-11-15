# Performance

**Status:** `STUB` — not yet written · **Owner:** W2 · **Phase:** P7 · **Gate:** G7

## Why this document exists
60 fps is the rubric floor and 72 fps is our floor. This document replaces the budget with measurements.

## Questions this must answer
- What do we actually spend per frame, on which device, at which GPU level?
- Where does thermal throttling start, and what do we degrade first?
- Which of the fixed-foveation / MSAA / resolution choices did we keep, and what did each buy?

## Planned outline
1. Method (OVR Metrics Tool, 15-min run, cold boot)
2. Measured table: Quest 3 vs 3S, min/avg/1% low
3. Thermal curve
4. What we turned off and what it bought
5. Cold-boot time to first interaction
6. Regression checklist for every future change

## Inputs
`TECH-STACK.md` §4 budget

## Output / done when
A recorded overlay in the submission video showing ≥72 fps for 15 minutes on a 3S.
