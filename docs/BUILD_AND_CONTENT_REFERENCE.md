# Expedition Survivors — build, reward and character reference

> **Applies to:** 0.8.0 Shared Simulation release candidate
> **Purpose:** player-facing rules reference and authoring contract for future content
> **Runtime sources of truth:** `ContentDefinitions.cs`, `BuildSystem.cs`, `SharedPlayerModel.cs`, `SharedEffectModel.cs` and `ProductionContent.asset`

## 1. The most important level rule

Taking a weapon or gear reward increases that item by exactly one level. A level applies the modifier written for that specific level; it does **not** apply every attribute mentioned in the item's general description.

Examples:

- Frost Axe level 2 increases damage, level 3 reduces attack interval, level 4 adds pierce and level 6 adds the second projectile.
- A Frost Axe at level 8 must have two projectiles per volley and two pierce before the Jotunn Rune or Jotunn Cleaver bonuses.
- Raven Guard levels 5 and 8 improve both damage and frequency. Other Raven Guard levels grant damage or armor as listed.
- Gear repeats its listed modifier at every level unless its table explicitly says otherwise.

Reward cards show the exact next-level effect. The Expedition Build screen shows the resulting live values, not only the names of acquired items.

## 2. Terminology and formulas

| Term | Meaning |
| --- | --- |
| Damage | Raw damage before the target's mitigation rule. |
| Interval | Seconds between automatic activations. Lower is better. |
| Rate | Activations per second: `rate = 1 / interval`. |
| Projectiles | Axes created in one Frost Axe volley. |
| Pierce | Number of enemy impacts available to one projectile before it is released. |
| Critical chance | Probability that a Frost Axe deals `2×` damage and uses its stronger radius/knockback. |
| Armor | Flat contact-damage reduction: `received = max(1, raw contact damage - armor)`. |
| XP magnet | Radius used to collect experience gems. |
| Ultimate interval | Full cooldown after activation. Lower is better. |
| Area/radius | World-space distance from the effect center. |

Multiplicative upgrades compound. Two `+26%` damage levels produce `base × 1.26²`, not `base × 1.52`. Displayed values may be rounded; the simulation retains floating-point precision.

## 3. Characters and powers

All current characters can receive the same 0.8.0 item pool and begin every expedition with Frost Axe level 1 and Raven Guard level 1. Character-exclusive weapons and reward pools do not exist yet.

| Character | Stable ID | Role | Health | Move speed | Armor | Ultimate |
| --- | --- | --- | ---: | ---: | ---: | --- |
| Haldor Stormborn | `ravenbound.haldor` | Expedition Leader | 150 | 4.45 | 2.0 | Ravenstorm |
| Eira Raven-Sworn | `ravenbound.eira` | Storm Scout | 122 | 5.05 | 0.5 | Murder of Ravens |

### Haldor Stormborn — Ravenstorm

- Base damage: `145`.
- Base radius: `6.8`.
- Base cooldown: `60 s`.
- Activation grants `1.25 s` of damage immunity and damages every enemy overlapping the radius.
- Haldor mastery modifies starting Frost Axe damage before run upgrades: `24 × (1 + min(0.15, mastery × 0.005))`.

### Eira Raven-Sworn — Murder of Ravens

- Base damage: `112`.
- Base radius: `5.8`.
- Base cooldown: `52 s`.
- Activation grants `1.25 s` of damage immunity and damages every enemy overlapping the radius.

### Shared Ultimate rules

- A run starts with remaining Ultimate time equal to `30%` of the current cooldown: Haldor waits `18 s`; Eira waits `15.6 s`.
- Raven Hourglass level `n`: `cooldown = max(28, base cooldown × 0.9ⁿ)`.
- Final Verse level `n`: `damage = base damage × 1.3ⁿ`.
- Final Verse area derives from the same multiplier: `radius = base radius × (1 + (damage multiplier - 1) × 0.35)`.
- Improving cooldown preserves the current charge percentage instead of granting a free activation.

## 4. Weapons

### Frost Axe

Base behavior: automatically targets the nearest enemy, travels at `9.5` world units/s for up to `3.2 s`, and rolls critical chance independently for each projectile.

