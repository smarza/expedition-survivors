# Project Expedition — Demonstration Content 0.10.0 (release candidate)

An original survivors-like prototype built for Unity 6 LTS. The milestone is intentionally self-contained: it has no purchased packages or external asset dependencies and bootstraps itself from an empty scene.

For the complete product vision, technical roadmap, launch gates and new-chat handoff, read [`docs/PROJECT_MASTER_PLAN.md`](docs/PROJECT_MASTER_PLAN.md). Exact characters, weapons, level tables, gear, rewards, formulas and evolution rules are documented in [`docs/BUILD_AND_CONTENT_REFERENCE.md`](docs/BUILD_AND_CONTENT_REFERENCE.md).

## Play it

1. Install Unity Hub and Unity `6000.5.4f1` or a newer supported patched release. Earlier builds must not be used for release artifacts because of CVE-2025-59489.
2. Add this folder through **Unity Hub → Add project from disk**.
3. Open `Assets/Scenes/Bootstrap.unity`.
4. Press **Play**.

If this branch was opened before the Unity 6000.5 package hotfix, close the Editor, pull the latest commit and reopen the project. Unity Package Manager must resolve Input System `1.19.0` and Unity Test Framework `1.4.6`; if stale PackageCache errors remain, remove the generated `Library/PackageCache` directory and reopen.

Without Unity installed, run `python tools/validate_project.py` for fast repository, syntax-balance, scene-reference and art checks.

## Continuous integration

