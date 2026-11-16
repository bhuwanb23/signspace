#!/usr/bin/env python3
"""Lock payoff: timing check + a previs render (P2.5).

Two jobs, both of them things that can be done before a headset exists:

1. **The timing check.** The payoff must complete in under 1.2 s, or it becomes a cutscene that the
   third repetition skips. The timeline is mirrored from `LockPayoff.cs`, every phase is printed with
   its window, and the script fails if the total is over budget - so "1.04 s" is arithmetic that CI can
   check rather than a number in a document.

2. **A previs.** A frame-accurate render of what the timeline does - hand brightening as it shrinks,
   object forming inside it, arcing to its shelf slot - so the 0:45 shot can be planned, and so the edit
   points are known before anyone stands in a room waving.

   This is **not** footage. It is a diagram of the timing, it goes to `lock-payoff-previs.mp4`, and it
   must never be used in the submission video as if it were the app. `take01.mp4` has to be shot on the
   device, and the shot list is in `phases/p2-core-loop/lock-payoff.md` section 6.

Usage:
  python3 scripts/payoff_previs.py                 # timing table + previs + storyboard
  python3 scripts/payoff_previs.py --timing-only
"""
import argparse
import math
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
VIDEO = ROOT / "assets" / "raw" / "video"
DOCS = ROOT / "phases" / "p2-core-loop"

# ---- mirrored from LockPayoff.cs (keep in step) ----------------------------------------
DISSOLVE = 0.40
FORM_START = 0.30
FORM = 0.22
ARC = 0.50   # P4.2: the arc reaches a real shelf slot
SETTLE = 0.12
HAND_RETURN = 0.12
BUDGET = 1.20
FULL_FIDELITY_REPEATS = 3
HURRY_SCALE = 0.55
ARC_HEIGHT = 0.12

TOTAL = max(FORM_START + ARC, DISSOLVE) + SETTLE + HAND_RETURN


def timing():
    rows = [
        ("hand dissolves (scale + fade + brighten)", 0.00, DISSOLVE),
        ("object starts forming inside the hand", FORM_START, FORM_START + FORM),
        ("object arcs to its shelf slot", FORM_START, FORM_START + ARC),
        ("overshoot and settle", FORM_START + ARC, FORM_START + ARC + SETTLE),
        ("reference hand returns", FORM_START + ARC + SETTLE, TOTAL),
    ]
    return rows


def check():
    ok = True
    rows = timing()
    print("=" * 78)
    print("P2.5 lock payoff timeline (mirrors LockPayoff.cs)")
    print("=" * 78)
    print(f"  {'phase':46s} {'from':>6s} {'to':>6s} {'len':>6s}")
    for name, a, b in rows:
        print(f"  {name:46s} {a:6.2f} {b:6.2f} {b - a:6.2f}")
    print(f"  {'TOTAL':46s} {0.0:6.2f} {TOTAL:6.2f} {TOTAL:6.2f}")

    overlap = DISSOLVE - FORM_START
    print(f"\n  overlap between the hand going and the object arriving: {overlap:+.2f} s")
    if overlap <= 0:
        print("  FAIL: there is a frame with nothing on screen. That reads as a cut, not a transformation.")
        ok = False
    else:
        print("  pass: the object starts forming before the hand is gone - no empty frame.")

    if TOTAL > BUDGET:
        print(f"  FAIL: {TOTAL:.2f} s exceeds the {BUDGET:.2f} s budget.")
        ok = False
    else:
        print(f"  pass: {TOTAL:.2f} s inside the {BUDGET:.2f} s budget "
              f"({BUDGET - TOTAL:.2f} s of headroom, {TOTAL / BUDGET * 100:.0f}% used).")

    hurried = TOTAL * HURRY_SCALE
    print(f"\n  repetitions 1-{FULL_FIDELITY_REPEATS}: {TOTAL:.2f} s   afterwards: {hurried:.2f} s "
          f"(x{HURRY_SCALE})")
    print("  the trap this avoids: a payoff that feels long is skipped, and a skipped payoff is not a reward.")
    return ok, rows


# ---- previs ----------------------------------------------------------------------------
W, H = 960, 720
FPS = 60
SKY = (14, 16, 22)
HAND = (226, 214, 196)
OBJECT = (196, 138, 78)


def lerp(a, b, t):
    return a + (b - a) * t


def ease_in(t):
    return t * t


def form_ease(t):
    """Grow fast, then a small bulge. Not a back-ease: a back-ease is 77% grown after a fifth of the
    time, which reads as a pop rather than as something assembling."""
    return (1 - (1 - t) ** 3) + math.sin(t * math.pi) * 0.08