| Level | Modifier granted at this level | Damage | Interval | Rate | Projectiles | Pierce |
| ---: | --- | ---: | ---: | ---: | ---: | ---: |
| 1 | Base weapon | 24.00 | 0.820 s | 1.22/s | 1 | 1 |
| 2 | `+26%` damage | 30.24 | 0.820 s | 1.22/s | 1 | 1 |
| 3 | `-14%` interval | 30.24 | 0.705 s | 1.42/s | 1 | 1 |
| 4 | `+1` pierce | 30.24 | 0.705 s | 1.42/s | 1 | 2 |
| 5 | `+26%` damage | 38.10 | 0.705 s | 1.42/s | 1 | 2 |
| 6 | `+1` projectile per volley | 38.10 | 0.705 s | 1.42/s | 2 | 2 |
| 7 | `-14%` interval | 38.10 | 0.606 s | 1.65/s | 2 | 2 |
| 8 | `+26%` damage | 48.01 | 0.606 s | 1.65/s | 2 | 2 |

Additional values:

- base critical chance: `8%`;
- critical damage: `2×`;
- normal/critical hit radius: `0.25 / 0.32`;
- normal/critical knockback: `0.18 / 0.42`;
- multiple projectiles use a `10°` spread and can visually overlap while travelling toward the same nearby target;
- Haldor mastery changes the damage column proportionally but does not change interval, count or pierce.

### Raven Guard

Base behavior: automatically releases a defensive area pulse centered on its owner.

| Level | Modifier granted at this level | Damage | Interval | Rate | Armor gained from item |
| ---: | --- | ---: | ---: | ---: | ---: |
| 1 | Base weapon | 20.00 | 5.200 s | 0.19/s | 0 |
| 2 | `+42%` damage | 28.40 | 5.200 s | 0.19/s | 0 |
| 3 | `+1` armor | 28.40 | 5.200 s | 0.19/s | 1 |
| 4 | `+42%` damage | 40.33 | 5.200 s | 0.19/s | 1 |
| 5 | `+42%` damage and `-14%` interval | 57.27 | 4.472 s | 0.22/s | 1 |
| 6 | `+1` armor | 57.27 | 4.472 s | 0.22/s | 2 |
| 7 | `+42%` damage | 81.32 | 4.472 s | 0.22/s | 2 |
| 8 | `+42%` damage and `-14%` interval | 115.47 | 3.846 s | 0.26/s | 2 |

Additional values:

- base radius: `2.55`;
- knockback: `0.72`;
- Raven Guard does not heal until it evolves into Storm Aegis.

## 5. Gear and boons

| Item | Stable ID | Max | Effect per level | Maximum contribution |
| --- | --- | ---: | --- | --- |
| Longship Boots | `gear.longship_boots` | 5 | `+0.46` move speed | `+2.30` move speed |
| Bear-Blooded | `gear.bear_blooded` | 5 | `+24` maximum health and `+24` current health | `+120` maximum health |
| Raven Armor | `gear.raven_armor` | 5 | `+1` armor | `+5` armor |
| Saga Carver | `gear.saga_carver` | 5 | `+9` percentage points Frost Axe critical chance | `+45` points; total chance is capped at `55%` |
| Raven Hourglass | `gear.raven_hourglass` | 5 | Multiply base Ultimate cooldown by `0.9` | Haldor `35.43 s`; Eira `30.71 s` |
| Final Verse | `gear.final_verse` | 5 | Multiply Ultimate damage by `1.3`; derive radius from that multiplier | `3.713×` damage; Haldor radius `13.26`, Eira radius `11.31` |
| Jotunn Rune | `gear.jotunn_rune` | 1 | `+1` Frost Axe pierce and unlock the Jotunn Cleaver recipe | One catalyst level |
| Field Rations | `boon.field_rations` | 99 | Restore `24` health immediately | No slot; cannot exceed maximum health |

Field Rations is a fallback when no eligible build reward can be offered. It does not remain in the build inventory.

## 6. Evolutions

An evolution becomes eligible only when the base weapon is at maximum level, the catalyst is owned and the base weapon has not already evolved.

### Jotunn Cleaver

- Stable ID: `evolution.jotunn_cleaver`.
- Recipe: Frost Axe level 8 + Jotunn Rune.
- Immediately multiplies Frost Axe damage by `1.55` and adds `2` pierce.
- Every axe impact also creates a radius `1.25` explosion dealing `42%` of that projectile's damage to overlapping enemies.

