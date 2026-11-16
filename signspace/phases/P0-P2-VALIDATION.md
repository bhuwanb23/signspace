# P0–P2 validation — what is actually true before P3 starts

**Written:** 16 Nov 2026 · **Method:** every checkable claim in `p0-foundation/`, `p1-derisk/` and
`p2-core-loop/` was re-derived from the repository — scripts re-run, code compiled, artefacts counted —
rather than read from the phase write-ups. Where a document disagreed with the tree, the document was
corrected and the correction is listed in §5.

**One-line verdict:** the *code and the numbers* for P0–P2 hold up; **none of the three gates is green**,
and all three are blocked on the same missing object — a headset — not on unfinished work. P3's code can
continue; **G3 cannot pass before the P1.5/P2 device session**, because its honest-mirror count and its
zero-false-positive record both come from that session.

---

## 1. Scoreline

| Phase | Gate | What the gate needs | Met today | Blocked by |
|---|---|---|---|---|
| **P0** | G0 | empty build on `Competition` + invite URL for a stranger | **5 of 9** (2 partial, 2 open) | headset (2 boxes) · owner strings (channel name, package check) · signer reply |
| **P1** | G1 | grab feels good; ≥8 of 20 shapes track | **0 of 9** — 8 of 9 have a compiled rig, 0 have a reading | headset, entirely |
| **P2** | G2 | a stranger locks one shape, uncoached | **5 of 8** by repository measurement | headset (20/20 + 0/20 run, payoff footage) · one stranger |

The distinction that matters: **P1's zero is not zero work.** Nine criteria, nine measurement rigs built
and compiled, and no readings — that is a phase waiting on hardware, not a phase that failed.

---

## 2. What I re-ran, and what it returned

All commands from `signspace/scripts/`. The scripts are deterministic: re-running them left the working
tree byte-identical, so these numbers are the committed evidence regenerated, not new claims.

| Check | Command | Result |
|---|---|---|
| Pose schema | `python validate_poses.py --self-test` | **29 cases passed**; 3 broken fixtures rejected |
| Pose set | `python validate_poses.py` | **28 shapes, 0 problems** (6 shipped + 20 probe + 2 facing probe) |
| Joint names | `python check_alias_tables.py` | **61 spellings, C# and Python agree** |
| Metric | `python tune_match.py` | nearest pair **26.0°**, noise p95 **6.7°**, score **3.89**; sensitivity holds to σ=4.0°/joint |
| Bands | `python band_sim.py` | at `closeDeg 24`: **0.04% false close**, **0.0%** lock from the neighbouring shape; 24 survives ~40% more noise than modelled |
| Hold | `python hold_sim.py --cases` | **12/12 state-machine cases pass** (single latch, grace, decay, forget) |
| Payoff | `python payoff_previs.py --timing-only` | **0.99 s of 1.20 s** (82%), 0.54 s after 3 locks |
| Palm | `python palm_sim.py` | wrist jitter sd **1.41°**, p99 6.43° → 15° slack = **10.6σ**; decision `k = 1.00` |
| Mirror | `python mirror_sim.py` | all 26 bones **within 0.01°**, all **36 ordered pairs** deviation **0.0°** |
| Copy lint | `python lint_copy.py` | **0 violations**, 20 strings in 7 files |
| Compile | Unity `-executeMethod BuildScript.ApplySettingsOnly` | **EXIT 0, 0 `error CS`** (Unity 6000.3.28f1) |
| Probe set | `ls StreamingAssets/Probe/*.json` | **20 poses** + index + meta |
| Shipped set | `ls StreamingAssets/Signs/*.json` | **6 poses** + index + meta + weights-side files |
| P1 glue | `grep IHandStateProvider / BindCanonical` | **MetaHandSource implements both**; binds via `poseSource.BindCanonical`, so the "AutoBind has no caller" defect is genuinely closed |
| Scene wiring | `grep MetaHandSource Scenes/Main.unity` | `HandRig` with `TrackedHand_Left` / `TrackedHand_Right`, built by `SceneBuilder` → `HandRigBuilder` |
| APK | `sha256sum Builds/Android/signspace.apk` | `2879b8d0…`, 62 MB, versionCode 10 |

**The P2 report's central claim — every constant is traceable to a re-runnable script — is true.** Four
simulators reproduced their published figures exactly.

---

## 3. Phase by phase

### P0 — Foundation · 5 of 9

Verified in the tree: `ProjectVersion.txt` = **6000.3.28f1**; `manifest.json` = **Meta XR 207.0.0**;
`package-name.txt` = `com.signspace.signspace` = `ProjectSettings.applicationIdentifier.Android`;
six shapes + `index.json` present; a built APK exists.

Not verified, and not verifiable from here:
- **The channel is named `Competition`** — a string on Meta's dashboard. Unread.
- **The build installs and shows hands + passthrough** — needs the headset.
- **The evidence folder is empty** (`evidence/README.md` only). The invite URL is owner-confirmed but has
  no screenshot filed, which G0's own criterion asks for.
- The signer brief is sent; no reply.

Correction made: G0 claimed "no `[VERIFY]` left in the tech stack table" while `TECH-STACK.md` still
carried one on the Unity row. The toolchain **is** pinned, so the document was fixed rather than the tick
removed.

### P1 — De-risk · 0 of 9, all hardware

Everything with a rig exists and compiles: `MetaHandSource`, `HandVisual`, `ReferenceHand` (3 rotation
variants), `SignSetLoader`, `ProbeBuilder` + 20 probe poses, `MrukBoot` + `SurfaceProbe`,
`PersistenceProbe`, `PerfProbe`. The two READ-MAP defects — grab could never fire, and the matcher was fed
nothing — are closed, and I confirmed the closure by grepping for the bridge, not by trusting the log.