def hand_polys(cx, cy, scale, bright):
    """A crude hand: a rounded palm plus five tapered fingers. Has to read at 480 px wide."""
    palm_w, palm_h = 104, 92
    fingers = (
        ("thumb", (-0.86, 0.18), (-1.62, 0.62), 0.40),
        ("index", (-0.50, 0.36), (-0.62, 1.34), 0.33),
        ("middle", (-0.14, 0.42), (-0.16, 1.52), 0.35),
        ("ring", (0.21, 0.38), (0.30, 1.38), 0.32),
        ("little", (0.54, 0.26), (0.74, 1.10), 0.27),
    )
    rings = []
    for _name, (bx, by), (tx, ty), width in fingers:
        x0, y0 = cx + bx * palm_w * scale, cy - by * palm_h * scale
        x1, y1 = cx + tx * palm_w * scale, cy - ty * palm_h * scale
        w = width * palm_w * scale
        dx, dy = x1 - x0, y1 - y0
        n = math.hypot(dx, dy) or 1
        nx, ny = -dy / n, dx / n
        tipw = 0.46
        rings.append([
            (x0 + nx * w / 2, y0 + ny * w / 2), (x1 + nx * w * tipw / 2, y1 + ny * w * tipw / 2),
            (x1 - nx * w * tipw / 2, y1 - ny * w * tipw / 2), (x0 - nx * w / 2, y0 - ny * w / 2),
        ])
        # a knuckle bump so the silhouette is not a rake
        rings.append([(x0 + nx * w * 0.62, y0 + ny * w * 0.62), (x0 - nx * w * 0.62, y0 - ny * w * 0.62),
                      (x0 - nx * w * 0.5 + dx * 0.12, y0 - ny * w * 0.5 + dy * 0.12),
                      (x0 + nx * w * 0.5 + dx * 0.12, y0 + ny * w * 0.5 + dy * 0.12)])
    # palm: an octagon, wider at the knuckles than at the wrist
    hw_top, hw_bot = 0.66, 0.50
    palm = [
        (cx - palm_w * scale * hw_top, cy - palm_h * scale * 0.30),
        (cx - palm_w * scale * hw_top * 0.92, cy - palm_h * scale * 0.52),
        (cx - palm_w * scale * hw_bot * 1.05, cy + palm_h * scale * 0.55),
        (cx - palm_w * scale * hw_bot * 0.62, cy + palm_h * scale * 0.70),
        (cx + palm_w * scale * hw_bot * 0.62, cy + palm_h * scale * 0.70),
        (cx + palm_w * scale * hw_bot * 1.05, cy + palm_h * scale * 0.55),
        (cx + palm_w * scale * hw_top * 0.92, cy - palm_h * scale * 0.52),
        (cx + palm_w * scale * hw_top, cy - palm_h * scale * 0.30),
    ]
    colour = tuple(min(255, int(c * (1.0 + bright))) for c in HAND)
    return palm, rings, colour


