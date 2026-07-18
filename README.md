# Project Expedition — Shared Simulation 0.8.0 (development)

An original survivors-like prototype built for Unity 6 LTS. The milestone is intentionally self-contained: it has no purchased packages or external asset dependencies and bootstraps itself from an empty scene.

## Play it

1. Install Unity Hub and Unity `6000.5.4f1` or a newer supported patched release. Earlier builds must not be used for release artifacts because of CVE-2025-59489.
2. Add this folder through **Unity Hub → Add project from disk**.
3. Open `Assets/Scenes/Bootstrap.unity`.
4. Press **Play**.

Without Unity installed, run `python tools/validate_project.py` for fast repository, syntax-balance, scene-reference and art checks.

## Controls

- Solo: `WASD` or arrows; Ultimate with `Space`. Any active gamepad works.
- Local co-op P1: `WASD` plus `Space` for Ultimate.
- Local co-op P2: arrows plus `Enter` for Ultimate.
- Online co-op: each instance uses `WASD`; Ultimate with `Space`.
- Gamepad movement: left stick. Ultimate: right shoulder or right trigger.
- Menus: D-pad/left stick, South/A to confirm and East/B to return.
- `1`, `2`, `3`, `4`: select one of four level-up rewards.
- `Tab` or gamepad View/Select: open the build and statistics reference.
- `F3` or gamepad Left Shoulder + View/Select: toggle production performance metrics.
- `Esc`: pause.
- Combat is otherwise automatic.

Device assignment is intentionally predictable: with one gamepad in co-op, P1 uses the keyboard and P2 receives the gamepad; with two gamepads, each player receives one. During level-up, only the chooser's assigned device is active. Gamepad West/North/East/right-shoulder choose cards 1/2/3/4.

## What is implemented

- Haldor Stormborn character presentation, original key art and a generated fallback portrait.
- Shared character/map content catalog and pre-run selection flow.
- Haldor's Ravenstorm and Eira's Murder of Ravens as strategic manual Ultimates.
- Automatic Frost Axe weapon with projectile count, pierce, cooldown, damage and critical upgrades.
- Automatic Raven Guard pulse and strategic character Ultimates.
- Enemy spawning, chase behavior, escalating difficulty and a Jotunn boss.
- Experience gems, magnet pickup, slower level curve and four-choice rewards.
- Alternating per-player reward turns in Local and Online Co-op.
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
- Two-player online expedition with Haldor as host and Eira as client.
- Host-authoritative players, combat, Draugr swarm, health, XP, targeted/shared rewards, builds, evolutions and Jotunn victory.
- Compact quantized 15 Hz enemy snapshots, attack events, interpolation, payload/RTT counters and clean disconnect flow.
- Resolution-independent prototype HUD and menu.
- Runtime-generated visual assets, allowing a clean first checkout.
- Component pools for enemies, Frost Axes, experience gems and combat pulses; steady-state local combat no longer destroys these objects.
- Pooled enemy, attack and pulse presentation for Online Co-op.
- Spatial hash queries for nearest-target and area attacks in both local and online simulations.
- A Resources-backed ScriptableObject database for characters, maps, items, evolutions and enemy archetypes, with validated code fallback.
- Deterministic local run seeds; results expose the seed and can replay the exact same sequence.
- Toggleable production metrics with `F3` or gamepad Left Shoulder + View/Select.
- Startup foundation checks covering deterministic random sequences, spatial membership and stable content IDs.

## Architecture

- `GameDirector`: run state, spawning, difficulty, progression and orchestration.
- `PlayerController`: movement, health, damage and character Ultimate.
- `LocalInputRouter`: deterministic keyboard/gamepad ownership for the co-op spike.
- `ContentDefinitions`: shared characters, maps, Ultimates and balance rules.
- `ContentAssets`: ScriptableObject authoring records, runtime loading, validation and enemy archetypes.
- `BuildSystem`: item catalog, slots, reward generation, build state and evolution recipes.
- `ProductionFoundation`: generic component pools, spatial hash, deterministic random source, performance metrics and startup checks.
- `OnlineCoopSpike`: direct-IP/LAN host-authoritative expedition using compact custom messages.
- `WeaponSystem`: data-like runtime weapon state and automatic attacks.
- `Enemy`, `AxeProjectile`, `ExperienceGem`: focused simulation actors.
- `SaveService`: versioned local persistence with atomic temporary-file writes.
- `RuntimeAssets`: dependency-free prototype sprites and fallback art.
- `Assets/Resources/Art/Haldor_Stormborn_KeyArt.png`: original Haldor selection-screen key art.
- `GameHUD`: complete prototype interface using Unity IMGUI.

The core content, pooling, spatial-query and deterministic-run foundations are now in place. Production development should next consolidate more local/online simulation rules, replace IMGUI with UI Toolkit/uGUI, add Unity Test Framework PlayMode coverage and introduce Relay/lobby services only after the direct host/client simulation remains stable under load.

## Milestone definition

This repository is the first playable implementation, not the entire content-complete commercial game. See `docs/BUILD_AND_REWARD_0.6.md` for the acceptance pass and `docs/NEXT_MILESTONES.md` for the production path.
