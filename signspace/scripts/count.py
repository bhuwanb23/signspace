#!/usr/bin/env python3
"""Verify the submission's hard limits. Run before every paste into Devpost."""
import re, sys, pathlib

LIMITS = {"tagline": 140, "description": 500}

def main(doc="docs/06-submission/SUBMISSION-COPY.md"):
    t = pathlib.Path(doc).read_text()
    desc = t.split("## 3. DESCRIPTION")[1].split("\n---")[0]
    body = "\n".join(l for l in desc.split("\n")[2:] if l.strip() and not l.startswith("*limit"))
    words = len(body.split())
    tags = [s.strip() for s in re.findall(r"```\n(.+?)\n```", t, re.S)]
    tags += re.findall(r"^`([^`]{20,})`$", t, re.M)
    ok = True
    for i, tag in enumerate(tags, 1):
        flag = "OK " if len(tag) <= LIMITS["tagline"] else "OVER"
        ok &= len(tag) <= LIMITS["tagline"]
        print(f"[{flag}] tagline {i}: {len(tag)}/{LIMITS['tagline']} chars")
    flag = "OK " if words <= LIMITS["description"] else "OVER"
    ok &= words <= LIMITS["description"]
    print(f"[{flag}] description: {words}/{LIMITS['description']} words")
    sys.exit(0 if ok else 1)

if __name__ == "__main__":
    main(*sys.argv[1:])
