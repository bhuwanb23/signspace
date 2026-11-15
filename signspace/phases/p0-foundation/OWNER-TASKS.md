# OWNER TASKS — the only things that need a human

**Owner:** you · **Written:** 15 Nov 2026 (Day 1) · **Task:** P0.3, P0.4, P0.5, P0.7, P0.8, P0.9 → **gate G0**
**Companion to:** [`toolchain.md`](toolchain.md) (settings) · [`accounts.md`](accounts.md) (IDs) · [`devices.md`](devices.md) (P0.7 artefact)

Everything in this file needs an account, a physical headset, or another person's calendar.
**Everything else in P0/P1 is being handled in parallel by the build agent.**

---

## 0. THE THREE-LINE VERSION

If you only have ten minutes, do these three, in this order:

| # | Do this | Why it is first |
|---|---|---|
| **1** | [Accept the Unity Asset Store EULA](#a) | It gates the Meta XR SDK download, which gates the whole build |
| **2** | [Sign in to Unity Hub](#b) | Activates your Personal licence — batch-mode builds refuse to run without it |
| **3** | [Contact the signer](#e) | The only task that can still end this project |

Everything else on this page can happen after those three.

---

## A. Unity Asset Store EULA — 5 min

**Why:** the Meta XR SDKs are distributed through the Unity Asset Store. The store will not serve a
package to an account that has never accepted the Asset Store EULA, and the SDK's "Add to My Assets"
button silently does nothing when the EULA is unaccepted. Meta's own docs call this out: the 404s in
the browser console are the symptom.

| Step | Where | What |
|---|---|---|
| A1 | [id.unity.com](https://id.unity.com/) | Sign in to your existing Unity account |
| A2 | [Asset Store](https://assetstore.unity.com/) → any **free** asset → **Add to My Assets** | This triggers the EULA prompt |
| A3 | The prompt → **Accept** | Licences: **Unity Personal** (free, covers us) and the **Asset Store EULA** |
| A4 | [Meta XR publisher page](https://assetstore.unity.com/publishers/25353) | Confirm you can see Meta's packages and the **Add to My Assets** button works |

> **Do not** add the packages to My Assets yet — the build agent will do that from
> `Packages/manifest.json` so the version is pinned to `com.meta.xr.sdk.all` **207.0**.
> If you add it manually you may end up on a different patch.

**Done when:** A4 shows the Meta XR All-in-One SDK with a working **Add to My Assets** button.
**Send back:** nothing. Just say "EULA done".

---

## B. Sign in to Unity Hub + activate the licence — 5 min

**Why:** Unity 6 will not run a batch-mode build without an activated licence. The Hub activates a
Personal licence automatically once you are signed in with a verified account.

| Step | Where | What |
|---|---|---|
| B1 | `E:\unity\Unity Hub\Unity Hub.exe` | Launch the Hub (**already installed** — version 3.22.2) |
| B2 | ⚙️ **Preferences → Account** | Sign in with the same Unity account as A1 |
| B3 | ⚙️ **Licenses** | Should show **Personal — active**. If it shows "No licence", click **Add → Get a free Personal licence** |
| B4 | ⚙️ **Locations** | Set **Unity Editors** install location to `E:\unity\Editors` (the agent is also setting this via CLI — if you see it already correct, leave it) |

**Why the licence matters:** `C:\ProgramData\Unity\` is currently **missing** on this machine — there is
no `.ulf` file at all, so nothing has ever been activated here. Until this is done, no build can run.

**Send back:** a screenshot of the Licenses page showing Personal active, OR just "licence active".

---

## C. Meta developer dashboard — app + `Competition` channel — 20 min

**Why:** the deliverable is *"APK on a release channel named exactly `Competition` + an Invite URL"*.
Meta states plainly: **"APKs will not be considered."** A wrong or reused channel name invalidates the entry.

🔗 **[developer.oculus.com](https://developer.oculus.com/)** → *My Applications* → *Create App*

| Step | Field | Value | Notes |
|---|---|---|---|
| C1 | App name | `SIGNSPACE` | |
| C2 | **Package name** | `com.<yourorg>.signspace` | **IRREVERSIBLE.** You pick the org. Lowercase, no spaces, no underscores. Tell the agent the exact string so it goes in `toolchain.md` §1 **and** `accounts.md` §5 |
| C3 | Platform | **Quest / Horizon OS** | |
| C4 | Category | Productivity (or Education if forced) | |
| C5 | Age rating | 10+ | non-violent, non-social, no UGC |
| C6 | Release channel | create one named exactly **`Competition`** | **Case-sensitive.** Not "competition", not "Competition 2026" |
| C7 | Release channel | create a second one named `Internal` | keeps `Competition` clean for the real build |
| C8 | Upload | any throwaway APK to `Competition` | proves the path works before you need it |

**Send back:**
```
package name = com.____________.signspace
app id       = ____________
channels     = Competition, Internal
```

---

## D. Quest 3 — get the hardware today

**Why:** a Quest 3 (or 3S) is required for four things that nothing else can substitute:
G0's *"installs from the `Competition` channel"* criterion · the **P1.5 twenty-shape trackability probe** ·
the **72 fps measurement** on real silicon · the **P8 playtest**.

The good news: everything else — building, the poses, the code, the FoV and gaze behaviour — is
verifiable on this PC through **Meta XR Simulator**, and the competition explicitly accepts
**XR Simulator footage** in the video.

| Step | What | Notes |
|---|---|---|
| D1 | Confirm you can borrow a **Quest 3** from your friend — and for how long | Ask for **Tuesday and Wednesday** specifically. Day 3 needs it (the signer review + playtest); Day 4 needs it for the video |
| D2 | If the friend has it, get the **device to you** by Monday evening | Wireless ADB over the same Wi-Fi works — no need for a second PC |
| D3 | If no loan: **buy or order today.** A Quest 3S is the better buy (it is the weaker judging target) | Do not skip this. It is the long pole |

> ⚠️ **Two facts you should not be surprised by later:**
> 1. **Quest 3 has no eye tracking** — same as the 3S. Gaze evidence comes from the VR Glasses
>    simulator profile. The shipped device path will be an honest dwell/head-gaze fallback.
> 2. If you only ever get a **Quest 3** and never a **3S**, you cannot verify the 72 fps floor on the
>    weaker judging device. We will then hold to 3S-conservative settings and **say so explicitly** in
>    `PERFORMANCE.md` and the description. That is defensible; quietly assuming it is not.

**Send back:** which headset, from whom, and on which day you will physically have it.

---

## E. The signer — contact them TODAY

**Why:** this is the project's only fatal risk. Everything else is recoverable by working later;
this one depends on **another person's calendar**. `RISKS.md` R1 is explicit: a sign-language product
built without Deaf participation is the one version of this idea that fails badly.

**The good news — the plan changed so you only need ONE call, not two.**
The old protocol needed the signer to record their hands first, *then* us to author 3D poses from that
recording (~2 h of work), *then* a second call for the signer to approve what we authored. That
sequence does not fit in four days.

**The new sequence:** the poses are authored **now, from published ASL references, with no signer
involved**. The signer then does a **single** approval call: consent → credit wording → holds each of
the six shapes on camera (10 s each, two angles) as ground truth → says yes/no to our authored poses.
If they reject a shape, we re-author it and send them a video of the re-made one asynchronously — no
second synchronous call needed.

### The message to send (adapt and send today)

> Hi — I'm building a small VR practice app for ASL handshapes for a Meta developer competition and
> I'd really value a Deaf signer's eyes on it before I submit.
>
> **What it is:** six fingerspelling handshapes (B, U, V, W, C, O) as 3D reference hands you can
> rotate and copy with your own hand. It gives feedback on *which* sign you accidentally made. It is
> practice software, not a course, and not a replacement for a Deaf teacher — and the app says so.
>
> **What I need from you (~45 min on a video call):**
> 1. Consent to use the shapes in a public competition entry (and to credit you by name and city).
> 2. You hold each of the six shapes for ~10 seconds, front-on and turned ~30°, so I have a ground truth.
> 3. I show you each 3D handshape I built and you tell me yes/no — is this the correct shape?
> 4. We agree the exact credit line.
>
> **What you get:** your name and city in the app's credits, and a link to the submitted entry.
> I can also send you the build to try first if that's more interesting.
>
> **Timing:** I need the call by **Monday evening, 16 Nov** at the latest — the submission deadline is
> Wednesday 18 Nov. Could you offer two or three slots this weekend or Monday?
>
> Would you be up for this? Completely fine if not — just say so and I'll ship it with the credit line
> saying the shapes were authored from published ASL references and that Deaf signer review is in progress.

### The booker-side checklist

| Step | What | Done when |
|---|---|---|
| E1 | Send the message above to **≥3 people** | 3 sent |
| E2 | Ask each for **2–3 specific slots** before Mon 16 Nov 18:00 IST | Slots offered |
| E3 | Record the outcome here | Below |
| E4 | Send the **consent + credit wording** for a wet signature or typed confirmation | Consent form sent |
| E5 | If **nobody replies by Mon 16 Nov 18:00 IST** → tell the agent | Ship state B, say so |

### Signer tracker

| Candidate | How you know them | Contacted | Slots offered | Confirmed | Consent sent |
|---|---|---|---|---|---|
|  |  | ⬜ | ⬜ | ⬜ | ⬜ |
|  |  | ⬜ | ⬜ | ⬜ | ⬜ |
|  |  | ⬜ | ⬜ | ⬜ | ⬜ |

> **If the call slips or fails entirely, the fallback is already built:** `set-meta.json` ships in
> state B — *"Handshapes authored against published ASL references. Deaf signer review in progress."*
> That is a **weaker claim, not a false one**, and it is one JSON file to flip if a signer does approve.
> What is not acceptable is shipping state B while implying state A. The description and the video
> must match whichever state ships.

---

## F. Devpost draft — 20 min

🔗 **[start-developer-competition-26.devpost.com](https://start-developer-competition-26.devpost.com/)**

| Step | What | Notes |
|---|---|---|
| F1 | Log in with the account that holds Start membership | This is the account that submits |
| F2 | Create the entry, name it `SIGNSPACE` | |
| F3 | **Save the draft** and note the submission ID | The ID goes in `accounts.md` §5 |
| F4 | Paste the tagline and description from [`docs/06-submission/SUBMISSION-COPY.md`](../../docs/06-submission/SUBMISSION-COPY.md) | Tagline 118/140 chars · description 494/500 words — **already verified by `scripts/count.py`** |
| F5 | Leave everything else for P9 | The form map is in [`CHECKLIST.md`](../../docs/06-submission/CHECKLIST.md) §1 — field by field |

**Send back:** `devpost submission id = ____________`

---

## G. Invite URL verification — 20 min — *after C and D*

**Why:** this is the single most common disqualification. A broken Invite URL means the judge cannot
launch the app, and the whole entry scores zero.

| Step | What | Pass |
|---|---|---|
| G1 | Build is uploaded to the `Competition` channel | |
| G2 | Generate the **Invite URL** for that channel | |
| G3 | Open it in a **private/incognito window, signed OUT of Meta** | Must not prompt for a Meta login to view the page |
| G4 | Open it on a **phone on mobile data** (not the build PC's Wi-Fi) | The install prompt must appear |
| G5 | Install it on the Quest 3 | The app name must read `SIGNSPACE`, **not "Untitled"** |
| G6 | Launch it **with controllers removed from the runtime** | It must still run |

**Send back:** the Invite URL + a screenshot of the install prompt → `evidence/p0.5-invite-url-2026-11-15.png`

---

## H. Sign-offs — 2 min each

### P0.8 — Definition of Done

Read [`docs/05-build/DEFINITION-OF-DONE.md`](../../docs/05-build/DEFINITION-OF-DONE.md), then paste:

```
DoD signed: I accept the Definition of Done as the contract for what "done" means in this build,
including that a submitted build has no failure state, no percentage score, no SDK type inside Matcher,
and no commit after submission.
```

### P0.8 — cut ladder

Read the cut ladder in [`MASTER-PLAN.md` §6](../../docs/00-plan/MASTER-PLAN.md), then paste:

```
Cut ladder confirmed: I confirm the cut order in MASTER-PLAN §6 — cut from the bottom, never skip a
rung, and never cut the near-miss, the honest mirror, gaze highlight, the shelf, signer credits, or the
accessibility toggles.
```

---

## I. What to hand back — one message

Paste this once at the end, and fill the files it points at.

```
PACKAGE / APP
  package name   = com.____________.signspace
  app id         = ____________
  channels       = Competition, Internal
  devpost id     = ____________
  invite url     = ____________

TOOLCHAIN (fill these in too — the agent cannot read them off your machine)
  Unity version  = ____________
  Meta XR SDK    = ____________   (com.meta.xr.sdk.all, should be 207.0)
  graphics API   = ____________   (Vulkan or GLES3 — must match the fps number)
  Android SDK/NDK= ____________
  hands+passthrough on headset = yes/no
  fps (empty scene, hands + passthrough on) = ______

JOINTS  ← the single most useful thing you can bring back
  joint count    = ______
  joint names in SDK order = ____________________
  (the skeleton prints these; SETUP.md §4 says paste them into the Matcher)

HARDWARE
  headset            = ____________
  headset model/OS   = ____________
  who has it, from when = ____________
  eye tracking       = no (Quest 3 / 3S have none)
  room + lighting for testing = ____________

SIGNER
  booked?  = yes/no · slot = ____________ · consent sent = yes/no

SIGNOFFS
  DoD signed      = yes
  cut ladder confirmed = yes
```

---

## J. URLs — everything in one place

### Install / SDK
| What | Link | You need this? |
|---|---|---|
| Unity Hub | `E:\unity\Unity Hub\Unity Hub.exe` | already installed — no |
| Unity releases & support dates | [unity.com/releases/unity-6/support](https://unity.com/releases/unity-6/support) | reference |
| Unity editor archive | [unity.com/releases/editor/archive](https://unity.com/releases/editor/archive) | reference |
| Unity ID / sign in | [id.unity.com](https://id.unity.com/) | **yes** — B2 |
| Unity Asset Store | [assetstore.unity.com](https://assetstore.unity.com/) | **yes** — A2 |
| Unity Asset Store EULA | [unity.com/legal/as-terms](https://unity.com/legal/as-terms) | read it |
| Meta XR on the Asset Store | [assetstore.unity.com/publishers/25353](https://assetstore.unity.com/publishers/25353) | **yes** — A4 |
| Meta XR All-in-One SDK **v207** | [developers.meta.com/vr/downloads/package/meta-xr-sdk-all-in-one-upm/207.0](https://developers.meta.com/vr/downloads/package/meta-xr-sdk-all-in-one-upm/207.0) | reference |
| Meta XR Core SDK v207 | [developers.meta.com/horizon/downloads/package/meta-xr-core-sdk](https://developers.meta.com/horizon/downloads/package/meta-xr-core-sdk) | reference |
| Meta XR Simulator (Windows) 207.1 | [developers.meta.com/vr/downloads/package/meta-xr-simulator-windows/207.1/](https://developers.meta.com/vr/downloads/package/meta-xr-simulator-windows/207.1/) | **yes** — click Download |
| Meta XR Simulator → Unity guide | [developers.meta.com/vr/documentation/unity/xrsim-getting-started](https://developers.meta.com/vr/documentation/unity/xrsim-getting-started/) | reference |
| Meta VR Glasses support (FOV sim) | [developers.meta.com/vr/documentation/unity/unity-support-meta-vr-glasses](https://developers.meta.com/vr/documentation/unity/unity-support-meta-vr-glasses) | reference |
| Meta VR CLI + agent skills | [developers.meta.com/horizon/downloads/unity/](https://developers.meta.com/horizon/downloads/unity/) | agent handles |
| Meta agentic tools (skills) | [github.com/meta-quest/agentic-tools](https://github.com/meta-quest/agentic-tools) | agent handles |
| Unity Hub CLI reference | [docs.unity.com/en-us/hub/use-hub-cli](https://docs.unity.com/en-us/hub/use-hub-cli) | agent handles |

### Competition
| What | Link | You need this? |
|---|---|---|
| Devpost entry | [start-developer-competition-26.devpost.com](https://start-developer-competition-26.devpost.com/) | **yes** — F1 |
| Devpost FAQ | [/details/faqs](https://start-developer-competition-26.devpost.com/details/faqs) | reference |
| Competition rules | [/rules](https://start-developer-competition-26.devpost.com/rules) | reference |
| Meta developer dashboard | [developer.oculus.com](https://developer.oculus.com/) | **yes** — C1 |
| Meta VR Start program | [developers.meta.com/horizon/resources/meta-start-developer-program](https://developers.meta.com/horizon/resources/meta-start-developer-program/) | already done — P0.1 |
| Meta Horizon Start forum (membership flair) | [communityforums.atmeta.com](https://communityforums.atmeta.com/t5/Developer-Forums/ct-p) | already done — P0.1 |

---

## K. Where each answer goes

The repo rule is **"if it isn't here, it didn't happen."** So each answer also lands in a file.

| Your answer | File | Where |
|---|---|---|
| Package name | `toolchain.md` §1 **and** `accounts.md` §5 | two places — it is irreversible |
| App id, channel names | `accounts.md` §5 | table |
| Unity / SDK / graphics versions | `toolchain.md` §2 "Record here" | table |
| fps, first-build result | `toolchain.md` §2 "Record here" | table |
| Headset, owner, room, lighting | **`devices.md`** (new file) | P0.7 artefact |
| Devpost id | `accounts.md` §5 | table |
| Invite URL | `accounts.md` §5 | table |
| Screenshots | `evidence/p0.3-competition-channel-2026-11-15.png`, `evidence/p0.5-invite-url-2026-11-15.png` | naming rule in `evidence/README.md` |
| Signer outcome | `phases/p5-content/signer-tracker.md` | new file |
| Sign-offs | `phases/p0-foundation/tasks/p0.8-risks-and-dod.md` | `**Status:** ✅ DONE — 15 Nov 2026, owner-confirmed` |
| Task completion | each `tasks/pN.M-*.md` | `**Status:**` line |
| Gate verdict | `phases/p0-foundation/gate.md` | Decision record table |
| Phase progress | `docs/00-plan/MASTER-PLAN.md` §8 | status board |

> 🔒 **Never** write a password, a 2FA code, a recovery code, or the release keystore password into
> this repo. IDs, emails and names only. Put the keystore password in a password manager now — you
> need it at P9.1 to freeze the build.

---

*Part of the [master plan](../../docs/00-plan/MASTER-PLAN.md). P0 → G0.*
