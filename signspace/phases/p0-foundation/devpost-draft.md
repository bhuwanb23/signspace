# Devpost draft — the paste pack

**Task:** P0.2 · **Gate:** G0 · **Purpose:** create the draft entry, and make Day-11 form-filling a copy-paste job instead of a memory test.

---

## 1. FIRST VISIT — 20 minutes

1. Open `devpost.com/submit-to/31024-meta-vr-start-developer-competition-2026/manage/submissions` signed in as the submitting account.
2. Start a submission and **save the draft immediately**.
3. Fill only the fields in §2 — nothing you would have to invent.
4. **Screenshot the whole form**, top to bottom, including collapsed sections → `evidence/p0.2-form-fieldlist-<date>.png`.
5. Note anything in §3 that differs from our assumptions, and correct `docs/06-submission/CHECKLIST.md` §1.
6. Record the submission ID in `accounts.md` §5.

**Why now:** two things can only be learned from the live form — the exact optional/required split, and any Devpost-only widget (gallery images, built-with tags). Everything else is already written.

---

## 2. FIELDS TO FILL TODAY

| Field | Paste exactly |
|---|---|
| Submission name | `SIGNSPACE` |
| Tagline | `Every app that teaches sign language is a video with extra steps. This one tells you which sign you accidentally made.` |
| Track | `Productivity` |
| Division | `New Experience` |
| Description | **leave blank** — P9.5, character-counted last |
| Target launch date | `Q1 2027` |
| Video URL | blank — P9.4 |
| Invite URL | blank — P9.2 |
| Team | `[CONFIRM]` — the representative receives the prize either way |

---

## 3. CONFIRM AGAINST THE LIVE FORM

The only real unknowns.

| # | Question | Why it matters |
|---|---|---|
| 1 | Is "Target launch date" a date picker or free text? | `Q1 2027` may need to be `2027-03-31` |
| 2 | Are gallery images mandatory? | If yes they must be shot at P9.3, not improvised on Day 11 |
| 3 | Is there a "Built with" tag field? | Cosmetic, but visible |
| 4 | Is the optional Hand Interactions field actually present? | If yes, `CHECKLIST.md` §2.5 is 168 free words of Technical Implementation |
| 5 | One description box, or Inspiration / How-built / Challenges as separate boxes? | Changes how the 494 words are structured |
| 6 | Does the form require an APK upload, or only the Invite URL? | Requirements say Invite URL; the form may differ |

---

## 4. DO NOT

- Fill the description from memory on Day 11 — it lives in `SUBMISSION-COPY.md` and the script's count is the authority.
- Submit early "to test the flow". There is no resubmit worth relying on.
- Paste the tagline without running `python3 scripts/count.py`.
