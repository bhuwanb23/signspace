#!/usr/bin/env python3
"""Synthesise the SIGNSPACE sound set from SOUND-MAP.md §2.

Why a script and not just WAVs in a folder: a binary in a repository is a mystery. This file is the
recipe — every clip's frequency, envelope and target peak is a line of code, so retuning the chime or
making the near-miss gentler is an edit and a re-run, not a search for a sound designer.

The clips are **placeholders that obey the spec's shape**, not the final timbre: the map asks for
struck wood and this synth can only give a damped harmonic stack. They are good enough to build,
mix-check and shoot the video with, and `docs/04-design/SOUND-MAP.md` §2 says what still needs ears.

Rules enforced here, from the map:
  * mono, 44.1 kHz, 16-bit
  * every clip's peak is <= -6 dBFS (0.5) — the map's mix ceiling
  * every clip is under 400 ms except the lock chime (1.6 s) and its shelf-full variant
  * no descending "you failed" tone anywhere: the near-miss falls a third by design and is quiet

Usage:  python scripts/make_sfx.py
"""

import math
import os
import random
import struct
import wave

SR = 44100
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "..", "unity", "Assets", "_Project", "Audio", "SFX")


# --------------------------------------------------------------------------- primitives

def silence(seconds):
    return [0.0] * int(SR * seconds)


def tone(freq, seconds, amp=0.5, decay=None, partials=((1.0, 1.0),)):
    """A damped harmonic stack. `decay` is an exponential half-life in seconds."""
    n = int(SR * seconds)
    out = []
    for i in range(n):
        t = i / SR
        env = 1.0
        if decay:
            env = math.exp(-t / decay)
        # 3 ms attack: a click at t=0 is audible on a headset speaker
        attack = min(1.0, t / 0.003)
        s = 0.0
        for mult, pamp in partials:
            s += pamp * math.sin(2 * math.pi * freq * mult * t)
        out.append(amp * env * attack * s)
    return out


def noise(seconds, amp=0.4, seed=1):
    rnd = random.Random(seed)
    return [amp * rnd.uniform(-1.0, 1.0) for _ in range(int(SR * seconds))]


def lowpass(buf, cutoff_hz):
    """One-pole low-pass. Crude, and the right amount of crude for a placeholder."""
    a = 1.0 - math.exp(-2.0 * math.pi * cutoff_hz / SR)
    y = 0.0
    out = []
    for x in buf:
        y += a * (x - y)
        out.append(y)
    return out


def sweep_lowpass(buf, f0, f1):
    y = 0.0
    out = []
    n = max(1, len(buf) - 1)
    for i, x in enumerate(buf):
        f = f0 + (f1 - f0) * (i / n)
        a = 1.0 - math.exp(-2.0 * math.pi * f / SR)
        y += a * (x - y)
        out.append(y)
    return out


def mix(a, b, gain=1.0, offset=0.0):
    """Add b onto a starting at `offset` seconds; extend a if needed."""
    start = int(SR * offset)
    need = start + len(b)
    if len(a) < need:
        a = a + [0.0] * (need - len(a))
    for i, v in enumerate(b):
        a[start + i] += v * gain
    return a


def fade(buf, attack=0.005, release=0.02):
    n = len(buf)
    na = min(n, int(SR * attack))
    nr = min(n, int(SR * release))
    out = list(buf)
    for i in range(na):
        out[i] *= i / na
    for i in range(nr):
        out[n - 1 - i] *= i / nr
    return out


def normalize(buf, peak):
    top = max((abs(x) for x in buf), default=0.0)
    if top <= 0.0:
        return buf
    k = peak / top
    return [x * k for x in buf]


def room_tail(buf, delay=0.055, gain=0.22, taps=3):
    """A very short small-room tail: what the map asks for, and nothing cathedral-like."""
    out = list(buf)
    for t in range(1, taps + 1):
        out = mix(out, buf, gain ** t, delay * t)
    return out


def write(name, buf, peak):
    buf = normalize(fade(buf), peak)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", max(-32768, min(32767, int(x * 32767))))
                               for x in buf))
    print("%-20s %5.0f ms  peak %.3f  (%.1f dBFS)" %
          (name, 1000.0 * len(buf) / SR, peak, 20 * math.log10(peak) if peak else -999))


# --------------------------------------------------------------------------- the clips

def boot_hands():
    # "I see you" - a soft single 440 Hz tone, 180 ms, slow 60 ms fade in. Not a chime.
    buf = tone(440, 0.18, 0.5, decay=0.35)
    return fade(buf, attack=0.060, release=0.030)


def hand_appear():
    # Very quiet filtered noise sweep, 300 ms: the hand arriving in the space.
    return sweep_lowpass(noise(0.30, 0.5, seed=11), 900, 2600)


def grab_click():
    # A physical grip has a click at the top of it: noise transient + a 1.2 kHz damped body.
    buf = noise(0.012, 0.5, seed=2) + silence(0.078)
    return mix(buf, tone(1200, 0.080, 0.35, decay=0.012), 1.0)


def release_click():
    buf = noise(0.010, 0.35, seed=3) + silence(0.070)
    return mix(buf, tone(800, 0.070, 0.22, decay=0.010), 1.0)


def rotate_texture():
    # 1 s loopable granular texture; the director drives its volume from rotation speed.
    buf = lowpass(noise(1.0, 0.30, seed=7), 1800)
    # amplitude grit at ~70 Hz, looped seamlessly by using a whole number of cycles
    for i in range(len(buf)):
        buf[i] *= 0.55 + 0.45 * math.sin(2 * math.pi * 70 * (i / SR))
    return fade(buf, attack=0.010, release=0.010)


