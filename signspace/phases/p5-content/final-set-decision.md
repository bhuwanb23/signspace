# P5.6 — Final set decision

**Date:** 16 Nov 2026 · **Owner:** W3 · **Gate:** G5
**Inputs:** `unity/Assets/StreamingAssets/trackability.json` (P1.5, `measured: false`),
`unity/Assets/StreamingAssets/Signs/*.json` (P5.4, 6 shipped + 6 deferred),
`scripts/tune_match.py` metric v3.

## The decision

**Ship six: B, U, V, W, C, O. Defer six: A, D, I, L, Y, F.**

The cut rule ("below ~70% lock rate in normal light is removed") **cannot fire**, because the
trackability probe has not run: every row in `trackability.json` is `status: "unmeasured"`,
`lockRate: -1`. A rule that needs a measurement cannot promote a shape on the strength of a guess.
The conservative reading is therefore the one already in `SIGNSET.md` §1.5: the six whose design
class is `fine` ship; the six whose design class is `may-resolve` stay authored, validated and
unshipped until the probe says otherwise.

This is not a retreat from the 12-shape protocol. It is the protocol's own decision rule applied
honestly: **the set may shrink; it may not include a shape that has not been measured.**

## Per-shape reason

| Shape | Family | `expected` | Decision | Reason |
|---|---|---|---|---|
| B | spread | fine | **ship** | baseline; lowest tracking risk in the set |
| U | spread | fine | **ship** | minimal-pair partner of V |
| V | spread | fine | **ship** | most recognisable handshape; partner of U |
| W | spread | fine | **ship** | three fingers forces the tracker to see fingers |
| C | closure | fine | **ship** | curvature without contact — the risk case that still holds |
| O | closure | fine | **ship** | partner of C; contact may help the tracker |
| A | closure | may-resolve | defer | thumb alongside vs across is not separable by this format |
| D | spread | may-resolve | defer | ring/little→thumb contact is a depth question |
| I | spread | may-resolve | defer | isolated little finger is the weakest joint |
| L | spread | may-resolve | defer | thumb-out is unambiguous on paper but unmeasured |
| Y | spread | may-resolve | defer | two ends of the hand at once; little-finger abduction |
| F | closure | may-resolve | defer | index–thumb contact; hardest in the set |

`may-resolve` is a design expectation, not a failure. The probe can promote any of these six; until
it does, they are content candidates, not shipped content.

## Do the near-miss pairs survive the cut?

Measured with the shipped metric v3 (`scripts/tune_match.py`, `metric_v3_weighted`), on the actual
pose files, all twelve shapes:

| Pair | Distance | Survives? | Note |
|---|---|---|---|
| **U ↔ V** | **26.0°** | **yes — exactly at the 26° threshold** | the tightest pair in the product; any re-authoring of U or V must keep this ≥26° |
| **C ↔ O** | **65.0°** | yes | the best demo in the app |
| V ↔ W | 118.1° | yes | |
| B ↔ U | 117.0° | yes | |
| A ↔ O | 116.7° | *(deferred pair)* | would survive if A is promoted |
| I ↔ Y | 133.8° | *(deferred pair)* | would survive if both are promoted |
| L ↔ Y | 115.3° | *(deferred pair)* | would survive if both are promoted |
| D ↔ F | 133.5° | *(deferred pair)* | would survive if both are promoted |

**Nearest neighbour per shipped shape:** B→C 101.9°, U→V 26.0°, V→U 26.0°, W→B 117.0°,
C→O 65.0°, O→C 65.0°. No shipped shape is closer to a deferred shape than to its own partner, so the
cut does not create a new ambiguity.

**What the cut costs:** nothing in the shipped demo. The four shipped pairs — U↔V, C↔O, V↔W, B↔U —
all survive. The deferred pairs (A↔O, I↔Y, L↔Y, D↔F) are not in the build and are not claimed.

## What would change this decision

1. **P1.5/P3.6 runs and a deferred shape measures ≥70% lock rate.** Promote it, re-run the pair
   check, and update `index.json`, `SIGNSET.md` and the description together.
2. **A shipped shape measures <70%.** Remove it to the honest mirror and re-check its partner pair:
   removing U or V would break the U↔V demo, which is the one pair the video depends on.
3. **A re-authored pose moves U↔V below 26°.** Re-tune or fix the shape before shipping; the pair is
   already at the threshold and has no margin.

## Artefacts

- `unity/Assets/StreamingAssets/Signs/*.json` — six shipped poses
- `unity/Assets/StreamingAssets/Signs/deferred/*.json` — six validated deferred poses
- `unity/Assets/StreamingAssets/Signs/index.json` — lists the six shipped shapes
- This document; `SIGNSET.md` §1.5/§2–5 updated to match
