# TECH STACK
## Every piece of the build, and why it is that and not something else

**Rule of this document:** if a tool is listed, it has a job. If it doesn't have a job, it isn't in the stack. Anything uncertain is tagged `[VERIFY]` and must be confirmed against the v207 release notes before P0.4 closes.

---

## 1. THE STACK, IN ONE TABLE

| Layer | Choice | Version | Why this one |
|---|---|---|---|
| **Engine** | Unity | **6.3 LTS, 6000.3.28f1** — pinned 15 Nov 2026 and confirmed by `ProjectSettings/ProjectVersion.txt`; clears Meta's 6000.0.66f2 floor | Best-documented gaze + hand path; every Meta sample project ships a Unity version; fastest solo route to a finished APK |
| **XR runtime** | Meta XR All-in-One SDK | **v207** | The competition names v207 APIs explicitly; All-in-One bundles Core + Interaction + MRUK in one import |
| **Interaction** | Meta XR Interaction SDK (ISDK) | v207 | Grab, Poke, Gaze building blocks — hand-roll only what ISDK doesn't cover |
| **Room understanding** | MRUK (MR Utility Kit) | v207 | The only sanctioned surface detection + anchor API; also the named rubric technology |
| **Render pipeline** | URP | Unity 6 bundled | Cheapest path on Quest silicon; built-in VR optimisations |
| **Hands** | ISDK hand tracking (26 joints/hand) | v207 | Native, no third-party sensor, no extra hardware |
| **Gaze** | ISDK Eye Gaze Interaction building block | v207 | Device-agnostic rig; auto-switches Quest ↔ VR Glasses |
| **Audio** | Unity built-in AudioSource, spatial blend 1.0 | — | Enough. No FMOD, no Wwise |
| **Data** | JSON in `StreamingAssets` | — | Hand-editable, git-diffable, no runtime parsing cost worth worrying about |
| **Build** | IL2CPP · ARM64 · min API 32 · target API 34 `[VERIFY against current Meta requirements]` | — | Store and competition requirement |
| **Profiling** | OVR Metrics Tool + Unity Profiler + Immersive Debugger | latest | On-device fps/thermal is the only number that counts |
| **Simulation** | Meta XR Simulator (desktop) + **VR Glasses profile** | latest | Lets us test the 70° FoV and eyes-only paths without the hardware that ships in 2027 |

---

## 2. WHY NOT THE ALTERNATIVES

| Option | Why we rejected it | What would change our mind |
|---|---|---|
| **Unreal 5** | Gaze integration is thinner; the ISDK-equivalent building blocks are less mature; a solo dev pays a learning tax that costs 2–3 of our 11 days | A team member who already ships Unreal VR |
| **WebXR / Immersive Web SDK** | Gaze + MRUK + hand-grab parity isn't there; performance ceiling on a 3S for 26-joint continuous tracking is a gamble | More than 6 weeks |
| **Meta Spatial SDK** (Android native) | Excellent for panels/2D-in-space; wrong tool for real-time skinned hand geometry | A panel-based product, which this is not |
| **Godot / OpenXR generic** | No ISDK, no MRUK, no gaze building block. We'd rebuild the SDK instead of the product | Never, in 11 days |
| **Leap Motion / Ultraleap** | Extra hardware. Violates the whole premise (the headset must be enough) | Never |
| **Cloud CV / an ML recogniser** | Adds latency, network dependency, privacy surface, and a failure mode we can't debug on Day 10. **Deterministic joint comparison is provably sufficient** for 12 static shapes | If we needed 200+ dynamic signs |
| **Controllers (even as fallback)** | The rubric's hands-first test is absolute, and the mechanic collapses with a controller | Never |

---

## 3. WHAT WE ACTUALLY USE FROM ISDK

| Building block | Where it goes | Why not hand-rolled |
|---|---|---|
| **Hand Grab Interaction** | The reference hand (Beat 2) | Grab physics, hand-pose matching on grab, throw — all free |
| **Eye Gaze Interaction** | Finger highlight layer (Beat 3) | Correct device rig switching + a proven dwell implementation |
| **Poke Interaction** | Accessibility toggles only | Cheap, and poke is the most forgiving target for reduced dexterity |
| **Ray Interaction** | **Not used in the product.** Enabled only in a debug build | The product must be gaze-first, not ray-first |
| **Hand visual** | **Custom**, not the stock ISDK hand | The stock hand looks like a demo. Ours is the product's main character |
| **Hand pose recognition** | **Custom** (`MATCH-ALGORITHM.md`) | ISDK's built-in pose detection is binary and not tunable to bands |

---

## 4. THE PERFORMANCE BUDGET

