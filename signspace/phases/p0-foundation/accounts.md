# Accounts & Eligibility Record

**Owner:** you · **Created:** 7 Nov 2026 · **Task:** P0.1 → gate G0
**Status:** 🟢 **P0.1 COMPLETE** (owner-confirmed 14 Nov 2026) — membership, Developer Access and Devpost access all confirmed present. Specific IDs deliberately not committed.
**Repository rule:** IDs, emails and names **only**. Never a password, a 2FA code, a recovery code, or a keystore password. If this repo is ever made public, scrub this file first.

---

## 1. WHY THIS FILE EXISTS

G0 cannot be marked green without an archived screenshot proving Start membership on the account that will submit. That screenshot is worthless in three weeks unless someone wrote down **which account** it was, **when** it was checked, and **where the file lives**.

So this is not admin. It is the evidence locker for the first gate.

---

## 2. THE RULES, AS VERIFIED ON 7 NOV 2026

Read from the official rules and FAQ — not from memory, and not from a summary.

| Fact | Detail | Source |
|---|---|---|
| **Membership is mandatory** | *"THIS CONTEST IS OPEN ONLY TO MEMBERS OF THE META VR START PROGRAM"* | [Rules](https://start-developer-competition-26.devpost.com/rules) §preamble |
| **When it's required** | *"Representative or person submitting an Entry must be a member of the Meta VR Start Program **by the time of submission**"* | Rules §2(d) |
| **Late joiners are welcome** | *"I joined Start … after September 24, 2026. Can I still enter?"* → **"Yes.** As long as your Developer Access is enabled and you meet all other eligibility requirements by the time you submit" | [FAQ](https://start-developer-competition-26.devpost.com/details/faqs) |
| **Developer Access must be enabled** | *"have an active and valid Meta account with Developer Access enabled"* | Rules §2(c) |
| **Age** | 18+ and age of majority in your jurisdiction | Rules §2(a) |
| **Geography** | Not resident in Brazil, Quebec, or a sanctioned area (Crimea, Russia, Cuba, Iran, North Korea, Syria). **India is eligible.** | Rules §2(b) |
| **One entry per person** | An individual submits one Entry, solo or as a Team Representative | Rules §2 "Entry Limit" |
| **Funding conflict** | Apps *previously funded or currently in development with Meta financial support* (RFP etc.) are **not eligible**. We have none — recorded here so it can't be forgotten. | FAQ |
| **Review turnaround** | Start applications are reviewed **on a rolling basis**; Meta publishes **no SLA** | [Program page](https://developers.meta.com/horizon/resources/meta-start-developer-program/) |
| **Program benefits include** | *"Competition access: Participate in developer competitions and hackathons hosted through DevPost."* | Program page |

> **The one number that matters:** membership is required by **18 Nov 2026, 12:00 PT**, and approval has **no published turnaround**. Treat it as a five-business-day risk and resolve it today. "It's allowed until submission" is not the same as "it will be approved in time."

---

## 3. ELIGIBILITY SELF-CHECK — fill this in from the logged-in account

| # | Requirement | Status | Evidence / note |
|---|---|---|---|
| 1 | Meta account exists and is in good standing | ✅ | Owner-confirmed 14 Nov 2026 |
| 2 | **Developer Access enabled** on that account | ✅ | Owner-confirmed |
| 3 | **Start program membership active** | ✅ | Owner-confirmed ("I have all members, developers and everything") |
| 4 | Age ≥ 18 and age of majority (India) | ✅ | Self-declared |
| 5 | Country of residence eligible (not Brazil / Quebec / sanctioned) | ✅ | Chennai, Tamil Nadu, India — eligible |
| 6 | No Meta financial support (RFP / funded programme) attached to this project | ✅ | Declared: none |
| 7 | Devpost account exists and is the one that will submit | ✅ | Owner-confirmed |
| 8 | Devpost email == or is linked to the Meta developer account email | ✅ | Owner-confirmed linked |

> **Evidence note.** No screenshot is committed for rows 2–3. The owner confirmed all three accounts are in place. If the repository is ever audited or handed over, capture `evidence/p0.1-start-status-<date>.png` at that point — it is a 30-second job now and an unrecoverable one after the account context changes.

**If all eight are ✅, P0.1 is done.** If any is ❌, go to §6.

---

## 4. MEMBERSHIP STATUS — what to look for

There is no single canonical "badge" URL that we can hard-code, because Meta reorganises the dashboard. Instead, accept **any of these three** as proof, strongest first:

| Priority | Evidence type | Where to find it |
|---|---|---|
| **A** | Program membership status page showing active / "member" state | The Start program page while signed in with the developer account: `developers.meta.com/vr/programs/start/` → application/status area |
| **B** | Acceptance or welcome email from Meta confirming Start membership | Your inbox — search `Meta Horizon Start` / `Start program` / `developers.meta.com` |
| **C** | Devpost entry form letting you proceed past the Start-membership eligibility confirmation | The competition's own form is an indirect but real check |

**Archive the strongest one you have** as `evidence/p0.1-start-status-<YYYY-MM-DD>.png`, redacting anything you don't want in git (email is fine to keep; a phone number or address is not).

---

## 5. THE LINKAGE TRAP

The most common failure here is not missing membership — it is **membership on an account you won't be submitting from**.

Three things must be true of the *same* identity:

1. The **Meta developer account** that holds Start membership.
2. The **Devpost account** that will submit the entry.
3. The **Meta developer organisation** that will own the app, the `Competition` release channel and the package name (P0.3).

| Link | Recorded value |
|---|---|
| Meta developer account email | *(withheld from the repo by choice)* |
| Meta organisation name | *(withheld)* |
| **Meta app ID** | **`1444571625411296`** |
| **Package name** | **`com.signspace.signspace`** |
| **Release channels** | **`competition`** · **`internal`** — see the case warning below |
| Devpost account email | *(withheld)* |
| Devpost username / profile URL | |
| Devpost submission ID | *(from P0.2)* |
| **Are the Meta and Devpost accounts the same email?** | ✅ yes — owner-confirmed 14 Nov |

### ⚠️ The package name and the channel name are two different risks

**Package name — `com.signspace.signspace`** · fixed 15 Nov 2026 · **irreversible after the first upload.** It must match `PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, …)` exactly, which `BuildScript.cs` reads from `unity/Assets/_Project/package-name.txt`. That file now exists with exactly this string. Do not edit one without the other.

**Channel name — `competition` (lowercase, as created).** The Official Rules and the *What to Submit* section both say a channel **named "Competition"**, capital C. Meta's channel names may or may not be case-sensitive in their system, but the risk is asymmetric: renaming is a two-minute job now and impossible to explain on 18 Nov. **Action: rename the channel to `Competition` if the dashboard allows mixed case; if it refuses, screenshot the refusal and record it here.** Either way, keep `Internal` for test builds.

**If they differ:** that is allowed, but write down *why* and make sure you can still receive Devpost mail at the submitting address. Two addresses is a maintenance cost on Day 11; one address is not.

---

## 6. IF MEMBERSHIP IS MISSING OR PENDING

Do not proceed to P0.2–P0.4 and hope. Run this tree **today**.

| Situation | Action | Time |
|---|---|---|
| **Never applied** | Apply now: `developers.meta.com/vr/programs/start/apply/` → sign in with the Meta developer account → complete the form with your developer background and project details → submit | 15 min |
| **Applied, pending** | Do nothing but record: application date, any application/reference ID, and the date you'll escalate | 5 min |
| **Applied and rejected** | Re-read the eligibility criteria (18+, good standing, eligible country, Developer Access enabled). Correct the gap and re-apply the same day | 20 min |
| **Developer Access not enabled** | Enable it in the developer dashboard first — membership is meaningless without it | 10 min |
| **No Meta developer account at all** | Create one, verify, enable Developer Access, *then* apply to Start. This is the worst case: budget a day, not an hour | 2–4 h |

**Escalation clock**

| Date | If not yet a member |
|---|---|
| **Sat 7 Nov (today)** | Apply. Log it below. |
| **Mon 9 Nov** | Email Devpost support (`support@devpost.com`) — they answer eligibility questions — and ask whether a pending application is acceptable at submission with proof of application |
| **Wed 11 Nov** | If still pending: contact Meta developer support; keep every reply |
| **Fri 13 Nov** | **Hard decision point.** If membership is still not active, the risk of submitting an ineligible entry is now larger than the risk of switching concept. Escalate to the fallback-concepts decision noted in `RISKS.md` R1 — not because the concept changed, but because the *entry* might not be valid. |

**Application record** *(fill from §6)*

| Field | Value |
|---|---|
| Applied? | ⬜ yes / ⬜ no / ⬜ already a member |
| Application date | |
| Reference / application ID | |
| Expected decision | *(Meta publishes no SLA — this is your estimate)* |
| Escalation contacted (date + who) | |
| Outcome | |

---

## 7. CHANGE LOG

| Date | Change | By |
|---|---|---|
| 7 Nov 2026 | File created; rules and program page read and quoted from source; eligibility self-check and linkage trap added | agent |
| 14 Nov 2026 | Owner confirmed Start membership, Developer Access and Devpost account all in place. §3 all ✅. **P0.1 closed.** Specific emails/IDs withheld from the repo by choice | owner |
| | | |
