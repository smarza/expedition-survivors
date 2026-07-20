# Camp and Progression 0.11.0

> **Status:** owner-approved design brief for milestone implementation  
> **Applies from:** `v0.10.0` demonstration content  
> **Exit gate:** a new player completes one Scout run, returns to camp, spends renown on a meaningful unlock, and starts a second run with clear goals

## 1. Renown economy

- **TotalRenown** — lifetime renown earned from expeditions (unchanged accrual from 0.8+).
- **SpentRenown** — renown committed to camp unlock purchases.
- **Available renown** — `TotalRenown - SpentRenown`; shown on the camp ledger and results screen.

Run renown accrual (per run, unchanged):

- `recoveredRenown` from pickups during the run;
- `max(1, kills / 10)` bonus;
- `+50` on victory.

Typical first Scout run yields **50–80 available renown** after return to camp.

## 2. Unlock catalog

Purchases deduct from available renown and append the stable content ID to `UnlockedContentIds`.

| Stable ID | Display name | Cost | Category |
| --- | --- | ---: | --- |
| `ravenbound.haldor` | Haldor Stormborn | 0 | hero |
| `frostbound.scout` | The Frostbound Shore (Scout) | 0 | expedition |
| `oathbound.sylva` | Sylva Reedwalker | 50 | hero |
| `ravenbound.eira` | Eira Raven-Sworn | 60 | hero |
| `ironway.mara` | Captain Mara Voss | 75 | hero |
| `frostbound.saga` | The Frostbound Shore: Long Night | 100 | expedition |

Fresh saves start with Haldor and Scout only. Locked heroes and expeditions cannot be selected until purchased at camp.

## 3. Mastery tracks

Each hero earns mastery on runs where they participate. Accrual per run:

- `max(1, kills / 25) + (victory ? 3 : 0)` applied to each participating hero's mastery field.

Combat bonus on the hero's primary starter weapon (same cap as Haldor):

- `damage × (1 + min(0.15, mastery × 0.005))`

| Hero | Save field | Affected weapon |
| --- | --- | --- |
| Haldor Stormborn | `HaldorMastery` | Frost Axe |
| Sylva Reedwalker | `SylvaMastery` | Grove Thorn Lash |
| Captain Mara Voss | `MaraMastery` | Signal Flare |
| Eira Raven-Sworn | `EiraMastery` | Raven Guard |

Local Co-op awards mastery to both selected heroes.

## 4. Codex

`DiscoveredCodexIds` records stable IDs the player has encountered. Visibility states:

| State | Rule |
| --- | --- |
| Hidden | Never seen; omitted from codex UI |
| Hint | Evolution recipe when base weapon and catalyst are both discovered but evolution not yet achieved |
| Discovered | Entry visible with name and description |

Discovery triggers:

- **Hero** — selected for an expedition.
- **Expedition** — map started.
- **Weapon / gear** — first level-up reward applied for that item in a run.
- **Catalyst** — catalyst appears in a level-up offer (seen, not necessarily taken).
- **Evolution** — evolution applied to a build.
- **Relic** — relic earned on victory.

Camp codex groups entries: Heroes, Expeditions, Weapons, Gear, Evolutions, Relics.

## 5. Camp onboarding

`CampOnboardingComplete` persists in meta save. First camp visit shows four dismissible panels:

1. Ledger and available renown balance.
2. Unlock board — spend renown on survivors and expeditions.
3. Relic vault — trophies from Scout victories.
4. Codex — discoveries from your expeditions.

Combat first-run hints (presentation preferences) add Scout objective, extraction beacon, and renown unlock guidance.

## 6. Save envelope v4

Bump `SaveMigration.CurrentVersion` to **4**. New `MetaProgress` fields:

- `SpentRenown`
- `UnlockedContentIds`
- `DiscoveredCodexIds`
- `SylvaMastery`, `MaraMastery`, `EiraMastery`
- `CampOnboardingComplete`

### v3 → v4 migration

- Initialize starter unlocks: `ravenbound.haldor`, `frostbound.scout`.
- Zero new mastery fields if absent; preserve `HaldorMastery`.
- `SpentRenown = 0`; do not deduct retroactive unlock costs.

Retroactive generosity (migration only):

- If `RunsCompleted >= 1` **or** `TotalRenown >= 50` → add `oathbound.sylva` to unlocks.
- If any relic collected → add `ravenbound.eira`.
- If `RunsCompleted >= 2` and `BestKills >= 150` → add `ironway.mara`.
- If `RelicsCollected` contains `relic.jotunn_echo` or `relic.jotunn_echo_warden` → add `frostbound.saga`.

Preserves v1 legacy, v2 envelope, and v3 relic migration chain.
