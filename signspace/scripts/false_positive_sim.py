#!/usr/bin/env python3
"""P3.2 - twenty sloppy attempts, and the gates that have to stay shut.

The task asks for a 20-attempt false-positive test with "fast, sloppy, wrong-but-not-close hands",
target zero fires. A real hand is not available here, so each attempt is defined as a reproducible
60 fps sequence with a name, a motion profile and a confidence profile, and run through the FULL gate
stack:

    confidence >= floor for the preceding 400 ms   (a stable window, not the first good frame)
    AND the decision names the SAME shape continuously for 400 ms
    AND 20 s since the last fire
    AND the rule itself (P3.1) says fire

Every attempt is chosen because some naive rule would fire on it. The point is not to prove the gates
good, it is to show which one is load-bearing for which failure mode.

Run:  python3 scripts/false_positive_sim.py
"""

import json
import math
import pathlib
import random
import sys

import hand_model as hm
import palm_sim as psim
import tune_match as tm
from nearmiss_sim import all_distances, load_set, SIGNS

FPS = 60.0
LOCKED_DEG = 12.0
MARGIN_DEG = 2.0
CONFIDENCE_FLOOR = 0.7
STABLE_SECONDS = 0.4
COOLDOWN_SECONDS = 20.0

IDENTITY = (0.0, 0.0, 0.0, 1.0)


def slerp(a, b, t):
    dot = sum(a[i] * b[i] for i in range(4))
    if dot < 0.0:
        b = tuple(-c for c in b)
        dot = -dot
    dot = min(1.0, max(-1.0, dot))
    theta = math.acos(dot)
    if theta < 1e-6:
        return a
    s = math.sin(theta)
    return tuple((math.sin((1 - t) * theta) * a[i] + math.sin(t * theta) * b[i]) / s for i in range(4))


def add_noise(pose, rng, sigma):
    out = {}
    for name, q in pose.items():
        if name in ("Wrist", "Palm") or sigma <= 0.0:
            out[name] = q
            continue
        axis = (rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))
        if axis == (0.0, 0.0, 0.0):
            axis = (1.0, 0.0, 0.0)
        out[name] = hm.q_mul(q, hm.q_from_axis_angle(axis, rng.gauss(0.0, sigma)))
    return out


def blend(a, b, t, rng=None, sigma=0.0):
    out = {}
    for j in hm.JOINT_NAMES:
        out[j] = slerp(a[j], b[j], t)
    return add_noise(out, rng, sigma) if rng is not None else out


def turn_wrist(pose, degrees):
    return psim.rotate_hand(pose, (0.0, 1.0, 0.0), degrees)


# ------------------------------------------------------------------ the gate stack

class Gates:
    def __init__(self):
        self.floor = CONFIDENCE_FLOOR
        self.good_conf = 0.0
        self.candidate = None
        self.candidate_time = 0.0
        self.cooldown = 0.0
        self.count = 0
        self.first_fire_frame = None

    def tick(self, decision, confidence, dt, frame):
        """One frame. Returns True when the app is allowed to speak on this frame."""
        self.good_conf = self.good_conf + dt if confidence >= self.floor else 0.0
        self.cooldown = max(0.0, self.cooldown - dt)

        fires, shape_id = decision
        if not fires:
            self.candidate = None
            self.candidate_time = 0.0
            return False

        if shape_id != self.candidate:
            self.candidate = shape_id
            self.candidate_time = 0.0
        self.candidate_time += dt

        if (self.good_conf >= STABLE_SECONDS and self.candidate_time >= STABLE_SECONDS
                and self.cooldown <= 0.0):
            self.cooldown = COOLDOWN_SECONDS
            self.count += 1
            if self.first_fire_frame is None:
                self.first_fire_frame = frame
            return True
        return False


def rule(target_id, distances):
    fires, sid, _deg, _reason = psim_rule_strict(target_id, distances)
    return fires, sid


def psim_rule_strict(target_id, distances):
    from nearmiss_sim import rule_strict
    return rule_strict(target_id, distances)


# ------------------------------------------------------------------ the twenty attempts

