"""Generator for phases/ — one folder per master-plan phase.
Data files are `scripts/_phases_pN.py`; this module only knows how to render."""
import pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent


def T(pid, slug, title, est, deps, ws, why, steps, done, artefacts, traps):
    return dict(pid=pid, slug=slug, title=title, est=est, deps=deps, ws=ws,
                why=why, steps=steps, done=done, artefacts=artefacts, traps=traps)


def task_file(t, phase_id, gate):
    s = (f"# {t['pid']} — {t['title']}\n\n"
         f"**Phase:** {phase_id} · **Est:** {t['est']} · **Depends on:** {t['deps']} · **Workstream:** {t['ws']}\n"
         f"**Gate fed:** {gate}\n\n## Why this exists\n{t['why']}\n\n## Do this\n")
    for i, st in enumerate(t['steps'], 1):
        s += f"{i}. {st}\n"
    s += "\n## Done when\n" + "".join(f"- [ ] {d}\n" for d in t['done'])
    s += "\n## Artefacts produced\n" + "".join(f"- {a}\n" for a in t['artefacts'])
    s += "\n## Traps\n" + "".join(f"- {x}\n" for x in t['traps'])
    return s


def gate_file(phase_id, gate_no, question, criteria, red_plan):
    s = (f"# GATE G{gate_no} — {question}\n\n"
         f"**Phase:** {phase_id} · **Verdict:** ⬜ not yet evaluated\n\n"
         "## Criteria — all must be true\n")
    s += "".join(f"- [ ] {c}\n" for c in criteria)
    s += f"\n## If red\n{red_plan}\n\n## Decision record\n\n"
    s += ("| Field | Value |\n|---|---|\n| Verdict | ⬜ PASS / ⬜ FAIL |\n| Date evaluated | |\n"
          "| Evaluated by | |\n| Evidence (screenshot / log / recording) | |\n| Notes | |\n")
    return s


def phase_readme(ph, tasks, gate_q, objective, inputs, outputs, traps, budget, links):
    tid = ph['id']
    s = (f"# {tid} — {ph['name']}\n\n"
         f"**Day {ph['day']} · Budget {ph['hours']} h · Gate:** G{ph['gate_no']} — {gate_q}\n"
         "**Status:** ⬜ Not started\n\n## Objective\n" + objective + "\n\n## Why it sits here\n" + ph['why_here'] + "\n\n")
    s += "## Sub-phases\n\n| # | Task | Est | Workstream | Status |\n|---|---|---|---|---|\n"
    for t in tasks:
        s += f"| [{t['pid']}](tasks/{t['pid'].lower()}-{t['slug']}.md) | {t['title']} | {t['est']} | {t['ws']} | ⬜ |\n"
    s += f"\n**Budget:** {budget}\n\n## Inputs required before starting\n"
    s += "".join(f"- {i}\n" for i in inputs)
    s += "\n## Outputs — the phase is done when all of these are true\n"
    s += "".join(f"- [ ] {o}\n" for o in outputs)
    s += f"\n## Gate\n[`gate.md`](gate.md) — {gate_q}\n\n## Traps specific to this phase\n"
    s += "".join(f"- {t}\n" for t in traps)
    s += f"\n## Related documents\n{links}\n\n---\n*Part of the [master plan](../../docs/00-plan/MASTER-PLAN.md).*\n"
    return s


def write_phase(ph, tasks, gate_q, gate_criteria, gate_red, objective, inputs, outputs, traps, budget, links):
    base = ROOT / 'phases' / f"{ph['id'].lower()}-{ph['slug']}"
    (base / 'tasks').mkdir(parents=True, exist_ok=True)
    (base / 'README.md').write_text(
        phase_readme(ph, tasks, gate_q, objective, inputs, outputs, traps, budget, links))
    (base / 'gate.md').write_text(
        gate_file(ph['id'], ph['gate_no'], gate_q, gate_criteria, gate_red))
    for t in tasks:
        (base / 'tasks' / f"{t['pid'].lower()}-{t['slug']}.md").write_text(
            task_file(t, ph['id'], f"G{ph['gate_no']}"))
    return base


def phase_index(phases):
    """phases/README.md — the index of all phase folders."""
    s = ("# PHASES\n\nOne folder per phase of the [master plan](../docs/00-plan/MASTER-PLAN.md). "
         "Each folder holds a charter (`README.md`), a gate (`gate.md`), and one file per sub-phase under `tasks/`.\n\n"
         "**Rule:** a phase is only started when its predecessor's gate is green.\n\n"
         "| Phase | Day | Budget | Sub-phases | Gate | Folder |\n|---|---|---|---|---|---|\n")
    for p in phases:
        s += (f"| **{p['id']}** {p['name']} | {p['day']} | {p['hours']} h | {p['n_tasks']} | "
              f"G{p['gate_no']} — {p['gate_q']} | [`{p['id'].lower()}-{p['slug']}/`]({p['id'].lower()}-{p['slug']}/) |\n")
    s += ("\n## Critical path\n\n`P0 → P1 → P2 → P3 → P7 → P8 → P9 → SUBMIT`, with **P5 (signers)** running in "
          "parallel from Day 1 because it is the longest-lead item in the project and the only fatal risk.\n\n"
          "## How to work a phase\n\n"
          "1. Read the phase `README.md`. Check every input on the list is actually available.\n"
          "2. Work the `tasks/` files in order. Each one has a **Done when** list — do not proceed on a partial.\n"
          "3. Tick the phase outputs in the README as they complete.\n"
          "4. Run the gate in `gate.md`. Record the verdict, the date, and the evidence.\n"
          "5. Update the status board in `docs/00-plan/MASTER-PLAN.md` §8.\n\n"
          "## If a gate goes red\n\nUse the pre-agreed cut ladder (`MASTER-PLAN.md` §6) in order. "
          "Never invent a workaround; never skip a rung; never cut a never-cut item.\n")
    return s
