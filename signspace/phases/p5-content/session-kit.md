# P5.3 — Capture session kit

**Status:** ⛔ **blocked on the signer.** The kit is ready; the session itself needs a person in the
room and cannot be run by the agent. Nothing below was executed on 16 Nov 2026.

**Use with:** [`docs/03-content/CAPTURE-PROTOCOL.md`](../../docs/03-content/CAPTURE-PROTOCOL.md)
(§2 one-pager, §4 takes, §5 naming, §6 manifest, §8 retake, §9 schedule).

---

## 0. The 30 minutes before the signer arrives

| # | Check | Pass condition |
|---|---|---|
| 1 | Both cameras record landscape, 1080p, to local storage | a 10 s test clip plays back |
| 2 | Lighting | front light, no strong backlight; the back of the hand is lit, not silhouetted |
| 3 | Background | plain; no patterned wall behind the hand |
| 4 | Signer's hand | wrist visible; **no rings, watch or reflective jewellery** |
| 5 | Table height | the hand can rest at chest height without the arm blocking the palm camera |
| 6 | Recording consent line loaded | §10 of the protocol open on screen |
| 7 | Take sheet printed | one row per take, a pen per take |
| 8 | Manifest folder created | `assets/masters/<signer-slug>/<YYYY-MM-DD>/` |
| 9 | Timer | 90 minutes on the wall, visible from the capture chair |

**If any of 1–4 fails, fix it before the signer sits down.** A retake costs the session; setup costs
half an hour.

---

## 1. The call, in order (90 minutes)

| Time | What | Who |
|---|---|---|
| 0–5 | Recording started; consent script read aloud (§10); signer answers | team |
| 5–10 | Credit wording agreed (§11); compensation confirmed in writing | both |
| 10–12 | Neutral, start: 2 s, palm down, three vantages | signer |
| 12–46 | 12 shipped shapes in family order (B,U,V,W · A,C,O,D · I,L,Y,F) | signer |
| 46–60 | Review each authored 3D shape: yes/no per shape | signer |
| 60–75 | Blind-spot set M, N, T, R, S, E (front + side) | signer |
| 75–80 | Neutral, end + re-shoot rejected takes | signer |
| 80–85 | Transcribe the approval table from the recording | team |
| 85–90 | Confirm two takes read correctly; fee paid; link sent; archive | both |

---

## 2. Take sheet (tick per take)

One row per shape. `F` front · `L` side-left · `R` side-right. `AP` = approved by the signer.

| Shape | F | L | R | AP | Retake note |
|---|---|---|---|---|---|
| B | ⬜ | ⬜ | ⬜ | ⬜ | |
| U | ⬜ | ⬜ | ⬜ | ⬜ | |
| V | ⬜ | ⬜ | ⬜ | ⬜ | |
| W | ⬜ | ⬜ | ⬜ | ⬜ | |
| A | ⬜ | ⬜ | ⬜ | ⬜ | |
| C | ⬜ | ⬜ | ⬜ | ⬜ | |
| O | ⬜ | ⬜ | ⬜ | ⬜ | |
| D | ⬜ | ⬜ | ⬜ | ⬜ | |
| I | ⬜ | ⬜ | ⬜ | ⬜ | |
| L | ⬜ | ⬜ | ⬜ | ⬜ | |
| Y | ⬜ | ⬜ | ⬜ | ⬜ | |
| F | ⬜ | ⬜ | ⬜ | ⬜ | |
| M | ⬜ | ⬜ | — | ⬜ | honest mirror |
| N | ⬜ | ⬜ | — | ⬜ | honest mirror |
| T | ⬜ | ⬜ | — | ⬜ | honest mirror |
| R | ⬜ | ⬜ | — | ⬜ | honest mirror |
| S | ⬜ | ⬜ | — | ⬜ | honest mirror |
| E | ⬜ | ⬜ | — | ⬜ | honest mirror |

**Retake rule (§8):** out of frame · rotated past 30° · unlit · jewellery · motion blur · wrong shape.
Re-ask on the call; never ship a bad take.

---

## 3. The signer's own words (this becomes the in-app copy)

Record their description of each shape **in their words**, not ours. Do not paraphrase; quote.

| Shape | The signer's description (verbatim) |
|---|---|
| B | |
| U | |
| V | |
| W | |
| A | |
| C | |
| O | |
| D | |
| I | |
| L | |
| Y | |
| F | |
| M | |
| N | |
| T | |
| R | |
| S | |
| E | |

**Why this row exists:** the shipped descriptions are working notes by the team. P5.4 and P5.7 both
need the signer's register, and a paraphrase loses it.

---

## 4. Playback confirmation (last 5 minutes)

Play back **two** takes — the one the signer was most confident about and the one they hesitated on —
and ask, on the recording:

> "Does this read as the shape you meant?"

Record the answer. If the second one does not read, it is not approved, and the shape goes to state B
in `set-meta.json` rather than shipping on a guess.

---

## 5. After the session (P5.4 hand-off)

1. Transcribe the approval table from the recording (§12 step 1).
2. Copy takes into `assets/masters/<signer-slug>/<YYYY-MM-DD>/` using §5's naming.
3. Fill `assets/masters/MANIFEST.md` row 1.
4. Hand the takes to P5.4 (retarget/clean) and the signer's descriptions to P5.4/P5.7.

---

## Blocked rows (cannot be run by the agent)

- [ ] All 12 shapes captured from 3 vantage points
- [ ] Blind-spot set captured
- [ ] Signer has confirmed each take reads correctly
- [ ] Signer's own descriptions recorded
- [ ] `assets/masters/<signer>/` populated
- [ ] `assets/masters/MANIFEST.md` complete

**This is a human task.** The protocol, consent, compensation and credit documents are ready; the
session needs a Deaf signer present. G5 stays open until it happens.
