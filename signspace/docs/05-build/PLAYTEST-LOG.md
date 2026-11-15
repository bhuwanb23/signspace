# Playtest Log
**Status:** ✅ v1 (template) — **filled on Day 9, and it includes the failures** · **Owner:** W1 · **Phase:** P8 · **Gate:** G8

> **The friction log is the only honest measure of whether the design works.** It is written on Day 9
> and it includes the failures — including the ones we did not fix. A log with only the good parts
> is a press release.

---

## 1. The method

**Three strangers. Three different hands. Nobody helped.**

| Rule | Why |
|---|---|
| **Uncoached** | the moment we explain something is the moment we find out the design is unclear. We do not explain |
| **Recorded** | we watch it again afterwards, and so does the tester |
| **At least three different hand sizes** | a shape app that only works on one hand is not a shape app |
| **One tester with limited dexterity, if we can find one** | `ACCESSIBILITY.md` path 4 |
| **Their own room, their own light** | not our desk under a lamp |
| **We say nothing unless they ask a question twice** | and if they ask twice, we write it down as a friction row |

**We write down every moment a tester stops moving.** Not the moments we expected — the moments they
actually hesitated.

## 2. What we measure

| Metric | Target | Source |
|---|---|---|
| **Time to first lock** | **< 75 s** | the recording |
| Time to the first near-miss | < 30 s of play | the recording |
| Did the near-miss land? | the tester can say what it told them, unprompted | ask after, not during |
| Did the tester rotate the reference hand unprompted? | yes | the recording |
| Did they touch the shelf? | yes | the recording |
| Did they try anything we did not intend? | **list them all** | the recording |
| Where they stopped | every pause over two seconds | the recording |
| What they said | verbatim, especially the wrong words | the recording |

**"Did the near-miss land?"** is measured by asking the tester, after the session, *"what did the
app tell you when that happened?"* If they cannot say it in their own words, the copy failed, no
matter how good it looked on screen.

## 3. The tester profiles

**Filled on Day 9.**

| # | Name / initials | Hand size | Dominant hand | Dexterity notes | Lighting | Room | Own headset? |
|---|---|---|---|---|---|---|---|
| 1 | | | | | | | |
| 2 | | | | | | | |
| 3 | | | | | | | |

## 4. Session notes — verbatim

**Per tester. Verbatim quotes, especially the wrong words.**

### Tester 1
- **What they did first:**
- **What they said:**
- **Where they stopped:**
- **What they tried that we did not intend:**
- **What they did not notice:**

### Tester 2
- **What they did first:**
- **What they said:**
- **Where they stopped:**
- **What they tried that we did not intend:**
- **What they did not notice:**

### Tester 3
- **What they did first:**
- **What they said:**
- **Where they stopped:**
- **What they tried that we did not intend:**
- **What they did not notice:**

## 5. The friction log

**At least ten rows. Ranked by how many testers hit it.** A row nobody hit is not friction and does
not go in the table.

| # | Friction point | Testers hit | What happened | Severity | Fix | Shipped? |
|---|---|---|---|---|---|---|
| 1 | | | | | | ⬜ |
| 2 | | | | | | ⬜ |
| 3 | | | | | | ⬜ |
| 4 | | | | | | ⬜ |
| 5 | | | | | | ⬜ |
| 6 | | | | | | ⬜ |
| 7 | | | | | | ⬜ |
| 8 | | | | | | ⬜ |
| 9 | | | | | | ⬜ |
| 10 | | | | | | ⬜ |

**Severity:**

| | |
|---|---|
| **Blocker** | they could not complete the loop at all |
| **Major** | they completed it, but something was confusing or wrong |
| **Minor** | it was friction, not confusion |

## 6. The top three, fixed

**We fix exactly three.** `P8.4`: *"Fix the top three, and only the top three."* Not four, not
"as many as we can". The temptation to fix everything is how Day 9 becomes Day 10.

| # | Friction | The fix | How we know it worked | Shipped |
|---|---|---|---|---|
| 1 | | | | ⬜ |
| 2 | | | | ⬜ |
| 3 | | | | ⬜ |

**Everything else is recorded and not fixed.** That is not a failure — it is a backlog, and the
post-mortem is where it goes.

## 7. The re-test

A **fresh** stranger. Not one of the three — they have learned the app, and they are no longer
strangers.

| | Tester 4 (fresh) |
|---|---|
| **Time to first lock** | ⬜ seconds — **target: under 75** |
| Time to the first near-miss | ⬜ seconds |
| Did they rotate the reference hand unprompted? | ⬜ |
| Did the near-miss land? | ⬜ |
| Anything new? | ⬜ |

## 8. What we knowingly shipped broken

**This section is not optional. It is the most important part of the document.**

| Issue | Why it is still broken | What we would do with a day more |
|---|---|---|
| | | |

**A log that claims nothing is broken is not credible, and a judge can tell.** We are an AI team
shipping a sign-language product with a four-day build. There are things that are broken. Saying
which ones, and why we left them, is more convincing than pretending they are not there.

## 9. Accessibility and performance regression

**After every fix, the following still hold. A fix that breaks one of these is not a fix.**

- [ ] one-handed playthrough still completes
- [ ] eyes-only playthrough still completes
- [ ] the five toggles still work and still persist
- [ ] ≥72 fps still holds (or whatever the measured number is)
- [ ] no new crash in a cold launch
- [ ] the near-miss still fires for U→V

## 10. The verdict

| | |
|---|---|
| First lock, three strangers | ⬜ s / ⬜ s / ⬜ s — **target < 75 s** |
| First lock, fresh stranger | ⬜ s |
| Friction rows | ⬜ (minimum 10) |
| Top three fixed | ⬜ / 3 |
| Known broken, documented | ⬜ items |

**G8 is green when:** a stranger reaches their first mastered shape in under 75 seconds with nobody
helping, and the top three friction points are closed.

**If G8 is red, ship anyway** and write the honest friction log. A shipped entry with a known gap
and an honest log beats an unfinished one.

## 11. Inputs
`UX-STATES.md` · `FIRST-FIVE-MINUTES.md` · `ACCESSIBILITY.md` · `PERFORMANCE.md` ·
`VIDEO-SCRIPT.md` (the near-miss hook, which the playtest has to justify)

## 12. Output / done when
- [x] the method, the rules, and what we measure
- [x] the friction table structure, ranked by how many testers hit it
- [x] the "fix exactly three" rule
- [x] the re-test protocol with a fresh stranger
- [x] **the "what we knowingly shipped broken" section**
- [ ] **the log is filled**
- [ ] **the top three are closed**
- [ ] **the fresh stranger is under 75 seconds**
