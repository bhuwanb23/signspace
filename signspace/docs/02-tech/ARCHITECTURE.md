# Architecture

**Status:** ✅ v1 · **Owner:** W2 · **Phase:** P2 · **Gate:** G2

## 1. The shape of the system

```
 tracking SDK ──► SkeletonPoseSource ──► Matcher ──► events ──► Feedback (visual + audio)
                                              │                   │
 signer/ref poses ──► SignSetLoader ──────────┘                   ▼
                                                              Shelf (MRUK anchor)
```

Six systems. Each owns exactly one thing.

| System | Owns | Never does |
|---|---|---|
| `SignSetLoader` | Reading JSON once at launch | Know about matching |
| `SkeletonPoseSource` | Turning tracked bones into `Quaternion[]` + confidence | Know what a shape is |
| **`Matcher`** | Distance, bands, hold, near-miss | Touch UI, audio, or the shelf. **Emits events only** |
| `Feedback` | Band visuals, lock payoff, near-miss line | Compute distance |
| `ShelfAnchor` | MRUK anchor, persistence, object slots | Know how a shape was matched |
| `AppState` | Which screen/mode is active | Anything spatial |

**The single most important boundary:** `Matcher` knows nothing about Meta's SDK. It consumes `Quaternion[]` and emits events. That is why an SDK upgrade cannot break the product's core logic, and why the matcher can be unit-tested with recorded data on a laptop.

## 2. Session state machine

```
Boot ─► HandsMissing ─► Ready ─► Inspect ⇄ (Gazing | Rotating | Warming | Close)
                                   │
                                   ├─► Locking ─► Locked ─► (object to shelf) ─► Ready
                                   ├─► NearMiss ─► Inspect
                                   └─► Paused (tracking lost) ─► previous state
```

Every state is in `docs/04-design/UX-STATES.md`. **There is no failure state** — that is a structural property, not a UI choice.

## 3. Data flow for one frame

1. `SkeletonPoseSource.Update` copies ~21 `localRotation`s into a reused `Quaternion[]` (no allocation).
2. `Matcher.Tick(rotations, confidence, dt)`:
   - computes `D(target)` and `D(bestOther)`;
   - updates the band with 150 ms hysteresis;
   - advances or pauses the hold timer;
   - evaluates the near-miss rule;
   - raises events.
3. Subscribers react. The matcher's job ends.

**Allocation budget: zero per frame.** The buffer is reused, the shape list is fixed at launch, and no LINQ runs in `Update`.

## 4. Persistence

One file, written on lock and on quit:

```json
{ "schema": 1, "roomId": "…", "anchorId": "…", "unlocked": ["B","U"], "calibrated": false }
```

Keyed to the **MRUK anchor**, never a world position — a world position survives the app but not the room. If the room cannot be re-resolved, the plinth state is used silently.

## 5. Failure modes and what the user sees

| Failure | Visible response |
|---|---|
| No hands tracked | Dim outlines, warm to solid when found |
| Confidence below floor | Match display dims and **pauses** |
| No surface found | Plinth in the same position, no message |
| A pose file fails to load | Logged, skipped; the shape simply isn't offered |
| Eye tracking unavailable | Larger head-gaze dwell targets, one explanatory line |
| App backgrounded | Freezes in place |

**Nothing in this table produces an error message.** That is the point.
