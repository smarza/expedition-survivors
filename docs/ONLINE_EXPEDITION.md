# Online multiplayer — deferred

Online Co-op is not part of the active 0.8.0 product scope.

The original direct-IP host/client implementation began as a networking POC and later accumulated its own player, enemy, weapon, effect and UI rules. Solo and Local Co-op already use one gameplay implementation, but the Online POC behaved as a second game and created a disproportionate testing burden.

The active runtime therefore contains:

- Solo;
- two-player Local Co-op;
- no Online menu entry;
- no Netcode for GameObjects or Unity Transport dependency;
- no Online simulation adapter.

The previous POC remains recoverable from Git history through the 0.3–0.8 commits. It must not be restored directly into the product runtime.

## Conditions for restarting Online development

Online work may restart only after:

1. Solo and Local Co-op share stable player, enemy, weapon, effect, reward and run models.
2. The shared simulation has deterministic automated coverage.
3. Content can be added once and behave identically in both active modes.
4. Performance budgets are established for enemies, projectiles and effects.
5. The local gameplay loop and UI direction are approved.

## Required future architecture

The future Online layer must be an adapter around the same gameplay simulation:

- clients send player commands;
- the host or server advances the shared simulation;
- snapshots serialize read-only shared state;
- client prediction, interpolation and reconciliation live outside gameplay rules;
- transport, lobby, Relay, authentication and matchmaking remain replaceable services;
- no Online-only copies of characters, enemies, weapons, rewards or balance formulas are allowed.

## Future acceptance boundary

When Online returns, every gameplay rule test must run against the shared simulation without knowing whether the adapter is Solo, Local or Online. Networking tests then cover only commands, authority, serialization, latency, packet loss, reconnect and host/server lifecycle.
