#!/usr/bin/env python3
"""Band threshold and hysteresis simulation (P2.3).

Two things in this task have to be measured rather than chosen: the band edges, and the claim that the
display does not strobe. Both are decidable offline, because both depend only on how far a distance
wobbles frame to frame - and that wobble is already measured (P2.2, `tune_match.py --v3-only`).

Method
------
1. Put an ideal hand at a known true error `delta`: rotate every joint of one finger (the one that
   dominates D) by `delta` about its own local axis. Because D is a weighted mean *within* a finger,
   an even error across one finger's joints reads back as `D ~= delta`, which is what makes the band
   edges mean something a human can act on ("every joint of my worst finger is about 12 deg off").
2. Add real tracking noise on top - the same 2 deg/joint model as the matcher's tuning - and measure
   the distance again. The spread of that measurement at each `delta` is the jitter that decides
   whether a threshold flickers.
3. Run the matcher's actual band state machine at 72 Hz over 30 s and count how many times the
   *displayed* band changes.

Pass conditions, stated before the numbers were looked at:
  * **flicker**          a hand parked on an edge changes band < 1 time per 30 s
  * **false close**      a hand making the neighbouring sign is told "close" < 1% of frames
  * **hold survival**    a hand inside the lock band completes the 0.7 s hold > 95% of the time
  * **approach**         a monotone approach changes band at most twice, never backwards

Usage: python3 scripts/band_sim.py
"""
import random
import sys

import tune_match as tm

DT = 1.0 / 72.0
SECONDS = 30
TRIALS = 300
HOLD_SECONDS = 0.7          # Matcher.holdSeconds
HYSTERESIS_SECONDS = 0.15   # Matcher.bandHysteresisSeconds

#: The joint the deliberate error is applied to: the little finger is the weakest and the thumb the
#: most forgiving, so use the index, which sits in the middle of both the weight table and the eye.
ERROR_FINGER = "index"


def _axis():
    return (1.0, 0.0, 0.0)


def offset_pose(target, finger, delta_deg):
    """The target with every joint of `finger` rotated `delta_deg` out of place."""
    out = dict(target)
    for name in tm.hm.FINGERS[finger]:
        out[name] = tm.hm.q_mul(target[name], tm.hm.q_from_axis_angle(_axis(), delta_deg))
    return out


def noisy(current, rng, sigma_deg=2.0):
    """Tracking noise on top of a pose: the shipped 2 deg/joint model."""
    out = {}
    for name, q in current.items():
        if name in ("Wrist", "Palm"):
            out[name] = q
            continue
        axis = (rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))
        if axis == (0.0, 0.0, 0.0):
            axis = (1.0, 0.0, 0.0)
        out[name] = tm.hm.q_mul(q, tm.hm.q_from_axis_angle(axis, rng.gauss(0.0, sigma_deg)))
    return out


def jitter_at(poses, weights, delta_deg, whole_hand=False, trials=400, seed=11):
    """(mean, sd, p95 |deviation|) of the measured D for a hand truly `delta_deg` off.

    Two variants, because they bracket the truth:
      * `whole_hand=False` - the error sits on one finger, so the max is pinned to that finger and
        the jitter is close to zero. This is the *optimistic* case.
      * `whole_hand=True` - every finger is `delta_deg` off, so which finger is the max wanders from
        frame to frame. This is the *pessimistic* case, and the one a threshold must survive.
    """
    rng = random.Random(seed)
    shape_ids = tm.SHAPE_IDS
    fingers = (list(tm.hm.FINGERS.keys()) if whole_hand else [ERROR_FINGER])
    samples = []
    for i in range(trials):
        sid = shape_ids[i % len(shape_ids)]
        true_pose, target = dict(poses[sid]), dict(poses[sid])
        for finger in fingers:
            true_pose = offset_pose(true_pose, finger, delta_deg / 2.0)
            target = offset_pose(target, finger, -delta_deg / 2.0)
        live = noisy(true_pose, rng)
        d, _f, _flat, _w = tm.metric_v3_weighted(target, live, weights)
        samples.append(d)
    n = len(samples)
    mean = sum(samples) / n
    var = sum((s - mean) ** 2 for s in samples) / n
    dev = sorted(abs(s - mean) for s in samples)
    return mean, var ** 0.5, dev[int(0.95 * n)]


