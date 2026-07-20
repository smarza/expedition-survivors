# Milestone 0.11.0 — Camp and progression

- Added `SharedMetaProgressionModel` with presentation-free renown unlock purchases, per-hero mastery accrual, codex discovery rules and starter unlock defaults (Haldor + Scout).
- Bumped save envelope to **v4** with `SpentRenown`, `UnlockedContentIds`, `DiscoveredCodexIds`, Sylva/Mara/Eira mastery fields and `CampOnboardingComplete`.
- Added v3 → v4 migration with retroactive unlock generosity for existing profiles (no retroactive renown charges).
- Added Frostbound Camp **Unlock Board** — spend available renown on Sylva (75), Eira (110), Mara (145) and Long Night (200).
- Gated character and expedition selection to purchased unlocks; locked entries show camp renown cost.
- Extended mastery combat bonuses to Sylva (Grove Thorn Lash), Mara (Signal Flare) and Eira (Raven Guard).
- Added camp **Codex** with Heroes, Expeditions, Weapons, Gear, Evolutions and Relics; evolution recipe hints when base + catalyst are discovered.
- Added first-visit camp onboarding (four panels) and three additional Scout first-run combat hints for objectives, extraction and renown unlocks.
- Results screen now shows renown earned, available balance and affordable-unlock callout.
- Added `docs/CAMP_AND_PROGRESSION_0.11.md` and `docs/TESTING_0.11.md`.
- Extended automated gate inventory to 74 EditMode + 13 PlayMode tests.

# Milestone 0.10.0 — Demonstration content

- Added authored Frostbound landmarks: driftwood wreck, rune circle, boss-approach markers and a north extraction beacon placeholder with phase-tinted RuntimeAssets sprites.
- Added `FrostboundLandmarkTint` so landmarks brighten as the Scout route advances through shoreline, driftwood, warlord approach, boss and extraction phases.
- Added Sylva Reedwalker and Captain Mara Voss compositional silhouettes in `HeroPresentation`.
- Added `SharedExpeditionRouteModel` EditMode coverage for Scout objectives, extraction completion and relic tier resolution.
- Added `SharedWeaponRegistry` EditMode coverage preserving Frost Axe level-table parity with the legacy shared weapon model.
- Added a simplified PlayMode Scout extraction-victory flow regression after boss defeat and beacon arrival.
- Added `docs/DEMONSTRATION_CONTENT_0.10.md` as the owner-approved content brief and `docs/TESTING_0.10.md` as the acceptance matrix.
- Extended automated gate inventory to 55 EditMode + 10 PlayMode tests.

# Milestone 0.9.0 — Presentation foundation (release candidate)

- Added persistent accessibility settings for UI scale, high contrast, reduced flashes and screen shake.
- Added independent master, music and SFX buses with browser-compatible imported clips and startup after interaction.
- Replaced unsupported runtime PCM generation after owner validation found silent music and SFX in the Web build.
- Added bounded, prioritised SFX voices and Menu/Expedition/Boss/Reward/Result music states.
- Added real P1 keyboard rebinding for movement, Ultimate, Submit, Back, Pause and Expedition Build.
- Added semantic prompts for keyboard, Xbox, PlayStation, Switch, Steam Deck and generic gamepads.
- Added a Settings screen reachable from both the main menu and Pause, returning to the correct owner state.
- Added safe-area layout math and a 1280×800 Steam Deck presentation contract.
- Added pooled projectile trails, impacts, enemy deaths, pickups, Raven Guard, Ultimate, down/revive and result feedback.
- Added reduced-flash-aware VFX and adjustable centralized camera trauma.
- Added Haldor/Eira compositional silhouettes, idle/movement/attack/hit/Ultimate animation and deterministic Frostbound snow.
- Extended F3 diagnostics with VFX, SFX voice and music-state telemetry.
- Added 7 EditMode and 2 PlayMode presentation regressions, bringing the gate to 48 EditMode + 9 PlayMode tests.
- Added `docs/PRESENTATION_FOUNDATION_0.9.md` and `docs/TESTING_0.9.md` as the architecture and acceptance contracts.

# Milestone 0.8.0 — Shared simulation

