# unity/

Unity project root: open this folder with Unity **6000.3.28f1**. Packages are pinned in `Packages/manifest.json`; `Assets/_Project/Scenes/Main.unity` is the main scene. Follow [SETUP.md](SETUP.md), and see [the repository audit](../docs/05-build/REPOSITORY-AUDIT.md) for unverified changes and required device tests.

## Non-negotiables
- `Library/`, `Temp/`, `Build/`, `Logs/` are gitignored. Never commit them.
- The Meta XR SDK is imported once at a pinned version (**v207**). Record the exact package versions in `docs/02-tech/TECH-STACK.md` when you pin them.
- `StreamingAssets/Signs/*.json` is the only place pose data lives.
- Every commit must leave `main` buildable.
