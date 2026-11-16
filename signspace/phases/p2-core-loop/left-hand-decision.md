# Left-hand support — the decision, and the evidence for it

**Phase:** P2 · **Task:** P2.7 · **Gate:** G2 · **Status:** decided, built, tested offline; one device
test outstanding
**Raw output:** `phases/p2-core-loop/mirror-sim-raw.txt` (regenerate: `python3 scripts/mirror_sim.py`)
**Fixtures:** `phases/p2-core-loop/mirrored-fixtures/` (B-left, C-left, V-left)

---

## The answer, in one paragraph

**This build reads the right hand. The left hand is switched off in one place, on purpose, and the
reason is written down where a user can find it.** Not because mirroring is hard — the mirror turns out
to be *exact* on our data, to the bit, and the matcher needs no change for a second hand at all — but
because the one thing a pose file cannot contain is the answer to whether OpenXR hands us left-hand
joint frames that are already mirrored. Get that wrong and a perfect left hand reads ~180°, which is
exactly the silence it reads today. Two of the three possible outcomes are "a left-hander sees
nothing", and the third needs hardware. Half-support is worse than none, so: none, said out loud, with
the enable path gated behind a single constant that states its condition.

Along the way this task found something that was not about left-handers at all, and fixed it: **both
hands were feeding one matcher.**

---

## 1. What was mirrored, and what was measured against

The six shipped poses in `unity/Assets/StreamingAssets/Signs/`, loaded off disk rather than
re-synthesised, so the test is of the files that ship. Every one is authored `handedness: "right"`, and
every one moves the thumb substantially (87–152° at its worst joint), which is what makes the trap in
§3 testable rather than hypothetical.

Three of the six were mirrored and written out as fixtures — **B** and **V** because `POSE-FORMAT §4.3`
names them as the device-test pair, and **C** because it is the shape where a wrong mirror is most
visible (see §3). They validate clean (`validate_poses.py`: 3 shapes, 0 problems) and they live in
`phases/`, **outside `StreamingAssets`**, so no mirrored file can reach a build by accident. That keeps
the rule `POSE-FORMAT §4.3` already carries — *no mirrored file ships before the device test* — at a
cost of zero, because of §2.

---

## 2. The mirror, derived — and the trap that is not where the brief says it is

A reflection in the plane `x = 0` is `M = diag(-1, 1, 1)`. A rotation `R` becomes `M R M`, which is
still a rotation: that is why a left pose is not a new kind of object, only a conjugated one.
Conjugating a unit quaternion by that reflection gives

```
rot   (x, y, z, w)  ->  (x, -y, -z, w)          bones  (dx, dy, dz) -> (-dx, dy, dz)
```

which is the same rotation as `(-x, y, z, -w)`, because `q` and `-q` are the same rotation. The P2.7
brief says "negate the X axis", and **that is right — once the double cover is acknowledged.** The
version that negates `x` and keeps `w` is not the mirror; it is an ordinary rotation, and it is wrong in
a way that looks fine.

Measured against the true geometric mirror, worst bone direction over all 26 joints, per shape:

| rule | B | U | V | W | C | O | verdict |
|---|---|---|---|---|---|---|---|
| `(x, -y, -z, w)` — canonical, `pose_schema.mirror_right_to_left` | 0.00 | 0.00 | 0.00 | 0.00 | 0.00 | 0.00 | **exact** |
| `(-x, y, z, -w)` — "negate X and w" | 0.00 | 0.00 | 0.00 | 0.00 | 0.00 | 0.00 | **exact** |
| `(-x, y, z, w)` — "negate X", literal | **80.05** | **89.62** | **89.62** | **83.98** | **83.31** | **75.99** | not a mirror |

### The thumb trap, as measured

The brief warns that "thumb opposition usually breaks first". On this data that is **only partly true,
and the real failure is worse**:

| shape | thumb | index | middle | ring | little | worst |
|---|---|---|---|---|---|---|
| B | 25.6 | 11.8 | 0.0 | 15.3 | **26.4** | little |
| U | 9.0 | 7.9 | 0.0 | **23.2** | 18.7 | ring |
| V | 9.0 | **88.7** | 0.0 | 23.2 | 18.7 | index |
| W | 2.3 | **73.3** | 0.0 | 71.5 | 18.7 | index |
| C | **44.4** | 36.0 | 40.1 | 37.8 | 28.4 | thumb |
| O | 1.4 | 41.9 | **45.2** | 42.1 | 34.0 | middle |

