# P3.3 — Near-miss copy

**Status:** 🟡 drafted and constraint-checked; **not yet read by a non-signer** (that is a people
test, not something this repository can perform).

## Constraints (from `tasks/p3.3-copy.md`)

- ≤ 12 words
- no "incorrect", no "error", no exclamation mark
- appears at the emotional low point of an attempt, so it names the shape without scolding
- a non-signer should understand in one read what the app is telling them

## Variants

| # | Line | Words | Verdict |
|---|---|---|---|
| 1 | That's not U. That's V. | 5 | **Selected.** Shortest line that is still specific. |
| 2 | Not quite U — that's V. | 5 | Warmer, but "not quite" is softer than the fact. |
| 3 | You made V. The target is U. | 7 | Clear, but reads like a report. |
| 4 | That shape is V, not U. | 6 | Neutral; loses the turn of phrase that makes it memorable. |
| 5 | Almost U. Your hand made V. | 6 | "Almost" flatters a different shape. |
| 6 | That's V. U is the shape here. | 8 | Explains the task instead of naming the sign. |
| 7 | Close to U, but that's V. | 6 | "Close" is the band's word, not the diagnosis's. |
| 8 | Your hand is making V, not U. | 7 | Accurate; slightly clinical. |
| 9 | Not U yet — that's V. | 5 | "Yet" implies the app is grading, which it is not. |
| 10 | U is the target. You made V. | 7 | Two sentences of instruction; too much for two seconds. |

## Selected

> **"That's not {0}. That's {1}."**

`{0}` = the target shape, `{1}` = the shape the hand actually made. Five words. No degree number:
the magnitude belongs in the debug overlay, where it can be checked, not in a line that has to be
read while the hand is still moving. The `NearMissDisplay` template keeps an optional `{2}` slot for
a measured delta, but the shipped template leaves it unused.

Why this one: it states both facts in the order the learner needs them — *what I was trying* then
*what I did* — and nothing else. No apology, no instruction, no praise. The product's voice
elsewhere is equally plain, and the line has to survive being read at a glance.

## Still owed

- [ ] Read variants 1, 2 and 4 to a non-signing friend and ask what they think the app wants them
      to do next. Pick only from what they understood.
- [ ] Record the final string in `docs/06-submission/SUBMISSION-COPY.md` under in-product copy.
- [ ] High-contrast / text-only variant for P6 (same words, larger type, no halo behind it).
