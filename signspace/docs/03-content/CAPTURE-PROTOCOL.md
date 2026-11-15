# Capture Protocol


## 0. COMPRESSED MODE — signer-verified, not motion-captured (read first if the calendar is short)

We do not need mocap hardware. The integrity requirement is that every shipped handshape traces to a named, credited Deaf signer who has confirmed it reads correctly — not that it came off a rig.

**The 60-minute remote protocol:**

1. **Video call** with the Deaf signer, recorded with consent. They hold each target shape to camera for ~10 s, from two angles (front, turned ~30°).
2. **We author the 3D poses by hand** from the recording — six shapes, roughly 20 minutes each in the editor.
3. **They approve by video.** We screen-record each authored pose rotating in the reference hand, send the clips, and the signer answers yes/no per shape. Rejected poses are re-authored and re-sent.
4. **The credit and the "not a replacement for a Deaf teacher" line are agreed on that call.**

**Preserved:** signer authorship, signer verification, real credit.
**Cost:** fidelity — hand-authored poses are slightly less natural than retargeted mocap. Acceptable at six shapes; not at sixty.
**Non-negotiable:** step 3 still happens. A pose we shaped that a Deaf signer has never seen is exactly the failure mode R1 exists to prevent.

**Status:** `STUB` — not yet written · **Owner:** W3 · **Phase:** P5 · **Gate:** G5

## Why this document exists
The longest-lead item in the project and the mitigation for the only fatal risk. It must be written before we meet the signer, not during.

## Questions this must answer
- What exactly do we ask the signer to do, in what order, and how long will it take?
- What are the camera, lighting and background requirements?
- How many takes per shape, from how many vantage points?
- What consent do we need, in what language, covering what rights?
- How do we handle the shapes we *can't* track (we still need them for the honest mirror)?
- How is every take traced back to a person and a date?

## Planned outline
1. Session plan (a printable one-pager for the signer)
2. Equipment and room setup
3. Take list: 12 shapes × 3 vantage points + blind-spot set + neutral
4. Naming and manifest convention
5. Consent script and rights scope
6. Compensation and credit agreement
7. Post-session: retarget, clean, validate
8. Contingency if a take is unusable

## Inputs
`SIGNSET.md`, `POSE-FORMAT.md`, reference rig

## Output / done when
A signer can be booked, briefed and captured in under 90 minutes, with a manifest that survives an audit.
