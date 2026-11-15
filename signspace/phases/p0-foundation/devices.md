# Device matrix and test rig
**Owner:** W5 · **Task:** P0.7 · **Phase:** P0 → gate G0 · **Written:** 15 Nov 2026
**Related:** [`toolchain.md`](toolchain.md) · [`OWNER-TASKS.md`](OWNER-TASKS.md) §D

> "Judges test on Quest 3 **and** 3S. We build against the weaker device so the strong one is free.
> Without a named device list, nobody knows what 'it works' means." — P0.7

---

## 1. Headsets

| # | Device | Owner | Where it is | Available from | Primary or verification |
|---|---|---|---|---|---|
| 1 | **Quest 3** | the owner | in transit | ⬜ **day to confirm** | ⬜ primary / verification |
| 2 | **Quest 3S** | ⬜ | ⬜ | ⬜ | ⬜ |

**Status (15 Nov 2026): a headset is on its way — the exact day is not yet recorded.** One Quest 3 was
the expected route (friend's loan); a Quest 3S is still not secured. The matrix cannot be *signed* until
the model and the arrival day are written in. See [`OWNER-TASKS.md`](OWNER-TASKS.md) §D.

> **The dependency, stated plainly.** Four things cannot be done without a headset on the table:
> G0's *"installs from the `Competition` channel"* criterion · the **P1.5 twenty-shape
> trackability probe** · the **72 fps measurement** on real silicon · the **P8 playtest**.
> Everything else — building, the shapes, the code, the FoV and gaze behaviour — is verifiable on
> this PC through Meta XR Simulator, and the competition explicitly accepts **XR Simulator footage**
> in the video.
>
> **Needed by: Day 2–3 (Mon 16 – Tue 17 Nov).** Booking note: ask for Tue 17 **and** Wed 18 — Day 3 needs the device for the signer review call and the playtest, Day 4 needs it for the video. Original hard date: After that the P1.5 probe and the fps number have no
> home.

### The gaps this leaves, and what we do about them

| Gap | Consequence | Mitigation |
|---|---|---|
| No Quest 3S | **R10 (perf collapse on the weaker device) cannot be measured.** The 72 fps floor is a judging criterion on both devices. | Hold to 3S-conservative settings: fixed foveated rendering on, MSAA 4×, no realtime shadows, no realtime lights, ≤120 draw calls, ≤150k triangles (`TECH-STACK.md` §4). **Disclose in `PERFORMANCE.md` and in the description that the 3S was not physically tested.** That is honest and defensible; quietly assuming it is not. |
| No headset at all | G0 cannot go green | `OWNER-TASKS.md` §D, with a hard deadline |
| No Quest Pro / VR Glasses | **Eye tracking cannot be demonstrated on judge hardware** — neither the Quest 3 nor the 3S has it | Gaze is evidenced through the **Meta XR Simulator VR Glasses profile**, which simulates eye gaze and hand tracking by default, and through the **FOV Simulation for Meta VR Glasses** added to `OVRManager` and the Immersive Debugger in Meta XR Core SDK v207. The shipped device path is an honest dwell / head-gaze fallback, and `GAZE-INPUT.md` says so. |

## 2. The development machine

This is the build rig, and it is not the test rig.

| Item | Value | Note |
|---|---|---|
| OS | Windows 11 Pro, build 10.0.26200 | |
| CPU | Intel Core Ultra 7 265K, 20 cores | |
| GPU | NVIDIA GeForce RTX 5060 | Meta XR Simulator 207.1 added AMD GPU support in 207.1; NVIDIA is the primary path |
| RAM | 31.5 GB | ample for IL2CPP |
| Free disk | C: 27.9 GB · **D: 138 GB** · **E: 153 GB** | Unity lives on E:; the Unity project's `Library/` lives on D: inside the repo and is gitignored |
| Unity | 6000.3.28f1 at `E:\unity\Editors\6000.3.28f1` | 6.3 LTS |
| Unity CLI | `C:\Users\Bhuwan\AppData\Local\Unity\bin\unity.exe` | headless project creation, builds, licence |
| Android SDK | `C:\Users\Bhuwan\AppData\Local\Android\Sdk` | platforms 34/35/36 · NDK 27.0/27.1 |
| JDK | Temurin 17.0.18 | |
| adb | `…\Android\Sdk\platform-tools\adb.exe` | for install, logcat and fps capture |
| Meta XR Simulator | 207.1 | Quest 3 / 3S / VR Glasses profiles |
| Meta VR CLI | `metavr` (npm global) | device, log, perf, capture, store, xrsim |

## 3. Room and lighting

The environment that represents a judge's, not a lab.

| Condition | Where | Note |
|---|---|---|
| **Primary** | ⬜ living room, ⬜ time of day | The condition the shapes are captured and tested in |
| Bright | ⬜ | daylight through a window |
| Dim | ⬜ | one lamp, no daylight — this is where tracking degrades |
| **Surface for the shelf** | ⬜ a table · ⬜ a lap · ⬜ a bed · ⬜ a tray | MRUK must find a horizontal surface in at least the first two; the plinth fallback covers the rest |

> The lighting rows are not decoration. `P1.5` has to probe the shapes in **both** daylight and
> lamplight, because "which shapes survive in which lighting" is the whole content of the honest
> mirror panel (`TRACKABILITY.md`).

## 4. Test rig checklist

| # | Item | Status |
|---|---|---|
| 1 | Headset named, owner named, location named | 🟡 model known (Quest 3, loan), **arrival day ⬜** |
| 2 | **Primary build device** declared | ⬜ — **the 3S if one exists, otherwise the 3 and the gap is documented above** |
| 3 | **Verification device** declared | ⬜ |
| 4 | Room and lighting conditions written down | ⬜ |
| 5 | Eye-tracking availability known | ✅ **known: neither the Quest 3 nor the 3S has eye tracking.** Simulator + VR Glasses profile only |
| 6 | Quest headset developer mode enabled, adb authorised | ⬜ |
| 7 | A second device or a phone on mobile data, for the invite-URL test | ⬜ |
| 8 | The owner can receive the Devpost mail at the submitting address | ⬜ |

## 5. Change log

| Date | Change | By |
|---|---|---|
| 15 Nov 2026 | Headset status updated: **in transit**, day to confirm. |
| 15 Nov 2026 | File created. Development machine recorded in full. Headset inventory opened — **none in hand**. The three gaps (no 3S, no eye tracking, no headset today) and their mitigations are written down rather than left implicit. | agent |
