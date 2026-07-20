# Demonstration Content 0.10.0

> **Status:** owner-approved design brief for milestone implementation  
> **Applies from:** `v0.9.0` presentation foundation  
> **Exit gate:** an external player completes the Scout expedition without developer instruction

## 1. Factions and heroes

Three factions, three selectable heroes. Eira remains in content for co-op variety but the demonstration roster centers on one hero per faction.

| Hero | Stable ID | Faction | Role | Ultimate | Starter weapons |
| --- | --- | --- | --- | --- | --- |
| Haldor Stormborn | `ravenbound.haldor` | Ravenbound Vikings | Expedition Leader | Ravenstorm | Frost Axe + Raven Guard |
| Sylva Reedwalker | `oathbound.sylva` | Oathbound Grove | Canopy Warden | Verdant Tempest | Grove Thorn Lash + Canopy Vortex |
| Captain Mara Voss | `ironway.mara` | Ironway Expedition Corps | Field Captain | Orbital Barrage | Signal Flare + Supply Pulse |

### Sylva Reedwalker — Oathbound Grove

- **Faction fantasy:** a fictional woodland culture bound by oaths to living groves, not a portrayal of any real Indigenous people.
- **Silhouette:** layered leaf-cloak, antler brooch, thorn-wrapped staff.
- **Stats:** 128 HP, 4.85 move, 1.0 armor.
- **Ultimate Verdant Tempest:** 54 s cooldown, 118 damage, 6.2 radius — brief invulnerability and a expanding thorn ring.

### Captain Mara Voss — Ironway Expedition Corps

- **Faction fantasy:** modern expedition corps soldier with signal gear and field medicine.
- **Silhouette:** hard-shell pack, visor band, flare launcher.
- **Stats:** 135 HP, 4.65 move, 1.5 armor.
- **Ultimate Orbital Barrage:** 58 s cooldown, 132 damage, 6.5 radius — brief invulnerability and a concentrated strike zone.

## 2. Weapon catalog (12)

| Stable ID | Name | Behavior | Faction flavor |
| --- | --- | --- | --- |
| `weapon.frost_axe` | Frost Axe | Projectile volley + crit | Ravenbound |
| `weapon.raven_guard` | Raven Guard | Owner pulse | Ravenbound |
| `weapon.north_wind_spear` | North Wind Spear | Directed projectile | Ravenbound |
| `weapon.rune_bolt` | Rune Bolt | Fast projectile | Ravenbound |
| `weapon.oath_ring` | Oath Ring | Orbit blades | Oathbound |
| `weapon.grove_thorn_lash` | Grove Thorn Lash | Fast owner pulse | Oathbound |
| `weapon.canopy_vortex` | Canopy Vortex | Radial burst | Oathbound |
| `weapon.driftwood_staff` | Driftwood Staff | Slow radial burst | Oathbound |
| `weapon.signal_flare` | Signal Flare | Explosive projectile | Ironway |
| `weapon.supply_pulse` | Supply Pulse | Heal pulse | Ironway |
| `weapon.iron_beacon` | Iron Beacon | Large owner pulse | Ironway |
| `weapon.tide_caller` | Tide Caller | Wide projectile | Shared shore |

## 3. Gear catalog (12)

| Stable ID | Name | Effect |
| --- | --- | --- |
| `gear.longship_boots` | Longship Boots | +0.46 move speed |
| `gear.bear_blooded` | Bear-Blooded | +24 max/current health |
| `gear.raven_armor` | Raven Armor | +1 armor |
| `gear.saga_carver` | Saga Carver | +9 crit points (projectile weapons) |
| `gear.raven_hourglass` | Raven Hourglass | -10% Ultimate cooldown |
| `gear.final_verse` | Final Verse | +30% Ultimate damage/area |
| `gear.windswept_cloak` | Windswept Cloak | +0.38 move speed |
| `gear.hollow_gourds` | Hollow Gourds | +20 max/current health |
| `gear.oath_feather` | Oath Feather | +1 armor |
| `gear.signal_magnet` | Signal Magnet | +0.55 XP magnet |
| `gear.field_manual` | Field Manual | -10% Ultimate cooldown |
| `gear.jotunn_rune` | Jotunn Rune | Frost Axe catalyst (+1 pierce) |

Additional catalysts: `gear.grove_seed`, `gear.flare_core`, `gear.oath_band`.

## 4. Evolutions (6)

| Stable ID | Recipe | Behavioral change |
| --- | --- | --- |
| `evolution.jotunn_cleaver` | Frost Axe max + Jotunn Rune | Cluster explosions on hit |
| `evolution.storm_aegis` | Raven Guard max + Bear-Blooded | Larger pulse + heal on hit |
| `evolution.grove_crown` | Grove Thorn Lash max + Grove Seed | Thorns leave damaging ground patches |
| `evolution.signal_storm` | Signal Flare max + Flare Core | Flares chain to two nearby enemies |
| `evolution.oath_maelstrom` | Oath Ring max + Oath Band | Extra orbit blade + wider radius |
| `evolution.iron_sanctuary` | Iron Beacon max + Field Manual | Beacon persists as a moving shield zone |

## 5. Frostbound Scout expedition route

Phases for `frostbound.scout`:

| Phase | Time | Announcement |
| --- | ---: | --- |
| Shoreline | 0–90 s | THE SHORE AWAKENS |
| Driftwood | 90–180 s | DRIFTWOOD RUN |
| Warlord Approach | 180–240 s | THE COAST TIGHTENS |
| Boss | 240 s+ | THE JOTUNN HAS FOUND YOU |
| Extraction | After boss kill | REACH THE BEACON |

Objectives:

1. **Required — Cull the Raider Tide:** defeat 80 Draugr (`objective.draugr_cull`, target 80). Unlocks boss eligibility even before timer.
2. **Optional — Recover Rune Shards:** collect 5 shard pickups (`objective.rune_shards`, target 5). Grants bonus relic tier.

Extraction: after Jotunn defeat, a beacon spawns at `(0, 14)`. Players must reach within 3.5 units or survive 15 s at the beacon to complete victory.

## 6. Relic rewards

| Stable ID | Name | Grant rule |
| --- | --- | --- |
| `relic.jotunn_echo` | Jotunn Echo | Scout victory |
| `relic.jotunn_echo_warden` | Jotunn Echo Warden | Scout victory + optional objective complete |

Relics persist in save envelope v3 (`MetaProgress.RelicsCollected`).

## 7. Elite enemy

`enemy.frost_wraith_captain` — elite miniboss spawning in Warlord Approach phase when objective progress ≥ 50%.
