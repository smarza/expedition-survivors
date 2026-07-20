# Testing 0.10.0 — Demonstration Content

The automated gate is **55 EditMode + 10 PlayMode tests**. GitHub Actions must also pass static validation, WebGL compilation, GitHub Pages deployment and the Windows milestone build before owner validation.

## Automated contracts

EditMode adds coverage for:

- Scout route objective initialization and kill-gated boss eligibility;
- extraction completion at the beacon radius and by survival timer;
- victory relic tier resolution with and without optional shard completion;
- `SharedWeaponRegistry` Frost Axe level-table parity with the legacy shared weapon model;
- default Ravenbound starter weapon registration.

PlayMode adds coverage for:

- Frostbound Scout map selection and opening shoreline phase;
- boss-kill transition into extraction with an active beacon;
- beacon arrival triggering a single terminal victory outcome.

## Final owner matrix

Use the GitHub Pages preview first, then the Windows artifact for the native device gate.

### Scout expedition route

1. Start **The Frostbound Shore** and confirm the opening announcement **THE SHORE AWAKENS**.
2. Confirm the required objective **Cull the Raider Tide** appears and tracks Draugr defeats toward 150.
3. Collect optional rune shards when visible and confirm shard progress toward 5.
4. Reach 50% objective progress during **THE COAST TIGHTENS** and confirm the Frost Wraith Captain elite can emerge once.
5. Defeat 150 Draugr before the timer or survive until the Jotunn phase; confirm **THE JOTUNN HAS FOUND YOU**.
6. Defeat the Jotunn and confirm **REACH THE BEACON** with a visible extraction beacon at the north marker.
7. Reach the beacon within 3.5 units or survive 15 seconds there to complete the run.
8. Confirm victory grants **Jotunn Echo**, or **Jotunn Echo Warden** when all shards were collected.

### Biome landmarks and readability

1. Locate the driftwood wreck southwest of spawn and confirm it brightens during **DRIFTWOOD RUN**.
2. Locate the rune circle northeast of center and confirm it brightens during the opening shoreline phase.
3. Confirm boss-approach markers brighten during **THE COAST TIGHTENS**.
4. Confirm the north extraction placeholder is visible early and becomes the active beacon after the boss dies.
5. Landmarks must remain readable without obscuring enemies, XP gems or reward UI.

### Hero silhouettes

1. Haldor remains the readable Ravenbound shield/axe figure from 0.9.0.
2. When Sylva or Mara are selectable, confirm leaf-cloak/staff and pack/visor/flare silhouettes respectively.
3. Confirm idle breathing, movement lean, attack anticipation, hit response and Ultimate response for each visible hero.

### Regression carryover from 0.9.0

1. Settings, rebinding, glyphs, audio buses, VFX pool, safe-area layout and reduced-flash behavior from `docs/TESTING_0.9.md` remain required.
2. Solo level-up still offers four rewards and resumes simulation correctly.
3. Same-seed replay still preserves the run seed and resets progress.

Record any failure with browser/native target, resolution, device layout, settings values, run seed, reproduction steps and screenshot/video when visual.