- Pinned development to Unity `6000.5.4f1`, a supported release newer than the first patched Unity 6.0 LTS version for CVE-2025-59489.
- Updated the Unity 6000.5 package matrix to Input System `1.19.0`, Rider Editor `3.0.40` and Visual Studio Editor `2.0.27`.
- Added validation that rejects obsolete project APIs and package combinations that produced `GetInstanceID` and `CreateAssetWithContent` compilation errors on Unity 6000.5.
- Replaced project usages of the Unity 6000.5-obsolete `GetInstanceID` and ordered object lookup APIs while preserving projectile hit tracking and main-camera selection behavior.
- Disabled deprecated Dynamic Batching for Standalone builds while retaining Static Batching.
- Added a repository security policy requiring patched-editor rebuilds for all distributable targets.
- Added source-control hygiene for Unity caches, local settings, IDE files and build artifacts.
- Added a validation guard that rejects vulnerable Unity 6.0 Editor versions.
- Split production runtime, EditMode tests and PlayMode tests into explicit assemblies.
- Added Unity Test Framework `1.4.6` and 41 EditMode regression tests for deterministic RNG, shared run/player/enemy/spawn/projectile/effect behavior, exact weapon level tables/descriptions, pooling, spatial membership, content IDs, build slots, rewards, evolutions, balance and save migration.
- Added 7 disk-safe PlayMode smoke tests for bootstrap initialization, shared player/enemy/reward projection, Solo level-up, same-seed replay and terminal run results.
- Added a backward-compatible save migration from the original unversioned payload to versioned envelope format 2 without changing the existing save-file path.
- Expanded the fast repository validator to enforce assembly boundaries, package version and the critical automated-test inventory.
- Moved the pool test probe out of the Editor-only assembly so Unity can attach it to a `GameObject` during EditMode pool-reuse validation.
- Added the presentation-free `SharedRunModel` as the first Phase C extraction, owning local run phase, clock, boss trigger, XP, reward-turn alternation and terminal outcome.
- Routed `GameDirector` progression through the shared model while retaining its existing GameObject, spawning, reward-effect and UI responsibilities.
- Added five EditMode model tests and PlayMode phase-parity assertions for start, level-up, replay and result transitions.
- Updated PlayMode object discovery to Unity 6000.5's unsorted `FindObjectsByType<T>()` overload, removing the deprecated `FindObjectsSortMode` warnings.
- Extracted Frost Axe flight, lifetime, collision radius and pierce rules into the presentation-free `SharedProjectileModel` used by Solo and Local Co-op.
- Extracted player attributes, movement requests, damage/armor, invulnerability, knockdown/revival and Ultimate rules into `SharedPlayerModel`; `PlayerController` now projects that shared state for both Solo and Local Co-op.
- Extracted enemy derived attributes, pursuit movement, contact cadence, knockback and death into `SharedEnemyModel`; the pooled `Enemy` component now handles target selection, presentation, spatial updates and drops around that state.
- Extracted wave cadence, difficulty ramp, active-enemy cap, group growth and spawn-ring rules into `SharedSpawnModel`; `GameDirector` now only materializes the requested enemies.
- Extracted Frost Axe and Raven Guard timing/statistics, upgrades, Ultimates and evolution effects into `SharedWeaponModel` and `SharedEffectPipeline` while preserving the existing balance values.
- Routed reward application, projectile requests, area damage, healing and evolution explosions through the shared effect boundary; local components now provide targeting, collision and presentation adapters.
- Clarified that item rewards grant the modifier for one specific level; reward cards now preview that exact modifier for every recipient.
- Locked Frost Axe level 8 to two projectiles through both model and PlayMode adapter regressions.
- Implemented the promised Raven Guard frequency improvements at levels 5 and 8 without removing its existing damage progression.
- Expanded Expedition Build into complete Survivor, Frost Axe and Raven Guard live-stat panels with current/next item effects.
- Added `docs/BUILD_AND_CONTENT_REFERENCE.md` as the complete character, power, build, reward, formula and future-content authoring contract.
- Removed the experimental Online Co-op runtime, menu entry, Netcode package and Transport assembly dependencies from the active product scope.
- Deferred Online multiplayer until the Solo/Local shared simulation is mature; the former POC remains recoverable from Git history and its future replacement must reuse the common gameplay core.
- Added GitHub Actions continuous integration with static validation, Unity EditMode/PlayMode tests, a Web build and automatic GitHub Pages preview deployment.
- Reserved Windows CI builds for `main`, explicit milestone dispatches and `[windows]` milestone commits so everyday gameplay validation can use the browser without weakening the native release gate.
- Enabled Unity's Web decompression fallback for static hosting and retained downloadable Web artifacts alongside the live preview.
- Added license preflight, isolated Unity Library caches, cancellation of superseded branch runs and 14-day test/build artifacts.
- Removed the validator's external Pillow dependency by reading the PNG header directly, keeping the fast CI guard self-contained.

