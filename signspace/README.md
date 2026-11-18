# SIGNSPACE

> **Every app that teaches sign language is a video with extra steps. This one tells you which sign you accidentally made.**

A seated, hands-only 3D ASL handshape practice prototype. Turn a 3D reference hand with your wrist, match it with your hand, and receive near-miss feedback. The checked-in poses are reference-authored; Deaf signer review and consent are not evidenced in this repository.

**Entry:** Meta VR Start Developer Competition 2026 · **Track:** Productivity / New Experience · **Deadline:** 19 Nov 2026, 01:30 IST

---

## Where things stand

| | |
|---|---|
| **Project** | Unity code, scenes, prefabs and offline tests exist; see the [repository audit](docs/05-build/REPOSITORY-AUDIT.md) |
| **Active content** | Six reference-authored shapes: B, U, V, W, C, O; `Signs/index.json` is the runtime list |
| **Evidence** | Local static tests pass; Unity compile and device behavior were not verified in this audit |
| **Phase gates** | Repository gate files remain open; signer, device and submission evidence may be stored outside git |
| **Signer rule** | Do not claim Deaf signer approval or release unverified content without confirmed consent, review and agreed credit |

---

## Read in this order

| # | Document | What it answers |
|---|---|---|
| 1 | **[`docs/00-plan/MASTER-PLAN.md`](docs/00-plan/MASTER-PLAN.md)** | The 11 phases, their sub-phases, gates and cut ladder. **Start here.** |
| 1b | **[`phases/`](phases/README.md)** | **76 per-sub-phase work files** — steps, acceptance criteria, artefacts and traps for every phase |
| 2 | [`docs/01-idea/CONCEPT.md`](docs/01-idea/CONCEPT.md) | The whole product on one page |
| 3 | [`docs/01-idea/NOVELTY-AND-PRIOR-ART.md`](docs/01-idea/NOVELTY-AND-PRIOR-ART.md) | What already exists, what we must stop claiming, the five things nobody is doing |
| 4 | [`docs/01-idea/COMPETITION-FIT.md`](docs/01-idea/COMPETITION-FIT.md) | Rubric → evidence map, the three hard tests, award targeting |
| 5 | [`docs/02-tech/TECH-STACK.md`](docs/02-tech/TECH-STACK.md) | Every package, every rejection, the performance budget |
| 6 | [`docs/03-content/SIGNSET.md`](docs/03-content/SIGNSET.md) | The 12 shapes, the 3 families, the deliberate exclusions |
| 7 | [`docs/05-build/RISKS.md`](docs/05-build/RISKS.md) | 12 risks and the one that can kill the project |
| 8 | [`docs/06-submission/SUBMISSION-COPY.md`](docs/06-submission/SUBMISSION-COPY.md) | Submission copy; run `scripts/count.py` for current limits |

## Repository map

```
phases/                             one folder per phase: README (charter) · gate.md · tasks/pN.M-*.md
docs/
├── 00-plan/MASTER-PLAN.md          ← phases, gates, cut ladder, status board
├── 01-idea/                        CONCEPT · NOVELTY-AND-PRIOR-ART · COMPETITION-FIT · IDEA-EVALUATION
├── 02-tech/                        TECH-STACK · ARCHITECTURE · POSE-FORMAT · MATCH-ALGORITHM
│                                   GAZE-INPUT · MRUK-SHELF · PERFORMANCE
├── 03-content/                     SIGNSET · CAPTURE-PROTOCOL · TRACKABILITY · ACCESSIBILITY
├── 04-design/                      FIRST-FIVE-MINUTES · UX-STATES · SOUND-MAP
├── 05-build/                       RISKS · DEFINITION-OF-DONE · PLAYTEST-LOG
├── 06-submission/                  SUBMISSION-COPY · VIDEO-SCRIPT · CHECKLIST
├── 07-research/                    competition deep dive, hackathon strategy, battle card
└── 08-decisions/                   six ADRs: engine, language, forgiveness, scope, offline, positioning

unity/                              Unity project root — created at P0.4
assets/                             media; masters stay out of git
scripts/                            count.py (submission limits) · status.py (doc progress)
```

## The three sentences

1. **You cannot rotate a video** — that's why sign-language learning is broken on 2D.
2. **This is the first app whose content is your own hand** — tracking isn't the control scheme, it's the subject.
3. **It tells you which sign you accidentally made** — feedback no 2D resource can give.

## Rules of this repo

1. **No phase starts before its gate is green.** Gates exist because the one fatal failure mode is a broken APK on Day 11.
2. **Every phase writes a file.** If it isn't here, it didn't happen.
3. **Scope only moves down.** The cut ladder is pre-agreed; nothing new after Day 6.
4. **`main` is always buildable.**
5. **No handshape ships without a credited Deaf signer.** That is not negotiable and never has been.

```bash
python3 scripts/status.py      # doc progress: done vs stub
python3 scripts/count.py       # tagline ≤140 chars, description ≤500 words
```