*(millimetres of tip displacement under the literal rule; the correct mirror's residual is `0.00e+00`
mm on every joint of every shape.)*

Worst-displaced finger counted over the six: **thumb 1, index 2, middle 1, ring 1, little 1.**

So the thumb is worst on one shape of six, not "usually first". The mechanism is not opposition
specifically — it is **any rotation about `Y` or `Z`**, which is abduction and opposition alike, and our
shapes spend their large angles on ring and little abduction. That makes the trap *more* dangerous than
stated, not less: a literal mirror breaks every chain at once, so there is no single finger you can look
at and declare the mirror sound. A wrong mirror looks like a hand. The only way to pass this is to
**derive** the mirror, which is why §2's canonical row is exact rather than approximately right.

---

## 3. Is the metric mirror-invariant? Yes — so the matcher needs nothing for a second hand

`D(M P, M Q) == D(P, Q)` for all 36 ordered pairs of the six shapes: **deviation 0.0°, bit for bit**,
including P2.6's palm term — and since every authored pose leaves the wrist at identity (P2.6's own
finding), the palm term was re-measured with the wrist turned 20/45/90°, where it has a value at all:
**deviation 0.00e+00°**.

Two consequences:

1. **A mirrored pose locks exactly as often as the original** (2°/joint noise, 500 trials per cell):
   100.0% for the right hand, **100.0% for the mirrored left**, **0.0% for a left hand that is not
   mirrored**. The mirror is sufficient for the data.
2. **The comparison is the wrong place to fix this.** Mirroring is invariant in the metric, so it has
   to happen once, either to the shipped pose or to the live input. Given §5, the live input is the
   better place: one call per joint fixes the whole set at once and ships no mirrored asset.

### What a left hand actually sees in this build today

A correct left-hand attempt, compared against the right-handed reference, with no mirroring:

| shape | D_fingers | O (palm) | D_total | band |
|---|---|---|---|---|
| B | 145.1 | 0.0 | 145.1 | warming |
| U | 168.5 | 0.0 | 168.5 | warming |
| V | 168.5 | 0.0 | 168.5 | warming |
| W | 168.6 | 0.0 | 168.6 | warming |
| C | 97.0 | 0.0 | 97.0 | warming |
| O | 176.1 | 0.0 | 176.1 | warming |

**97–176° on a perfect shape.** Not "close", not "warming slightly" — the coldest state the app has, for
a user doing everything right. That is what "half-support" means here, and it is why the decision is not
"we'll get to it".

---

## 4. The device question this test cannot answer

The mirror above answers everything about the **files**. It cannot answer anything about the **rig**,
because a pose carries rotations and no positions:

- a rig that keeps the right hand's rest offsets on the left hand puts the thumb on the wrong side, and
  no amount of correct quaternion maths can see it;
- OpenXR may hand out left-hand joint frames that are already mirrored. If it does, mirroring the input
  mirrors it twice and a perfect left hand reads ~180° — the same silence as today.

Both are settled by one measurement, and `POSE-FORMAT §4.3` already writes the procedure:

1. mirror `B` and `V` (done — `mirrored-fixtures/`), `handedness: "left"`, `mirrorOf` in the notes;
2. on device, make the **right**-hand `B` with the **left** hand. It should not lock. If it locks, the
   joints are already same-handed and the mirror is a no-op;
3. make the mirrored left `B`. It should lock. If it does not, author left poses directly
   (`make_pose.py --left`) rather than shipping a mirror that is wrong for everybody.

Then the check nobody has run yet, which is the one that actually protects G2:

4. **two hands in frame** — right hand holding a shape to `locked`, left hand open at your side. The
   lock must survive. Before this task it would not have, and nothing would have said why.

---

## 5. What changed in the build — the half-broken path, found and removed

The live defect was worse than "the left hand doesn't work". `HandRigBuilder` built both hands and bound
**both** pose sources to the **same** `Matcher`, so:

> whichever hand ticked the matcher last won the frame. A left hand anywhere in view overwrote the right
> hand's distance 60 times a second, so the 0.7 s hold in P2.4 could never complete while your other hand
> was visible — and the band would flicker between two hands' geometries.

That is a core-loop bug that would have killed G2 for everyone, not just left-handers, and it was
introduced by treating "the left hand exists" as if it were "the left hand is supported".

