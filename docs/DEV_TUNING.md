# Development Tuning (F4)

Hidden development overlay for balancing loot, spawn pressure, XP curves, player survival, challenge modifiers, maps, heroes, enemies and weapons.

## Access

- Press **F4** on keyboard to open or close the overlay.
- Press **R1 + View/Select** on gamepad (mirrors the F3 metrics chord on L1 + View/Select).
- Tap the **DEV** button at the top-right when touch controls are available.
- There is no menu entry; the shortcuts are intentionally hidden.
- While open, simulation time is paused (`Time.timeScale = 0`).

Inside the panel:

- **L1 / R1** (shoulder buttons) switch tabs.
- Tab headers are clickable/tappable for mouse and touch.
- Long tab lists scroll automatically as you move selection with **UP/DOWN**; a counter appears when scrolling is active.
- Footer actions (Export, Reset, Apply, Close) stay pinned inside the panel frame.

## Tabs

| Tab | Adjusts |
|-----|---------|
| Loot | Global drop curve, activation count, per-color duration/intensity, force drops |
| Spawn | Enemy cap, distances, group growth, interval acceleration |
| XP | Collision radius, enemy level offsets, XP curve constants |
| Player | Magnet radius, immunity windows, revive tuning |
| Challenge | Veteran and mutator multipliers |
| Maps | Per-map duration, spawn intervals, objectives, extraction |
| Heroes | Per-hero stats and ultimate tuning |
| Enemies | Per-enemy health, speed, contact damage, XP |
| Weapons | Per-weapon damage, cooldown, pierce, radii |

Use **LEFT/RIGHT** (or the on-screen `−` / `+` buttons) to adjust values. **UP/DOWN** moves selection. **ESC** closes the panel.

## Loot tab details

The loot tab supports all five party loot colors:

- Healing Embers (blue)
- Critical Flare (orange)
- Swift Trail (green)
- Wrath Embers (red)
- Aegis Veil (purple)

| Field | Scope | Notes |
|-------|-------|-------|
| SELECT LOOT | Per-color editor | Choose which loot definition to tune |
| BASE / MIN DROP CHANCE | Global | Applies to the shared any-loot roll |
| RARITY REDUCTION PER LEVEL | Global | Applies to all colors |
| REQUIRED COUNT | Global | Pickups needed to activate any color |
| EFFECT DURATION | Selected loot | Override duration for the selected color |
| Intensity label | Selected loot | Changes by effect type (see below) |
| FORCE LOOT DROPS | Global | Forces a loot drop every kill; color is still random |

Intensity semantics by color:

- Blue: HP/s regeneration
- Orange: additional crit chance (0.25 = +25%)
- Green: flat move speed bonus
- Red: damage multiplier (1.25 = +25% weapon damage)
- Purple: invulnerability (intensity ignored)

## Reset

**RESET TO DEFAULTS** restores every tuning value and content override from the live code/catalog defaults. This also clears the separate development PlayerPrefs profile.

## Export

**EXPORT TO CLIPBOARD** copies the full active tuning profile as formatted JSON to the system clipboard (`GUIUtility.systemCopyBuffer`). Paste it into a chat, file or diff tool so production defaults can be updated from your fine-tuned values.

A toast confirms success or failure. Export does not reset or modify the saved profile.

## Persistence

Development tuning is stored in PlayerPrefs key `project-expedition-dev-tuning-v1`.

It does **not** write to:

- `project-expedition-save-v1.json` (camp renown, unlocks, mastery)
- `project-expedition-presentation-v1` (UI scale, audio, bindings)

## Active run

During gameplay, use **APPLY TO RUN** after changing hero or loot values to refresh the current players and loot tracker. Map and spawn changes apply on the next spawn tick through the resolver.

Loot apply refreshes all per-color progress counters using the resolved catalog.

## Related docs

- [`docs/TESTING_0.13_COMBAT_LOOT.md`](TESTING_0.13_COMBAT_LOOT.md) — manual loot verification
- [`docs/BUILD_AND_CONTENT_REFERENCE.md`](BUILD_AND_CONTENT_REFERENCE.md) — catalog defaults
