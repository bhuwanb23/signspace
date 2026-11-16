# The near-miss line: ten drafts, one read, one pick

**Phase:** P3 · **Task:** P3.3 · **Gate:** G3 · **Status:** final string chosen and shipped
**Recorded in:** `docs/06-submission/SUBMISSION-COPY.md` §7

> The line appears at the emotional low point of an attempt. It has to be warm, short and specific —
> a scolding or a paragraph undoes the whole mechanic.

---

## The line that ships

> **"That's O — 3° away. C opens a little."**

- **9 words** (budget 12), worst case across the eight pairs **11**, plain form **6**
- no banned word, **no exclamation mark**, no percentage
- **one thing to do next**, and it is the thing the two shapes actually differ by

Plain form for the high-contrast / text-only path (P6.5 selects it):
> **"That's O. C opens a little."**

When a pair has no authored hint, the app falls back to the first sentence alone — *"That's O — 3° away."*
Shorter, still true, and no invented correction.

---

## 1. Ten drafts, mechanically checked

The mechanical pass is narrow on purpose: word count, banned words (`lint_copy.py`'s list), exclamation
marks, linguistic jargon, and whether the line contains an actual instruction. What it cannot check is
whether a human understands it — that is §3.

| # | line | words | banned | jargon | has an action | |
|---|---|---|---|---|---|---|
| 1 | That's O. C opens a little. | 6 | 0 | 0 | yes | kept — the plain form |
| **2** | **That's O — 3° away. C opens a little.** | **9** | **0** | **0** | **yes** | **SHIPS** |
| 3 | That's O — 3° away. C wants a little more space. | 11 | 0 | 0 | yes | finalist |
| 4 | That's O, 3° away. For C, open the fingers a little. | 11 | 0 | 0 | yes | finalist |
| 5 | You made O — 3° away. C opens the hand. | 10 | 0 | 0 | yes | rejected: "You made" reads as a report on the person |
| 6 | That's O. C leaves a small gap. | 7 | 0 | 0 | yes | rejected: describes C, does not say to do anything |
| 7 | That's O — 3° away. | 5 | 0 | 0 | **no** | rejected: correct but useless — the old placeholder's shape |
| 8 | That's O. Loosen the fingers a little for C. | 9 | 0 | 0 | yes | rejected: "loosen" is true of C but vaguer than "opens" |
| 9 | That's O — 3° away. Try C with a little more space. | 12 | 0 | 0 | yes | rejected: at the 12-word ceiling after the hint is added |
| 10 | That's O. C is the same shape, opened up. | 9 | 0 | 0 | yes | rejected: "the same shape" invites comparison of the whole hand |
| — | *That's not C. That's O — 3° away.* (shipped before this task) | 8 | 0 | 0 | **no** | **replaced — negative framing and no action** |

Two mechanical facts that decided more than taste did:

1. **The degree number eats 3 of the 12 words.** P3.1 requires the magnitude in the message, so every
   variant that also says what to do is spending a third of its budget on a number. That is why the
   winner is 9 words rather than 6.
2. **The old line fails the done-when, not just the taste test.** *"That's not C"* is negative framing,
   and the sentence contains nothing to do. It passed `lint_copy.py` because "not" is not on the banned
   list — a reminder that the lint is a floor, not a standard.

---

## 2. What each clause is doing

| Clause | Why it exists | Why it is safe to say |
|---|---|---|
| "That's O" | names the shape the hand made, which is the entire feature | the rule only fires when the hand is inside the lock band on O (P3.1), so it is measured, not guessed |
| "3° away" | P3.1's magnitude, in whole degrees, never 0 | it is `D(O)` — how close the hand came to the shape being named, the only number in the claim a user can check |
| "C opens a little" | the specific half: one instruction, aimed at the target | derived from the authored deltas: C flexes 38/48/34 where O flexes 52/72/54 and adds thumb contact, so from O the way to C is **less** curl |

That last row is the trap in this task, and it is why the hints are derived rather than remembered. The
intuitive guess — "C is more curved than O, so curl more" — is backwards: **O is the more closed shape.**

---

## 3. The one-read test

Step 2 of the task asks a non-signing friend what they think the app wants them to do next. One reader
was available (the project owner, a non-signer of the pair under test), presented with the four finalists
and the situation *"the app has asked for C; you've made O; it fires when your hand is ~3° from O"*:

- **Chosen: "That's O — 3° away. C opens a little."** — the shortest of the finalists, and the only one
  where the instruction is a verb the reader can act on without knowing the alphabet.
- **Runners-up:** "…C wants a little more space." (more concrete for a non-signer, but 2 words longer) and
  "…For C, open the fingers a little." (unmistakable target, reads like a manual).

**Honest limitation:** one reader, and that reader is on the project. This is *not* the non-signer test
the task asks for. §5 is the protocol for the real one, and the criterion "understood by a non-signer in
one read" stays open until it runs.

---

## 4. The eight hints

Every ordered pair in the testable set, worst case 11 words, all lint-clean. Derived from
`hand_model.SHAPES`, not from the alphabet.

| target | made | hint | | target | made | hint |
|---|---|---|---|---|---|---|
| C | O | C opens a little. | | O | C | O closes the circle. |
| V | U | V spreads those two. | | U | V | U holds them together. |
| W | V | W straightens the ring finger. | | V | W | V folds the ring finger down. |
| U | B | U folds the last two. | | B | U | B straightens all four. |

**Four pairs have no hint on purpose**: A/O, I/Y, L/Y and D/F need `A`, `D`, `F`, `I`, `L`, `Y`, which are
still `probeOnly` poses (P5 content). A hint for a shape the app cannot show would be a correction with
nothing to look at — the fallback sentence is the honest answer until the shapes exist.

---

## 5. The test that is still owed

Run this with **two people who do not sign** (the task asks for one; two is what it takes to notice a
misread rather than a preference):

1. Show the four finalists, one at a time, with no context.
2. Ask: *"What does the app want you to do next?"* Record the answer verbatim.
3. Pass: the answer is *"make a C"* or *"open my hand"* — a shape or an action. Fail: *"it says I'm
   wrong"*, or any answer that only repeats the sentence back.
4. If one finalist collects both passes, it is the line and the picked one becomes the alternate.

Until then the shipped line is the best available read, and `SUBMISSION-COPY.md` §7 records it as
**pending non-signer confirmation** so nobody later mistakes it for tested copy.

---

## 6. Done when

- [x] **A final string ≤ 12 words** — 9; worst case across eight pairs 11; plain form 6
- [~] **Understood by a non-signer in one read** — one reader, and that reader is on the project. The pick
      is recorded; the real test is §5 and the criterion stays open
- [x] **No negative framing** — "That's not C" is gone; the line asserts what the hand *is* and what the
      target *needs*, and `lint_copy.py` reports 0 violations across 29 user-facing strings

### Traps

| Trap | How it was met |
|---|---|
| Explaining the linguistics in the UI | no jargon survives the mechanical check (opposition / abduction / flexion / minimal pair are all rejected), and the hints are one clause long. The pair structure — why C and O are confusable — is for the video, not the wall |
| *(found while writing)* guessing the anatomy | the intuitive hint ("C curls a little more") is exactly backwards: O is the more closed shape (52/72/54 flex plus thumb contact against C's 38/48/34). A line that taught the wrong direction eight times a session is worse than no line |
| *(found while writing)* a lint that passes a bad line | the old line passed `lint_copy.py` while failing the done-when: "not" is not a banned word, and there was nothing to do in the sentence |
