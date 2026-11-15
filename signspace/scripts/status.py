#!/usr/bin/env python3
"""Report which docs are written, stubbed, or missing. Run at the end of every day."""
import pathlib, re

ROOT = pathlib.Path(__file__).resolve().parent.parent

def state(p: pathlib.Path):
    """STUB if the doc carries the stub marker, otherwise treated as written."""
    t = p.read_text(errors="ignore")
    return "stub" if "`STUB`" in t else "done"

files = sorted(ROOT.glob("docs/**/*.md"))
phase_files = sorted(ROOT.glob("phases/**/*.md"))
rows = [(str(f.relative_to(ROOT)), state(f), len(f.read_text(errors="ignore").split())) for f in files]
done = sum(1 for _, s, _ in rows if s == "done")
stub = sum(1 for _, s, _ in rows if s == "stub")
words = sum(w for _, _, w in rows)

print(f"{'document':52s} {'state':6s} {'words':>6s}")
print("-" * 68)
for path, s, w in rows:
    mark = {"done": "DONE", "stub": "STUB"}[s]
    print(f"{path:52s} {mark:6s} {w:6d}")
print("-" * 68)
print(f"{len(rows)} documents · {done} done · {stub} stubs · {words:,} words")
pw = sum(len(f.read_text(errors="ignore").split()) for f in phase_files)
open_tasks = sum(1 for f in phase_files if "## Do this" in f.read_text(errors="ignore"))
print(f"{len(phase_files)} phase files · {open_tasks} sub-phase task files · {pw:,} words")
