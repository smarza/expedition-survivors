# MVP Content 0.12.0

Milestone **0.12.0** completes the slice MVP: six heroes, three biomes, twelve evolutions, challenge modifiers and a production content-authoring workflow.

## Heroes (6)

| Hero | ID | Faction | Starter weapons |
| --- | --- | --- | --- |
| Haldor Stormborn | `ravenbound.haldor` | Ravenbound Vikings | Frost Axe, Raven Guard |
| Eira Raven-Sworn | `ravenbound.eira` | Ravenbound Vikings | Frost Axe, Raven Guard |
| Sylva Reedwalker | `oathbound.sylva` | Oathbound Grove | Grove Thorn Lash, Canopy Vortex |
| Bren Oakhart | `oathbound.bren` | Oathbound Grove | Driftwood Staff, Oath Ring |
| Captain Mara Voss | `ironway.mara` | Ironway Expedition Corps | Signal Flare, Supply Pulse |
| Rex Calder | `ironway.rex` | Ironway Expedition Corps | Iron Beacon, Tide Caller |

## Expeditions (6 maps, 3 biomes)

| Map | ID | Biome | Scout/Saga | Boss |
| --- | --- | --- | --- | --- |
| The Frostbound Shore | `frostbound.scout` | `frostbound` | Scout (5 min) | Jotunn Warlord |
| The Frostbound Shore: Long Night | `frostbound.saga` | `frostbound` | Saga (12 min) | Jotunn Warlord |
| The Verdant Canopy | `oathbound.scout` | `oathbound.canopy` | Scout (5 min) | Heartwood Colossus |
| The Verdant Canopy: Deep Root | `oathbound.saga` | `oathbound.canopy` | Saga (12 min) | Heartwood Colossus |
| The Scorched Relay | `ironway.scout` | `ironway.relay` | Scout (5 min) | Siege Automaton |
| The Scorched Relay: Siege Line | `ironway.saga` | `ironway.relay` | Saga (12 min) | Siege Automaton |

Each map declares `biomeId`, regular/elite/boss enemy IDs, kill objective labels, optional pickup labels and standard/bonus victory relic IDs.

## Build catalog (30 slot items + 12 evolutions)

- **13 weapons** including `weapon.root_lance` (Bren's directed lance volley).
- **17 gear** items including `gear.sap_vial` (sap regen) and `gear.siege_plating` (armor + Breach Beacon catalyst).
- **12 behavioral evolutions** — the original six plus Canopy Eye, Root Cathedral, North Gale, Supply Chain, Root Lance Bloom and Breach Beacon.
- **1 boon** (`boon.field_rations`) that does not consume a build slot.

## Enemies (9)

| Role | Frostbound | Canopy | Relay |
| --- | --- | --- | --- |
| Regular | Draugr Raider | Bramble Stalker | Scrap Drone |
| Elite | Frost Wraith Captain | Canopy Warden | Signal Raider |
| Boss | Jotunn Warlord | Heartwood Colossus | Siege Automaton |

## Challenge structure

- **Standard / Veteran** tiers adjust enemy health, spawn pressure and renown payout.
- **Mutators:** Iron Resolve (Frostbound), Swarm Surge (Canopy), Relentless Clock (Relay), plus Glass Cannon for saga clears.
- Save envelope **v5** retroactively unlocks canopy/relay scouts, Bren/Rex and challenge entries for eligible v4 profiles.

## Authoring workflow

1. Edit `Assets/Resources/Content/ProductionContent.asset` (YAML) using stable dotted IDs.
2. Run **Expedition → Validate Production Content** in the Unity Editor (`ProductionContentValidator`).
3. Keep code fallbacks in `ContentDefinitions`, `BuildSystem`, `ContentAssets` and `SharedWeaponRegistry` aligned with the asset.
4. `python3 tools/validate_project.py` enforces minimum counts, evolution recipes, weapon level tables and automated test inventory before CI.

See `docs/BUILD_AND_CONTENT_REFERENCE.md` for level-effect rules and `docs/TESTING_0.12.md` for acceptance coverage.
