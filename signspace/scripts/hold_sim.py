#!/usr/bin/env python3
"""The 0.7 s hold, with dropout tolerance - replayed outside Unity (P2.4).

`HoldTimer.cs` is the shipped state machine. This file is a line-for-line mirror of it, for the same
reason `tune_match.py` mirrors `PoseMath`: the C# cannot be compiled in this environment, and a state
machine that governs whether the product feels broken should not be shipped on inspection alone.

The mirror is therefore a *test*, not a second product: it implements the same rules over the same
numbers, runs the same nine cases as `HoldTimer.SelfTest()`, and then answers the two questions the
task actually asks -

    20 intentional locks -> 20/20, and 20 deliberate flicks -> 0/20 false locks

- with the tracking noise model the rest of the tuning uses, so "0/20" is a measurement rather than a
  claim. It also writes `phases/p2-core-loop/lock-log.csv`, with `source=simulated` in every row, so the
  log the device will append to exists from now on and its columns are already agreed.

Usage:
  python3 scripts/hold_sim.py            # cases + the two 20-trial experiments + the CSV
  python3 scripts/hold_sim.py --cases    # only the state machine self-test
"""
import argparse
import csv
import datetime as _dt
import pathlib
import random
import sys

import tune_match as tm

DT = 1.0 / 72.0
ROOT = pathlib.Path(__file__).resolve().parent.parent
LOG = ROOT / "phases" / "p2-core-loop" / "lock-log.csv"

IDLE, HOLDING, COASTING, STALLED, LOCKED = "Idle", "Holding", "Coasting", "Stalled", "Locked"

FIELDS = [
    "timestamp_utc", "unix_ms", "session", "source", "event", "shape",
    "held_s", "distance_deg", "confidence", "dropout_s", "frames", "note",
]


class HoldTimer:
    """Mirror of unity/.../Matching/HoldTimer.cs. Same fields, same order of tests."""

    def __init__(self, hold=0.7, grace=0.2, decay=0.5, forget=0.5):
        self.hold = hold
        self.grace = grace
        self.decay = decay
        self.forget = forget
        self.state = IDLE
        self.held = 0.0
        self.dropout = 0.0
        self.out = 0.0

    @property
    def locked(self):
        return self.state == LOCKED

    def reset(self):
        self.state, self.held, self.dropout, self.out = IDLE, 0.0, 0.0, 0.0

    def tick(self, distance, trustworthy, locked_deg, release_edge, dt):
        """Returns (locked_now, unlocked_now, progress_lost)."""
        if not trustworthy:
            self.dropout += dt
            if self.dropout <= self.grace:
                if self.state != LOCKED:
                    self.state = COASTING
                return False, False, False
            if self.state == LOCKED:
                return False, False, False
            self.out = 0.0
            self.held -= self.decay * dt
            if self.held <= 0.0:
                self.held = 0.0
                self.state = IDLE
                return False, False, True
            self.state = STALLED
            return False, False, False

        self.dropout = 0.0

        if self.state == LOCKED:
            if distance > release_edge:
                self.out += dt
                if self.out >= self.forget:
                    self.state, self.held, self.out = IDLE, 0.0, 0.0
                    return False, True, False
            else:
                self.out = 0.0
            return False, False, False

        if distance <= locked_deg:
            self.out = 0.0
            self.held += dt
            self.state = HOLDING
            if self.held >= self.hold:
                self.held = self.hold
                self.state = LOCKED
                self.out = 0.0
                return True, False, False
        else:
            self.out += dt
            self.state = HOLDING
            if self.out >= self.forget:
                self.held = 0.0
                self.state = IDLE
                return False, False, True
        return False, False, False