# Milestone 0.7.1 — Foundation runtime fixes

- Moved `ProductionContentDatabase` to a matching source file and repaired the asset's MonoScript reference, eliminating the misleading `CharacterContentRecord` warnings and `CODE FALLBACK` loading path.
- Added a radial gamepad movement deadzone so Editor-only stick noise cannot move a survivor diagonally.
- Increased Online Expedition button-label contrast and added a gold selection outline for keyboard/gamepad focus.
- Added a visible `REPLAYING SEED <number>` announcement so deterministic replay can be confirmed immediately.
- Expanded repository validation to guard the asset GUID, deadzone, lobby readability and replay confirmation fixes.

# Milestone 0.7.0 — Production foundation

## Added in 0.7.0

- Generic reusable component pools with prewarming, active/available tracking and reuse metrics.
- Pooled local enemies, Frost Axe projectiles, experience gems, Raven Guard pulses and Ultimate pulses.
- Pooled Online Co-op enemy views, attack visuals and pulse visuals.
- A cell-based spatial hash for nearest-enemy and radius queries in local and online combat.
- Deterministic local run random source covering arena decoration, enemy spawning/stats, critical hits, renown and reward generation.
- Run seed shown in results and `REPLAY SAME SEED` for reproducible balancing and bug reports.
- Resources-backed `ProductionContentDatabase` ScriptableObject containing characters, maps, items, evolutions and enemy archetypes.
- Stable-ID and evolution-reference validation with safe code fallback if the asset cannot be loaded.
- Runtime startup checks for deterministic RNG, spatial membership updates and content integrity.
- Toggleable F3 performance overlay with FPS, frame time, worst frame, active actors, pool capacity/reuse, spatial cells/queries, seed and content source.
- Static repository checks for asset/script identity, required content IDs, pooling invariants and absence of steady-state projectile destruction.

## Changed in 0.7.0

- Local combat no longer creates or destroys enemies, projectiles, gems or recurring pulse effects during steady-state play.
- Online enemy-area and nearest-target operations use spatial queries rather than full dictionary scans.
- Online per-frame enemy updates reuse scratch collections instead of allocating a new ID list.
- Result replay preserves the current seed; starting from map selection creates a new seed.

## Validation boundary

- Automated checks validate repository structure, C# delimiter balance, asset IDs, pool/spatial integration and runtime self-test wiring.
- Unity PlayMode and Profiler remain required to confirm compilation, imported ScriptableObject data and frame-time behavior on the target machine.

# Milestone 0.6.2 — Targeted UI layout polish

## Improved in 0.6.2

- Separated character Ultimate descriptions, player-device instructions and Ready buttons into independent vertical regions.
- Added a compact responsive map-title style and a taller title region for Long Night.
- Replaced the overflowing combat control sentence with a concise, dedicated HUD hint row.
- Reduced and left-aligned the compact `P1/P2 BUILD` labels so they never wrap behind item tokens.
- Rebuilt Expedition Build statistics as real label/value columns with section headers, alternating rows and right-aligned values.
- Rebuilt the result card with a safe multiline heading, protected map-name summary and a compact performance row.
- Applied the aligned statistic-card treatment and compact build labels to Online Co-op.
- Added automated layout-invariant checks for the new regions and stat columns.

## Validation boundary

- Repository structure, C# delimiter balance and the new UI layout invariants are validated automatically.
- Final pixel-level verification still requires PlayMode screenshots because Unity font metrics are resolved at runtime.

# Milestone 0.6.1 — Responsive and readable interface

## Improved in 0.6.1

