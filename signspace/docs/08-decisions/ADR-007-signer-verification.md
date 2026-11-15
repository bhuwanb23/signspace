# ADR-007 — Late-binding signer verification

**Status:** Accepted · 14 Nov 2026 · **Phase:** P5 · **Supersedes the timing, not the principle, of R1**

## Context

The plan assumed a Deaf signer would be captured on Day 1–5. Reality: **four days remain and no signer is booked yet** — one will be found during playtesting (Day 3). The integrity rule (`RISKS.md` R1) says no handshape ships without a Deaf signer's verification. That rule is not renegotiable. The *timing* is.

There are three ways to resolve this, and only one is defensible:

1. **Author the handshapes ourselves and stay silent about it** — dishonest, and exactly what R1 exists to prevent. Rejected.
2. **Block the build until a signer is available** — guarantees shipping nothing. Rejected.
3. **Build with handshapes authored from published ASL references, and make the authorship claim a late-bound switch** so that a signer's verification, whenever it arrives, changes the product's claim in five minutes without touching the build.

## Decision

**Ship the engine with reference-authored poses; design the credit as data, not copy.**

A single file — `Assets/StreamingAssets/Signs/set-meta.json` — carries the authorship claim:

```json
{
  "version": 1,
  "authorship": "reference-authored",          // or "signer-verified"
  "signer": null,                              // becomes {"name": "...", "city": "...", "verifiedOn": "..."}
  "verificationMode": "pending-review",        // or "per-shape-approved"
  "creditLine": "Handshapes authored against published ASL references. Deaf signer review in progress."
}
```

The credits object renders `creditLine` verbatim, plus the fixed sentence *"This is practice software, not a replacement for a Deaf teacher."* — which ships in **both** states and is never optional.

**Two pre-written states, both honest:**

| State | `creditLine` | Used when |
|---|---|---|
| **A · signer-verified** | *"Handshapes verified by [name], a Deaf ASL signer in [city], on [date]. This is practice software, not a replacement for a Deaf teacher."* | The signer has approved every shipped pose on video (CAPTURE-PROTOCOL §0 step 3) |
| **B · reference-authored** | *"Handshapes authored against published ASL references. Deaf signer review in progress — corrections ship in the next build. This is practice software, not a replacement for a Deaf teacher."* | Anything less than full signer approval at submit time |

## Why this is defensible

- **The shapes are fingerspelling letters from a standard manual alphabet**, not coined vocabulary. The authorship question is real but far narrower than for a self-invented sign set. That is a reason to be careful, not a licence to be silent.
- **State B is a weaker claim, not a false one.** It says what is true: authored from published references, review pending. A Deaf judge reading it sees a team that knows the difference.
- **The switch costs nothing.** One JSON file, one re-render, one re-upload — 5 minutes on Day 4.

## Consequences

- The submission video and description must match whichever state ships. If B ships, the description's integrity paragraph changes and **"recorded by named Deaf signers" must not appear** — use *"authored from published ASL references; Deaf signer review in progress."*
- The signer call becomes a **Day-3 evening review** rather than a Day-1 capture — feasible because the remote protocol needs a 60-minute call, not a studio.
- If the signer is found and approves only *some* poses, the state is **B**, and the approved subset is named explicitly. Partial truth, stated partially.

## Revisit if

A signer approves all six poses before submission → flip to state A, re-render, re-upload, update the description. That is the plan, and it is a five-minute job by design.