def render_frame(i, frames):
    from PIL import Image, ImageDraw

    t = i / FPS
    img = Image.new("RGB", (W, H), SKY)
    d = ImageDraw.Draw(img, "RGBA")

    # floor line + anchor marker
    anchor = (W * 0.76, H * 0.46)
    d.line([(0, H * 0.72), (W, H * 0.72)], fill=(30, 34, 44), width=2)
    d.ellipse([anchor[0] - 74, anchor[1] - 74, anchor[0] + 74, anchor[1] + 74], outline=(48, 55, 68), width=2)

    hand_c = (W * 0.34, H * 0.50)

    # --- object: form, arc, settle
    if t >= FORM_START:
        f = min(1.0, (t - FORM_START) / FORM)
        size = 0.16 * form_ease(f)
        a = min(1.0, (t - FORM_START) / ARC)
        x = lerp(hand_c[0], anchor[0], a * a * (3 - 2 * a))
        y = lerp(hand_c[1], anchor[1], a * a * (3 - 2 * a)) - math.sin(a * math.pi) * ARC_HEIGHT * H
        s = max(1.0, size * H)
        if a >= 1.0:
            st = min(1.0, (t - FORM_START - ARC) / SETTLE)
            s *= 1 + math.sin(st * math.pi) * 0.06
        # Crossfade in: the object becomes solid *through* the dissolving hand, which is what makes
        # the two read as one thing changing rather than one thing replacing another.
        oa = int(235 * min(1.0, f * 1.8))
        # wooden slab with a lit top face
        d.rounded_rectangle([x - s, y - s * 0.72, x + s, y + s * 0.72], radius=s * 0.18,
                            fill=OBJECT + (oa,))
        d.polygon([(x - s, y - s * 0.72), (x + s, y - s * 0.72),
                   (x + s * 0.72, y - s * 0.95), (x - s * 0.72, y - s * 0.95)],
                  fill=(226, 176, 118, oa))
        if a < 1.0:
            d.line([(hand_c[0], hand_c[1]), (x, y)], fill=(120, 96, 72, 60), width=2)

    # --- hand: dissolve
    if t < DISSOLVE:
        k = ease_in(min(1.0, t / DISSOLVE))
        scale = lerp(1.0, 0.12, k)
        # brighten while shrinking: the hand concentrates instead of being switched off, so it is
        # still visible in the frame where the object starts forming inside it
        alpha = int(255 * (1 - 0.55 * k))
        palm, rings, colour = hand_polys(*hand_c, scale, 1.6 * k)
        for poly in rings:
            d.polygon(poly, fill=colour + (alpha,))
        d.polygon(palm, fill=colour + (alpha,))
    # --- the hand returns
    ret_start = FORM_START + ARC + SETTLE
    if t >= ret_start:
        k = min(1.0, (t - ret_start) / HAND_RETURN)
        palm, rings, colour = hand_polys(*hand_c, lerp(0.85, 1.0, k), 0.0)
        for poly in rings:
            d.polygon(poly, fill=colour + (int(200 * k),))
        d.polygon(palm, fill=colour + (int(200 * k),))

    # --- HUD: this is a diagram, say so
    phase = ("hand -> object" if t < FORM_START else
             "object forming" if t < DISSOLVE else
             "arcing" if t < FORM_START + ARC else
             "settling" if t < FORM_START + ARC + SETTLE else "hand returns")
    d.rectangle([0, 0, W, 54], fill=(8, 9, 13))
    d.text((18, 18), f"PREVIS (not footage)  {t * 1000:4.0f} ms   {phase}", fill=(210, 216, 228))
    d.rectangle([0, H - 34, W, H], fill=(8, 9, 13))
    bar = (W - 36) * min(1.0, t / TOTAL)
    d.rectangle([18, H - 22, 18 + (W - 36), H - 14], outline=(60, 66, 80))
    d.rectangle([18, H - 22, 18 + bar, H - 14], fill=(196, 138, 78))
    d.text((18, H - 32 - 14), f"0.00 s", fill=(120, 128, 144))
    d.text((W - 96, H - 32 - 14), f"{TOTAL:.2f} s", fill=(120, 128, 144))
    return img


def render(out_mp4, storyboard_png):
    from PIL import Image
    import imageio_ffmpeg

    VIDEO.mkdir(parents=True, exist_ok=True)
    total_frames = int(round(TOTAL * FPS))

    def frames():
        """Two passes, so it is long enough to watch and loop without scrubbing."""
        for _ in range(2):
            for i in range(total_frames):
                yield i
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    cmd = [ffmpeg, "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS),
           "-i", "-", "-an", "-c:v", "libx264", "-preset", "medium", "-crf", "20",
           "-pix_fmt", "yuv420p", str(out_mp4)]
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    keys = []
    for i in frames():
        img = render_frame(i, total_frames)
        proc.stdin.write(img.tobytes())
        marks = (0,                                   # the hand, whole
                 int(0.36 * FPS),                     # both present: the transformation
                 int(0.52 * FPS),                     # the hand is gone, the object is whole
                 int(0.62 * FPS),                     # in flight
                 int(0.80 * FPS),                     # settling on the anchor
                 total_frames - 1)                    # the hand is back, the object is placed
        if i in marks:
            keys.append(img.copy())
    proc.stdin.close()
    proc.wait()

    # storyboard strip: the six moments that matter, for anyone who will not watch an mp4
    cols, rows = 3, 2
    tw, th = W // 2, H // 2
    sheet = Image.new("RGB", (tw * cols, th * rows), (0, 0, 0))
    for idx, im in enumerate(keys[:6]):
        sheet.paste(im.resize((tw, th)), ((idx % cols) * tw, (idx // cols) * th))
    sheet.save(storyboard_png)
    return out_mp4, storyboard_png, total_frames * 2


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--timing-only", action="store_true")
    args = ap.parse_args()

    ok, _rows = check()
    if args.timing_only:
        return 0 if ok else 1

    mp4, sheet, frames = render(VIDEO / "lock-payoff-previs.mp4", DOCS / "lock-payoff-storyboard.png")
    print(f"\n  previs: {frames} frames at {FPS} fps -> {mp4.relative_to(ROOT)} ({mp4.stat().st_size // 1024} kB)")
    print(f"  storyboard: {sheet.relative_to(ROOT)}")
    print("  NOT footage. `take01.mp4` must be shot on the device (shot list in lock-payoff.md section 6).")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
