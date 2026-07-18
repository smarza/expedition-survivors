# Continuous integration

The `Unity CI` GitHub Actions workflow is the automated acceptance boundary before manual gameplay validation. Its normal handoff is a browser-playable build at <https://smarza.github.io/expedition-survivors/>.

## Pipeline

Every push to `main` or an `agent/**` development branch, plus every manual dispatch, runs:

1. **Static validation** — runs `python3 tools/validate_project.py` without requiring Unity or a license.
2. **Unity license preflight** — fails quickly with a readable message when the repository secrets are incomplete.
3. **Unity tests** — imports the project with Unity `6000.5.4f1` and runs EditMode plus PlayMode tests.
4. **Web compilation** — builds WebGL only after the tests pass.
5. **GitHub Pages deployment** — publishes the successful Web build for immediate owner validation.

Test reports and the Web build are retained as GitHub Actions artifacts for 14 days. Unity `Library` caches for tests, Web and Windows builds are isolated because the jobs use different Editor images and target configurations.

The repository has one Pages site, so the latest successful development-branch deployment is the active preview. Superseded runs for the same branch are cancelled, while Pages deployments are serialized to prevent two publications from writing concurrently.

## Windows milestone gate

Windows remains the first commercial target, but it is no longer rebuilt for every development commit. `StandaloneWindows64` runs:

- on every push to `main`;
- from **Actions → Unity CI → Run workflow** when `Build the Windows acceptance player` is enabled.

Use the manual option before closing a milestone or whenever a change is likely to behave differently in the native player. Windows artifacts are retained for 14 days.

New commits cancel an older in-progress run for the same branch. This prevents outdated builds from consuming runner time.

## GitHub Pages setup

The repository must use **Settings → Pages → Build and deployment → Source: GitHub Actions**. The Web player enables Unity's decompression fallback because GitHub Pages does not expose custom compression headers. The workflow verifies `index.html`, adds `.nojekyll`, uploads a downloadable Web artifact and then publishes the same directory.

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
3. wait for `Unity CI`, including the Web build and Pages deployment;
4. inspect every failed job and its artifacts;
5. fix technical failures and repeat until green;
6. request manual gameplay, controller, usability and visual validation through the published Web preview;
7. keep the pull request as draft until both automated and required manual checks pass.

CI establishes that the project imports, tests, builds and can be deployed. Browser validation does not replace native Windows checks for packaging, performance, input-device behavior or release readiness.

## Branch protection after the first green run

After the workflow has completed successfully at least once, configure the `main` branch rule to require `Static validation`, `Unity EditMode and PlayMode tests`, `Web compilation` and `Publish Web preview` before merge. Windows milestone compilation remains a release gate rather than a per-commit required check. Do not require a check name until GitHub has registered it through a completed workflow run.
