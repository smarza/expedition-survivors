# Testing 0.12.0 — MVP Content

The automated gate is **97 EditMode + 14 PlayMode tests**. GitHub Actions must also pass static validation, WebGL compilation, GitHub Pages deployment and the Windows milestone build before owner validation.

## Automated contracts

EditMode adds coverage for:

- `SharedChallengeProfileModel` renown, veteran scaling, Swarm Surge group size, Glass Cannon damage modifiers and Iron Resolve healing gate;
- production content load/validate after `ProductionContentRuntime.Load()` (six heroes, six maps, thirty slot items, twelve evolutions, nine enemies);
- evolution recipe base/catalyst integrity across the full catalog;
- canopy and relay Scout route objectives, boss announcements and heartwood relic tiers;
- save envelope v4 → v5 migration with retroactive canopy/relay/Bren/Rex unlocks;
- Bren (175) and Rex (210) renown purchase costs.

PlayMode adds coverage for:

- canopy Scout extraction victory after Heartwood boss defeat and beacon arrival (mirrors Frostbound Scout smoke).

Existing 0.11 camp/progression, 0.10 expedition route, 0.9 presentation and 0.8 shared-simulation suites remain required.

## Editor validation

Run **Expedition → Validate Production Content** after editing `ProductionContent.asset`. The validator checks stable IDs, map biome/enemy/relic fields, evolution recipes and `SharedWeaponRegistry` weapon profiles.

## Final owner matrix

Use the GitHub Pages preview first, then the Windows artifact for native validation.

### Three-biome loop

1. Unlock or migrate into canopy Scout (`oathbound.scout`) and relay Scout (`ironway.scout`).
2. Complete one canopy Scout run — confirm STALKERS objective, SAP pickups and Heartwood boss announcement.
3. Complete one relay Scout run — confirm DRONES objective, CRATE pickups and Siege Automaton announcement.
4. Purchase or migrate Bren and Rex; confirm distinct starter weapons and silhouettes.

### Build variety

1. Evolve at least one new 0.12 recipe (e.g. Canopy Eye, Root Lance Bloom, Breach Beacon).
2. Confirm no single evolution is mandatory to finish any Scout route.

### Challenge modifiers

1. After first Scout victory, confirm Veteran tier unlock.
2. Confirm biome-default mutator unlocks follow relic grants (Iron Resolve / Swarm Surge / Relentless Clock).
3. Run a Veteran + mutator Scout — confirm earlier boss or larger groups without broken reward/healing rules.

### Regression carryover

1. Full matrices in `docs/TESTING_0.11.md`, `docs/TESTING_0.10.md` and `docs/TESTING_0.9.md` remain required.
2. Save migration from legacy v1–v4 payloads must preserve renown, relics and mastery without retroactive purchase charges.

Record failures with browser/native target, resolution, save state, map/hero/challenge selection and reproduction steps.