- Replaced independent horizontal/vertical UI stretching with a proportional virtual canvas and safe letterboxing.
- Level-up, build details, pause and results now replace the run HUD instead of rendering on top of it.
- Moved run announcements to a dedicated bottom-center banner clear of health, timer and combat panels.
- Enlarged health, Ultimate and XP bars and added explicit XP progress text.
- Rebuilt the compact build tray with bordered 3-letter item tokens, `L#` levels and separate capacity rows.
- Rebuilt build details around a three-column, four-row grid that supports all 12 build slots.
- Increased item-card height and separated title and description regions to prevent clipping.
- Improved panel opacity, spacing, borders, typography hierarchy and contrast.
- Added dark, high-contrast text to colored player destination badges.
- Made the full level-up card clickable while retaining the explicit action button.
- Unified mouse hover and gamepad selection around the same gold focus border.
- Locked static-label colors across normal, hover, active and focused GUI states, removing false click affordances.
- Applied the same responsive canvas, modal, build-grid, reward-card and hover behavior to Online Co-op.

## Validation boundary

- Repository structure, layout invariants and UI-state requirements are validated automatically.
- Pixel-level verification still requires PlayMode screenshots from the target Unity build and display matrix.

# Milestone 0.6 — Player builds, targeted rewards and evolutions

## Added in 0.6

- Four level-up reward cards in Solo, Local Co-op and Online Co-op.
- Alternating reward ownership: P1 and P2 take turns choosing with only the assigned device enabled.
- Build-aware reward targets: self, the other survivor or both players.
- Colored character destination badges and an explicit post-choice recipient announcement.
- Independent weapon and gear inventories with map-specific 4+4 or 6+6 slot limits.
- Persistent item levels within a run and reward eligibility that respects max levels and full slots.
- Jotunn Cleaver evolution: max Frost Axe plus Jotunn Rune adds explosive cluster damage.
- Storm Aegis evolution: max Raven Guard plus Bear-Blooded adds a larger healing pulse.
- Compact per-player build trays with item abbreviations, level/evolution state and slot usage.
- Detailed build screens correlating health, armor, movement, Ultimate, Frost Axe and Raven Guard statistics to equipped items.
- Host-authoritative synchronization of targeted rewards, per-player builds, item levels, evolutions and combat statistics.
- Online reward commands rejected unless they come from the peer whose turn is active.

## Production boundary

- The systems use stable string IDs but remain runtime definitions. ScriptableObject authoring, final iconography, tooltip navigation and content validation remain production tasks.
- Online build details are non-pausing by design so one peer cannot freeze the authoritative simulation.

# Milestone 0.5 — Production core: content, input and Ultimates

## Added in 0.5

- A shared data catalog for characters, tribes, maps, attributes, Ultimates and timing profiles.
- Character selection for Solo and independent two-player Local Co-op selection.
- Map selection with a five-minute scout expedition and a twelve-minute quick expedition.
- Haldor's strategic **Ravenstorm** Ultimate and Eira's **Murder of Ravens**.
- High-impact manual Ultimates while normal weapons and Raven Guard remain automatic.
- Ultimate cooldown and damage rune upgrades with a protected 28-second minimum cooldown.
- A slower XP curve targeting meaningful effort before the first reward.
- A 35% co-op XP requirement increase to account for two survivors dealing damage.
- Per-character health, speed, armor, Ultimate damage, radius and cooldown.
- Gamepad navigation for main menu, character/map selection, level-up, pause, results and online lobby.
- Stable activity-based gamepad claims for two-player local co-op.
- Solo reads the strongest active gamepad instead of assuming the first enumerated device.
- Player-specific gamepads navigate and confirm shared rune choices.
- Online map choice, map timing, XP balance and Ultimate state synchronized by the host.
- Boss-required victory: reaching the target time enters overtime instead of granting a free win.

## Production boundary

- Solo, Local and Online now share content and balance definitions.
- Consolidating their simulation implementation, pooling actors and spatial partitioning remains the next production-core slice.

## Milestone 0.4 — Networked gameplay slice

## Added in 0.4

- A real two-player online expedition that starts automatically when the second survivor connects.
- Haldor Stormborn as host hero and Eira Raven-Sworn as the remote co-op hero.
- Host-authoritative movement, Raven Rush, health, damage, knockdown and proximity revival.
- Host-simulated Draugr waves, difficulty scaling and a Jotunn boss encounter.
- Automatic Frost Axe attacks, multishot, piercing follow-up hits, criticals and Raven Guard pulses.
- Shared XP, level-up pauses and first-choice-wins rune selection from either peer.
- Synced victory/defeat state and host-owned persistent run rewards.
- Quantized enemy snapshots designed to remain below a typical UDP MTU at the 96-enemy cap.
- Client interpolation, weapon/pulse events, live payload size and RTT telemetry.
- Online-specific HUD, individual health bars, timer, boss countdown, build statistics and results.

