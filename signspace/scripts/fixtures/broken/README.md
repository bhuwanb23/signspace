# Deliberately broken pose fixtures

These files are **invalid on purpose**. They are not content, they are the test that proves the
validator bites.

`scripts/validate_poses.py --self-test` reads each one and asserts it is **rejected**. If a fixture
ever starts passing, the validator has lost a rule and the self-test fails loudly — which is the only
way to know a validator still works after someone edits it.

| File | What is wrong | P2.1 rule it exercises |
|---|---|---|
| `broken-joint-count.json` | `rot` has 100 floats instead of 104 | `rot` must be `jointCount * 4` |
| `broken-unnormalized-quat.json` | joint 2's quaternion is scaled by 1.4 | every quaternion normalised within 1% on \|q\|² |
| `broken-no-version.json` | no `version` field | `version` is required; the loader refuses unknowns |

**Never copy these into `unity/Assets/StreamingAssets/`.** The shipped folder is validated separately
and a file here that reaches it would ship a shape that silently never locks.