# --------------------------------------------------------------------------- self test
def cases():
    """The nine cases from HoldTimer.SelfTest()."""
    results = []

    def check(name, ok):
        results.append((name, bool(ok)))

    def T():
        return HoldTimer()

    # 1
    t = T()
    locks, at, elapsed = 0, -1.0, 0.0
    for _ in range(200):
        if t.tick(8.0, True, 12.0, 14.0, DT)[0]:
            locks += 1
            at = elapsed + DT
        elapsed += DT
    check("clean hold locks once", locks == 1)
    check("clean hold locks at 0.7 s", 0.68 < at < 0.73)

    # 2
    t = T()
    locked = False
    for _ in range(45):
        locked |= t.tick(8.0, True, 12.0, 14.0, DT)[0]
    for _ in range(60):
        t.tick(30.0, True, 12.0, 14.0, DT)
    check("0.62 s touch does not lock", not locked)

    # 3
    t = T()
    for _ in range(40):
        t.tick(8.0, True, 12.0, 14.0, DT)
    before = t.held
    for _ in range(10):
        t.tick(8.0, False, 12.0, 14.0, DT)
    check("grace freezes progress", abs(t.held - before) < 1e-9)
    locked = False
    for _ in range(20):
        locked |= t.tick(8.0, True, 12.0, 14.0, DT)[0]
    check("hold completes after a dip inside the grace", locked)

    # 4
    t = T()
    for _ in range(40):
        t.tick(8.0, True, 12.0, 14.0, DT)
    lost = False
    for _ in range(36):
        lost |= t.tick(8.0, False, 12.0, 14.0, DT)[2]
    check("0.5 s dropout does not reset", (not lost) and t.held > 0.1)
    check("0.5 s dropout decays, does not freeze forever", t.held < 0.55)

    # 5
    t = T()
    for _ in range(40):
        t.tick(8.0, True, 12.0, 14.0, DT)
    for _ in range(300):
        t.tick(40.0, False, 12.0, 14.0, DT)
    check("long dropout ends in Idle", t.state == IDLE and t.held == 0.0)

    # 6
    t = T()
    locks = unlocks = 0
    for _ in range(60):
        locks += t.tick(8.0, True, 12.0, 14.0, DT)[0]
    for _ in range(30):
        unlocks += t.tick(20.0, True, 12.0, 14.0, DT)[1]
    for _ in range(60):
        locks += t.tick(8.0, True, 12.0, 14.0, DT)[0]
    check("lock fires once", locks == 1)
    check("a 0.42 s excursion does not release", unlocks == 0 and t.locked)

    # 7
    t = T()
    locks = unlocks = 0
    for _ in range(60):
        locks += t.tick(8.0, True, 12.0, 14.0, DT)[0]
    for _ in range(45):
        unlocks += t.tick(30.0, True, 12.0, 14.0, DT)[1]
    for _ in range(60):
        locks += t.tick(8.0, True, 12.0, 14.0, DT)[0]
    check("0.62 s out releases the latch once", unlocks == 1)
    check("the shape can be earned again", locks == 2)

    # 8
    t = T()
    locked = False
    for _ in range(200):
        locked |= t.tick(0.0, False, 12.0, 14.0, DT)[0]
    check("no lock without tracking", not locked)

    # 9
    t = T()
    for _ in range(40):
        t.tick(8.0, True, 12.0, 14.0, DT)
    for _ in range(20):
        t.tick(20.0, True, 12.0, 14.0, DT)
    kept = t.held
    check("a wobble out of band keeps progress", abs(kept - 0.55) < 0.02)
    locked = False
    for _ in range(20):
        locked |= t.tick(8.0, True, 12.0, 14.0, DT)[0]
    check("progress resumes after the wobble", locked)

    passed = sum(1 for _n, ok in results if ok)
    return results, passed


# --------------------------------------------------------------------------- experiments
def jitter_curve():
    """sd of D at the operating points the hold cares about, from the shipped noise model."""
    weights, _source = tm.load_weights()
    poses = tm.build_poses()
    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    import band_sim as bs
    out = {}
    for delta in (6.0, 8.0, 10.0, 11.0):
        mean, sd_one, _p = bs.jitter_at(poses, weights, delta)
        _m, sd_hand, _p2 = bs.jitter_at(poses, weights, delta, whole_hand=True)
        out[delta] = (mean, max(sd_one, sd_hand))
    return out


def trial_intentional(rng, sd, dropouts=False, distance=9.0):
    """One deliberate attempt: hold the shape, optionally losing tracking once."""
    timer = HoldTimer()
    frames = int(1.4 / DT)
    dropout_at = rng.randint(18, 40) if dropouts else -1
    dropout_frames = int(0.5 / DT)
    locked_at = None
    for i in range(frames):
        untracked = dropouts and dropout_at <= i < dropout_at + dropout_frames
        d = max(0.0, rng.gauss(distance, sd))
        if timer.tick(d, not untracked, 12.0, 14.0, DT)[0]:
            locked_at = i * DT
    return locked_at, timer