72 fps = **13.9 ms per frame**. Quest 3S has less thermal headroom than Quest 3, so the budget is set against the weaker device.

| Slice | Budget | Notes |
|---|---|---|
| Total frame | **13.9 ms** | 72 Hz |
| CPU | ≤ 8.0 ms | Matching runs on 2 hands × 26 joints — microseconds, not milliseconds |
| GPU | ≤ 6.5 ms | The real constraint |
| Draw calls | ≤ 120 | The shelf is 12 objects; everything else is a handful of quads |
| Triangles (visible) | ≤ 150k | Reference hand ~8k tris, low-poly, no textures on geometry |
| Materials | ≤ 12 unique | Shared, unlit/very cheap lit |
| Realtime lights | **0** | Baked or unlit only |
| Shadows | **Off** | Non-negotiable on 3S |
| MSAA | 4× `[TUNE]` | Measure; may drop to 2× and buy pixels back |
| Fixed Foveated Rendering | **On**, medium | Invisible on a hand at arm's length |
| Application SpaceWarp | **Off** | Conflicts with aggressive hand motion; revisit only if we're desperate |
| Textures | ≤ 512² except one hero 1024² | Audit at P7.4 |

**Measured numbers, not these targets, go in `PERFORMANCE.md` at P7.4.**

---

## 5. REPO / PROJECT LAYOUT

```
signspace/
├── docs/                     ← everything human-readable (this tree)
├── assets/                   ← media; large files excluded from git
├── scripts/                  ← capture helpers, pose converters, build scripts
└── unity/                    ← the Unity project root (created at P0.4)
    └── Assets/
        ├── _Project/
        │   ├── Scenes/           Boot, Main, Shelf
        │   ├── Scripts/
        │   │   ├── Core/         AppState, SessionController, SaveSystem
        │   │   ├── Hands/        HandVisual, HandPose, PoseProvider
        │   │   ├── Matching/     Matcher, Weights, Band, NearMiss
        │   │   ├── Gaze/         GazeHighlighter, DwellFallback
        │   │   ├── Spatial/      ShelfAnchor, MrUkProvider, PlinthFallback
        │   │   └── UI/           Captions, Toggles, TrackingConfidence
        │   ├── Prefabs/          ReferenceHand, ShelfObject, FingerHighlight
        │   ├── Materials/        Shared, unlit-first
        │   ├── Audio/            SFX
        │   └── Data/             Signs/*.json (StreamingAssets)
        └── Plugins/              Meta XR SDK (gitignored where possible)
```

**Branching:** `main` is always buildable. One feature branch per phase (`p2-core-loop`). Tag the submission `v1.0-competition`.

---

## 6. THE FIVE TECHNICAL UNKNOWNS *(resolved in P1, not here)*

| # | Unknown | Resolved by | If it fails |
|---|---|---|---|
| 1 | Does grab-and-rotate with a **free-floating** object feel natural, or does it need a physical pivot? | P1.3 | Add a hold-still-to-rotate mode |
| 2 | How many of the 20 probed handshapes survive on **Quest 3S** in normal room light? | P1.5 | Cut to 6 signs / 1 family |
| 3 | Does MRUK find a horizontal surface reliably on a lap, a bed, a plane tray? | P1.6 | Permanent virtual plinth |
| 4 | Does gaze highlight feel precise enough at **arm's length** on fingers 15 mm apart? | P6.1 | Highlight *regions* (finger groups), not single fingers |
| 5 | Is 72 fps sustainable for 15 minutes **with continuous 26-joint tracking plus shelf objects**? | P7.4 | Drop to 60 fps floor, disclose it |

---

## 7. TOOLING THAT SAVES DAYS

| Tool | Use |
|---|---|
| **Meta XR Simulator** — VR Glasses profile | Test the 70°×66° FoV and the eyes-only path with no hardware |
| **Immersive Debugger** | Read live match scores and band transitions *on device*, not via logcat |
| **OVR Metrics Tool** | The fps/thermal number that goes in the video |
| **Android Logcat** | Hand-tracking init failures, permission denials |
| **`metavr` CLI / MCP** (Meta's AI tooling) | Scaffold snippets and doc lookups; never trusted for API correctness without a device test `[VERIFY the current command set]` |
| **Device: Quest 3S** | The judging device. Build *against the weaker one* |

---

## 8. WHAT WE DELIBERATELY DON'T HAVE

No backend. No login. No analytics SDK. No network permission. No AI model. No third-party asset store dependency that could break the build on Day 10.

**Every one of those is a failure mode removed.** It's also a one-sentence answer in the video: *"It works on a plane, offline, with nothing but a headset."*
