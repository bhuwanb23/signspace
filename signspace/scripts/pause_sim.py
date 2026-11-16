#!/usr/bin/env python3
"""Does the app stop measuring when it stops trusting itself? (P3.5)

ADR-003 says *pause rather than penalise*: while tracking confidence sits below the matcher's floor,
the hold timer must not advance, must not lock, and the display must not move. The rule lives in
`HoldTimer`/`Matcher` in C#, which cannot be compiled in this environment - so this file replays the
same state machine (`scripts/hold_sim.py`, a line-for-line mirror) over the three degradations the task
names, with a hand parked *inside* the lock band the whole time. That is the adversarial case: the
distance is perfect, so the only thing standing between the hand and a lock is whether the timer is
allowed to count.

Four questions, answered with numbers:

  1. Occluded, fast motion, low light: is a lock ever awarded on a frame below the floor?   0, three times
  2. The same scenarios with the floor removed: is one?                                    yes - the control.
     Without this, question 1 proves nothing: a timer that never locks proves nothing about the floor.
  3. Does the hold resume once the hand is readable again?                                 yes, or the
     "pause" is really a penalty.
  4. Does the visible state strobe at the floor, and does `TrackingConfidence.exitSeconds` stop it?

Usage:  python3 scripts/pause_sim.py      # exit 1 if any answer is wrong
"""
import pathlib
import random
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from hold_sim import HoldTimer, LOCKED  # noqa: E402

FLOOR = 0.70          # Matcher.confidenceFloor
LOCKED_DEG = 12.0     # Matcher.lockedDeg
RELEASE_EDGE = 14.0   # lockedDeg + bandReleaseDeg
HOLD = 0.7            # Matcher.holdSeconds
DT = 1.0 / 72.0       # Quest 3S frame time, the rate hold_sim uses
INSIDE = 8.0          # degrees - comfortably inside the lock band

#: (spans of (start, end, confidence), total duration). Confidence is 1.0 outside the spans.
#: Each scenario starts with 0.4 s of good tracking, so the hold is already half-full when the tracker
#: goes blind. That is what makes the control bite: 0.3 s of blind time is then enough for a timer that
#: is not frozen to reach the 0.7 s lock, and a paused one to earn nothing.
SCENARIOS = {
    # one hand passes behind the other, for half a second, while the shape is already held
    "occluded": ([(0.0, 0.40, 1.00), (0.40, 0.90, 0.18)], 2.0),
    # fast motion: joint detail comes and goes in patches - the case a single grace window cannot cover
    "fast motion": ([(0.0, 0.40, 1.00), (0.40, 0.50, 0.45), (0.50, 0.62, 1.00),
                     (0.62, 0.72, 0.40), (0.72, 0.84, 1.00), (0.84, 0.94, 0.48)], 2.5),
    # low light: sustained and flat - the state the app must sit in without looking broken
    "low light": ([(0.0, 0.40, 1.00), (0.40, 2.20, 0.55)], 3.4),
}


def expand(spans, duration):
    """(start, end, conf) spans -> per-frame (dt, confidence, distance)."""
    frames, n = [], int(round(duration / DT))
    for i in range(n):
        t = i * DT
        conf = 1.0
        for a, b, c in spans:
            if a <= t < b:
                conf = c
                break
        frames.append((DT, conf, INSIDE))
    return frames


def replay(frames, floor):
    """Tick the real state machine. Returns (locks, blind_locks, held_at_end, resumed).

    `blind` is always measured against the SHIPPED floor (0.70), whatever floor is passed in - that is
    the whole point of the control: it asks "would a lock have been awarded on a frame the real product
    considers blind?"
    """
    timer, locks, blind, resumed = HoldTimer(hold=HOLD), 0, 0, False
    for dt, conf, dist in frames:
        trustworthy = conf >= floor
        locked_now, _, _ = timer.tick(dist, trustworthy, LOCKED_DEG, RELEASE_EDGE, dt)
        if locked_now:
            locks += 1
            if conf < FLOOR:
                blind += 1
            else:
                resumed = True
    return locks, blind, timer.held, resumed or timer.state == LOCKED


def transitions(frames, exit_seconds, margin):
    """Mirror of TrackingConfidence's enter-fast / leave-slow rule; counts visible state changes."""
    watching, changes, timer = False, 0, 0.0
    for dt, conf, _ in frames:
        if conf < FLOOR:
            timer, now = 0.0, True
        elif watching:
            timer = max(0.0, timer + (dt if conf >= FLOOR + margin else -dt))
            now = not timer >= exit_seconds
        else:
            now = False
        if now != watching:
            changes, watching = changes + 1, now
    return changes


def main():
    ok = True
    print("=" * 78)
    print(f"P3.5 pause behaviour - floor {FLOOR}, hold {HOLD}s, {1/DT:.0f} Hz, hand parked at {INSIDE} deg")
    print("=" * 78)

    print("\n1-3. the three degradations: blind locks, control, and recovery\n")
    print(f"{'scenario':<14}{'locks':>7}{'blind (floor)':>15}{'blind (no floor)':>18}{'resumed':>9}")
    for name, (spans, duration) in SCENARIOS.items():
        frames = expand(spans, duration)
        locks, blind, held, resumed = replay(frames, FLOOR)
        _, blinded, _, _ = replay(frames, 0.0)
        print(f"{name:<14}{locks:>7}{blind:>15}{blinded:>18}{str(resumed):>9}")
        if blind != 0:
            print(f"    FAIL: {blind} lock(s) awarded on a frame below the floor")
            ok = False
        if blinded == 0:
            print("    FAIL: the control awards nothing either - the floor is not doing the work")
            ok = False
        if not resumed:
            print("    FAIL: the hold never resumed - that is a penalty, not a pause")
            ok = False

    print("\n   control, in plain words: with the floor removed the same hands lock at 0.7 s")
    print("   while the tracker is blind - the app would award a lock it never earned.")

    print("\n4. hysteresis at the floor, and why the readout has an exit window\n")
    rng, wobble, conf = random.Random(7), [], FLOOR
    for _ in range(int(4.0 / DT)):
        conf = max(FLOOR - 0.06, min(FLOOR + 0.06, conf + rng.uniform(-0.02, 0.02)))
        wobble.append((DT, conf, INSIDE))
    raw = transitions(wobble, exit_seconds=0.0, margin=0.0)
    windowed = transitions(wobble, exit_seconds=0.25, margin=0.05)
    print(f"   visible state changes over 4 s at the floor, no window:     {raw}")
    print(f"   visible state changes over 4 s at the floor, 0.25 s window: {windowed}")
    if windowed >= raw:
        print("    FAIL: the exit window does not reduce strobing")
        ok = False

    print("\n" + "=" * 78)
    print("  PASS - pauses in all three, never locks blind, always resumes, does not strobe"
          if ok else "  FAIL - the pause behaviour does not hold up")
    print("=" * 78)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
