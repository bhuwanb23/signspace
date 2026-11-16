# GATE G4 — Kill the app, relaunch — is the shelf still on the same real surface, with the same objects?

**Phase:** P4 · **Verdict:** ⬜ not yet evaluated

## Criteria — all must be true
- [ ] Shelf anchors to a real surface and faces the user
  *(selection + facing desk-verified; a real surface NOT RUN)*
- [ ] Objects spawn, settle and idle within budget
  *(budget PASS: 26 draw calls / 912 triangles worst case; device frame time NOT RUN)*
- [ ] Persistence survives 5 relaunches and 1 reboot
  *(5/5 relaunches PASS across separate processes; the reboot NOT RUN)*
- [ ] Plinth fallback appears instantly with no error state
  *(editor PASS: 4-part plinth, no Canvas, no error copy; dark room NOT RUN)*
- [ ] Matched passthrough on/off clips captured *(toggle built; no clips)*
- [ ] All 12 slots legible at the VR Glasses profile extent
  *(geometry PASS: 12 slots, widest object edge 22.2° inside the 27.5° half-cone; visual legibility
  NOT RUN)*
- [ ] **A judge can flip passthrough off and watch the experience lose its ground**

## If red
Persistent failure → make the plinth the permanent design and remove surface-dependence from the pitch (the claim weakens but does not break). Never ship a shelf that silently resets: a progress artefact that forgets is worse than no artefact at all.

## Decision record

| Field | Value |
|---|---|
| Verdict | ⬜ PASS / ⬜ FAIL |
| Date evaluated | |
| Evaluated by | |
| Evidence (screenshot / log / recording) | |
| Notes | Desk evidence complete for P4.1–P4.6 (see `persistence-matrix.md` and the task files); the verdict waits on a headset because five criteria are device/video rows. |