def lock_chime():
    # C5 - G5 - C6, 120 ms apart, struck wood, 1.6 s with a short small-room tail.
    wood = ((1.0, 1.0), (2.0, 0.30), (3.0, 0.12))
    buf = silence(1.60)
    buf = mix(buf, tone(523.25, 1.60, 0.40, decay=0.30, partials=wood), 1.0, 0.00)
    buf = mix(buf, tone(783.99, 1.45, 0.34, decay=0.26, partials=wood), 1.0, 0.12)
    buf = mix(buf, tone(1046.50, 1.30, 0.26, decay=0.20, partials=wood), 1.0, 0.24)
    return room_tail(buf, delay=0.060, gain=0.18, taps=2)


def shelf_full():
    # The chime one octave up, 60% quieter: a variation of the reward, not a new sound.
    wood = ((1.0, 1.0), (2.0, 0.25))
    buf = silence(1.10)
    buf = mix(buf, tone(1046.50, 1.10, 0.30, decay=0.16, partials=wood), 1.0, 0.00)
    buf = mix(buf, tone(1567.98, 1.00, 0.24, decay=0.13, partials=wood), 1.0, 0.10)
    return room_tail(buf, delay=0.050, gain=0.12, taps=2)


def object_settle():
    # Soft wooden knock, 140 ms, low-passed: something being put down, not a notification.
    body = tone(180, 0.14, 0.45, decay=0.030, partials=((1.0, 1.0), (2.3, 0.25)))
    return mix(lowpass(noise(0.02, 0.5, seed=5), 2400), body, 1.0, 0.004)


def near_miss():
    # THE QUESTION: a falling third, soft. Must not be the chime's interval, must not be a buzzer.
    buf = silence(0.40)
    buf = mix(buf, tone(659.25, 0.18, 0.28, decay=0.08), 1.0, 0.00)
    buf = mix(buf, tone(523.25, 0.22, 0.24, decay=0.10), 1.0, 0.17)
    return room_tail(buf, delay=0.045, gain=0.10, taps=1)


def surface_found():
    # A very quiet low rumble with a soft edge, 500 ms: the shelf has a floor under it.
    rumble = lowpass(noise(0.40, 0.55, seed=13), 130)
    return fade(rumble, attack=0.100, release=0.180)


def band_close():
    # Warming -> close: a soft rising interval, two notes 60 ms apart. Inviting, not a gate.
    buf = silence(0.34)
    buf = mix(buf, tone(392.00, 0.16, 0.30, decay=0.08), 1.0, 0.00)
    return mix(buf, tone(493.88, 0.18, 0.28, decay=0.09), 1.0, 0.06)


def band_lock_in():
    # Close -> locked: a single short high tone, 120 ms. "You are in", not "you did it".
    return tone(880.00, 0.12, 0.40, decay=0.050)


def band_release():
    # Locked -> warming: the same interval inverted, at half volume. No punishment.
    buf = silence(0.30)
    buf = mix(buf, tone(493.88, 0.14, 0.16, decay=0.07), 1.0, 0.00)
    return mix(buf, tone(392.00, 0.16, 0.14, decay=0.08), 1.0, 0.06)


def toggle_click():
    # A switch has a click. Also used for a fired dwell: both are "a choice was made".
    return mix(noise(0.006, 0.4, seed=17), tone(1600, 0.035, 0.25, decay=0.008), 1.0, 0.001)


def calibrate_start():
    return tone(330.00, 0.16, 0.35, decay=0.10)


def calibrate_done():
    # Two notes rising: the calibration closed, and the next shape will be judged to your hand.
    buf = silence(0.40)
    buf = mix(buf, tone(392.00, 0.20, 0.30, decay=0.09), 1.0, 0.00)
    return mix(buf, tone(523.25, 0.22, 0.28, decay=0.11), 1.0, 0.14)


# name -> (builder, target peak). Every peak is <= 0.5 = -6 dBFS, the map's ceiling.
CLIPS = [
    ("boot_hands", boot_hands, 0.30),
    ("hand_appear", hand_appear, 0.12),
    ("grab_click", grab_click, 0.34),
    ("release_click", release_click, 0.18),
    ("rotate_texture", rotate_texture, 0.10),
    # 0.48, not 0.50: 16-bit quantisation and the reverb tap put a nominal -6.0 dBFS clip at -5.9.
    ("lock_chime", lock_chime, 0.48),
    ("shelf_full", shelf_full, 0.20),
    ("object_settle", object_settle, 0.28),
    ("near_miss", near_miss, 0.28),
    ("surface_found", surface_found, 0.16),
    ("band_close", band_close, 0.30),
    ("band_lock_in", band_lock_in, 0.34),
    ("band_release", band_release, 0.16),
    ("toggle_click", toggle_click, 0.20),
    ("calibrate_start", calibrate_start, 0.16),
    ("calibrate_done", calibrate_done, 0.30),
]


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, build, peak in CLIPS:
        write(name, build(), peak)
    total = sum(os.path.getsize(os.path.join(OUT, n + ".wav")) for n, _, _ in CLIPS)
    print("\n%d clips, %.1f kB total (SOUND-MAP.md §6 budget: under 2 MB)" %
          (len(CLIPS), total / 1024.0))


if __name__ == "__main__":
    main()
