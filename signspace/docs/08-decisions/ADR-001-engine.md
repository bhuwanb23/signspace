# ADR-001 — Unity as the engine

**Status:** Accepted · 7 Nov 2026 · **Phase:** P0

## Context
Eleven days. One developer. The product needs hand tracking, gaze, MRUK, passthrough and a 72 fps budget on a Quest 3S. Whichever engine we pick, we spend the first 1–2 days becoming fluent in it.

## Options
1. **Unity 6 LTS + Meta XR SDK v207** — mature ISDK building blocks, every Meta sample is Unity, huge body of gaze/MRUK examples.
2. **Unreal 5** — better visuals, thinner gaze integration, fewer worked examples for exactly these building blocks.
3. **WebXR / IWSDK** — fast to start, no ISDK/MRUK parity, unknown perf ceiling for continuous 26-joint tracking.
4. **Spatial SDK (Android native)** — excellent for panels in space, wrong for real-time skinned geometry.

## Decision
**Unity 6 LTS + Meta XR All-in-One v207.**

## Consequences
- We inherit ISDK's grab, gaze and poke building blocks for free — days saved.
- Visual ceiling is lower than Unreal; we accept that and lean on art direction (unlit, few materials, one hero hand) rather than fidelity.
- The 72 fps target becomes achievable without heroics.

## Revisit if
The build stalls for a full day on an engine-level problem that Unreal would not have. At that point the answer is not "switch engines," it is "cut scope."