| File | Change |
|---|---|
| `Scripts/Matching/HandSupport.cs` *(new)* | The whole decision in one place: `LeftHandSupported = false`, `MirrorsInput()`, `FeedsMatcher()`, the one-line explanation and the dev-facing reason. Two visible outcomes out of three need hardware, so the constant states its own condition for being flipped |
| `Scripts/Matching/HandMirror.cs` *(new)* | The derived mirror — `(-x, y, z, -w)` — plus `IsMirrorInvariant()`. Gated off by `HandSupport`; shipped because the enable path is otherwise folklore someone re-derives under deadline |
| `Scripts/Hands/SkeletonPoseSource.cs` | `driveMatcher` + `mirrorInput`; the buffer now fills **whether or not** the hand feeds the matcher (P1.3 rates with the left hand and `WristRotation` reads the buffer); a static claim on the matcher, so a second source **logs and switches itself off** instead of fighting |
| `Scripts/Hands/MetaHandSource.cs` | Applies the policy at bind — the only moment both hand and pose source are known. `FeedsMatcher` is public, so a left hand is honestly "tracked, drawn, not playing" rather than half-wired |
| `Editor/HandRigBuilder.cs` | Wires only the hand the policy allows, and logs the decision per hand. The header now says why |
| `Scripts/UI/DebugOverlay.cs` | One dev line: which hand is feeding the matcher, and `HandSupport.Report()` for the other |
| `Scripts/UI/HonestMirror.cs` | `handLimitation`, rendered as its own line in the panel — the user-facing half of "no half-broken path" |

`HandSupport.LeftHandSupported` is a `const`, and every leg of the decision reads it, so there is **no
configuration that half-enables left-hand play**: not the builder, not the scene, not a ticked bool in
the inspector. That is the accepted meaning of "no half-broken left-hand path left in the build".

---

## 6. The user-facing part: one line, in the honest mirror

> **"This build reads your right hand. Left-hand play is not in this version."**

It lives in `HonestMirror` — the panel that publishes what the sensor cannot do — and **not** in a toast
or a first-run message, because `UX-STATES.md §6` gives the first five minutes **3 words total** and a
limitation is something a user chooses to look up. It is also not an error state: `LeftHandOff` is
recorded in `UX-STATES.md §4` as **0 words at the moment it happens** and no change of visual state,
because there is no failure in this product, only things the harness cannot see.

Copy lint: 0 violations (18 user-facing strings, up from 17).

**The honest risk, written down rather than smoothed over:** G2 asks whether *a stranger* can lock a
handshape uncoached. If that stranger is left-handed, this build gives them nothing until they find the
honest mirror, and a third of people are left-handed. That is a real gate risk and it is the reason §5's
single most important next step is not a feature — it is the 15-minute device test that would let us
flip one constant.

---

## 7. Done when

- [x] **A definite answer, tested, with the evidence** — **no left-hand play in this build**. The
      mirror is exact for the data (0.00° on all 26 joints, 6 shapes), the metric is mirror-invariant
      (0.0° over 36 pairs and under wrist turns), a mirrored pose locks 100% where an unmirrored left
      hand locks 0%, and a correct left hand reads 97–176° today. What remains is not a design question
      but one device measurement, named in `POSE-FORMAT §4.3` and in §4 above.
- [x] **No half-broken left-hand path left in the build** — the left hand is off in exactly one place,
      it still tracks and still reports, the second-source-per-matcher defect is fixed and self-guarding,
      the overlay states the policy, and the honest mirror states it to the user.

### Traps, and where each one was met

| Trap | How it was met |
|---|---|
| Assuming mirroring is symmetric — thumb opposition usually breaks first | measured, and *corrected*: the literal "negate X" breaks every chain (up to 89.6°, 88.7 mm of tip), and the thumb is worst on only 1 shape of 6. The named mechanism (opposition) is not special; **any rotation about Y or Z** breaks, so no single finger can be spot-checked. The derived mirror is exact on all 26 joints — the fix is derivation, not vigilance |
| *(found while doing it)* "the left hand is wired, so the left hand is supported" | both hands were bound to one matcher, so a visible left hand silently prevented every right-hand lock. Fixed, guarded, and logged |
| *(found while doing it)* testing the mirror but not the metric | mirroring is invariant in the metric, so a "mirrored pose locks" test passes for the wrong reason if the mirror itself is wrong; both were measured, and the wrong mirror was measured too |
