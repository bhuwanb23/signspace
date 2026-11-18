# Launch sketch — Q1 2027, one page

**Phase:** P10 · **Task:** P10.3 · **Status:** sketch, not a plan of record · **Written:** 18 Nov 2026

> The submission says "60 signs at launch, Q1 2027". This page is what that sentence has to mean for it
> to survive contact with a Store review, and which of its dependencies do not exist yet.

---

## 1. What "60 signs" means

**Sixty handshapes, one language at a time — ASL at launch.** Not sixty words, not sixty lessons, and not
sixty across several sign languages: each shape needs an authored joint-level pose, a tolerance band, and
at least two near-miss neighbours to remain diagnosable.

| Bucket | Count | Now | What it needs |
|---|---|---|---|
| Fingerspelling handshapes (A–Z, minus the ones a camera cannot resolve) | 24 | 20 shipped | 4 more, plus documented exclusions with reasons |
| Numbers 0–9 | 10 | 0 | 10 poses; `6`–`9` share kinematics with letters and can reuse them |
| Core word-sign handshapes (family, home, help, …) | 26 | 0 | signer review per shape — the expensive bucket |
| **Total** | **60** | **20** | |

**What stays out:** facial grammar, motion path, location and palm-orientation grammar. Those are what
make a sign a sign; adding them is a v2 project, and the description already says so.

## 2. What the Store requires that the competition channel does not

| Requirement | Competition channel | Quest Store | Gap |
|---|---|---|---|
| Developer organisation verified for Store distribution | no | yes | Meta org verification + tax/banking |
| Privacy policy URL and Data Use declaration | no | yes | hand-tracking data is on-device only — the declaration is short but mandatory |
| Store listing: icon, 5 screenshots, trailer, short + long description | minimal | full | capture on device, not the desk |
| Content rating questionnaire + age rating | no | yes | 10+ expected |
| Performance / technical review (frame rate, startup, comfort) | no | yes | **the 15-minute thermal run G7 owes is a Store prerequisite, not a nice-to-have** |
| Accessibility declaration (captions, contrast, one-handed) | no | recommended | already built in P6 — needs declaring, not building |
| Hand tracking + passthrough entitlement justification | no | yes | one paragraph each |

## 3. Milestones

| # | Milestone | Date | Exit criterion | Estimate |
|---|---|---|---|---|
| M1 | Judging closes; freeze released | 11 Dec 2026 | entry intact per `reverify-log.md` | 0 (observation) |
| M2 | Device truth: fps, thermals, 20 cold launches, five hostile conditions | 31 Dec 2026 | G7's NOT RUN rows become numbers | 3–4 days |
| M3 | Content to 40 shapes (numbers + eight word-signs) | 15 Jan 2027 | each shape has a tolerance band and a near-miss neighbour | 8–10 days |
| M4 | Signer review of all 40 | 22 Jan 2027 | written review, per-shape notes, no unresolved objection | 3 days of the reviewer's time |
| M5 | Store assets and declarations | 31 Jan 2027 | listing complete in the developer dashboard | 4–5 days |
| M6 | Store review submission | 8 Feb 2027 | submitted with the technical review package | 1 day |
| M7 | Content to 60 and launch | 15 Mar 2027 | 60 shapes reviewed, build passes Store review | 10–12 days |

The dates are the plan's, not a promise; M3 and M4 are the two that decide whether Q1 holds.

## 4. Dependencies that do not exist yet

1. **A Deaf signer under contract for 60 shapes.** The competition build's review was a one-off. Sixty
   shapes at three review passes each is a paid, scheduled engagement — and the entry's integrity claim
   depends on it. `assets/masters/COMPENSATION-AGREEMENT.md` is the template; nobody has signed it.
2. **Device time.** M2 cannot be done from a desk, and M2 is a Store prerequisite.
3. **A second language decision.** The engine is language-agnostic; the content is not. Launching ASL
   only is defensible and cheaper; claiming multi-language is not, until a second reviewer exists.
4. **Support and moderation.** A Store app has a support inbox and an age-rating obligation; neither
   exists today.

## 5. The honest risk

Sixty reviewed handshapes in fourteen weeks needs the signer engaged in December, not February. If that
contract is not signed by **15 Dec 2026**, the right move is to launch with 40 and say forty — the
product's whole credibility is that its numbers and its claims match.
