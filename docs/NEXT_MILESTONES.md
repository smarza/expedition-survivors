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
- New Input System with rebinding and controller glyphs.
- UI Toolkit/uGUI interface and accessibility settings.
- Audio mixer, music state and prioritised SFX voices.
- PlayMode/EditMode test suites and deterministic run seeds. **Deterministic local seeds and runtime/static foundation checks implemented; Unity Test Framework suites remain.**
- Client prediction/reconciliation plus artificial latency, jitter and loss profiles.

Implemented through 0.7.0: stable-ID ScriptableObject content, character/map selection, shared XP/map rules, strategic Ultimates, gamepad ownership/navigation, per-player build slots, targeted reward turns, item levels, behavioral evolutions, correlated build UI, deterministic local seeds, pooled actors, spatial queries and performance instrumentation. Next: consolidate more local/online simulation rules, add Unity Test Framework coverage and begin the demonstration-slice content expansion.

## M5.1 — Shared simulation core (0.8.0 planned)

- Upgrade the project and rebuild all executables with Unity `6000.5.4f1` or newer to remediate CVE-2025-59489.
- Extract shared combat, progression and effect rules from the local and online orchestrators.
- Add an extensible effect/status pipeline for future weapons, passives, Ultimates and evolutions.
- Introduce Unity EditMode and PlayMode coverage for deterministic runs, pooling, content validation and save migrations.
- Establish GitHub pull-request validation and keep release builds outside source control.

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
