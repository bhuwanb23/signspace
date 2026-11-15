# assets/

Binary and source media for SIGNSPACE. **Nothing large lives in git** — see `.gitignore`.

| Folder | Contents | Stored where | Status |
|---|---|---|---|
| `masters/` | Raw mocap takes from Deaf signers (FBX / BVH / CSV) | External drive or LFS — **not git** | Pending capture |
| `raw/` | Handheld video of the signing sessions (consent-cleared reference) | External, encrypted | Pending capture |
| `hand/` | The shipped reference hand rig (low-poly, 26 joints, skinned) | Git (small) | `[STUB]` |
| `audio/` | Spatial confirmation SFX, UI ticks | Git (small) | `[STUB]` |
| `brand/` | Wordmark, app icon, submission-video lower thirds | Git (small) | `[STUB]` |

**Rule:** any asset traced to a signer carries the signer's name, the capture date, and the consent form ID in `assets/masters/MANIFEST.md`.
