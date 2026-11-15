# Gaze Input

**Status:** `STUB` — not yet written · **Owner:** W2 · **Phase:** P6 · **Gate:** G6

## Why this document exists
Gaze is a named rubric technology and the mechanism that makes Beat 3 possible — you cannot point at a finger when both hands are busy.

## Questions this must answer
- Which ISDK building block, and what does its default rig do on Quest vs VR Glasses?
- How do we *prove* eye tracking is active rather than silently falling back to head ray?
- How does dwell selection work with hands full?
- What is the fallback when eye tracking is unavailable or the user declines calibration?

## Planned outline
1. Building block configuration
2. Rig switching by device
3. Verification method (the proof, not the assumption)
4. Dwell tuning: target size, dwell time, cancel gesture
5. Eyes-only playthrough path
6. Fallback chain
7. Known limits (arm's length, 15 mm finger separation)

## Inputs
`TECH-STACK.md` §3, Meta XR Simulator VR Glasses profile

## Output / done when
A debug log on device proves eye-gaze selection, and an eyes-only playthrough completes the full loop.
