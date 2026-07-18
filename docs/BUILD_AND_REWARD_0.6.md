# Build and reward core 0.6 — acceptance guide

## Purpose

This slice replaces the shared prototype rune with a production-oriented run build. Each survivor owns weapons, gear, item levels and evolutions. XP remains shared, while the right to choose alternates so both players have regular agency.

## Reward contract

- Every level-up presents four cards.
- In co-op, the chooser alternates P1, P2, P1, P2.
- Only the chooser's assigned keyboard/gamepad or online instance may navigate and confirm.
- At least the first two generated cards target the chooser when eligible.
- Later cards can target the teammate; a team card can grant the same valid item to both.
- Every card carries a P1, P2 or P1+P2 destination badge.
- After confirmation, the HUD names the chooser, item and actual recipient.
- Rewards never exceed an item's maximum level or a map's category slot limit.
- If no normal reward is available, Field Rations heal without consuming a slot.

## Build rules

| Map | Weapon slots | Gear slots |
| --- | ---: | ---: |
| Frostbound Shore — Scout | 4 | 4 |
| Frostbound Shore — Long Night | 6 | 6 |

Frost Axe and Raven Guard begin at level 1 and occupy two weapon slots. Gear occupies its own capacity. Existing items can continue leveling even when all slots are filled.

## Evolution recipes

| Evolution | Recipe | Behavioral result |
| --- | --- | --- |
| Jotunn Cleaver | Frost Axe level 8 + Jotunn Rune | Axes gain damage/pierce and explode around every impact. |
| Storm Aegis | Raven Guard level 8 + Bear-Blooded | The pulse grows and heals its owner based on enemies hit. |

An eligible evolution is weighted above ordinary numerical upgrades. The evolved item keeps the original item's accumulated levels and occupies the same slot.

## UI contract

- The run HUD shows a compact row for each player with item abbreviation, level or `E`, and weapon/gear capacity.
- Tab or gamepad View/Select opens the detailed build reference.
- The details screen shows base survivability, movement, Ultimate damage/cooldown, axe damage/rate/count/pierce/critical chance and Raven Guard damage beside the items that produced them.
- Solo and Local Co-op pause while details are open. Online does not pause the authoritative run.
- Modal screens never draw the normal run HUD behind their content.
- Up to 12 equipped items fit in a three-column grid without title or description overlap.
- Static text does not change color on mouse hover. Every element that does react to hover is actionable.
- Reward cards are fully clickable and use the same gold focus language as gamepad navigation.
- The 16:9 canvas scales proportionally; wider and taller aspect ratios receive safe letterboxing instead of distorted typography.
- Character descriptions, Ultimate copy, device instructions and confirmation buttons occupy separate layout regions.
- Long map names receive a dedicated wrapping title region instead of sharing space with metadata.
- Survivor and combat statistics use aligned label/value columns; numeric values do not rely on spaces in a proportional font.
- The result summary reserves its own region for the selected map name and performance totals.

## Manual test checklist

1. Start Solo and verify four cards at the first level-up.
2. Fill Scout gear slots and confirm existing gear can level while new gear stops appearing.
3. Open build details and verify each chosen reward changes the expected number.
4. Reach Frost Axe level 8 with Jotunn Rune, choose Jotunn Cleaver and confirm impact explosions.
5. Reach Raven Guard level 8 with Bear-Blooded, choose Storm Aegis and confirm larger healing pulses.
6. Start Local Co-op with two gamepads and verify P1 then P2 alternate choices.
7. During P1's turn, verify P2's gamepad cannot move the selection or confirm; repeat in reverse.
8. Choose a teammate card and a P1+P2 card; verify the announcement and both build trays.
9. Repeat steps 6–8 Online and confirm both instances show identical builds, statistics and evolution state.
10. At 1920×1080, verify Haldor's Ultimate never touches the device hint or Ready button.
11. Select Long Night and verify its full name is readable in map selection, gameplay announcement and results.
12. Open Build Details after taking damage and verify health, armor and combat values remain vertically aligned.
