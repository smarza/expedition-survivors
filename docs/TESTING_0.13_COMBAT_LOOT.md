# Testing 0.13 — Combat Progression, Obstacles and Loot

Automated coverage extends the 0.12 gate with EditMode tests for enemy level scaling, obstacle collision, loot drop curves and party-shared activation.

## Automated contracts

EditMode adds coverage for:

- `SharedEnemyLevelTests` — spawn level offsets, `Max(time, level)` difficulty, XP scaling;
- `SharedObstacleModelTests` — movement blocking, spawn rejection, simple enemy steering;
- `SharedLootProgressTests` — rarity curve floor, per-color counters, discard-while-active rule;
- `SharedTemporaryEffectTests` — stackable multi-color buffs and independent expiration.

Existing 0.8–0.12 suites remain required.

## Manual owner matrix

### Enemy level progression

1. Start a Scout run at level 1 and confirm new enemies show level **2** labels.
2. Level up once and confirm **new** spawns show level **3** while older enemies keep their original label.
3. Confirm weaker leftover enemies drop less XP than freshly spawned higher-level enemies.

### Obstacles

1. Move into geometric blockers and confirm the player slides instead of passing through.
2. Kite enemies around a wall cluster and confirm they path around rather than clipping through.
3. Repeat in Local Co-op and confirm tethering still works near obstacles.

### Loot and party effects

1. Defeat enemies until colored loot pickups appear (blue, orange, green, red, purple).
2. Confirm the objective rail shows:
   - the leading color summary as `N/10` in **PARTY LOOT**;
   - all five colors simultaneously in **ALL COLORS** with per-color `N/10`, mini bars and active timers.
3. Collect ten units of one color and confirm:
   - announcement activation toast;
   - glow on party members;
   - effect-specific HUD hint (regen, crit, speed, damage, shield);
   - counter resets for that color only.
4. While one color is active, confirm other colors can still be collected toward their own counters.
5. Activate two different colors and confirm both buffs run simultaneously until each timer expires.
6. In co-op, confirm either player can increment the shared counter for any color.

| Color | Loot | Expected effect |
|-------|------|-----------------|
| Blue | Healing Embers | Party regeneration |
| Orange | Critical Flare | Increased crit chance |
| Green | Swift Trail | Increased move speed |
| Red | Wrath Embers | Increased weapon damage |
| Purple | Aegis Veil | Party invulnerability |

### Regression carryover

1. Level-up pause, boss route, extraction beacon and shard objectives still behave as in `docs/TESTING_0.12.md`.
2. Keyboard, gamepad and touch flows from 0.11 input parity remain green.

Record failures with map/hero, player count, seed and reproduction steps.
