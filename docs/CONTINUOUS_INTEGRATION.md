# Continuous integration

The `Unity CI` GitHub Actions workflow is the automated acceptance boundary before manual gameplay validation.

## Pipeline

Every update to an open pull request targeting `main`, every push to `main`, and every manual dispatch runs:

1. **Static validation** — runs `python3 tools/validate_project.py` without requiring Unity or a license.
2. **Unity license preflight** — fails quickly with a readable message when the repository secrets are incomplete.
3. **Unity tests** — imports the project with Unity `6000.5.4f1` and runs EditMode plus PlayMode tests.
4. **Windows compilation** — builds `StandaloneWindows64` only after the tests pass.

Test reports and the Windows build are retained as GitHub Actions artifacts for 14 days. Unity `Library` caches for tests and Windows builds are isolated because the jobs use different Editor images and target configurations.

New commits cancel an older in-progress run for the same pull request. This prevents outdated builds from consuming runner time.

## Required repository secrets

Open **GitHub → expedition-survivors → Settings → Secrets and variables → Actions**. Never commit or paste these values into issues, pull requests, source files or chat.

For Unity Personal, configure:

- `UNITY_LICENSE`: the complete contents of `C:\ProgramData\Unity\Unity_lic.ulf` on Windows;
- `UNITY_EMAIL`: the Unity account email;
- `UNITY_PASSWORD`: the Unity account password.

If `Unity_lic.ulf` is missing, use **Unity Hub → Preferences → Licenses → Add → Get a free personal license** first.

For Unity Pro, configure:

- `UNITY_SERIAL`;
- `UNITY_EMAIL`;
- `UNITY_PASSWORD`.

Only one of `UNITY_LICENSE` or `UNITY_SERIAL` is required. The workflow does not print secret values.

Official activation instructions: https://game.ci/docs/github/activation/

## Development handoff rule

The agent workflow for every implementation is:

1. implement and run the local static validator;
2. commit and push to the pull request branch;
3. wait for `Unity CI`;
4. inspect every failed job and its artifacts;
5. fix technical failures and repeat until green;
6. request manual gameplay, controller, usability and visual validation from the owner;
7. keep the pull request as draft until both automated and required manual checks pass.

CI establishes that the project imports, tests and builds. It does not replace judgment-based tests such as gameplay feel, readability, controller comfort, balance and visual quality.

## Branch protection after the first green run

After the workflow has completed successfully at least once, configure the `main` branch rule to require `Static validation`, `Unity EditMode and PlayMode tests`, and `Windows compilation` before merge. Do not require a check name until GitHub has registered it through a completed workflow run.
