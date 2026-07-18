# Expedition Survivors — master plan and project handoff

> **Document status:** living source of truth for product, engineering and release planning.  
> **Last updated:** 2026-07-18  
> **Stable baseline:** `main` — `0.7.1-foundation-runtime-fixes`  
> **Active development:** `agent/0.8.0-shared-simulation`  
> **Active pull request:** [PR #1 — Start 0.8.0 shared simulation foundation](https://github.com/smarza/expedition-survivors/pull/1)

## 1. How to use this document in a new chat

This file exists so a new development session can recover the intent and technical state of the project without relying on old conversation history.

At the start of a new chat, give the agent the repository URL and ask it to read, in this order:

1. `docs/PROJECT_MASTER_PLAN.md` — product direction, roadmap and handoff;
2. `README.md` — current playable behavior and controls;
3. `RELEASE_NOTES.md` — chronological implementation history;
4. `SECURITY.md` — mandatory Editor and build restrictions;
5. the active PR and current branch diff.

Before changing code, the agent must run:

```bash
git status -sb
git log --oneline --decorate -5
python3 tools/validate_project.py
```

It must preserve unrelated changes, work from the active milestone branch, validate before publishing and update this document whenever a product-level decision changes.

### Suggested prompt for the next chat

> Continue development of Expedition Survivors from `https://github.com/smarza/expedition-survivors`. Read `docs/PROJECT_MASTER_PLAN.md`, `README.md`, `RELEASE_NOTES.md`, `SECURITY.md` and PR #1 before proposing or implementing work. The active milestone is 0.8.0 Shared Simulation. Use branches and commits instead of ZIP handoffs. Preserve all non-negotiable decisions, especially Haldor Stormborn, gamepad ownership, readable UI, per-player co-op rewards and manual high-cooldown Ultimates.

## 2. Executive summary

**Working title:** Expedition Survivors. The title is not yet commercially locked.

Expedition Survivors is an original survivors-like action RPG built around expeditions undertaken by heroes from distinct cultures, eras and fictional factions. The player primarily controls movement and build decisions while weapons attack automatically. A small number of powerful character Ultimates may be activated manually, creating deliberate strategic moments without turning the game into a twin-stick shooter.

The active prototype proves Solo and two-player Local Co-op through one gameplay implementation. It also proves individual controller ownership, alternating co-op rewards, build slots, evolutions, deterministic seeds, object pooling and spatial queries. An earlier Online POC was removed because it duplicated the game simulation. The project must now move from repeated proofs of concept to a shared, testable production architecture and a polished demonstration slice.

## 3. Non-negotiable product decisions

These decisions were explicitly established during development and must not be silently removed or reinterpreted.

1. **Haldor Stormborn is the flagship Viking and the owner's favorite character.** He must remain one of the main heroes and receive exceptional visual, audio and gameplay treatment.
2. **Most combat is automatic.** Movement, positioning and build choices remain the core moment-to-moment actions.
3. **Manual powers are exceptional.** Direct activation is primarily reserved for high-impact Ultimates with long cooldowns. Cooldown improvement can become part of builds and progression.
4. **The game must not become an endless collection of disconnected POCs.** New work must contribute to the launch architecture or to a clearly defined production risk reduction.
5. **Every interface must remain readable.** Text clipping, overlapping regions, misleading hover states and low-contrast buttons are regressions.
6. **Gamepads are first-class input devices.** Solo, Local Co-op, menus and reward selection must all work with gamepads. A future Online mode inherits the same requirement.
7. **Each local player owns an independent device.** With two gamepads, P1 and P2 each use their own controller. When a player must choose a reward, only that player's assigned device controls the choice.
8. **Co-op rewards have explicit ownership.** A level-up turn belongs to one player. Options may target that player, the partner or both, and the UI must show the recipients with character/player icons.
9. **Build growth must be legible.** The HUD and detail view must correlate weapons, gear, levels, stats, catalysts and evolutions.
10. **Progression should require engagement.** Early levels are faster, but the player must still earn the first reward rather than levelling immediately after the run starts.
11. **The intellectual property must be original.** Vampire Survivors is a genre and systems reference, not a source for copied names, art, lore, characters, code or exact content.

## 4. Product vision

### 4.1 Player fantasy

The player leads a legendary survivor into a hostile expedition, gradually assembling a build that changes the hero from vulnerable explorer into a screen-commanding force. Every faction should change the fantasy, silhouette, combat language and build possibilities without requiring a separate game engine.

### 4.2 Design pillars

| Pillar | Meaning |
| --- | --- |
| Movement under pressure | Navigation, spacing, rescue and risk management are the primary manual skills. |
| Automatic combat, strategic agency | Weapons attack automatically; agency comes from positioning, reward choices, builds and rare Ultimates. |
| Expressive builds | Item levels, synergies, catalysts and evolutions must produce visibly different runs. |
| Expedition identity | A map is a timed route with phases, events, objectives, boss pressure and a result, not only an endless arena. |
| Distinct tribes and eras | Heroes belong to recognizable but original factions with different mechanics and presentation. |
| Co-op cooperation without confusion | Shared danger and revival coexist with individual characters, devices, builds and reward ownership. |
| Readable spectacle | Enemy density and effects may become intense, but threats, players, pickups, UI and feedback must remain understandable. |

### 4.3 Initial audience and platform assumptions

These are planning assumptions, not final commercial commitments:

- first commercial target: Windows PC through Steam;
- Steam Deck compatibility is a priority during the demonstration slice and MVP;
- keyboard/mouse and common XInput/DualShock/DualSense-style gamepads;
- Local Co-op is a core product feature;
- Online Co-op is deferred until the shared Solo/Local simulation is mature and is not part of the active runtime;
- macOS, native Linux, consoles and mobile require separate business and certification decisions.

## 5. Core player experience

### 5.1 Session flow

1. Enter camp or main menu.
2. Choose Solo or Local Co-op.
3. Each player chooses a survivor.
4. P1/host chooses an expedition profile.
5. Survive timed phases while collecting XP and resources.
6. Alternate reward decisions between eligible players.
7. Build weapons and gear, level existing items and unlock evolutions.
8. Use Ultimates at high-value moments.
9. Confront the expedition boss and complete the route, or lose the party.
10. Review time, kills, renown, seed, build and unlock progress.
11. Return to camp, retry the same seed or begin another expedition.

### 5.2 Run duration and map structure

The current Frostbound Shore has two development profiles:

- Scout Expedition — 5 minutes, 4 weapon slots and 4 gear slots;
- Long Night — 12 minutes, 6 weapon slots and 6 gear slots.

Production maps should retain time-based pacing inspired by the genre, while adding an expedition identity through:

- authored phase tables;
- enemy-family transitions;
- elite and miniboss thresholds;
- environmental events and optional objectives;
- boss arrival and extraction/completion rules;
- map-specific slot limits, modifiers and rewards.

The exact final duration range must be tested. Short onboarding routes, standard runs and challenge expeditions may coexist.

### 5.3 Combat controls

- movement: keyboard, D-pad or left stick;
- standard weapons: automatic;
- defensive pulses and passive effects: automatic unless a future item explicitly justifies another rule;
- Ultimate: manual, powerful and infrequent;
- pause/build details: explicit menu commands;
- reward choice: only the current player's assigned device;
- gamepad drift must be filtered through a movement deadzone.

### 5.4 Ultimates

Ultimates are the controlled-action exception that differentiates Expedition Survivors without abandoning the survivors-like format.

Requirements:

- a clear ready/cooldown meter per player;
- strong audiovisual anticipation and payoff;
- meaningful tactical timing: rescue, boss burst, crowd clear, protection or recovery;
- long base cooldown;
- controlled cooldown improvements from character mastery, gear or build effects;
- no frequent activation that replaces automatic weapon play;
- deterministic resolution in every active mode; a future Online adapter must add server/host authority without changing the Ultimate rule.

Current examples:

- Haldor — Ravenstorm;
- Eira — Murder of Ravens.

## 6. RPG, build and reward systems

### 6.1 Run progression

- enemies drop experience gems;
- magnet range controls collection distance;
- XP thresholds grow by level and scale for co-op;
- early progression is faster but must not grant an almost immediate first level;
- each level presents four build-aware reward options;
- healing is a fallback when no valid build reward can be offered.

### 6.2 Item taxonomy

| Type | Role |
| --- | --- |
| Weapon | Automatic damage or control behavior with its own runtime level. |
| Defensive weapon | Automatic protection, retaliation, healing or zone control. |
| Gear/passive | Modifies survivor, weapon, economy or cooldown statistics. |
| Catalyst | Satisfies an evolution relationship and may add its own benefit. |
| Evolution | Replaces or transforms a qualified base item with new behavior, not only larger numbers. |
| Boon/resource | Immediate or run-limited benefit such as healing or renown. |
| Ultimate modifier | Rare improvement to power, utility or cooldown within strict balance limits. |

### 6.3 Levels and evolutions

Each item has a visible level and a maximum level. Level increases may alter damage, frequency, area, projectile count, pierce, critical chance, healing, armor or special behavior.

An evolution requires:

1. the correct base item;
2. its required level threshold;
3. the catalyst or relationship defined by stable content IDs;
4. any map/chest/event condition chosen during later balancing.

An evolution must change presentation and behavior. It must not be only a hidden percentage increase.

Current proven examples:

- Frost Axe + Jotunn Rune → Jotunn Cleaver with clustered explosions;
- Raven Guard + Bear-Blooded → Storm Aegis with larger healing pulses.

### 6.4 Co-op reward ownership

- a level-up chooses one active player as the reward-turn owner;
- only that player's device navigates the four cards;
- cards display P1, P2 or shared recipient badges;
- a selected self reward changes only the owner;
- a partner-targeted reward changes only the named partner;
- a shared reward changes both valid players;
- the detailed build view must show each survivor's independent inventory and derived stats.

### 6.5 Meta progression

Current persistence already records local renown, run history, best performance and Haldor mastery in JSON. Production progression should expand to:

- hero and faction unlocks;
- weapons, gear, relics and expedition unlocks;
- mastery tracks without mandatory grind walls;
- codex entries and evolution discovery;
- difficulty/challenge modifiers;
- versioned save migrations;
- Steam Cloud conflict-safe synchronization;
- explicit policy for progression wipes before Early Access or release.

Meta progression must add goals and variety without making a fresh run nonviable.

## 7. Characters, tribes and content direction

### 7.1 Flagship character: Haldor Stormborn

Haldor is the visual and gameplay quality bar for the roster.

- faction: Ravenbound Vikings;
- role: durable expedition leader and shield-bearer;
- silhouette: unmistakable Viking hero, strong shield/axe/raven/frost language;
- personality: relentless, mythic and charismatic rather than generic;
- Ultimate: Ravenstorm;
- production requirement: premium portrait, gameplay sprite/animation, VFX, voice treatment, sound identity and signature evolution path.

No roster expansion is allowed to reduce Haldor to a placeholder-quality character.

### 7.2 Initial faction direction

The demonstration slice targets three clearly differentiated factions:

1. Ravenbound Vikings;
2. a researched, respectful and fictionalized woodland culture rather than a stereotyped representation of a real Indigenous people;
3. a modern expedition corps/soldier faction.

Final names, cultural references and visual motifs require deliberate research and, where appropriate, sensitivity review. Mechanical identity should be derived from faction fantasy without caricature.

### 7.3 Content targets

Demonstration-slice target:

- 3 heroes across 3 factions;
- 12 weapons;
- 12 passives/gear items;
- 6 evolutions;
- 1 polished biome with multiple expedition profiles;
- complete boss, objective, extraction and relic reward loop;
- first-run onboarding, camp and codex slice.

MVP target:

- 6 heroes;
- 3 biomes;
- approximately 30 weapons/passives combined or more, subject to quality review;
- 12 meaningful evolutions;
- a complete progression route and difficulty structure.

Final launch quantity must be validated by playtime, build diversity and production capacity rather than by item count alone.

## 8. Game modes

### 8.1 Solo

- keyboard or any active gamepad;
- one independent build and Ultimate;
- baseline for balance, performance and deterministic testing;
- must be fully playable without multiplayer dependencies.

### 8.2 Local Co-op

- currently supports two players;
- shared camera, tether, XP pool and run state;
- independent health, character, device, build, Ultimate, knockdown and revival;
- with two controllers, P1 receives gamepad 1 and P2 receives gamepad 2;
- with one controller, current predictable fallback is keyboard P1 and gamepad P2;
- menu and reward focus must respect device ownership;
- production expansion beyond two players is not committed.

### 8.3 Online Co-op — deferred

The direct-IP POC was removed from the active runtime because it duplicated player, enemy, weapon, effect and UI rules. Its history remains in Git, but it must not be restored as a second simulation.

Requirements before Online development may restart:

- shared simulation with Solo/Local rather than duplicated rules;
- client prediction and reconciliation;
- latency, jitter, loss and reconnect testing;
- lobby/invite/Relay or platform transport decision;
- host migration or an explicit no-migration policy;
- version/protocol compatibility errors;
- abuse and trust-boundary review;
- save/reward authority and disconnect policy;
- scalable QA matrix.

Online Co-op is a future initiative. It may be scheduled only after the shared Solo/Local simulation and content pipeline are mature enough that networking can be implemented as commands, authority, snapshots and presentation around the same game core.

## 9. Interface, input and accessibility

### 9.1 Current interface rule

Only interactive elements may visually react to pointer hover or selection. Static labels must not change color as though clickable.

### 9.2 Readability requirements

- no clipped map, character, Ultimate, result or HUD text at supported aspect ratios;
- no labels behind bars, item tokens or modal panels;
- safe areas for 16:9, 16:10/Steam Deck and ultrawide evaluation;
- proportional layout rather than fixed-resolution stretching;
- opaque or sufficiently dimmed modal hierarchy;
- consistent typography, spacing, alignment and contrast;
- selected gamepad focus visible without relying only on color;
- concise combat HUD with deeper statistics available on demand;
- item icon, level, capacity, evolution readiness and recipient must be correlated.

### 9.3 UI production path

The current IMGUI interface is a functional prototype. It should be replaced incrementally by UI Toolkit or uGUI after the shared simulation boundary is stable.

Production UI work includes:

- reusable screens and components;
- input-action-based navigation;
- controller glyph system;
- rebinding;
- localization-ready text layouts;
- text-size and safe-area options;
- color-blind-safe markers;
- screen shake, flash and damage-number controls;
- subtitle/caption requirements for voiced or important audio cues.

## 10. Visual and audio direction

### 10.1 Current state

The prototype uses generated primitives and runtime-created visual assets, with original Haldor key art as the primary authored presentation reference. These visuals prove systems but do not represent release quality.

### 10.2 Production direction

- readable stylized top-down silhouettes;
- strong faction-specific shape and palette language;
- high-quality flagship treatment for Haldor;
- distinct enemy telegraphs and elite/boss states;
- evolutions visibly transform attacks;
- controlled VFX density and effect prioritization;
- readable pickups and player identifiers under heavy swarms;
- cohesive biome lighting, props and landmarks.

### 10.3 Audio requirements

- adaptive music states for calm, pressure, boss and victory/defeat;
- faction and character themes;
- prioritized SFX voice budget so critical threats remain audible;
- unique Ultimate charge/ready/activation feedback;
- pickup, level-up, evolution and reward confirmation language;
- Audio Mixer groups and user volume controls;
- accessibility consideration for events currently communicated only by sound.

## 11. Scoring, results and analytics

Current results expose:

- time survived;
- enemies defeated;
- renown recovered;
- run seed;
- victory/defeat;
- map and survivor context;
- best performance/run history in local save data.

A single arcade-style score formula is not yet approved. Before adding one, decide whether score should primarily reward survival, kills, objectives, difficulty, economy or build efficiency. If introduced, the formula must be transparent enough for leaderboards and deterministic enough for validation.

Future result screens should also include:

- per-player damage, healing, revivals and damage taken;
- weapon/evolution damage breakdown;
- level and build timeline;
- boss/objective completion;
- difficulty modifiers;
- unlocks and mastery progress;
- network/session status when relevant.

Telemetry must be privacy-conscious, opt-in where required, documented and separated from gameplay authority.

## 12. Current implementation status

### 12.1 Stable baseline: 0.7.1

Implemented and manually accepted:

- Solo and two-player Local Co-op;
- Haldor and Eira selection;
- Frostbound Shore Scout and Long Night profiles;
- automatic Frost Axe and Raven Guard;
- Haldor and Eira manual Ultimates;
- XP, slower level curve, four-card choices and recipient ownership;
- weapon/gear slots, individual item levels and two behavioral evolutions;
- gamepad assignment, independent co-op ownership and menu navigation;
- knockdown, revival, boss, victory, defeat and run-again flow;
- local JSON persistence;
- object pools for recurring simulation/presentation objects;
- spatial hash for enemy targeting and area queries;
- deterministic local RNG and replay-same-seed support;
- F3 performance/pool/spatial/content diagnostics;
- ScriptableObject content database with stable IDs and code fallback;
- static validation and runtime foundation self-checks;
- UI readability corrections made through 0.7.1.

### 12.2 Active milestone: 0.8.0

Started:

- repository migration and Git workflow;
- patched Unity Editor requirement;
- Unity 6000.5-compatible package matrix with Input System 1.19.0;
- CVE-2025-59489 validation guard;
- security policy and rebuild requirement;
- successful patched-Editor import, compilation and Solo/Local manual smoke pass confirmed on 2026-07-18;
- production runtime, EditMode and PlayMode assembly boundaries;
- Unity Test Framework 1.4.6 with 28 EditMode and 6 PlayMode regressions enforced by GitHub Actions;
- backward-compatible migration from legacy save payloads to versioned save envelope 2;
- first Phase C extraction: local clock, phase, boss trigger, XP, reward turn and outcome now route through `SharedRunModel`;
- Frost Axe flight, lifetime, collision-radius and pierce behavior are extracted into `SharedProjectileModel`;
- player attributes, movement requests, damage/armor, invulnerability, knockdown/revival and Ultimate rules are extracted into `SharedPlayerModel`;
- enemy derived attributes, pursuit movement, contact cadence, knockback and death are extracted into `SharedEnemyModel`;
- the experimental Online runtime, menu and networking packages are removed from active scope;
- incremental shared simulation extraction in progress.

Not yet completed:

- rebuilding the final 0.8.0 Windows acceptance executables with the patched runtime;
- shared simulation implementation;
- further extraction of enemy spawning, remaining weapon and effect rules into testable shared models.

## 13. Current technical architecture

| Component | Current responsibility |
| --- | --- |
| `GameBootstrap` | Creates the runtime prototype from the bootstrap scene. |
| `GameDirector` | Local GameObject orchestration and adapter from shared state to presentation. |
| `SharedRunModel` | Presentation-free run phase, clock, boss trigger, XP, reward turn and terminal outcome. |
| `SharedPlayerModel` | Presentation-free attributes, movement requests, damage, knockdown/revival and Ultimate state. |
| `SharedEnemyModel` | Presentation-free enemy attributes, pursuit, contact cadence, knockback and death state. |
| `SharedProjectileModel` | Presentation-free projectile flight, lifetime, collision radius and pierce budget. |
| `PlayerController` | Local input and GameObject/presentation adapter for `SharedPlayerModel`. |
| `Enemy` | Pooled GameObject/presentation, target selection, drops and spatial-index adapter for `SharedEnemyModel`. |
| `WeaponSystem` | Runtime automatic weapon behavior. |
| `AxeProjectile`, `ExperienceGem` | Focused local simulation actors. |
| `BuildSystem` | Catalog, build slots, levels, reward generation and evolution recipes. |
| `ContentDefinitions` | Characters, maps, Ultimates and balance rules. |
| `ContentAssets` | Authoring records, runtime content load, validation and enemy catalog. |
| `ProductionContentDatabase` | ScriptableObject root for editable production content. |
| `ProductionFoundation` | Pools, spatial hash, deterministic RNG, metrics and startup checks. |
| `LocalInputRouter` | Keyboard/gamepad assignment, ownership and menu actions. |
| `GameHUD` | Current local IMGUI menus, HUD, rewards, build and results. |
| `SaveService` | Versioned local JSON persistence with atomic replacement. |
| `SaveMigration` | Legacy payload migration and current versioned save-envelope serialization. |
| `RuntimeAssets` | Dependency-free prototype sprites and fallback art. |
| `ProjectExpedition.Runtime` | Production runtime assembly referenced by the game and automated suites. |
| `ProjectExpedition.EditModeTests` | Fast deterministic domain, content, build and migration regressions. |
| `ProjectExpedition.PlayModeTests` | Disk-safe bootstrap and expedition-flow smoke regressions. |

### 13.1 Primary architectural debt

Solo and Local Co-op share `GameDirector`, `PlayerController`, `Enemy` and `WeaponSystem`; they are configurations of one implementation. The former Online POC was deliberately removed after demonstrating that a duplicated simulation creates inconsistent behavior and an unsustainable QA matrix. A future Online mode must wrap the mature shared simulation and cannot introduce Online-only copies of gameplay rules.

Other significant debt:

- IMGUI is not the final UI system;
- current runtime-generated visuals are placeholders;
- the initial Unity suite is accepted on the target Editor, but still needs CI automation and broader shared-simulation coverage;
- no production cloud-save integration;
- save migration now covers legacy payload to envelope v2, but needs a durable migration chain and failure recovery before public progression begins;
- no final audio architecture or asset pipeline;
- no final performance target-hardware capture.

## 14. Target architecture for 0.8.0

The exact class names may change during implementation, but the boundaries are required.

### 14.1 Shared simulation layer

A deterministic, host-capable gameplay domain should own:

- run clock and phase state;
- players and derived statistics;
- enemy state and spawning rules;
- XP and level progression;
- weapons, cooldowns, targeting and effects;
- rewards, builds, item levels and evolutions;
- knockdown, revival, boss and run outcome;
- seeded random decisions;
- serializable snapshots/events where needed.

The shared layer must not directly render UI or read physical input devices. If networking returns later, it also must not send transport messages directly.

### 14.2 Adapters

- Local adapter: translates local input into simulation commands and presents state through local GameObjects/UI.
- Presentation adapter: maps simulation events to pooled visual/audio objects.
- Content adapter: resolves ScriptableObject records into validated runtime definitions.
- Persistence adapter: converts durable progression into versioned save models.

### 14.3 Effect pipeline

Weapons, gear, Ultimates and evolutions should compose validated effects rather than adding one-off branches to the directors.

Minimum effect vocabulary:

- direct and area damage;
- projectile, beam/pulse and persistent zones;
- healing, shielding and armor modification;
- knockback, slow and timed status;
- critical and proc conditions;
- spawn-on-hit/death/interval;
- cooldown, duration, area, speed, count and pierce modifiers;
- ownership and team targeting;
- evolution replacement/augmentation.

Every effect must define authority, target rules, stacking behavior, duration, deterministic random use and presentation event.

## 15. Detailed 0.8.0 execution plan

### Phase A — Secure Editor migration

Status: manual import, compilation and Solo/Local smoke testing accepted on 2026-07-18. A final patched-Editor Windows rebuild remains part of the 0.8.0 closeout.

1. Clone/switch to `agent/0.8.0-shared-simulation`.
2. Open with Unity `6000.5.4f1`.
3. Allow package resolution and asset reimport.
4. Confirm Package Manager resolves Input System `1.19.0` and Unity Test Framework `1.4.6` without Netcode/Transport dependencies.
5. Commit legitimate serialization/version/lockfile changes separately.
6. Confirm zero compile errors and no old ScriptableObject warnings.
7. Run Solo and Local smoke tests.
8. Rebuild the Windows acceptance executable.
9. Mark all pre-patch executables non-release.

Exit gate: the project imports and builds only with the patched supported Editor, and all existing modes still start successfully.

### Phase B — Test harness and assembly boundaries

Status: accepted on the target development machine and automated through GitHub Actions with 28 EditMode tests, 6 PlayMode tests, static validation and a gated Windows build.

1. Add runtime and test assembly definitions where they reduce coupling.
2. Add Unity Test Framework EditMode tests for RNG, content IDs, reward eligibility, build slots, evolution prerequisites, spatial membership and save migration.
3. Add PlayMode smoke tests for bootstrap, run start, level-up, replay seed and result flow.
4. Preserve `tools/validate_project.py` as a fast non-Unity guard.
5. Require the GitHub Actions test and Windows-build checks before requesting manual owner validation.

Exit gate: critical deterministic rules fail automatically when regressed.

### Phase C — Extract shared run model

Status: local run progression, player state, enemy state and Frost Axe projectile flight are implemented in presentation-free models. Enemy spawning and remaining-weapon state extraction continue.

1. Define commands, events and read-only state views.
2. Move clock, phase, XP, reward-turn and outcome rules behind the shared boundary.
3. Move player/enemy/weapon state incrementally while keeping the game playable.
4. Keep seeded random ownership explicit.
5. Compare existing Solo behavior before and after extraction.

Exit gate: Solo and Local Co-op runs use the shared model with no feature loss.

### Phase D — Shared effects and builds

1. Introduce effect definitions and runtime instances.
2. Reimplement Frost Axe, Raven Guard, both Ultimates and both evolutions through the shared pipeline.
3. Preserve current balance values until behavior parity is proven.
4. Add tests for targeting, cooldown, stacking, recipients and evolution transitions.

Exit gate: existing content is data/effect-driven and no longer depends on director-specific branches.

### Phase E — Local adapters and presentation

1. Route Local Co-op through the same simulation commands.
2. Keep Solo and Local Co-op as player-count configurations rather than separate rule paths.
3. Separate gameplay state, local input and presentation responsibilities.
4. Preserve independent device ownership and reward focus.
5. Leave networking outside the active milestone.

Exit gate: Solo and Local Co-op resolve the same weapon/build/reward rules from one source.

### Phase F — Regression and milestone close

1. Run the manual mode matrix with keyboard, one gamepad and two gamepads.
2. Validate reward ownership and device focus.
3. Validate same-seed replay, pooling and spatial metrics.
4. Capture high-density performance data.
5. Update this plan, README and release notes.
6. Merge the draft PR only after user acceptance.

## 16. Roadmap to 1.0

Dates are intentionally absent until production velocity and asset capacity are measured. A version advances only after its exit criteria are met.

| Version | Theme | Primary deliverables | Exit criteria |
| --- | --- | --- | --- |
| 0.8.0 | Shared Simulation | Patched Unity, shared Solo/Local rules, effect pipeline and tests; experimental Online removed. | Solo and Local Co-op use one rule source and pass regression. |
| 0.9.0 | Presentation Foundation | Production UI framework, rebinding/glyphs, audio mixer, initial VFX/animation pipeline, Haldor polish pass. | Haldor/Frostbound run is readable and stylistically coherent on PC and Steam Deck. |
| 0.10.0 | Demonstration Content | Three factions/heroes, 12 weapons, 12 gear items, 6 evolutions, polished biome and boss/objective route. | External player can complete the slice without developer instruction. |
| 0.11.0 | Camp and Progression | Camp, unlocks, mastery, codex, tutorial/onboarding, versioned save migrations. | New player reaches a second meaningful run and understands unlock goals. |
| 0.12.0 | MVP Content | Six heroes, three biomes, build variety, challenge/difficulty structure and content-authoring workflow. | Content supports repeated play without dominant mandatory build. |
| 0.13.0 | Co-op Production | Local Co-op polish and a fresh Online feasibility decision after the shared core is mature. | Local Co-op passes its device matrix; any approved future Online architecture has a separate plan and test budget. |
| 0.14.0 | Platform Services | Steam integration, Cloud, achievements, crash reporting, localization framework and telemetry policy. | Services survive offline/conflict/error scenarios without progression loss. |
| 0.15.0 | Alpha | Feature complete, representative content, full progression and internal balance. | No missing launch-critical system; known issues triaged. |
| 0.16.0 | Beta | Content lock, performance, accessibility, localization, compatibility and broad QA. | Target hardware/platform matrices and save migrations pass. |
| 0.17.0 | Release Candidate | Store assets, trailer capture, certification checks, build signing and release operations. | Candidate can ship without code/content changes beyond blockers. |
| 1.0.0 | Launch | Approved binaries, store deployment, support/telemetry monitoring and rollback plan. | Public release and operational ownership established. |

## 17. Quality and acceptance gates

### 17.1 Required checks for every implementation

- zero Unity compilation errors;
- `python3 tools/validate_project.py` passes;
- no new placeholder TODO/FIXME/NotImplemented markers in shipped paths;
- no unrelated file changes;
- relevant Solo regression;
- relevant Local Co-op regression when shared systems change;
- keyboard and gamepad navigation for affected UI;
- readable supported resolutions;
- release notes for user-visible changes;
- tests for new deterministic rules where practical.

### 17.2 Performance targets

Provisional goals, to be finalized against target hardware:

- 60 FPS target on supported desktop and Steam Deck presentation profiles;
- stable frame pacing during representative maximum swarm density;
- no steady-state Instantiate/Destroy churn for recurring enemies, projectiles, gems or pulse effects;
- no unbounded pool, entity or UI accumulation across replay cycles;
- metrics capture includes FPS, worst frame, actors, created/reused counts and spatial queries.

### 17.3 Determinism definition

A run seed controls the game's random sequence: arena decoration, spawn decisions, enemy variation, critical rolls, renown and reward generation. The result action `REPLAY SAME SEED` resets that random sequence. Player commands and frame timing are not currently recorded. Therefore the same seed reproduces random inputs, but a fully identical outcome requires equivalent input/timing or a future command-replay system.

## 18. Save, cloud and compatibility policy

- saves are versioned data, not raw runtime object serialization;
- every breaking schema change requires a migration or an explicit pre-release wipe policy;
- writes must remain atomic and recoverable;
- cloud sync must handle local/cloud conflicts without silent progression loss;
- multiplayer rewards are committed only by the authority defined for the mode;
- development/debug settings must not contaminate production saves;
- public test builds must record game version, save version and content version for support.

## 19. Security policy

CVE-2025-59489 affects executables built by vulnerable Unity Editor versions. The project was previously pinned to `6000.0.40f1`. Development from 0.8.0 is pinned to `6000.5.4f1`; the first corrected Unity 6.0 LTS release was `6000.0.58f2`.

Rules:

- do not distribute old prototype executables as release candidates;
- rebuild all supported platform binaries with a patched Editor;
- keep the Editor-version validation guard active;
- do not commit credentials, signing keys, platform secrets or private SDK configuration;
- the repository is currently public, so third-party/commercial assets require explicit redistribution rights;
- report security problems privately as described in `SECURITY.md`.

Official Unity advisory: https://unity.com/security/sept-2025-01

## 20. GitHub workflow

Repository: https://github.com/smarza/expedition-survivors

### Branches

- `main`: last user-accepted stable milestone;
- `agent/<version>-<description>`: active implementation branch;
- short-lived focused branches may be used when parallel work becomes useful.

### Pull requests

- open as draft while implementation is incomplete;
- document what changed, why, user impact and validation;
- keep changes scoped to the milestone;
- update after user PlayMode acceptance;
- merge only when the milestone's exit gate is met.

### Versions and releases

- source code is shared through Git, not ZIP handoffs;
- tags identify accepted milestones, for example `v0.7.1`;
- executable builds belong in GitHub Releases or the chosen distribution channel, not the source tree;
- Unity `Library`, `Temp`, `Logs`, `UserSettings`, IDE files and local builds remain ignored;
- large production assets may require Git LFS after repository and pipeline review.

## 21. Open product decisions

These decisions must be made deliberately and recorded here:

1. Final commercial title and trademark/domain availability.
2. Exact identity and name of the woodland faction after research/sensitivity review.
3. Whether and when Online Co-op returns after the shared game core is mature; it is not in the current runtime or 0.8 scope.
4. Steam Early Access versus full launch strategy.
5. Final art style, animation technique and asset-production capacity.
6. Final standard expedition duration and supported alternate modes.
7. Score/leaderboard philosophy and anti-cheat requirements.
8. Monetization and DLC policy; no design currently assumes microtransactions.
9. Launch languages and localization budget.
10. Target minimum PC and Steam Deck performance profiles.
11. Console/mobile scope and controller/certification implications.
12. Number of Local Co-op players at launch; two is the current commitment.

## 22. Definition of launch-ready 1.0

Expedition Survivors is not launch-ready merely because a run can be completed. Version 1.0 requires:

- a coherent, original identity and commercially approved title;
- polished Haldor and a roster/content offering that supports build variety;
- complete Solo and committed co-op modes;
- readable production UI with keyboard/gamepad navigation and accessibility settings;
- stable progression, migrations and cloud behavior;
- target-platform performance and compatibility passes;
- authored audio, VFX, animation and biome presentation;
- onboarding, codex and understandable evolution discovery;
- crash reporting, support diagnostics and privacy-compliant telemetry choices;
- security review and patched/signable builds;
- localization and store/certification assets;
- no launch-blocking defects or silent progression-loss paths;
- release, rollback, hotfix and community-support procedures.

## 23. Immediate handoff state

The next developer/agent should do the following, in order:

1. Work on `agent/0.8.0-shared-simulation` and PR #1.
2. Pull the Phase B test-harness commit and allow Unity `6000.5.4f1` to resolve Unity Test Framework `1.4.6`.
3. Run all 28 EditMode and 6 PlayMode tests using `docs/TESTING_0.8.md`; report any failure with its full stack trace.
4. Capture and commit only legitimate package-lock or serialization changes produced by the target Editor.
5. Continue extracting enemy spawning, weapon and effect state incrementally for the shared Solo/Local implementation.
6. Keep the game playable and run automated plus relevant Solo/Local regressions after every extraction step.
7. Rebuild patched Windows acceptance executables before closing 0.8.0.
8. Update this document whenever architecture, scope, platform or release decisions change.

Do not begin bulk content creation before the shared simulation and test boundary is credible. Do not replace Haldor's flagship role, per-player gamepad ownership, co-op reward targeting, readable UI or strategic Ultimate philosophy without explicit owner approval.
