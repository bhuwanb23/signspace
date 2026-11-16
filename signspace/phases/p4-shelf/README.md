# P4 — Spatial shelf

**Day 5 (Wed 11 Nov) · Budget 8 h · Gate:** G4 — Kill the app, relaunch — is the shelf still on the same real surface, with the same objects?
**Status:** 🟡 **P4.1–P4.6 complete as work and desk-verified; G4 still needs a headset** — the selection rule, the launch log, the shelf objects, schema-3 persistence with a five-relaunch cross-process pass and a leave/return pass, the designed plinth with its quiet surface offer, the development-only passthrough switch, and the measured FoV reflow. Every headset row, the reboot, the passthrough clips and the FoV capture remain

## Objective
Make the room hold the user's progress: anchor a shelf to real furniture, let mastered shapes accumulate on it, and make it survive everything a judge can do to it.

## Why it sits here
The shelf is the only part of the product that survives the session, which makes it the argument for coming back — and the only place MRUK and passthrough do real work rather than decorative work.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P4.1](tasks/p4.1-shelf-anchor.md) | Anchor the shelf to a real surface | 2 h | W2 | ✅ band + 60° yaw cone + capped area, seven rooms in `shelf_sim.py`; launch log per relaunch |
| [P4.2](tasks/p4.2-objects.md) | Mastered shapes become shelf objects | 2 h | W1 | ✅ 4 families / 6 shapes, 12 slots, one idle Update; `ShelfCheck`: 0/66 overlaps, 1 dim marker from launch, 26/120 draw calls |
| [P4.3](tasks/p4.3-persistence.md) | Persistence across relaunch, reboot and room | 1.5 h | W2 | 🟡 Editor check + 5/5 cross-process relaunches + leave/return pass; headset relaunch, reboot and MRUK rows NOT RUN |
| [P4.4](tasks/p4.4-plinth-fallback.md) | The plinth: never show an error, never depend on a surface | 1 h | W2 | 🟡 4-part plinth prefab, silent fallback, pinch-only surface offer; `P4.4` check PASS; dark-room/device NOT RUN |
| [P4.5](tasks/p4.5-passthrough-toggle.md) | Passthrough on/off comparison, built for the video | 45 min | W4 | 🟡 Dev-only `P` toggle, check PASS; the two 3 s clips NOT RUN |
| [P4.6](tasks/p4.6-fov-reflow.md) | Shelf reflow for narrow field of view | 1 h | W2 | 🟡 Measured reflow: 110° → 6 cols / 51.6°, 70° → 4 cols / 35.1° grid, widest object 22.2°, no scaling; check PASS; simulator/visual/capture NOT RUN |

**Budget:** 8 h. Runs parallel with P5 from Day 5.

## Inputs required before starting
- MRUK proven (P1.6, P1.7)
- Working lock event (P2.5)
- Twelve placeholder object models — *narrowed in P4.2 to four family silhouettes, because six shapes read as silhouettes and twelve unique models was three hours that bought nothing*
- XR Simulator with the VR Glasses profile

## Outputs — the phase is done when all of these are true
- [~] A persistent, anchored shelf *(selection, objects, schema-3 restore across five processes and
  the leave/return policy desk-verified; headset relaunch and reboot owed)*
- [~] A plinth fallback that can never fail *(prefab, silent routing and pinch-only offer verified in
  the editor; a dark room and a real lap/bed/tray owed)*
- [ ] Passthrough and FoV footage for the video *(neither clip exists; the toggle and the safe-cone
  draw are built)*

## Gate
[`gate.md`](gate.md) — Kill the app, relaunch — is the shelf still on the same real surface, with the same objects?

## Traps specific to this phase
- Rendering a beautiful shelf that costs 4 ms of GPU — it is 12 small objects, keep it cheap
- Treating the fallback as a rare path and not testing it

## Related documents
- [`docs/02-tech/MRUK-SHELF.md`](../../docs/02-tech/MRUK-SHELF.md)
- [`docs/01-idea/COMPETITION-FIT.md`](../../docs/01-idea/COMPETITION-FIT.md) — FoV and passthrough rows
- [`docs/02-tech/TECH-STACK.md`](../../docs/02-tech/TECH-STACK.md) §4

---
*Part of the [master plan](../../docs/00-plan/MASTER-PLAN.md).*
