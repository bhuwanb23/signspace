# SIGN SET
## The 12 handshapes, the 3 families, and the shapes we deliberately excluded

**Status:** proposal, pending (a) the P1.5 trackability probe and (b) sign-off by the Deaf signer(s) we capture from. **Descriptions in this document are working notes, not the product's copy** — the shipped wording is written with the signer, not about them.

---

## 1. THE DESIGN LOGIC

Three rules decided this set, in order:

1. **Only handshapes that today's tracking can actually resolve.** We do not ship a shape the sensor can't see; we publish its absence instead (`N2`).
2. **Group by articulatory feature, not alphabetically.** The product teaches *the grammar of handshape* — which fingers, how much spread, how much closure — not "the alphabet in order." Families are what make the **near-miss** possible: the closest wrong answer is usually the shape in the same family.
3. **Every family must contain at least one pair that differs by a single feature.** That pair is the demo, the game, and the lesson.

---

## 1.5 THE SHIPPED SET FOR THE FOUR-DAY BUILD — **6 shapes**

**Decision (14 Nov 2026, solo, 4 days): B · U · V · W · C · O.**

| Shape | Family | Tracking risk | Why it stays |
|---|---|---|---|
| **B** | Spread | Low | The neutral baseline everything is measured against |
| **U** | Spread | Low | Minimal-pair partner of V — one variable, universally legible |
| **V** | Spread | Low | The most recognisable handshape in existence |
| **W** | Spread | Medium | Three fingers forces the tracker to see fingers, not guess them |
| **C** | Closure | Medium | The hardest case we still believe in — curvature without contact |
| **O** | Closure | Medium-high | The partner of C; the best single demo in the app |

**Why these six and not the 12:**

1. **Four near-miss pairs from six shapes** — B↔U, U↔V, V↔W, C↔O. That is the highest ratio of demonstrable novelty to content in the entire set. The other six shapes would have added two pairs at double the capture cost.
2. **No thumb-under-finger shapes** — every finger is visible to the cameras in all six. Nothing here is at the edge of the sensor, so the demo cannot fail on a judge's hands.
3. **Two families is enough** to prove that families are a design idea; one would look arbitrary, three is unnecessary in four days.
4. **Every shape is a standard fingerspelling letter** — the narrowest possible authorship question (see `ADR-007`).

**Deferred to the next build:** A, D, I, L, Y, F (the extension family) and the blind-spot set M, N, T, R, S, E — the latter still captured *correctly* for the honest-mirror panel.


---

## 2. FAMILY 1 — SPREAD & SEPARATION
*Same base posture; the only variable is how far the fingers separate.*

| # | Shape | Working description | Why it's in the set |
|---|---|---|---|
| 1 | **B** | Flat hand, fingers together, thumb along the palm edge | The neutral baseline every other shape is measured against |
| 2 | **U** | Index and middle extended, **together** | The minimal-pair partner of V — one variable |
| 3 | **V** | Index and middle extended, **spread** | The most recognisable handshape in existence |
| 4 | **W** | Index, middle, ring extended and spread | Three fingers stresses the tracker more than two; if W works, the fingers are being seen |

**The lesson this family teaches:** *spread is a phoneme.* U and V are different words with identical finger selection. No video makes that legible; a rotatable reference makes it obvious.

---

## 3. FAMILY 2 — CLOSURE & CURVATURE
*Fingers and thumb are all involved; the variable is how far they close.*

| # | Shape | Working description | Why it's in the set |
|---|---|---|---|
| 5 | **A** | Closed fist, thumb alongside | The maximum-closure anchor |
| 6 | **C** | Curved hand, fingers and thumb forming a C | Curvature without contact — the hardest tracking case we still believe in |
| 7 | **O** | Fingertips meeting the thumb, forming an O | Curvature **with** contact — the minimal-pair partner of C |
| 8 | **D** | Index extended, remaining fingers meeting the thumb | Closure on one side, extension on the other |

**The lesson:** *the difference between C and O is 10–15° of curvature.* That is precisely the difference a flat video cannot show and a match meter can measure.

---

## 4. FAMILY 3 — EXTENSION & SELECTION
*The variable is which finger is chosen.*

| # | Shape | Working description | Why it's in the set |
|---|---|---|---|
| 9 | **I** | Pinky extended, fist closed | Isolation of the weakest finger — a genuine tracking stress test |
| 10 | **L** | Index up, thumb out, forming an L | Two-finger selection at 90° |
| 11 | **Y** | Thumb and pinky extended, middle fingers closed | The widest separation in the set |
| 12 | **F** | Thumb and index meeting, other three extended | Selection *plus* contact — cross-family difficulty |

**The lesson:** *selection is arbitrary and therefore must be learned.* Which finger you raise is the whole content of the shape.

---

## 5. THE NEAR-MISS PAIRS *(this table is the product)*

Ordered by how good they'll look in the submission video. `Δ` = the feature that separates them.

