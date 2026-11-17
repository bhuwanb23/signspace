# P7.5 — Cold boot and background cycles

**Phase:** P7 · **Instrument:** `BootProbe` (`Scripts/UI/BootProbe.cs`) · **Status:** ⬜ device rows NOT RUN

## What the probe measures

| Column | From → to | Why this boundary |
|---|---|---|
| `startup_ms` | process start → the first frame `BootProbe` sees | as close to "cold boot" as a scene can observe; splash and engine init happen before it |
| `hands_ms` | → the first frame the matcher reports a tracked hand | the hand is the product; until it exists there is nothing to do |
| `interactive_ms` | → the first frame with **both** a hand and a shelf | the task's "playable before the shelf resolves" line. A plinth counts: the loop runs on it |
| `resumes` / `resume_ms` | each `OnApplicationPause(false)` → first frame after | the five background/foreground cycles |

Each launch appends one row to `Application.persistentDataPath/boot-times.csv`:

```
utc,startup_ms,hands_ms,interactive_ms,resumes,resume_ms
```

## Why the file is on the device, not in the repository

A build that writes its own measurements into `StreamingAssets` is a build that measures a previous
build. The CSV is written at runtime and pulled with `adb pull /sdcard/Android/data/<package>/files/`.
The table below is filled from that file, not from a stopwatch.

## The five launches

| Launch | startup_ms | hands_ms | interactive_ms | Notes |
|---|---|---|---|---|
| 1 | NOT RUN | NOT RUN | NOT RUN | cold, after a reboot |
| 2 | NOT RUN | NOT RUN | NOT RUN | warm |
| 3 | NOT RUN | NOT RUN | NOT RUN | warm |
| 4 | NOT RUN | NOT RUN | NOT RUN | warm |
| 5 | NOT RUN | NOT RUN | NOT RUN | warm |
| **Median** | **NOT RUN** | | | target: **interactive_ms < 15000** |

## Background cycles

| Cycle | Resume | Shelf restored | Calibration restored | Notes |
|---|---|---|---|---|
| 1 | NOT RUN | NOT RUN | NOT RUN | |
| 2 | NOT RUN | NOT RUN | NOT RUN | |
| 3 | NOT RUN | NOT RUN | NOT RUN | |
| 4 | NOT RUN | NOT RUN | NOT RUN | |
| 5 | NOT RUN | NOT RUN | NOT RUN | |

## Desk-verified, in this phase

- [x] The probe exists, sees the matcher and the shelf, and has a 15 s budget (`P7.5 Check boot readiness`).
- [x] The splash is **static**: one frame, no dolly, no zoom. Unity's logo cannot be removed on this
      licence; it can be stopped from animating, and `SplashSettings` does that by reflection.
- [x] The loop does not wait for a surface: `ShelfAnchor` resolves the plinth as a mode, so
      `interactive_ms` is reachable with no MRUK data at all.
- [x] The save file survives a relaunch and restores the shelf, the unlocked shapes and the calibration
      (`PersistenceP43Checks`, unchanged by this phase).
- [x] A failed CSV write logs a warning instead of throwing: the probe must not be the thing that
      crashes the boot it is measuring.

## Not run

Everything in the two tables above. `HUMAN-TASKS` H24. A desktop run cannot produce a cold boot,
because there is no cold boot on a desktop.
