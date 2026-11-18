# Repository audit — static review and regression fixes

**Scope:** cloned GitHub snapshot at `de3c160`, followed by the changes in this working tree. This audit is not a certification that every code path works. Unity, its package cache, the Quest headset, and external signer/submission records were not available in this environment. Prior project reports of Unity checks are historical claims; they were not rerun here.

## Automated coverage

- Scanned **192 Markdown files** and 279 explicit Markdown links in the cloned repository. One broken local link was repaired; the scan is not a semantic review of every historical statement, code-fenced path, image, or anchor.
- Parsed all **109 current project C# files** with tree-sitter (no syntax errors); this is **not** a C# compile or Unity API check. All 35 current Python files compiled as Python source; all 45 Unity JSON files parsed.
- Unity asset `.meta` GUIDs had no duplicates. The initial scan found 13 serialized GUIDs not under `Assets`; these may be package assets and must be resolved after Unity downloads packages, not guessed or rewritten. `Assets/.gitkeep` is a placeholder, not a Unity asset.
- Pose validator: 34 shape files structurally valid; synthetic self-test 29/29. These do not establish sign accuracy or real tracking. Alias tables agree (61 spellings).
- Static persistence contract 4/4; shelf wiring 6/6; copy lint 0 violations; trackability consistency checker 0 violations with `measured: false`; submission counter 118/140 characters and 499/500 words. Simulators for shelf, budget, bands, hold, near miss, mirror, palm, false positives, pause, matcher and payoff ran with exit 0. Simulated output is not device evidence.
- New automated regression checks: `python3 scripts/test_repo_integrity.py` and `python3 scripts/test_trackability_import.py`. Run the Unity menu `SIGNSPACE/Audit/Check state change events` after importing; this editor check was **not** run here.

## Changes made

| Priority | Finding | Action | Verification and caveats |
|---|---|---|---|
| High | `MrUkSurfaceFinder` and `AppStateMachine` MonoBehaviour class names did not match their source file names. The main scene used an embedded `MonoScript` for the finder. | Renamed source and `.meta` files while preserving GUIDs; changed finder reference in `Main.unity` to its external script asset; updated path consumers. | Static GUID/name checks pass. **Reimport and open the scene in Unity**, and confirm neither component is missing. |
| High | MRUK v207 `LoadSceneFromDevice` has a third optional `SceneModel` parameter. Reflection searched for methods with only 0–2 parameters, so it could miss the documented overload. | Use the pinned v207 direct call with optional third argument; default scene-capture request to false in code, main-scene serialized value and product builder. | Meta v207 API reference checked; tree-sitter only. Compile and test actual permissions, load timing and system prompts on Quest. An MRUK prefab's own auto-load settings can still differ. |
| Medium | Missing offer visuals could cause a null dereference; an offer could remain after its surface disappeared or the save became protected. | Hide an orphaned visual, clear offers on polling when unavailable, and revalidate eligibility before accepting. | Static checks only; actual race and room transitions still need a Unity integration test. |
| Medium | `AppStateMachine.Back()` raised `(new, old)` while `Go()` raised `(old, new)`. | Use `(Previous, Current)` consistently; add editor check. | Static assertion passes; new editor test not executed. |
| High | Partial probe import could write a file that says `measured: false` but contains measured rows; malformed or missing values could be treated as zero. Repeats in one condition were not distinguishable from a single hold in the checker. | Reject invalid/missing rows, incomplete input and unknown IDs without writing; store `holdCount` and observed min/max; validate borderline repeats; label low lock rate `tracks-never-locks` rather than assuming camera visibility; generate doc membership from `Signs/index.json`. | Seven synthetic regression checks pass. Input completeness alone does not prove device provenance, true handshape correctness, wrong-shape behaviour, sufficient condition coverage, or signer review. |
| Medium | The unmeasured panel said cameras *cannot* see design-excluded shapes; measured text claimed “robustly trackable” based on lock fraction alone. | State that exclusions are by design pending measurements; measured text says “met the lock-rate threshold.” Synced current data checker and submission count line. | Static consistency and copy-length checks pass; per-shape explanatory notes still need signer and device review. |
| Low | A P5.7 relative link was wrong; top-level project and Unity READMEs said nothing had been built; setup named a nonexistent matcher parameter. | Repaired the link, refreshed the README/setup text, and corrected the P9 version-code statement. | Link scan passes; historical documents may still describe past states. |

## Open findings — require an integrated change or real-world evidence

1. **P4 first-frame / migration:** `ShelfAnchor.Begin` waits for MRUK readiness or timeout before `UsePlinth`, and `PollNow` can automatically restore a late saved home. This conflicts with P4.4's immediate plinth and never-auto-migrate acceptance. The existing editor check directly invokes `UsePlinth`, which does not test startup. Rework startup, offer/acceptance and protected-home semantics together; update P4.3/P4.4 integration tests and validate on device. Do not mark G4 green.
2. **Signer and content:** the repository's `set-meta.json` says reference-authored and pending review, and its G5 is open. Signed permissions, agreed fee/credit, actual signer's review and capture may exist outside Git but were not provided to this audit. Never turn schema validity into a claim of signer approval.
3. **Trackability provenance:** `ProbeController` JSONL does not establish hardware source or signer approval. The importer cannot detect a fabricated or simulator-generated log, and currently averages recorded lock fractions across supplied conditions. It marks the data measured with one hold per shape, even when some hand/lighting cells are absent; it also defaults to today's import date, Quest 3S and normal light rather than deriving provenance from the logs. Operators must supply the actual `--date`, `--device` and `--conditions` and verify the full planned coverage before release. Review raw logs, wrong-shape/dropout behavior and coverage before using any resulting number as a release decision. Classification below the threshold alone cannot identify the cause.
4. **Build version:** `BuildScript.cs` hardcodes Android version code 14; `version-code.txt` contains 13. There is no auto-increment in the inspected build path. Coordinate the next code with the actual release channel rather than assuming either number is safe to upload.
5. **All device gates:** verify P4 placement/persistence, passthrough, accessibility, full hands/SDK confidence, frame time and actual playtests with the specified hardware. Unity import, scene/prefab references, build, SDK permissions and the three external-package reference categories above are also unverified here.

## Re-run

```bash
python3 scripts/test_repo_integrity.py
python3 scripts/test_trackability_import.py
python3 scripts/test_persistence_contract.py
python3 scripts/test_shelf_wiring.py
python3 scripts/validate_poses.py --self-test
python3 scripts/validate_poses.py
python3 scripts/check_alias_tables.py
python3 scripts/check_trackability.py
python3 scripts/lint_copy.py
python3 scripts/count.py
```

`nearmiss_sim.py` writes `phases/p3-feedback/near-miss-test.csv` when run. Use a clean tree or preserve the file before running it; its generated output is simulation, not a live user test.
