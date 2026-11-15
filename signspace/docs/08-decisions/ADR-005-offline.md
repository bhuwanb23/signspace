# ADR-005 — Deterministic, offline, no AI

**Status:** Accepted · 7 Nov 2026 · **Phase:** P2

## Context
Adjacent 2025 winners (Language IRL) and Meta's own Spatial Lingo use Passthrough Camera Access plus cloud AI. Copying that shape would be the obvious route to "modern" — and would make us a lesser version of an existing winner.

## Options
1. **Cloud CV / an ML recogniser** — impressive vocabulary ceiling, adds latency, network dependency, privacy surface, and a failure mode we cannot debug on Day 10.
2. **On-device deterministic joint comparison** — provably sufficient for 12 *static* shapes, zero network, zero latency, debuggable line by line.

## Decision
**Deterministic and offline.** No network permission, no camera access, no model, no backend.

## Consequences
- The product works on a plane, in a basement, with the Wi-Fi off. That is a sentence in the video and a rubric beat in its own right.
- We deliberately forfeit any award that requires AI or PCA — and we say so internally so nobody wastes a day chasing one.
- Every failure is reproducible, which matters enormously when the whole build is 11 days.

## Revisit if
The product needs dynamic signs (v2). Even then, on-device first.