### Storm Aegis

- Stable ID: `evolution.storm_aegis`.
- Recipe: Raven Guard level 8 + Bear-Blooded.
- Immediately multiplies Raven Guard damage by `1.35`.
- Increases pulse radius from `2.55` to `3.35`.
- Heals `0.65` health for each enemy hit, capped at `9` health per pulse.

## 7. Rewards, slots and Local Co-op ownership

- Every level-up presents four build-aware options.
- Scout Expedition provides `4` weapon slots and `4` gear slots per player.
- Long Night provides `6` weapon slots and `6` gear slots per player.
- Starter Frost Axe and Raven Guard occupy weapon slots.
- A reward can target P1, P2 or both players. The card displays its recipients.
- In Local Co-op, reward-turn ownership alternates. Only the chooser's assigned device can navigate and confirm.
- A shared reward applies the same item to both eligible builds. If their current levels differ, the card shows the separate next level and modifier for each player.
- Evolutions and Field Rations are never generated as shared team rewards.
- Completed evolution recipes receive extra selection weight so their payoff is not hidden indefinitely.

## 8. Survivor, damage and revival rules

- Contact damage after armor is `max(1, raw damage - armor)`.
- A successful contact hit grants `0.22 s` of damage immunity.
- A downed player can be revived by a living partner within `1.8` world units.
- Full revive progress requires `2.5 s` of nearby rescue time.
- Progress decays at `35%` of normal speed while no rescuer is nearby.
- A revived player returns with `42%` maximum health and `1 s` of immunity.
- If every player is downed, the expedition ends in defeat.

## 9. XP, maps, enemies and spawning

XP required for the next level:

```text
solo XP = round(48 + level × 8 + level^1.35 × 2)
Local Co-op XP = round(solo XP × 1.35)
```

| Expedition | Duration | Boss time | Base/min spawn interval | Difficulty ramp | Slots |
| --- | ---: | ---: | --- | ---: | --- |
| Frostbound Shore — Scout | 300 s | 240 s | `0.86 / 0.24 s` | 46 s | 4 weapon + 4 gear |
| Frostbound Shore — Long Night | 720 s | 630 s | `0.82 / 0.18 s` | 42 s | 6 weapon + 6 gear |

- Difficulty at elapsed time `t`: `1 + t / map difficulty ramp`.
- Regular group size: `clamp(1 + floor(t / 35), 1, 7)`.
- Regular-enemy active cap: `260`; the boss is never suppressed by that cap.
- Enemies spawn between `8.5` and `11.8` world units from the current party center.
- Draugr health: `20 + difficulty × 5.5`; contact damage: `9 + difficulty × 0.6`.
- Jotunn health: `620 + difficulty × 80`; contact damage: `22`.

## 10. Reading the Expedition Build screen

The screen is divided into three live-stat blocks per player:

1. **Survivor** — health, armor, move speed, XP magnet, state and every Ultimate value.
2. **Frost Axe** — damage, interval, rate, projectile count, pierce, critical values, projectile movement/collision values and evolution explosion.
3. **Raven Guard** — damage, interval, rate, radius, knockback, healing and evolution state.

Each item card shows its current level effect and the exact next-level effect. Values in the stat blocks are authoritative for the current run and already include character bases, mastery, item levels and evolutions.

## 11. Required template for future content

Every new character, weapon, gear, boon, power or evolution must include all of the following before it is accepted:

1. stable content ID and category;
2. eligible characters/factions and slot behavior;
3. base statistics with units;
4. exact level-by-level table, including caps and multiplicative/additive semantics;
5. targeting, ownership, timing, stacking and deterministic-random rules;
6. formulas for every derived value;
7. evolution/catalyst recipe and exact behavioral change;
8. reward-card next-level text;
9. complete Expedition Build live-stat coverage;
10. player reference entry in this document;
11. EditMode tests for the level table and formulas;
12. PlayMode coverage proving the reward reaches the runtime adapter;
13. Solo and Local Co-op gameplay validation after CI is green.

Generic descriptions such as “improves damage and frequency” are supporting flavor only. They never replace the exact modifier shown on the reward card, build screen and level table.