def band_of(d, locked, close, release, current):
    """Matcher's band test with a degree-level release offset.

    `release` widens the band you are already in, so leaving costs more than entering. Entry always
    uses the plain edge; only the exit is widened, which is what stops a hand sitting exactly on an
    edge from strobing between two states.
    """
    lock_edge = locked + (release if current == 2 else 0.0)
    close_edge = close + (release if current >= 1 else 0.0)
    if d <= lock_edge:
        return 2
    if d <= close_edge:
        return 1
    return 0


def run(delta, sd, locked, close, release, seconds=SECONDS, seed=1, timed=True):
    """One 30 s hold. Returns (# displayed band changes, frames per band, final band)."""
    rng = random.Random(seed)
    frames = int(seconds / DT)
    shown = band_of(delta, locked, close, release, -1)
    pending, timer = shown, 0.0
    changes, counts = 0, [0, 0, 0]
    for _ in range(frames):
        d = max(0.0, rng.gauss(delta, sd))
        wanted = band_of(d, locked, close, release, shown)
        if wanted != shown:
            if timed:
                if wanted != pending:
                    pending, timer = wanted, 0.0
                timer += DT
                if timer >= HYSTERESIS_SECONDS:
                    shown = wanted
                    changes += 1
                    timer = 0.0
            else:
                shown = wanted
                changes += 1
        else:
            pending, timer = shown, 0.0
        counts[shown] += 1
    return changes, counts, shown


def hold_survival(delta, sd, locked, close, release, trials=200, seed=3, seconds=2.0):
    """Fraction of holds that complete within `seconds`, with a hand parked at `delta`.

    CORRECTION (P2.4): the first version of this function gave the simulated hand exactly
    HOLD_SECONDS of wall time to accumulate, which quietly required *every* frame to be inside the
    band - a run, not a timer - and produced the false finding that the effective lock edge was ~10 deg
    instead of 12. The matcher accumulates cumulatively, so the honest question is "does the hand bank
    0.7 s of in-band time within a couple of seconds", and the window has to be longer than the hold.
    """
    rng = random.Random(seed)
    frames = int(seconds / DT)
    survived = 0
    for _ in range(trials):
        shown, hold, out, ok = 0, 0.0, 0.0, False
        for _ in range(frames):
            d = max(0.0, rng.gauss(delta, sd))
            if band_of(d, locked, close, release, shown) == 2:
                hold += DT
                out = 0.0
                if hold >= HOLD_SECONDS:
                    ok = True
                    break
            else:
                out += DT
                if out >= 0.5:          # Matcher.resetAfterSeconds: a sustained departure forfeits progress
                    hold = 0.0
        survived += 1 if ok else 0
    return survived / trials


def _inside_fraction(centre, sd, edge, trials=100000, seed=77):
    """Share of single frames below `edge` for a distance distributed N(centre, sd) - no dwell timer."""
    rng = random.Random(seed)
    inside = sum(1 for _ in range(trials) if rng.gauss(centre, sd) <= edge)
    return inside / trials


def _false_close(nearest, sd, close, release, trials=200, seed=900):
    """Share of frames a neighbouring-sign hand is told it is 'close'."""
    total = close_frames = 0
    for t in range(trials):
        _c, counts, _s = run(nearest, sd, 12.0, close, release, seed=seed + t)
        total += sum(counts)
        close_frames += counts[1] + counts[2]
    return close_frames / total


