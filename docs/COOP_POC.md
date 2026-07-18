# Co-op POC — Milestone 0.2

## Product decision tested

Project Expedition uses one shared expedition rather than split-screen sessions. Players may move independently inside a soft separation limit; the camera follows the group center and zooms out as the party spreads.

## Implemented rules

- One or two local players selected from the main menu.
- Haldor Stormborn is P1; Eira Raven-Sworn is a temporary P2 co-op identity using the same prototype kit.
- Each player owns movement, rush cooldown, health, weapon timers and projectile origin.
- Enemies continuously select the nearest living player.
- XP and level are expedition-wide.
- One shared rune choice applies to every active player.
- A defeated player becomes downed instead of ending a co-op run.
- A living player revives a teammate by remaining within 1.8 world units for 2.5 seconds.
- The run ends only when every player is downed.
- Maximum player separation is 11.5 world units.
- Enemy count and health are not increased for co-op in this POC, allowing direct difficulty comparison.

## Device matrix

| Setup | P1 | P2 |
| --- | --- | --- |
| Solo, no gamepad | WASD + Space | — |
| Solo, one gamepad | Keyboard or gamepad 1 | — |
| Co-op, no gamepad | WASD + Space | Arrows + Enter |
| Co-op, one gamepad | WASD + Space | Gamepad 1 |
| Co-op, two gamepads | Keyboard or gamepad 1 | Arrows or gamepad 2 |

## Manual acceptance test

1. Start `LOCAL CO-OP POC`.
2. Verify both players move independently and have different markers/colors.
3. Separate until the shared camera zooms out and the soft tether stops further separation.
4. Verify axes originate from each player and enemies change target based on proximity.
5. Collect XP and confirm one shared choice upgrades both players.
6. Let one hero lose all health; confirm the run continues.
7. Stand the survivor near the downed hero until revival reaches 100%.
8. Down both heroes and confirm defeat.
9. Complete the Jotunn encounter and confirm shared progression is saved.

## Online architecture boundary

The next spike will preserve `PlayerController` as the presentation/owned-player boundary and replace direct local reads with an input command. The host will own enemy simulation, XP, upgrade resolution, boss state and rewards. Clients should receive snapshots/events rather than one network object for every swarm enemy.

## Evidence to record

- Controller model and operating system.
- Whether device assignment matched the table.
- Average and worst frame rate with two players.
- Any camera discomfort near maximum separation.
- Revival clarity and preferred revival duration.
- Whether shared upgrades feel cooperative or remove too much agency.
