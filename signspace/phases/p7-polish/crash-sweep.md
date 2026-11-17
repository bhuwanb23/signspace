# P7.6 — Crash sweep

**Phase:** P7 · **Status:** ⬜ 20-launch sweep NOT RUN · desk table verified

## The two halves, kept separate

**Desk-verified** means the code path exists, is reachable and has a written answer the check can
read. **NOT RUN** means a person, a headset and twenty launches produced a number. The first is not
evidence for the second, and this document never lets them share a row.

## The hostile conditions, and what the app does

`HostileGuard.Table` is the single source: the runtime logs these answers and `P7.6 Check crash
posture` asserts each row exists, appears once, and names what it disables.

| Condition | What the app does | What it switches off | Verified on the desk |
|---|---|---|---|
| **No surface** | the shelf resolves to the plinth in front of the user; the loop is unchanged | nothing — the plinth is a mode, not an error path | ✅ `ShelfAnchor` outcome reaches `Plinth` with no MRUK data |
| **No eye tracking** | gaze falls back to the head ray, and the HUD says so in one sentence | eye mode only; the head ray stays | ✅ `EyeTrackingProbe` refuses eye mode without an eye anchor (`P6.2`) |
| **Tracking lost** | the matcher pauses, the reference hand holds its pose, no sound plays | judging only; every visual stays live | ✅ `P6.3`/`P3.5`: silence is the documented answer |
| **Room changed** | the shelf re-resolves to its saved anchor, or the plinth when the anchor is gone | nothing; unlocked objects are restored to their slots | ✅ `PersistenceP43Checks` (unchanged by P7) |
| **No camera** | gaze picking is disabled, the HUD is disabled, one log line, no per-frame null | `GazeHighlighter`, `A11yHud` | ✅ `P7.6` disables the camera and asserts `NoCamera` is reported instead of throwing |

## The sweep

| Launch | Script | Crash? | Misbehaviour |
|---|---|---|---|
| 1–20 | cold launch → grab → rotate → lock a shape → open credits → toggle a switch → background/foreground | **NOT RUN** | |
| **Total** | | **NOT RUN** | |

| Extra condition | Count | Crash? | Notes |
|---|---|---|---|
| Mid-session backgrounding | 5 | **NOT RUN** | |
| Device sleep | 2 | **NOT RUN** | |
| Reboot then launch | 1 | **NOT RUN** | |
| No surface (MRUK denied or empty room) | 1 | **NOT RUN** | desk path verified, device path not |
| No eye tracking (Quest 3S) | 1 | **NOT RUN** | this is the shipped default; the row is still a device row |
| Tracking lost entirely (hands out of frame for 60 s) | 1 | **NOT RUN** | |
| Room changed (walk to another room mid-session) | 1 | **NOT RUN** | |

G7 requires **0 crashes across 20 launches and 4 hostile conditions**. Until the table above has
numbers, G7 is not evaluated.

## Desk-verified in this phase

- [x] Every hostile condition has a written answer, asserted non-empty and unique (`P7.6`).
- [x] The guard's `Inspect()` runs with nothing wired and with the camera disabled, and reports a
      state rather than throwing (`P7.6`).
- [x] A failed boot-CSV write logs a warning; a probe never becomes the crash it was measuring
      (`BootProbe`).
- [x] The tray's shader lookup has a null path: a stripped shader cannot take the tray down at `Start`
      (fixed during this phase).
- [x] The near-miss panel is an empty label until a near-miss fires; nothing is dereferenced before
      `Start` wiring.

## Not run

Every **NOT RUN** row. `HUMAN-TASKS` H25–H27.
