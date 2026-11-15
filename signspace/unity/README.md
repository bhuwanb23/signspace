# unity/

Unity project root. **Created at P0.4** — this folder is deliberately empty until then.

Until the project exists, the canonical layout lives in `docs/02-tech/TECH-STACK.md` §5. Do not scatter scripts here by hand; the folder structure is part of the design.

## Non-negotiables
- `Library/`, `Temp/`, `Build/`, `Logs/` are gitignored. Never commit them.
- The Meta XR SDK is imported once at a pinned version (**v207**). Record the exact package versions in `docs/02-tech/TECH-STACK.md` when you pin them.
- `StreamingAssets/Signs/*.json` is the only place pose data lives.
- Every commit must leave `main` buildable.