The `Unity CI` GitHub Actions workflow runs the static validator, all EditMode and PlayMode tests, builds the Web player and publishes it to [GitHub Pages](https://smarza.github.io/expedition-survivors/) before manual gameplay acceptance. Windows builds run on `main` and on explicit milestone dispatches. Setup and the development handoff contract are documented in [`docs/CONTINUOUS_INTEGRATION.md`](docs/CONTINUOUS_INTEGRATION.md).

## Automated tests

Unity Test Framework `1.4.6` is part of the project. After Unity finishes resolving packages, open **Window → General → Test Runner** and run both suites:

1. **EditMode** — 55 deterministic simulation, content, route, weapon-registry, persistence, presentation-settings, safe-layout, glyph, audio-asset and music-routing tests.
2. **PlayMode** — 10 bootstrap, shared-model/reward, presentation-service, VFX-pool, settings, level-up, replay-seed, scout extraction-victory and result-flow tests.

The PlayMode tests explicitly disable campaign persistence, so they do not overwrite the developer's local save. The current demonstration acceptance matrix is in [`docs/TESTING_0.10.md`](docs/TESTING_0.10.md).

## Controls

- Solo: configurable P1 keyboard bindings (WASD/Space by default) or any active gamepad.
- Local co-op P1: `WASD` plus `Space` for Ultimate.
- Local co-op P2: arrows plus `Enter` for Ultimate.
- Gamepad movement: left stick. Ultimate: right shoulder or right trigger.
- Menus: D-pad/left stick, South/A to confirm and East/B to return.
- `1`, `2`, `3`, `4`: select one of four level-up rewards.
- `Tab` or gamepad View/Select: open the build and statistics reference.
- `F3` or gamepad Left Shoulder + View/Select: toggle production performance metrics.
- `Esc`: pause.
- Combat is otherwise automatic.

Open **Settings** from the main or pause menu to rebind P1 keyboard actions and configure UI scale, contrast, reduced flashes, screen shake and master/music/SFX volume. Prompts follow the last active keyboard or controller family.

Device assignment is intentionally predictable: with one gamepad in co-op, P1 uses the keyboard and P2 receives the gamepad; with two gamepads, each player receives one. During level-up, only the chooser's assigned device is active. Gamepad West/North/East/right-shoulder choose cards 1/2/3/4.

## What is implemented

- Haldor Stormborn character presentation, original key art and a generated fallback portrait.
- Shared character/map content catalog and pre-run selection flow.
- Haldor's Ravenstorm and Eira's Murder of Ravens as strategic manual Ultimates.
- Automatic Frost Axe weapon with projectile count, pierce, cooldown, damage and critical upgrades.
- Automatic Raven Guard pulse and strategic character Ultimates.
- Enemy spawning, chase behavior, escalating difficulty and a Jotunn boss.
- Experience gems, magnet pickup, slower level curve and four-choice rewards.
- Alternating per-player reward turns in Local Co-op.
- Target badges for rewards granted to P1, P2 or both survivors.
- Map-specific weapon/gear slot limits: 4+4 on Scout and 6+6 on Long Night.
- Individual item levels, build-aware reward eligibility and fallback healing rewards.
- Frost Axe + Jotunn Rune evolution into Jotunn Cleaver with clustered explosions.
- Raven Guard + Bear-Blooded evolution into Storm Aegis with larger healing pulses.
- Compact build icon tray and a detailed, correlated combat-stat screen.
- Proportional 1920×1080 virtual canvas with letterboxing instead of aspect-ratio stretching.
- Opaque modal screens that no longer compete visually with the run HUD.
- Readable 12-item build grids, larger build tokens and non-overlapping announcements.
- Static text styles that never react to mouse hover; only real buttons and fully clickable reward cards provide interaction feedback.
- Dedicated Ultimate, control-hint and map-title regions that preserve readable spacing with longer content.
- Aligned survivor/combat statistic columns with alternating rows and right-aligned values.
- Redesigned result summary with a protected map-name region and unclipped expedition heading.
- Health, armor, movement, damage, kill, timer and run-result systems.
- Persistent JSON save with renown, run history, best performance and Haldor mastery.
- Keyboard and gamepad input through Unity's current Input System.
- One- or two-player local co-op with independent Input System device routing.
- Shared camera zoom/tether and XP, with alternating individual reward ownership.
- Activity-based independent gamepad ownership and full gamepad menu navigation.
- Configurable five- and twelve-minute Frostbound Shore expedition profiles.
- Slower XP progression with co-op scaling and boss-required victory.
- Knockdown and proximity revival when another player remains alive.
- Resolution-independent prototype HUD and menu.
- Runtime-generated visual assets, allowing a clean first checkout.
- Component pools for enemies, Frost Axes, experience gems and combat pulses; steady-state local combat no longer destroys these objects.
- Spatial hash queries for nearest-target and area attacks in Solo and Local Co-op.
- A Resources-backed ScriptableObject database for characters, maps, items, evolutions and enemy archetypes, with validated code fallback.
- Deterministic local run seeds; results expose the seed and can replay the exact same sequence.
- Toggleable production metrics with `F3` or gamepad Left Shoulder + View/Select.
- Startup foundation checks covering deterministic random sequences, spatial membership and stable content IDs.
- Runtime, EditMode and PlayMode assembly boundaries that keep tests separate from production builds.
- GitHub Actions gates for static validation, Unity tests, Web compilation and GitHub Pages deployment, with Windows builds reserved for `main` and milestone dispatches.
- Forty-one EditMode regression tests for deterministic foundations, shared run/player/enemy/spawn/projectile/effect behavior, exact weapon level tables/descriptions, builds, rewards, balance and versioned save migration.
- Reward cards expose the exact next-level modifier, and Expedition Build exposes complete Survivor, Frost Axe and Raven Guard live statistics.
- A complete player/content reference documents every current character, power, weapon, gear item, boon, evolution and calculation.
- Seven disk-safe PlayMode smoke tests for bootstrap, shared player/enemy/reward projection, Solo level-up, same-seed replay and terminal result flow.
- Backward-compatible migration from the original unversioned save payload to a versioned save envelope.
- Persistent presentation settings with 90–120% UI scale, high contrast, reduced flashes, screen shake and independent audio buses.
- P1 keyboard rebinding plus active-device glyphs for keyboard, Xbox, PlayStation, Switch, Steam Deck and generic gamepads.
- State-driven imported prototype music, prioritised bounded SFX voices and browser-compatible startup after interaction.
- Pooled Frost Axe trails, impacts and combat/result feedback with reduced-flash support.
- Haldor/Eira silhouette animation, Frostbound ambient snow and accessible camera trauma.

## Architecture

- `GameDirector`: local GameObject orchestration and presentation adapter for the shared run model.
- `SharedRunModel`: presentation-free phase, clock, boss trigger, XP, reward-turn and outcome state.
- `SharedPlayerModel`: presentation-free player attributes, movement requests, damage, knockdown/revival and Ultimate state.
- `SharedEnemyModel`: presentation-free enemy attributes, movement, contact cadence, knockback and death state.
- `SharedSpawnModel`: presentation-free wave cadence, difficulty ramp, active cap, group size and spawn-ring rules.
- `SharedProjectileModel`: presentation-free Frost Axe flight, lifetime, collision radius and pierce state.
- `SharedWeaponModel` / `SharedEffectPipeline`: presentation-free automatic weapon timing, derived combat requests, upgrades, Ultimates and evolutions.
- `PlayerController`: local input and GameObject/presentation adapter for the shared player model.
- `Enemy`: pooled GameObject/presentation, target selection, drops and spatial-index adapter for the shared enemy model.
- `LocalInputRouter`: deterministic keyboard/gamepad ownership, persistent P1 rebinding and active-device tracking.
- `PresentationDirector`: state-driven audio, VFX, ambience and camera-feedback adapter.
- `PresentationPreferences` / `PresentationLayout` / `PresentationTheme`: persistent accessibility and renderer-independent UI contracts.
- `PresentationAudioMixer`: bounded music/SFX buses and prioritised voice pool.
- `PresentationVfxPool` / `HeroPresentation`: pooled feedback and character silhouette/animation.
- `ContentDefinitions`: shared characters, maps, Ultimates and balance rules.
- `ContentAssets`: ScriptableObject authoring records, runtime loading, validation and enemy archetypes.
- `BuildSystem`: item catalog, slots, reward generation, build state and evolution recipes.
- `ProductionFoundation`: generic component pools, spatial hash, deterministic random source, performance metrics and startup checks.
- `WeaponSystem`: local targeting, GameObject and presentation adapter for the shared weapon/effect model.
- `AxeProjectile`, `ExperienceGem`: focused simulation actors.
- `SaveService`: versioned local persistence with atomic temporary-file writes.
- `SaveMigration`: backward-compatible deserialization and current save-envelope serialization.
- `RuntimeAssets`: dependency-free prototype sprites and fallback art.
- `Assets/Resources/Art/Haldor_Stormborn_KeyArt.png`: original Haldor selection-screen key art.
- `GameHUD`: code-driven view adapter using the presentation layout, theme, settings and glyph contracts.
- `ProjectExpedition.Runtime`: production runtime assembly shared by the game and test suites.
- `ProjectExpedition.EditModeTests` / `ProjectExpedition.PlayModeTests`: deterministic rule and expedition-flow regression assemblies.

The core content, pooling, spatial-query, deterministic-run and automated regression foundations are now in place. Production development is focused on one shared implementation for Solo and Local Co-op. Online multiplayer is deferred until that simulation is mature; its future adapter must reuse the same gameplay core instead of implementing separate combat rules.

## Milestone definition

This repository is the first playable implementation, not the entire content-complete commercial game. See [`docs/BUILD_AND_REWARD_0.6.md`](docs/BUILD_AND_REWARD_0.6.md) for the reward/build acceptance pass and [`docs/PROJECT_MASTER_PLAN.md`](docs/PROJECT_MASTER_PLAN.md) for the complete production path through launch.
