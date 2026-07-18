# Production milestones

## M1 — Current vertical slice

- One playable Viking.
- One frozen-shore arena.
- Two weapons/abilities and ten upgrades.
- Draugr swarm and Jotunn boss.
- Local persistent progression.

## M2 — Local co-op architecture spike (implemented)

- Independent players and device assignment.
- Shared camera/tether, XP, upgrades and run state.
- Individual health, weapons, knockdown and revival.
- Co-op manual acceptance matrix.

## M3 — Online connectivity spike (implemented)

- Direct-IP host/client connection.
- Host-owned player state and swarm probe simulation.
- Input commands, compact snapshots and client interpolation.
- Payload, peer, RTT and disconnect instrumentation.

## M4 — Networked gameplay slice (implemented)

- Haldor movement, health and active ability authority owned by the host; Raven Rush was later replaced by the production Ultimate system.
- Real enemy snapshot replication and client interpolation.
- Weapon events, damage, XP and shared upgrade resolution.
- Boss completion and rewards committed by the host.
- Online HUD, results and compact enemy snapshots below the target MTU.

## M5 — Production foundation (0.7.0 core implemented)

- ScriptableObject content definitions with stable IDs. **Implemented in 0.7.0.**
- Object pools and spatial partitioning. **Implemented in 0.7.0 for local simulation and online presentation/query paths.**
- New Input System with rebinding and controller glyphs. **Implemented in 0.9.0 for P1 keyboard and supported gamepad prompt families.**
- Responsive presentation framework and accessibility settings. **Implemented in 0.9.0 as safe layout/theme/settings contracts around the code-driven HUD view.**
- Audio mixer, music state and prioritised SFX voices. **Implemented as a dependency-free runtime foundation in 0.9.0; authored final assets remain future content work.**
- PlayMode/EditMode test suites and deterministic run seeds. **Implemented through 0.8.0 and expanded for presentation in 0.9.0.**
- Client prediction/reconciliation plus artificial latency, jitter and loss profiles.

Implemented through 0.9.0 release candidate: stable-ID content, one shared Solo/Local simulation, deterministic tests, presentation preferences, keyboard rebinding, controller prompts, audio buses/states, bounded SFX voices, pooled VFX, hero animation and Frostbound ambience. Next after owner acceptance: demonstration-slice content expansion.

## M5.1 — Shared simulation core (0.8.0 planned)

- Upgrade the project and rebuild all executables with Unity `6000.5.4f1` or newer to remediate CVE-2025-59489.
- Extract shared combat, progression and effect rules from the local and online orchestrators.
- Add an extensible effect/status pipeline for future weapons, passives, Ultimates and evolutions.
- Introduce Unity EditMode and PlayMode coverage for deterministic runs, pooling, content validation and save migrations.
- Establish GitHub pull-request validation and keep release builds outside source control.

## M5.2 — Presentation foundation (0.9.0 release candidate)

- Persistent accessibility, audio and keyboard-binding settings.
- Semantic active-device prompts for PC and Steam Deck controller families.
- State-driven music, prioritized SFX voices and browser-safe playback.
- Pooled feedback, Haldor/Eira animation and deterministic Frostbound ambience.
- Safe-area contracts and automated presentation coverage.

## M6 — Demonstration slice

- Three factions: Ravenbound, a researched/fictionalized woodland culture, and a modern expedition corps.
- Three heroes, twelve weapons, twelve passives and six evolutions.
- One polished biome with objectives, boss, extraction and relic reward.
- Camp progression, codex and first-run onboarding.
- Windows build, Steam Deck pass and performance capture.

## M7 — MVP

- Six heroes, three biomes, thirty weapons/passives and twelve evolutions.
- Local co-op production pass after single-player performance targets are met.
- Steam Cloud, achievements, localization and crash reporting.
- Balance telemetry and content-authoring validation.

## M8 — Release candidate

- Content lock, progression wipe policy and save migrations.
- Accessibility, certification, localization and device matrix.
- Store assets, trailer capture and release operations.
- Relay/lobby and production online co-op remain separately estimated after the authoritative gameplay slice.
