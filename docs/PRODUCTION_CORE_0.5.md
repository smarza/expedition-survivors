# Production core 0.5 — acceptance guide

## Purpose

This slice replaces hard-coded prototype assumptions with shared character, map, progression, Ultimate and input definitions. Solo, Local Co-op and Online consume the same balance rules even though full simulation consolidation and pooling remain the next engineering slice.

## Gamepad ownership

- Solo listens to every connected gamepad and uses the strongest active movement signal. A stale or virtual first device no longer blocks the real controller.
- With one gamepad in Local Co-op, keyboard controls P1 and the gamepad is assigned to P2.
- With two or more gamepads, the first active controller claims P1 and the next active controller claims P2.
- Once claimed, a controller remains assigned to that player for movement, Ultimate activation and choices.
- Main menus and non-player-specific screens accept any connected gamepad.

## Test checklist

1. Navigate the main menu with D-pad/left stick and confirm with South/A.
2. Start Solo and select Haldor or Eira using only a gamepad.
3. Select either map and confirm that the chosen duration and Jotunn arrival appear in the HUD.
4. Confirm the first level-up requires substantially more kills than 0.4.
5. Wait for the Ultimate meter, activate with right shoulder/right trigger and confirm the large impact and cooldown.
6. Confirm normal axes and Raven Guard remain automatic.
7. At level-up, navigate with D-pad/left stick and confirm with South/A.
8. Start Local Co-op with two gamepads. Use each controller during character selection to claim it.
9. Confirm each controller moves and activates only its own survivor.
10. Confirm either assigned controller can navigate and confirm the shared level-up reward.
11. Pause, continue, finish a run and navigate results using a gamepad.
12. Repeat an Online run and confirm map timing, XP and both individual Ultimate meters synchronize.

## Current maps

- **The Frostbound Shore — Scout:** five-minute target, Jotunn at 04:00.
- **The Frostbound Shore: Long Night:** twelve-minute target, Jotunn at 10:30.

The target duration is not a free victory. If the Jotunn remains alive, the expedition enters overtime until the boss is defeated or both survivors fall.
