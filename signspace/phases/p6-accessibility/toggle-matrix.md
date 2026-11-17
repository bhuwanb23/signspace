# P6.5 — Toggle matrix

**Phase:** P6 · **Task:** [P6.5](tasks/p6.5-toggles.md) · **Status:** built and checked · **Device playthroughs:** NOT RUN

One row per switch. "Reachable" means a 36 mm plate in the accessibility tray that appears within
arm's reach in front of the user at start — **not** a system menu, and **not** a controller gesture.
Every plate answers to a pinch and to a gaze-dwell, so no switch depends on the input it disables.

| # | Block (in-game title) | Default | Code path | Persisted field | Pinch | Dwell | Full loop with it on |
|---|---|---|---|---|---|---|---|
| 1 | **Captions** | on | `A11yCaptionPanel` — matcher events → text line | `captionsOn` | ✅ | ✅ | NOT RUN (headset) |
| 2 | **High contrast** | off | `A11yCaptionPanel.ApplyPalette`, `GazeCaption.ApplyPalette` | `highContrast` | ✅ | ✅ | NOT RUN (headset) |
| 3 | **Relaxed pace** | off (1×) | `A11ySettings.ApplyAll` → `Matcher.holdSeconds` ×1.5, `DwellSelector.dwellSeconds` ×1.5 | `speedScale` | ✅ | ✅ | NOT RUN (headset) |
| 4 | **One hand** | off | `A11ySettings` → `ReferenceHand.oneHanded` (follows the wrist with no pinch) | `oneHanded` | ✅ | ✅ | NOT RUN (headset) |
| 5 | **Eyes only** | off | `A11ySettings` → `EyesOnlyRouter` + hold ×1.35 | `eyesOnly` | ✅ | ✅ | NOT RUN (headset) |
| 6 | **Near-miss naming** | on | `A11ySettings` → `Matcher.NearMissEnabled` | `nearMissEnabled` | ✅ | ✅ | NOT RUN (headset) |
| 7 | **Reduced motion** | off | `ShelfIdle.breathing`, `ReferenceHand.idleDegreesPerSecond`, `LockPayoff.reducedMotion` | `reducedMotion` | ✅ | ✅ | NOT RUN (headset) |
| — | **Calibrate** | — | `Calibration.Begin` / `Skip` | `calibration`, `calibrated` | ✅ | ✅ | NOT RUN (headset) |

## What the desk checks prove

`P6Checks.CheckToggles` (32 assertions) and `P6Checks.CheckGaze`:

- seven switch blocks plus calibrate exist, one block per switch, all with both a `Grabbable` and a `DwellAction`;
- defaults are the safe ones (captions on, everything else off, near-miss naming on);
- every switch flips and flips back, and reads back the value it wrote;
- relaxed pace measurably lengthens both the hold and the dwell;
- the values live in `SaveSystem.Data` (the same object the save file serialises), so they persist;
- `Repair()` reports drift between `calibrated` and a usable record instead of hiding it;
- reset returns to the defaults.

## What only a headset can prove

The right-hand column. "Playable with each switch on" is a body-in-a-headset test, not a wiring test,
and it is written into `docs/00-plan/HUMAN-TASKS.md` with the exact steps and the recording filenames.
