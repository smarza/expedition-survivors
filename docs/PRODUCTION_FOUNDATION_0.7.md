# Production foundation 0.7 — acceptance and authoring guide

## Purpose

This milestone removes the highest-frequency runtime allocation paths and makes gameplay content editable as project data. It preserves the current Solo, Local Co-op and direct-IP Online Co-op behavior while establishing measurable performance and reproducible runs.

## Object lifecycle

Local combat prewarms and recycles:

- 96 enemies;
- 64 Frost Axe projectiles;
- 96 experience gems;
- 16 Raven Guard pulse visuals;
- 4 Ultimate pulse visuals.

Pools grow safely when demand exceeds their prewarmed capacity. Returning to camp, replaying a run or starting a new expedition releases every active pooled object before the run root is replaced.

Online presentation separately prewarms 96 enemy views, 64 attack visuals and 16 pulse visuals. Player and arena objects remain session-lifecycle objects rather than steady-state combat allocations.

## Spatial queries

A 2.5-unit spatial hash tracks active enemies. Enemy movement and knockback update cell membership. Frost Axe collision, automatic targeting, Raven Guard, Ultimates and evolved area damage query only intersecting cells and then perform exact radius checks.

The host-authoritative online simulation uses the same structure for nearest-target and radius operations. Enemy snapshots update client-side membership for consistent presentation cleanup.

## Deterministic runs

Starting an expedition from map selection generates a new seed. The seed controls local arena decoration, spawn positions, enemy variants/stats, critical hits, renown drops and reward choices. The result screen displays the seed and `REPLAY SAME SEED` resets the random stream before rebuilding the run.

Player input and frame timing remain external to the seed, so identical player movement is required for a fully identical combat outcome.

## Content authoring

The production database is `Assets/Resources/Content/ProductionContent.asset`. It contains:

- character records;
- map and timing records;
- weapons, gear, boons and evolution recipes;
- enemy archetype statistics.

Every record requires a globally unique stable ID. Evolution records must reference valid base-item and catalyst IDs. Runtime loading falls back to the code defaults if the database is absent, while startup validation reports missing, duplicate or invalid relationships.

## Performance overlay

Press `F3`, or hold gamepad Left Shoulder and press View/Select, during a local run. The overlay shows:

- smoothed FPS and frame time;
- worst frame in the last sampling window;
- active enemies, axes and experience gems;
- available pooled objects;
- objects created versus reused;
- occupied spatial cells and cumulative queries;
- current run seed and content source.

`CREATED` should stabilize after the pools have expanded to the run's peak demand, while `REUSED` should continue increasing.

## Manual acceptance checklist

1. Open the project and confirm the Console reports `Project Expedition 0.7 foundation: READY`, without `CharacterContentRecord`/`ProductionContentDatabase` script-reference warnings.
2. Start Solo, press F3 and confirm the content source reads `SCRIPTABLEOBJECT` rather than `CODE FALLBACK`.
3. Play through several waves and confirm `REUSED` increases while defeated enemies, expired axes and collected gems disappear normally.
4. Trigger Raven Guard, an Ultimate and both evolutions; confirm radius damage and visuals still work.
5. Finish or lose a run, record its seed and choose `REPLAY SAME SEED`; confirm the `REPLAYING SEED <number>` announcement, the same F3 seed, arena decoration and opening waves.
6. Return to camp and start a new expedition; confirm a different seed is generated.
7. Start Local Co-op and verify both players, assigned devices, alternating rewards, knockdown and revival.
8. Run Host and Client, verify enemy/attack visuals disappear correctly and repeated runs do not accumulate stale enemies.
9. In the Unity Profiler, verify no `GameObject.Instantiate`/`Object.Destroy` calls originate from steady-state enemies, axes, gems or pulse visuals.
10. Let Long Night reach a high enemy count and capture FPS, worst frame, active actors, created/reused totals and spatial query count.

An identical seed guarantees the same random sequence. A fully identical combat result also requires the same input and timing, because movement and frame timing are intentionally not recorded by the replay button.

## Current boundary

The automated repository validator and runtime startup checks do not replace Unity compilation, PlayMode coverage or target-hardware profiling. UI remains IMGUI, and deeper consolidation of the local and online simulation models is still planned.