def attempts(poses, weights, rng):
    """Twenty attempts, each one built to prove something.

    A FALSE-POSITIVE attempt is one where the hand NEVER comes inside the lock band on any shape, so
    any fire is a lie by construction. Each is measured and the minimum distance to any shape is
    printed - if that number is >= the lock band, the rule had no evidence, whatever it did.

    A GATE attempt is one where the hand DOES qualify, and a named gate is the only reason to stay
    silent. These are how the confidence window and the rate cap are shown to be load-bearing rather
    than decorative.
    """
    def morph_between(a, b, t):
        return lambda _t: blend(poses[a], poses[b], t)

    #: A hand that is far from every shape, for attempts that have to leave the set entirely.
    mush = blend(poses["W"], poses["C"], 0.5)

    def pass_through(shape, hold):
        """Arrive, hold briefly, leave. A hand in motion, not a sign being made."""
        def fn(t):
            arrive, leave = 0.25, 0.25 + hold
            if t < arrive:
                return blend(poses["B"], poses[shape], t / arrive)
            if t < leave:
                return poses[shape]
            # leave toward a shape nowehere near the set, NOT toward another sign: ending on W would
            # be the user genuinely making W, and naming W would then be correct, not a false positive
            u = min(1.0, (t - leave) / 0.25)
            return blend(poses[shape], mush, u)
        return fn

    def sweep():
        seq = ["B", "U", "V", "W", "C", "O"]
        def fn(t):
            u = min(0.999, t / 1.0) * (len(seq) - 1)
            i = int(u)
            return blend(poses[seq[i]], poses[seq[min(i + 1, len(seq) - 1)]], u - i)
        return fn

    def sloppy(shape, sigma):
        return lambda t: add_noise(poses[shape], rng, sigma)

    def off_by(pair_from, pair_to, target_deg, total_deg):
        """A hand `target_deg` away from `pair_to`, along the pair's own axis. Verifiable by design."""
        t = 1.0 - target_deg / total_deg
        return lambda t_: blend(poses[pair_from], poses[pair_to], t)

    def turned(shape, degrees):
        return lambda t: add_noise(turn_wrist(poses[shape], degrees), rng, 2.0)

    return [
        # ---- the hand never qualifies: any fire here is a false positive -------------------
        ("1  passes through O, 0.1 s hold", "fp", 1.0, pass_through("O", 0.10), lambda t: 1.0),
        ("2  passes through C, 0.2 s hold", "fp", 1.0, pass_through("C", 0.20), lambda t: 1.0),
        ("3  passes through V, 0.3 s hold", "fp", 1.0, pass_through("V", 0.30), lambda t: 1.0),
        ("4  sweeps through all six", "fp", 1.0, sweep(), lambda t: 1.0),
        ("5  sloppy wrong shape, 10 deg/joint", "fp", 1.0, sloppy("O", 10.0), lambda t: 1.0),
        ("6  sloppy wrong shape, 15 deg/joint", "fp", 1.0, sloppy("U", 15.0), lambda t: 1.0),
        ("7  sloppy wrong shape, 20 deg/joint", "fp", 1.0, sloppy("W", 20.0), lambda t: 1.0),
        ("8  sloppy wrong shape, 25 deg/joint", "fp", 1.0, sloppy("B", 25.0), lambda t: 1.0),
        ("9  halfway between C and O", "fp", 1.0, morph_between("C", "O", 0.5), lambda t: 1.0),
        ("10 halfway between U and V", "fp", 1.0, morph_between("U", "V", 0.5), lambda t: 1.0),
        ("11 halfway between V and W", "fp", 1.0, morph_between("V", "W", 0.5), lambda t: 1.0),
        ("12 halfway between B and U", "fp", 1.0, morph_between("B", "U", 0.5), lambda t: 1.0),
        ("13 near O but 14 deg out", "fp", 1.0, off_by("C", "O", 14.0, 65.0), lambda t: 1.0),
        ("14 near C but 15 deg out", "fp", 1.0, off_by("O", "C", 15.0, 65.0), lambda t: 1.0),
        ("15 near V but 16 deg out", "fp", 1.0, off_by("W", "V", 16.0, 118.1), lambda t: 1.0),
        ("16 near U but 18 deg out", "fp", 1.0, off_by("B", "U", 18.0, 117.0), lambda t: 1.0),
        ("17 wrong shape, wrist rolled 25 deg", "fp", 1.0, turned("O", 25.0), lambda t: 1.0),
        ("18 wrong shape, wrist rolled 40 deg", "fp", 1.0, turned("C", 40.0), lambda t: 1.0),
        # ---- the hand does qualify: only a gate can keep it silent ------------------------
        ("19 qualifies, but confidence is low for the first 0.5 s", "gate", 0.55,
         sloppy("O", 2.0), lambda t: 0.5 if t < 0.5 else 1.0),
        ("20 qualifies twice within 20 s - the rate cap must allow one", "gate", 2.6,
         lambda t: poses["O"] if (0.5 <= t < 1.0 or 1.6 <= t < 2.5) else blend(poses["O"], poses["W"], 0.8),
         lambda t: 1.0),
    ]