def approach(noise_sd, locked, close, release, seed=5, start=40.0, end=10.0, seconds=3.0):
    """A smooth approach from `start` to `end` degrees: (# changes, # backwards steps)."""
    rng = random.Random(seed)
    frames = int(seconds / DT)
    shown = band_of(start, locked, close, release, -1)
    pending, timer, changes, backwards, last = shown, 0.0, 0, 0, shown
    for i in range(frames):
        base = start + (end - start) * (i / frames)
        d = max(0.0, rng.gauss(base, noise_sd))
        wanted = band_of(d, locked, close, release, shown)
        if wanted != shown:
            if wanted != pending:
                pending, timer = wanted, 0.0
            timer += DT
            if timer >= HYSTERESIS_SECONDS:
                shown, timer = wanted, 0.0
                changes += 1
                if shown < last:
                    backwards += 1
                last = shown
        else:
            pending, timer = shown, 0.0
    return changes, backwards


def main():
    weights, _source = tm.load_weights()
    poses = tm.build_poses()
    nearest = tm.metric_v3_weighted(poses["U"], poses["V"], weights)[0]

    print("=" * 78)
    print("P2.3 band + hysteresis simulation (v3 metric, 2 deg/joint, 72 Hz, 30 s holds)")
    print("=" * 78)
    print(f"nearest accidental pair: U-V at {nearest:.1f} deg")

    # ---- 0. calibration: what does a degree of D mean? -------------------------------
    print("\n0. CALIBRATION - measured D for a hand whose worst finger is `true error` degrees off")
    print(f"   {'true error':>10s} {'measured D':>11s} {'sd (1 finger)':>14s} {'sd (whole hand)':>16s} {'p95 (worst)':>12s}")
    calib = {}
    for delta in (0.0, 4.0, 8.0, 12.0, 16.0, 20.0, 24.0, 26.0, 30.0):
        mean, sd_one, _p = jitter_at(poses, weights, delta)
        _m2, sd_hand, p95_hand = jitter_at(poses, weights, delta, whole_hand=True)
        calib[delta] = (mean, max(sd_one, sd_hand), p95_hand)
        print(f"   {delta:10.1f} {mean:11.1f} {sd_one:14.2f} {sd_hand:16.2f} {p95_hand:12.2f}")
    print("   -> D reads back the per-joint error of the worst finger, so an edge in D is an edge in")
    print("      something the learner can feel. The whole-hand column is the jitter a threshold has")
    print("      to survive: with one finger pinned the max is stable, with all five close together")
    print("      it wanders between them.")

    sd_at_lock = calib[12.0][1]
    sd_at_close = calib[20.0][1]
    sd_at_neighbour = calib[26.0][1]

    # ---- 1. the upper edge: can the neighbouring sign occupy 'close'? ----------------
    print(f"\n1. FALSE CLOSE - a hand at D = {nearest:.1f} deg (the neighbouring sign), 30 s, 300 trials")
    print("   requirement: told 'close' on < 1% of frames. Analytic bound: close <= D - 2.33*sd")
    bound = nearest - 2.33 * sd_at_neighbour
    print(f"   sd at that distance {sd_at_neighbour:.2f} deg -> close edge must be <= {bound:.1f} deg")
    print(f"   {'close edge':>10s} {'release':>8s} {'in-edge frames':>15s} {'displays Close':>15s} {'verdict':>8s}")
    for close in (24.0, 23.0, 22.0, 20.0, 18.0):
        for release in (0.0, 2.0):
            total = close_frames = inedge = 0
            for t in range(TRIALS):
                _c, counts, _s = run(nearest, sd_at_neighbour, 12.0, close, release, seed=100 + t)
                total += sum(counts)
                close_frames += counts[1] + counts[2]
            # how often a single frame is inside the edge at all, ignoring the dwell timer
            inedge = _inside_fraction(nearest, sd_at_neighbour, close)
            frac = close_frames / total
            print(f"   {close:10.1f} {release:8.1f} {inedge * 100:14.2f}% {frac * 100:14.1f}% "
                  f"{'LIES' if frac > 0.01 else 'ok':>8s}")
    print("   -> the dwell timer is doing most of the work: single frames drift inside the edge, but")
    print("      eleven consecutive frames almost never do. Both columns matter; only the right one")
    print("      is what a learner ever sees.")

    # ---- 2. flicker at the lock edge -------------------------------------------------
    print("\n2. FLICKER - hand parked on the lock edge (D = 12.0 deg) for 30 s, 300 trials")
    print(f"   {'hysteresis':>26s} {'changes / 30 s':>15s} {'worst':>7s} {'verdict':>9s}")
    for label, release, timed in (
        ("none (band = f(D))", 0.0, False),
        ("150 ms time only", 0.0, True),
        ("150 ms + 1 deg release", 1.0, True),
        ("150 ms + 2 deg release", 2.0, True),
        ("150 ms + 3 deg release", 3.0, True),
    ):
        total, worst = 0, 0
        for t in range(TRIALS):
            c, _counts, _s = run(12.0, sd_at_lock, 12.0, 20.0, release, seed=200 + t, timed=timed)
            total += c
            worst = max(worst, c)
        mean = total / TRIALS
        print(f"   {label:>26s} {mean:15.2f} {worst:7d} "
              f"{'STROBES' if mean > 1.0 else 'ok':>9s}")

    # ---- 3. hold survival ------------------------------------------------------------
    print("\n3. HOLD SURVIVAL - does a parked hand bank 0.7 s of in-band time within 2 s? (200 trials)")
    print(f"   {'parked at':>10s} {'150 ms only':>13s} {'+1 deg':>8s} {'+2 deg':>8s}")
    for delta in (4.0, 8.0, 10.0, 11.0, 12.0, 12.5, 13.0, 14.0):
        sd = calib[delta][1] if delta in calib else \
            max(jitter_at(poses, weights, delta)[1], jitter_at(poses, weights, delta, whole_hand=True)[1])
        row = [hold_survival(delta, sd, 12.0, 20.0, r) for r in (0.0, 1.0, 2.0)]
        print(f"   {delta:10.1f} {row[0]*100:12.0f}% {row[1]*100:7.0f}% {row[2]*100:7.0f}%")

    # ---- 3b. sensitivity: how wrong would the edges be if the device is noisier? -------
    print("\n3b. SENSITIVITY - the upper edge is a sigma question, so state it in sigmas")
    print("   rule: an edge must sit >= 2.33 sd from the neighbouring sign (1% one-tailed)")
    print(f"   {'noise x':>8s} {'sd @26 deg':>11s} {'margin, close 24':>17s} {'margin, close 20':>17s} {'reading':>34s}")
    for k in (1.0, 1.5, 2.0, 3.0):
        sd = sd_at_neighbour * k
        m24 = (nearest - 24.0) / sd
        m20 = (nearest - 20.0) / sd
        if m24 >= 2.33:
            note = "24 holds"
        elif m20 >= 2.33:
            note = "24 fails -> drop to 20"
        else:
            note = "measure the device before shipping"
        print(f"   {k:8.1f} {sd:11.2f} {m24:16.1f}s {m20:16.1f}s {note:>34s}")
    print(f"   -> at the modelled sd the 24 edge has a {(nearest - 24.0) / sd_at_neighbour:.1f} sigma margin:")
    print("      it survives about 40% more noise than the model predicts. Past that, 20 is the edge")
    print("      that still holds (it needs 3x the modelled noise to fall to the 2.33 sigma rule).")

    # ---- 4. approach ------------------------------------------------------------------
    print("\n4. APPROACH - 40 deg to 10 deg over 3 s (300 trials); 2 changes = warming->close->locked")
    for release in (0.0, 2.0):
        counts = []
        backwards = 0
        for t in range(TRIALS):
            c, b = approach(calib[12.0][1], 12.0, 20.0, release, seed=400 + t)
            counts.append(c)
            backwards += b
        print(f"   {'150 ms only' if release == 0 else '150 ms + 2 deg release':>26s}: "
              f"{sum(counts)/len(counts):.2f} mean, {max(counts)} max, {backwards} reverse steps")
    return 0


if __name__ == "__main__":
    sys.exit(main())