What P1 is *not*: tested. There is no grab trial, no rotation rating, no MRUK detection, no relaunch, no
fps sample. The phase's own logs are honest about this; four of them claimed "never compiled", which is
now false and has been corrected.

### P2 — Core loop · 5 of 8

Five criteria are satisfied by measurement in this repository and cite their script; three need hardware
(20/20 + 0/20 on device, the filmed payoff, the stranger). The constants in the build match the constants
in the scripts, enforced at build time by `BuildScript.VerifySceneTuning()` — this is the strongest thing
in the phase, because it makes the "scene silently kept the old thresholds" failure impossible.

---

## 4. The one gate defect that would have bitten P3 immediately

**G3 requires "all 8 documented near-miss pairs".** Four of those eight pairs — A↔O, I↔Y, L↔Y, D↔F — need
shapes that are not in the shipped six (`B U V W C O`). `SIGNSET.md` §5 is the 12-shape design; §1.5 cut
the set to six and with it four pairs.

So the criterion, as written, **cannot be passed by any build this project will submit.** It has been
corrected to the four pairs the set actually contains (B↔U, U↔V, V↔W, C↔O) in both `gate.md` and
`tasks/p3.1-near-miss-table.md`, with the other four explicitly deferred to P5's 12-shape build.

Second, smaller trap: **only U↔V sits inside the 26° confusion window.** The other three pairs are 100°+
apart, so they exercise the naming rule but never the hard case. The rule test
(`near-miss-test.csv`, 36 pairs, 30 fires, 0 disagreements) covers the rule; the *hard* case is still a
single pair, and that should shape what the demo video shows.

---

## 5. Corrections applied during this pass

| File | Was | Now |
|---|---|---|
| `docs/02-tech/TECH-STACK.md` | Unity row generic + `[VERIFY]` | pinned **6000.3.28f1**, confirmed against `ProjectVersion.txt` |
| `p0-foundation/README.md` | "verified toolchain" unticked | ticked, with the three files that prove it |
| `p1-derisk/README.md`, `tasks/p1.1–p1.4` | "never compiled" | **compiled 16 Nov, 0 errors**; device run still pending |
| `p1-derisk/grab-log.md`, `mruk-log.md`, `persistence-log.md` | "nothing has ever been compiled" | compiled; readings still absent |
| `p2-core-loop/hold-and-dropout.md` | "nothing … has ever been compiled" | compiled; the in-Unity self-test still has to be run |
| `p2-core-loop/phase-2-complete.md` | "0 of 56 compiled"; "17 user-facing strings" | **56 of 56 compile**; **20 strings** in 7 files |
| `p3-feedback/gate.md`, `tasks/p3.1` | 8 pairs, 12 shapes, degree number on screen | **4 pairs**, six shapes, magnitude logged not shown |
| `p0-foundation/gate.md`, `README.md`, `P0-AUDIT.md` | "6 of 9 met, 1 partial" | **5 of 9 met, 2 partial** — the tick list has five `[x]`; the 6 double-counted a row |

Nothing was changed in code, constants, or evidence files.

---

## 5b. Follow-up, same day: the four software gaps are closed

The audit above separated hardware measurements from software that was still missing. All four software
items were built and checked without a headset afterwards (details in `BUILD-LOG.md` §8.7):

| Was | Now | Check |
|---|---|---|
| Reference hand's fingers all began at the palm's centre | `HandVisual.RestOffsets` — knuckles across the palm, joints at parent-bone tips | `HandRigCheck`: 45 / 17 / 18 / 192 mm (spreads were 0) |
| Honest mirror was text-only | four shape renders from their real probe pose files | 4 slots in `Main.unity` |
| No session cap, no off switch | cap 3 + `NearMissToggle` block | `near_miss_sim.py`: 3 fires, 0 when off |
| No facing pair existed | K/P in `Facing/`, probe-only | `facing_pair_sim.py`: 0.00° fingers / 90.00° O / 75.00° total |

This does **not** change the verdict: the three gates are still waiting on device measurements, listed in
§6 below. It means the *software* half of P3 is finished, so a device session is the only thing between
the current build and G3.

## 6. Can P3 start? Yes — with two dependencies named up front

**Ready now (no hardware):**
- P3.1 near-miss rule — built and rule-tested; the 4-pair scope is now correct.
- P3.3 copy — draft exists; the owed step is reading it to one non-signer, which needs a person, not a headset.
- P3.4 honest mirror — panel, handle and withheld count are in the scene by design.

**Blocked on the device session, and nothing else:**
- **P3.6** needs P1.5's measured count. Until then the panel says *pending* — which is the correct state,
  not a placeholder to be filled with an estimate.
- **P3.2** needs 20 sloppy attempts for the zero-false-positive claim.
- **P3.5** needs the three degradation scenarios (hand leaves frame, low light, occlusion).
- **G3 criterion 8** needs a stranger for 30 seconds.

So the honest work order is: **device session (P1.5 → P2 items 2/4/6/8 → P3.2/P3.5/P3.6) → G3**, with
P3.3's read test done by any available person in the meantime. P3's remaining work is measurement, not
construction — the same shape P1 and P2 are in.

---

*Related:* [`p0-foundation/P0-AUDIT.md`](p0-foundation/P0-AUDIT.md) ·
[`p1-derisk/READ-MAP.md`](p1-derisk/READ-MAP.md) ·
[`p2-core-loop/phase-2-complete.md`](p2-core-loop/phase-2-complete.md) ·
[`p3-feedback/gate.md`](p3-feedback/gate.md)
