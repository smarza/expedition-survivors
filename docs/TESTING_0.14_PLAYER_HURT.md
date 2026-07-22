# Testing 0.14 — Player Damage Feedback

Manual verification for the dedicated player-hurt presentation layer. Automated EditMode contracts cover knockback, cue priority and imported `PlayerHurt` audio.

## Automated contracts

EditMode adds coverage for:

- `SharedPlayerModelTests.Damage_AppliesKnockbackAwayFromSource`;
- `SharedPlayerModelTests.Damage_KnockbackRespectsImmunityFrames`;
- `PresentationFoundationTests` — `PlayerHurt` priority above `Impact`, all cue assets imported.

Existing 0.8–0.13 suites remain required.

## Manual owner matrix

### Contact damage feel

1. Start a Scout run and allow an enemy to touch the hero.
2. Confirm a **distinct hurt sound** (not the weapon impact cue), red/orange body flash, brief screen-edge vignette, health bar ghost segment and light knockback.
3. Repeat with screen shake at 0% and reduced flashes ON — hurt sound and HUD pulse should remain readable.

### Invulnerability clarity

1. Activate **Aegis Veil** (purple loot counter to 10/10).
2. While shielded, confirm the hero shows a **soft cyan pulse**, not the red hurt flash, when immune hits are ignored.
3. After shield expires, confirm the next valid hit restores the hurt feedback stack.

### Heavy hits

1. Reach the boss and take a **ground slam** hit.
2. Confirm stronger camera shake, larger hurt burst, heavier knockback and **no duplicate impact SFX** from the slam resolver.
3. During boss charge contact, confirm stronger knockback than regular enemy contact.

### Low health warning

1. Reduce health below roughly 30% without going down.
2. Confirm the health bar shifts to a warmer pulsing tone until healed or downed.

### Co-op and devices

1. In local co-op, confirm each player receives hurt feedback tied to their own hits and health bars.
2. With a gamepad assigned, confirm brief rumble on hurt (disable **HAPTICS** in settings to verify toggle).

### Regression carryover

1. Weapon impacts on enemies still use the offensive `Impact` cue.
2. Down/revive, loot, level-up and boss proximity vignette behavior from 0.13 remain green.

Record failures with map/hero, player count, seed and reproduction steps.
