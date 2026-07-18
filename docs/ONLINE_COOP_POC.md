# Online co-op POC — Milestone 0.3

> Historical document. This connectivity POC passed and has been superseded by the playable Milestone 0.4 guide in `ONLINE_EXPEDITION.md`.

## Purpose

Validate host/client connectivity, authoritative input, compact swarm snapshots, interpolation, bandwidth scale and disconnect behavior before adapting the full expedition run.

This spike is intentionally isolated from Solo and Local Co-op. It does not yet network Haldor's complete combat run.

## Architecture under test

- Unity Netcode for GameObjects 2.7 as session and custom-message infrastructure.
- Unity Transport 2.6 over UDP port 7777.
- One host that owns player positions and the 64-probe swarm simulation.
- Clients send movement input at 30 Hz.
- Host sends player and probe snapshots at 15 Hz using unreliable-sequenced delivery.
- Clients interpolate received positions instead of controlling the world.
- No NetworkObject is created for every swarm entity.

## Test on one computer

1. Open the project and create a Windows build using `Assets/Scenes/Bootstrap.unity`.
2. Run the build and select **ONLINE POC → START HOST**.
3. In the Unity Editor, press Play and select **ONLINE POC**.
4. Leave the address as `127.0.0.1` and select **JOIN HOST**.
5. Move in each instance with WASD or its first gamepad.
6. Confirm the blue host and orange client move in both windows.
7. Confirm the 64 purple probes move smoothly in both windows.
8. Record snapshot size, sent/received counters and RTT.
9. Disconnect the client; confirm the host remains active.

## Test on two computers

1. Both computers must use the same project build and local network.
2. Start Host on the first computer.
3. Find the host's local IPv4 address, such as `192.168.1.42`.
4. Enter that address on the second computer and select Join Host.
5. Allow the application through Windows Firewall if prompted. UDP port 7777 is used.

## Acceptance criteria

- Client connects to localhost and LAN host.
- Both peers see consistent player identities and swarm motion.
- Client input is applied only by the host.
- Snapshot count increases continuously without errors.
- Snapshot payload stays below 1 KB for two players and 64 probes.
- Client interpolation remains visually stable at 15 Hz.
- Client disconnect does not terminate the host.
- Host shutdown produces a clear client disconnect state.

## Deliberate limitations

- Direct IP/LAN only; no Relay, lobby, matchmaking, NAT traversal or room codes.
- Connectivity spike only; no networked XP, weapons, enemies, boss or rewards yet.
- No prediction, reconciliation, reconnect or host migration.
- No artificial latency/loss profile yet.
- Maximum connection count and abuse controls are not production-ready.

## Decision after validation

If this spike passes, adapt the real run in vertical slices: player movement and damage first, then enemy snapshots, then weapons/events, then XP/upgrades, then victory/rewards. Relay and lobby should be evaluated only after the direct host/client simulation is stable.