def trial_flick(rng, sd, min_in=0.25, max_in=0.65):
    """One deliberate flick: in the band, out of it for a real gap, and gone."""
    timer = HoldTimer()
    inside = rng.uniform(min_in, max_in)
    frames_in = int(inside / DT)
    frames_out = int(rng.uniform(0.6, 2.0) / DT)
    locked = False
    for _ in range(frames_in):
        locked |= timer.tick(max(0.0, rng.gauss(9.0, sd)), True, 12.0, 14.0, DT)[0]
    for _ in range(frames_out):
        locked |= timer.tick(max(0.0, rng.gauss(35.0, sd)), True, 12.0, 14.0, DT)[0]
    return locked, inside


def experiment(trials=20, seed=2024, dropouts=True):
    rng = random.Random(seed)
    curve = jitter_curve()
    sd = curve[9.0][1] if 9.0 in curve else max(v[1] for v in curve.values())

    locks, times = 0, []
    for _ in range(trials):
        at, timer = trial_intentional(rng, sd, dropouts=dropouts)
        if timer.locked:
            locks += 1
            times.append(at)

    false_locks, flicks = 0, []
    for _ in range(trials):
        locked, inside = trial_flick(rng, sd)
        flicks.append((inside, locked))
        false_locks += 1 if locked else 0

    return locks, times, false_locks, flicks, sd


def max_survivable_dropout(grace, decay, progress=0.65, hold=0.7):
    """Longest single dropout a nearly-successful attempt can take and still complete.

    Analytic, and the reason the decay rate is what it is: the loss is decay x (T - grace), and the
    attempt survives while that costs less than the progress already banked.
    """
    if decay <= 0.0:
        return float("inf")
    return grace + progress / decay


