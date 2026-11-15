# P0.3 + P0.4 execution kit

**Purpose:** everything decided in advance so tonight is clicking, not deciding. Fill the blank fields as you go; this file is the evidence for G0.

---

## 1. P0.3 — the app and the channel

### Naming (decide once, it is permanent)

| Field | Value | Rule |
|---|---|---|
| **Package name** | `com.<yourorg>.signspace` | Reverse-DNS, lowercase, no spaces, no underscores. **Cannot be changed later.** Write the real one here: `______________________` |
| App display name | `SIGNSPACE` | |
| Release channel 1 | **`Competition`** | Case-sensitive, exactly this, or the entry is invalid |
| Release channel 2 | `Internal` | Day-to-day test builds; keeps `Competition` clean |
| Category | Productivity (or Education if forced) | |
| Age rating | 10+ | Content is non-violent, non-social, no UGC |

### Steps

1. Meta developer dashboard → **Create app** → platform **Quest / Horizon OS**.
2. Set the package name **exactly** as recorded above.
3. Create release channel **`Competition`**; then create **`Internal`**.
4. Upload any throwaway APK to `Competition` to prove the path works, then leave it.
5. Record app ID + both channel names in `accounts.md` §5.
6. Screenshot the channel list → `evidence/p0.3-competition-channel-<date>.png`.

**Done when:** `Competition` exists and has accepted an upload, and the package name is written down in two places (here and `accounts.md`).

---

## 2. P0.4 — the Unity build

### Versions — resolved from the competition page, 14 Nov 2026

| Item | Value | Status |
|---|---|---|
| SDK | **Meta XR All-in-One, v207** (*"Unity: Meta XR SDKs v81+ (v207) All-in-One XR"* — the competition page states it) | ✅ confirmed |
| Unity | **6 LTS (6000.x)**, latest patch that the v207 package supports | ✅ confirmed family / patch `[CONFIRM ON INSTALL]` |
| Android min API | 32 | ✅ |
| Android target API | 34 *(if the SDK complains, move to 35 and record it)* | ⚠️ confirm on install |
| Scripting backend | IL2CPP | ✅ |
| Architecture | ARM64 only | ✅ |
| Graphics API | Vulkan, then GLES3 — or GLES3 only if Vulkan is unstable. Record the choice. | ⚠️ measure |

### Player Settings to set before the first build

| Setting | Value | Why |
|---|---|---|
| Package name | from §1 | Must match the dashboard exactly |
| Minimum API level | Android 12 (API 32) | Meta floor |
| Target API level | Android 14 (API 34) | Store requirement band |
| Scripting backend | IL2CPP | ARM64 + performance |
| Target architectures | ARM64 | Quest is ARM64 only |
| Graphics APIs | Vulkan / GLES3 | Record what you ship |
| Multithreaded rendering | On | Free CPU |
| Color space | Linear | Correct lighting on Quest |
| Active input handling | Both | ISDK needs the new input system |
| **Texture compression** | ASTC | Quest default; anything else is slower |
| **Static batching** | On | Cheap win |
| XR Plug-in Management | Meta XR (OpenXR provider as the SDK requires) | Hand tracking and passthrough live here |

### The five-minute smoke test (this *is* the gate)

1. New 3D (URP) project at `unity/`.
2. Import **Meta XR All-in-One v207**.
3. Add the **Meta XR rig** to an empty scene.
4. Enable **hand tracking**; enable **passthrough** on the rig's camera.
5. Build → APK → install on the **3S**.
6. You should see your real room and both hands.

**If passthrough is black or hands are missing:** the cause is almost always one of four things — OpenXR feature not enabled, hand-tracking permission not granted, the rig's camera not set to passthrough, or the wrong graphics API. Fix in that order.

### Record here

| Item | Value |
|---|---|
| Unity version (exact) | |
| Meta XR SDK version (exact, all packages) | |
| XR provider / OpenXR features enabled | |
| Graphics API shipped | |
| First build: result on 3S | ⬜ pass / ⬜ fail — note |
| fps in the empty scene | |

**Done when:** hands + passthrough on the 3S, every version recorded, and `docs/02-tech/TECH-STACK.md` §1 has no `[VERIFY]` left.

---

## 3. What to send back

Three lines, and I can take P1 and P2 off you immediately:

1. `package name = ____`
2. `Unity = ____ , SDK = ____ , graphics = ____`
3. `hands+passthrough on 3S = yes/no, and the fps you saw`
