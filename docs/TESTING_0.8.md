# Expedition Survivors 0.8.0 testing guide

This guide is the acceptance contract for the first automated regression boundary. It complements manual Solo and Local Co-op testing; it does not replace device and UI validation.

## Required environment

- Branch: `agent/0.8.0-shared-simulation`
- Unity Editor: `6000.5.4f1` or a newer supported patched release
- Unity Test Framework: `1.4.6`, resolved from `Packages/manifest.json`
- Bootstrap scene: `Assets/Scenes/Bootstrap.unity`

Allow Unity Package Manager to finish before evaluating compile or test results. If this is the first import after pulling the test harness, a legitimate `Packages/packages-lock.json` update may be produced and should be reviewed separately from generated `Library` content.

The Test Runner menu is only registered after the project compiles and the Test Framework package finishes importing. If Unity opens in Safe Mode, resolve every Console compilation error first, exit Safe Mode and then open the Test Runner.

## Run in the Editor

1. Open **Window → General → Test Runner**.
2. Select **EditMode**, choose **Run All** and confirm 17 passes with zero failures.
3. Select **PlayMode**, choose **Run All** and confirm 4 passes with zero failures.
4. Run `python3 tools/validate_project.py` from the repository root.
5. If any test fails, capture the test name, assertion, full stack trace and Unity Console errors before changing code.

PlayMode tests set `SaveService.PersistenceEnabled` to false and reset only in-memory test data. They do not write to `project-expedition-save-v1.json`.

Reusable `MonoBehaviour` test doubles live in the player-compatible `ProjectExpedition.TestSupport` assembly. Unity cannot attach components compiled inside an Editor-only assembly to a `GameObject`.

## Automated inventory

| Suite | Coverage | Expected |
| --- | --- | ---: |
| EditMode / deterministic foundation | same-seed RNG, seed/range bounds, spatial update/removal, pool reuse, stable content IDs | 5 |
| EditMode / builds and rewards | slot behavior, evolution prerequisites, deterministic recipients/items, XP and Ultimate cooldown bounds | 4 |
| EditMode / persistence | legacy v1 migration and v2 envelope round-trip | 2 |
| EditMode / shared run | initialization, clock/boss trigger, XP overflow, co-op reward turns and idempotent outcome | 5 |
| EditMode / shared projectile | travel, collision radius and pierce-budget parity across adapters | 1 |
| PlayMode / expedition flow | foundation bootstrap, Solo level-up/resume, replay seed/reset, idempotent run result | 4 |
| **Total** |  | **21** |

## Manual regression after shared gameplay changes

| Mode | Minimum check |
| --- | --- |
| Solo | Keyboard and active gamepad movement; four-card level-up; Ultimate; build screen; replay same seed; result flow. |
| Local Co-op | Keyboard plus one gamepad and two-gamepad ownership; alternating chooser device; self/other/shared rewards; revival. |
| Presentation | 1920×1080 plus at least one different aspect ratio; no clipped headings, cards, hints or result map names. |
| Performance | F3 metrics show created counts stabilizing while reused counts continue to rise under sustained combat. |

## Acceptance rule

The accepted Phase B baseline is 15 passing tests. The current Phase C gate is 21: all 17 EditMode and 4 PlayMode tests must pass on the target patched Editor, the static validator must pass and no existing manual Solo/Local smoke path may regress. Phase C commits must add or update tests when they change deterministic rules.

## Branch and PR workflow

- `main` contains the last user-accepted milestone.
- `agent/0.8.0-shared-simulation` contains all 0.8.0 work.
- PR #1 remains the review and acceptance thread for the milestone.
- Each phase is committed as a small, reviewable unit and pushed only after its relevant local validation passes.
- The PR stays draft until the 0.8.0 exit gates pass; merging requires explicit user acceptance.
- Follow-up milestones start from updated `main` on new `agent/<version>-<topic>` branches and receive their own PRs.