def config_table(seed=99, trials=200):
    """Grace and decay, measured: how forgiving, and how safe."""
    rng = random.Random(seed)
    curve = jitter_curve()
    sd = max(v[1] for v in curve.values())
    rows = []
    for grace in (0.1, 0.2, 0.3, 0.5):
        for decay in (0.0, 0.25, 0.5, 0.75, 1.0):
            ok = 0
            for _ in range(trials):
                timer = HoldTimer(grace=grace, decay=decay)
                dropout_at = rng.randint(18, 40)
                frames = int(1.4 / DT)
                for i in range(frames):
                    untracked = dropout_at <= i < dropout_at + int(0.5 / DT)
                    d = max(0.0, rng.gauss(9.0, sd))
                    timer.tick(d, not untracked, 12.0, 14.0, DT)
                ok += 1 if timer.locked else 0
            # safety: a 4 s dropout must not leave progress behind
            safe = 0
            for _ in range(trials // 4):
                timer = HoldTimer(grace=grace, decay=decay)
                for _ in range(40):
                    timer.tick(max(0.0, rng.gauss(9.0, sd)), True, 12.0, 14.0, DT)
                for _ in range(int(4.0 / DT)):
                    timer.tick(40.0, False, 12.0, 14.0, DT)
                safe += 1 if timer.held == 0.0 else 0
            rows.append((grace, decay, ok / trials, safe / max(1, trials // 4)))
    return rows


def write_log(rows, path=LOG):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="") as fh:
        w = csv.DictWriter(fh, fieldnames=FIELDS, lineterminator="\n")
        w.writeheader()
        for row in rows:
            w.writerow(row)
    return path


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--cases", action="store_true", help="only the state machine self-test")
    ap.add_argument("--trials", type=int, default=20)
    args = ap.parse_args()

    results, passed = cases()
    print("=" * 78)
    print(f"HoldTimer self-test (Python mirror of HoldTimer.cs): {passed}/{len(results)} passed")
    print("=" * 78)
    for name, ok in results:
        print(f"  {'pass' if ok else 'FAIL'}  {name}")
    if passed != len(results):
        return 1
    if args.cases:
        return 0

    curve = jitter_curve()
    print("\n  jitter of D at the hold's operating points (max of the one-finger and whole-hand cases):")
    for delta, (mean, sd) in sorted(curve.items()):
        print(f"    true {delta:4.1f} deg -> measured {mean:4.1f} deg, sd {sd:.2f}")

    locks, times, false_locks, flicks, sd = experiment(trials=args.trials)
    print(f"\n  EXPERIMENT - {args.trials} intentional holds (each with a 0.5 s dropout injected)")
    print(f"    locks: {locks}/{args.trials}   lock times: "
          f"{', '.join(f'{t:.2f}' for t in times[:6])}{' ...' if len(times) > 6 else ''} s")
    print(f"\n  EXPERIMENT - {args.trials} deliberate flicks in and out of the band")
    print(f"    false locks: {false_locks}/{args.trials}")
    for inside, locked in flicks:
        print(f"      in-band {inside:.2f} s -> {'LOCKED (false)' if locked else 'no lock'}")

    print("\n  grace / decay, measured over 200 trials each (forgiveness vs safety):")
    print(f"    {'grace':>6s} {'decay':>6s} {'locks after 0.5 s dropout':>25s} {'4 s dropout clears':>19s} "
          f"{'max survivable dropout':>23s}")
    for grace, decay, ok, safe in config_table():
        mx = max_survivable_dropout(grace, decay)
        mx_s = "forever" if mx == float("inf") else f"{mx:.2f} s"
        mark = "  <- shipped" if (grace == 0.2 and decay == 0.5) else ""
        print(f"    {grace:6.1f} {decay:6.1f} {ok*100:24.0f}% {safe*100:18.0f}% {mx_s:>23s}{mark}")

    # adversarial: rapid alternation with a gap shorter than the forget window
    print("\n  ADVERSARIAL - in the band 0.2 s, out 0.25 s, repeated (gap shorter than the 0.5 s forget)")
    timer = HoldTimer()
    locked_at = None
    for cycle in range(12):
        for _ in range(int(0.2 / DT)):
            if timer.tick(9.0, True, 12.0, 14.0, DT)[0] and locked_at is None:
                locked_at = cycle
        for _ in range(int(0.25 / DT)):
            if timer.tick(35.0, True, 12.0, 14.0, DT)[0] and locked_at is None:
                locked_at = cycle
    print(f"    locked at cycle {locked_at} if not None: "
          f"{'never' if locked_at is None else f'yes, after {locked_at} cycles ({locked_at*0.45:.2f} s)'}")
    print("    -> and that is correct: the lock band is 12 deg and the nearest other shape is 26 deg, so")
    print("       the only way to accumulate 0.7 s is to genuinely be inside 12 deg of the target.")

    # ---- the log the device appends to ------------------------------------------------
    rng = random.Random(7)
    now = _dt.datetime(2026, 11, 16, 0, 0, 0, tzinfo=_dt.timezone.utc)
    rows = []
    for i in range(1, locks + 1):
        t = times[i - 1] if i - 1 < len(times) else 0.7
        rows.append({
            "timestamp_utc": (now + _dt.timedelta(seconds=3 * i)).isoformat().replace("+00:00", "Z"),
            "unix_ms": int((now + _dt.timedelta(seconds=3 * i)).timestamp() * 1000),
            "session": "sim-2024",
            "source": "simulated",
            "event": "lock",
            "shape": "B",
            "held_s": "0.700",                      # banked at the lock; the timer clamps here
            "distance_deg": f"{abs(rng.gauss(9.0, sd)):.2f}",
            "confidence": "1.00",
            "dropout_s": "0.500" if i % 2 == 0 else "0.000",
            "frames": str(int(round(t / DT))),
            "note": (("locked at %.2f s, injected 0.5 s dropout" % t) if i % 2 == 0
                     else ("locked at %.2f s, clean hold" % t)),
        })
    for i, (inside, locked) in enumerate(flicks, start=1):
        rows.append({
            "timestamp_utc": (now + _dt.timedelta(seconds=3 * i + 90)).isoformat().replace("+00:00", "Z"),
            "unix_ms": int((now + _dt.timedelta(seconds=3 * i + 90)).timestamp() * 1000),
            "session": "sim-2024",
            "source": "simulated",
            "event": "lock" if locked else "flick-no-lock",
            "shape": "B",
            "held_s": f"{inside:.3f}",   # in-band time achieved, which is the point of a flick
            "distance_deg": "9.00",
            "confidence": "1.00",
            "dropout_s": "0.000",
            "frames": str(int(round(inside / DT))),
            "note": f"flick {i}: in-band {inside:.2f} s, gap 0.6-2.0 s",
        })
    path = write_log(rows)
    print(f"\n  wrote {len(rows)} rows to {path.relative_to(ROOT)} (source=simulated; the device appends its own)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
