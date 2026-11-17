# P8 — fixes

**Owner:** W1 · **Task:** P8.4 · **Phase:** P8 → gate G8 · **Written:** 17 Nov 2026

> Fix the top three. And only the top three.
>
> Not four. Not "as many as we can". The temptation to fix everything is how Day 9 becomes Day 10.

---

## 1. The rule

| Severity | Definition | Action |
|---|---|---|
| **Blocker** | they could not complete the loop at all | fix immediately, even if it is not in the top three |
| **Major** | they completed it, but something was confusing or wrong | candidate for the top three |
| **Minor** | it was friction, not confusion | record, do not fix |

**We fix exactly three.** Everything else is recorded and not fixed. That is not a failure — it is a
backlog, and the post-mortem is where it goes.

## 2. The top three

**Filled after the sessions.**

| # | Friction | Severity | Testers hit | The fix | How we know it worked | Shipped |
|---|---|---|---|---|---|---|
| 1 | | | | | | ⬜ |
| 2 | | | | | | ⬜ |
| 3 | | | | | | ⬜ |

## 3. The verification protocol

For each fix:

1. **Re-run the exact friction scenario** — the same tester, or a fresh one, doing the same thing.
2. **Check the metric moved** — time-to-first-lock, near-miss landing, stall count.
3. **Run `SIGNSPACE/P6 Check all` and `SIGNSPACE/P7 Check all`** after every fix. A fix that breaks
   a toggle, a toggle's persistence, or the frame budget is not a fix.
4. **Record the result** in the table above. "It felt better" is not a result. A number is.

## 4. What we knowingly shipped broken

**This section is not optional. It is the most important part of the document.**

| Issue | Why it is still broken | What we would do with a day more |
|---|---|---|
| | | |

**A log that claims nothing is broken is not credible, and a judge can tell.** We are an AI team
shipping a sign-language product with a four-day build. There are things that are broken. Saying
which ones, and why we left them, is more convincing than pretending they are not there.

## 5. Change log

| Date | Change | By |
|---|---|---|
| 17 Nov 2026 | File created. Template for the top three and the known-broken section. | agent |
