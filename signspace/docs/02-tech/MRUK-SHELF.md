# MRUK Shelf

**Status:** `STUB` — not yet written · **Owner:** W2 · **Phase:** P4 · **Gate:** G4

## Why this document exists
The shelf is what turns a session into a habit, and MRUK is the named technology that lets it live on the user's actual furniture.

## Questions this must answer
- Which MRUK APIs, and what triggers detection?
- What happens on a lap, a bed, a plane tray, or an unmade room?
- How is the anchor persisted across sessions and across rooms?
- What is the fallback, and how does it avoid ever showing an error?
- How does the shelf reflow at 70°×66°?

## Planned outline
1. Surface detection & anchor lifecycle
2. Shelf layout & placement rules
3. Object spawn/settle animation
4. Persistence schema and room-matching
5. Plinth fallback
6. FoV reflow
7. Test matrix: desk / lap / bed / floor / no surface

## Inputs
`TECH-STACK.md`, FoV targets from `COMPETITION-FIT.md`

## Output / done when
Kill and relaunch restores the shelf on the same real surface, and every row of the test matrix behaves.