## Still intentionally deferred

- Internet Relay, lobby codes, matchmaking, reconnect, host migration and anti-cheat.
- Client prediction/reconciliation and artificial latency/loss profiles.
- Production object pooling, spatial partitioning, final art, audio and UI.

## Milestone 0.3.3 — Session lifecycle and clarity fix

## Fixed in 0.3.3

- Clarifies that the online screen is a connectivity/snapshot POC, not the full expedition.
- Displays an explicit connectivity pass state after the second player joins.
- Limits connection approval to one host and one remote client.
- Shuts down the transport gracefully before destroying `NetworkManager`.
- Prevents immediate session recreation while the previous singleton is closing.
- Prevents stale ghost peers after returning to camp and reconnecting.

## Milestone 0.3.2 — NetworkManager runtime setup fix

## Fixed in 0.3.2

- Creates `NetworkManager` as a scene-root object, as required by Netcode for GameObjects.
- Adds `UnityTransport` before `NetworkManager` so the transport exists during manager initialization.
- Explicitly initializes `NetworkConfig` when creating the manager entirely at runtime.
- Keeps the root manager alive across scene transitions and destroys it during session shutdown.

## Milestone 0.3.1 — Online package dependency fix

## Fixed in 0.3.1

- Added Unity's 3D Physics module required by Netcode's `NetworkRigidbody` implementation.
- Added the Animation module required by Netcode's high-level component assembly.
- Pinned Collections 2.6.7, the released version for Unity 6000.0.
- Extended validation so future packages cannot omit these compile dependencies.

## Milestone 0.3 — Online host/client connectivity spike

## Added in 0.3

- Isolated Online POC screen without destabilizing Solo or Local Co-op.
- Netcode for GameObjects 2.7 and Unity Transport 2.6.
- Direct-IP host/client connection on UDP port 7777.
- Host-authoritative movement input and two synchronized Viking identities.
- Sixty-four host-simulated swarm probes sent through compact snapshots.
- 30 Hz client input, 15 Hz snapshots and client-side interpolation.
- Live snapshot byte count, sent/received counters, peer count and RTT.
- Client disconnect handling while the host remains active.
- Localhost and two-computer LAN acceptance instructions.

## Previous 0.2 local co-op spike

- Solo/local-co-op selection from the main menu.
- Unity Input System 1.16 with explicit keyboard and gamepad ownership.
- Two concurrent player controllers and per-player weapon systems.
- Eira Raven-Sworn as a visually distinct temporary co-op partner.
- Shared camera center, dynamic zoom and soft party tether.
- Nearest-living-player enemy targeting.
- Shared expedition XP and team-wide rune upgrades.
- Individual knockdown state and proximity revival.
- Two-player HUD with individual health and revival progress.
- Co-op test matrix, acceptance steps and online architecture boundary.

## Previous 0.1 foundation

## Delivered

- Unity 6 LTS project that starts from a committed bootstrap scene.
- Original Haldor Stormborn key art and fallback runtime portrait.
- Main menu, run HUD, pause, level-up selection, victory and defeat screens.
- Keyboard movement, basic gamepad movement/button support and Raven Rush.
- Automatic axes, critical runes, piercing, multishot and shield pulse.
- Escalating Draugr swarm, experience drops and Jotunn boss encounter.
- Ten upgrade choices across offense, defense, mobility and pickup range.
- Persistent local renown, Haldor mastery, run count and best results.
- Static validation script and production-milestone documentation.

## Validation performed

- All JSON package data parsed.
- Bootstrap scene GUID matched against build settings.
- Fifteen C# files passed delimiter, namespace and unfinished-marker checks.
- Key art passed PNG and minimum-resolution checks.
- Archive contents and Unity project structure verified.

## Validation still required on Ricardo's machine

- Unity compilation with the installed Unity 6 LTS patch.
- PlayMode input, balance, frame pacing and resolution checks.
- Controller verification on the actual device.