def run(name, mode, seconds, frame_fn, conf_fn, poses, weights):
    rng = random.Random(hash(name) % 100000)
    gates = Gates()
    dt = 1.0 / FPS
    frames = int(seconds * FPS)
    fires = 0
    reasons = {}
    stretch = best_stretch = 0.0
    for frame in range(frames):
        t = frame * dt
        hand = frame_fn(t)
        dist = all_distances(poses, hand, weights)
        closest = min(dist.values())
        stretch = stretch + dt if closest < LOCKED_DEG else 0.0
        best_stretch = max(best_stretch, stretch)

        target = "B"          # the app is always asking for something; B is the shipped default
        decision = rule(target, dist)
        if gates.tick(decision, conf_fn(t), dt, frame):
            fires += 1
    return fires, gates, best_stretch


def main():
    global STABLE_SECONDS
    weights, _src = tm.load_weights()
    ids, poses = load_set(SIGNS)
    rng = random.Random(4)
    print("=" * 78)
    print("P3.2  TWENTY SLOPPY ATTEMPTS - does the app ever name a shape?")
    print(f"      floor {CONFIDENCE_FLOOR}  stable window {STABLE_SECONDS}s  cooldown {COOLDOWN_SECONDS}s  "
          f"lock band {LOCKED_DEG} deg")
    print("=" * 78)
    print(f"   {'attempt':56s} {'evidence':>10s} {'fires':>6s}   verdict")
    print(f"   {'':56s} {'s in band':>10s}")
    total = 0
    fp_fires = 0
    rows = []
    for (name, mode, seconds, frame_fn, conf_fn) in attempts(poses, weights, rng):
        fires, gates, stretch = run(name, mode, seconds, frame_fn, conf_fn, poses, weights)
        if mode == "fp":
            total += fires
            fp_fires += fires
        expected = 0 if mode == "fp" else (1 if "twice" in name else 0)
        ok = fires == expected
        verdict = ("silent" if fires == 0 else f"SPOKE {fires}x") + ("" if ok else "  <-- NOT EXPECTED")
        print(f"   {name:56s} {stretch:10.2f} {fires:6d}   {verdict}")
        rows.append((name, mode, stretch, fires, expected, ok, verdict))
    print(f"\n   FALSE-POSITIVE ATTEMPTS (1-18): {fp_fires} fires, target 0")
    print("   -> " + ("every attempt whose hand never came inside the lock band stayed silent"
                       if fp_fires == 0 else "the gates let something through"))

    # where the gate earns its keep: the same attempts with the stability window removed
    print("\n   WHICH GATE IS LOAD-BEARING?  same attempts, stability window = 0 s")
    total_naive = 0
    for (name, mode, seconds, frame_fn, conf_fn) in attempts(poses, weights, rng):
        saved = STABLE_SECONDS
        STABLE_SECONDS = 0.0
        fires, _g, _s = run(name, mode, seconds, frame_fn, conf_fn, poses, weights)
        STABLE_SECONDS = saved
        total_naive += fires
        if fires:
            print(f"   {name:58s} would have spoken {fires}x")
    print(f"   TOTAL without the window: {total_naive} fires")
    print("   -> the 400 ms window is what turns 'the rule is satisfied right now' into 'the hand is")
    print("      still doing that', which is the difference between the trap and the feature.")


if __name__ == "__main__":
    main()