| Pair | Δ (the only difference) | Demo value |
|---|---|---|
| **C ↔ O** | Curvature / contact | ★★★ The best single demo in the app. Nearly identical from the front; obvious in depth |
| **U ↔ V** | Spread | ★★★ Universally legible to a hearing audience too |
| **V ↔ W** | One more finger | ★★☆ |
| **A ↔ O** | Closure + orientation | ★★☆ |
| **I ↔ Y** | Thumb | ★★☆ |
| **L ↔ Y** | Which fingers separate | ★☆☆ |
| **B ↔ U** | Spread + thumb | ★☆☆ |
| **D ↔ F** | Contact point | ★☆☆ |

**Build note (P3.1):** the near-miss fires when the distance to the *wrong* shape falls below tolerance while the distance to the target does not — and only above a confidence floor. **Silence beats a wrong diagnosis.**

---

## 6. WHAT WE EXCLUDED, AND WHY *(this becomes the honest-mirror copy)*

| Excluded | Reason | Category |
|---|---|---|
| **M** | Thumb tucked under three fingers — the cameras cannot see a finger behind a finger | Not resolvable today |
| **N** | Thumb tucked under two fingers — same failure | Not resolvable today |
| **T** | Thumb between index and middle — occlusion + contact | Not resolvable today |
| **R** | Crossed fingers — crossing is not represented by the tracker | Not resolvable today |
| **S** | Thumb across the front of the fist — *may* resolve; competing with A | **Evaluated in P1.5** |
| **E** | Fingertips on the thumb base — cramped, high self-occlusion | **Evaluated in P1.5** |
| **X** | Hooked index — likely fine, but adds no new feature lesson | Deferred to v2 |
| **G, H, K, P, Q, Z, J** | Two-handed, moving, or orientation-dependent | Out of scope in v1 by design |

**The sentence this generates, and it's a feature:** *"These are the shapes today's cameras can't see. We'd rather tell you than teach you badly."*

---

## 7. TRACKABILITY TABLE *(filled from the P1.5 probe — currently unverified)*

| Shape | Expected | Measured (Quest 3S) | Daylight | Lamplight | Notes |
|---|---|---|---|---|---|
| B | High | — | — | — | Baseline |
| U | High | — | — | — | Watch finger separation |
| V | High | — | — | — | |
| W | Medium-high | — | — | — | Ring-finger tracking is the weak link |
| A | High | — | — | — | Thumb-alongside vs thumb-across is the ambiguity |
| C | Medium | — | — | — | Curvature without contact is the risk case |
| O | Medium-high | — | — | — | Contact may actually *help* the tracker |
| D | Medium | — | — | — | Ring/pinky→thumb contact is the risk |
| I | Medium | — | — | — | Isolated pinky is the weakest joint |
| L | High | — | — | — | |
| Y | High | — | — | — | |
| F | Low-medium | — | — | — | Hardest in the set; first candidate for the cut ladder |

**Decision rule after P1.5:** any shape below ~70% lock rate in normal light is **removed from the set and moved to the honest mirror**. The set is allowed to shrink to 6; it is not allowed to include a shape that fails.

**Where this table lives now.** The `Expected` column above is the design decision; the machine-readable
copy is `unity/Assets/StreamingAssets/trackability.json` (`expected` per shape), and the reasoning per
class — with one real example each — is `TRACKABILITY.md` §2.4. The shipped set, the excluded set and the
count the panel prints are derived from that file, not from this table, so **this table is not the place
to change a decision**: change the file, and this table is updated to match.

Two rows above have already moved from the pre-probe expectation: **F is `may-resolve`, not failing**
(its contact survives with fewer fingers) and **E is `may-resolve`**, so the by-design exclusion is
**M, N, R, T** — see `TRACKABILITY.md` §2.1's revision note and §3.3.

---

## 8. CAPTURE REQUIREMENTS *(detail in `CAPTURE-PROTOCOL.md`)*

| Requirement | Why |
|---|---|
| Every shape captured from a **named Deaf signer**, with a signed consent covering competition submission and future commercial release | R1 mitigation; the credit is part of the product |
| 2–3 vantage points per shape | The reference must look right when rotated — a single angle is exactly the flaw we're attacking |
| Both hands, or a documented left-handed mirror rule | One-handed users and left-handers are in scope |
| Neutral-pose capture per signer | All matching is *relative to neutral* |
| The excluded shapes captured too | We need them for the honest-mirror panel — the shapes we can't track still have to be *shown* correctly |
| Manifest entry: signer, date, consent ID, take numbers | `assets/masters/MANIFEST.md` |

---

## 9. THE CREDIT LINE *(draft, to be approved by the signer)*

> *Handshapes in this build were recorded by **[name]**, a Deaf ASL signer in [city]. This is practice software, not a replacement for a Deaf teacher — and if you want to learn ASL properly, learn it from one.*
