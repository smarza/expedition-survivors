# Online expedition — Milestone 0.6

## What this milestone proves

Milestone 0.4 turns the validated transport POC into a real two-player survivors-like run. The host owns the complete simulation; the client sends commands and renders interpolated snapshots and combat events.

The host plays Haldor Stormborn. The joining player controls Eira Raven-Sworn. Character selection is deliberately deferred until the faction roster exists.

## Test on one computer

1. Open the project and make a Windows build from `Assets/Scenes/Bootstrap.unity`.
2. Run the executable and choose **ONLINE CO-OP → START HOST — HALDOR**.
3. In Unity, press Play and choose **ONLINE CO-OP**.
4. Keep `127.0.0.1` and choose **JOIN — EIRA**.
5. The expedition must start automatically in both instances.
6. Move each survivor independently with WASD or its first gamepad.
7. Charge and activate the character Ultimate with Space, right shoulder or right trigger.
8. Confirm axes, pulses, enemies, health, XP and the timer remain synchronized.
9. At level-up, confirm that four rewards appear and identify P1, P2 or P1+P2 as their destination.
10. Only the instance named as chooser may select, using `1`–`4`, D-pad/stick + South/A, or West/North/East/right-shoulder.
11. Confirm that the status line names the chooser, item and recipient after selection.
12. Open build details with Tab or gamepad View/Select and confirm both builds match between instances.
13. Disconnect the client and confirm the host resets to a one-player waiting lobby.

## Test on two computers

1. Use the same build on both computers and the same local network.
2. Start the host and allow UDP port `7777` through the firewall.
3. Enter the host computer's local IPv4 address on the client.
4. Join and repeat the gameplay checks above.

## Authority model

- The host simulates movement, party tether, attacks, enemy AI, damage, revival, spawning, XP, upgrades, boss and results.
- The client sends movement at 30 Hz and discrete Ultimate/reward commands.
- The host sends run snapshots at 15 Hz over unreliable-sequenced delivery.
- Enemy IDs, positions and normalized health are quantized to keep the 96-enemy snapshot near a normal UDP MTU.
- Axes and pulses are lightweight presentation events; their damage is decided only by the host.
- Only the host commits persistent renown and run results.

## Acceptance checklist

- The second player automatically starts the same run in both windows.
- Haldor and Eira can move independently but cannot separate beyond the party tether.
- Enemies target the nearest living survivor and match in both views.
- Automatic attacks, Ultimates and Raven Guard cause host-authoritative damage.
- Both health bars, knockdown and proximity revival synchronize.
- XP and level are shared; reward ownership alternates between P1 and P2.
- Only the active chooser's command is accepted by the host.
- Four reward cards display their recipient and may benefit the chooser, teammate or both.
- Per-player item levels, slots, combat statistics and evolutions match in both views.
- The Jotunn spawns at 01:30; killing it synchronizes victory.
- Both survivors down synchronizes defeat.
- Snapshot payload stays below roughly 1.2 KB at the 96-enemy cap.
- Client disconnect removes the ghost peer and resets the host lobby.

## Deliberate limitations

- Direct IP/LAN only; there is no Relay, room code, matchmaking or NAT traversal.
- No reconnect, host migration, client prediction or rollback.
- No production anti-cheat, authentication or save ownership protocol.
- Final art, audio, pooling, spatial queries and production UI are future milestones.
