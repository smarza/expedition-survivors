# Testing 0.9.0 — Presentation Foundation

The automated gate is **48 EditMode + 9 PlayMode tests**. GitHub Actions must also pass static validation, WebGL compilation, GitHub Pages deployment and the Windows milestone build before owner validation.

## Automated contracts

EditMode adds coverage for:

- settings serialization and safe clamping;
- every rebindable keyboard action;
- 1920×1080 and 1280×800 safe-area layout;
- keyboard/Xbox/PlayStation/Steam Deck prompt semantics;
- master/music/SFX mix math and SFX priorities;
- Menu/Expedition/Boss/Reward/Result music routing.
- every music state and presentation cue resolves to an imported audio asset.

PlayMode adds coverage for:

- presentation-service initialization;
- music-state projection from the live director;
- Haldor presentation and Frostbound ambience creation;
- Settings ownership and time-scale restoration;
- transient VFX acquisition and return to its pool.

## Final owner matrix

Use the GitHub Pages preview first, then the Windows artifact for the native device gate.

### UI and accessibility

1. Open Settings from the main menu and from Pause; Back must return to the correct owner screen.
2. Test UI scale at 90%, 100% and 120% without clipped settings, rewards, build details or results.
3. Toggle high contrast and confirm text/focus borders remain readable.
4. Toggle reduced flashes and compare Frost Axe impacts and the Ultimate.
5. Set screen shake to 0%, then 100%, confirming the difference without camera drift.
6. Validate 1920×1080 and, if available, 1280×800/Steam Deck.

### Input

1. Rebind P1 movement, Ultimate, Submit, Back, Pause and Expedition Build; leave and reopen Settings to prove persistence.
2. Restore defaults and confirm WASD/Space/Escape/Tab.
3. Move between keyboard and gamepad; prompts must switch to the active device family.
4. Solo: keyboard and one gamepad.
5. Local Co-op: keyboard + one gamepad, then two gamepads.
6. During rewards, only the chooser's assigned device may navigate/confirm.

### Audio

1. Confirm music begins after the first interaction in the browser.
2. Visit menu, expedition, level-up and result states; each must have a distinct music state.
3. Reach the Jotunn and confirm the boss state.
4. Test master, music and SFX at 0%, 50% and 100%.
5. In a dense fight, Ultimates/down/result cues must remain audible without runaway overlapping voices.

### VFX, animation and Frostbound polish

1. Haldor must read as a shield/axe Ravenbound figure rather than a plain circle.
2. Confirm idle breathing, movement lean, attack anticipation, hit response and Ultimate response.
3. Confirm Frost Axe trails follow the projectiles and impacts occur at enemies.
4. Confirm Raven Guard, enemy death, XP pickup, down/revive and results have distinct feedback.
5. Confirm snow follows the expedition without obscuring enemies, XP or reward UI.
6. Toggle F3 and confirm VFX/SFX/music diagnostics remain bounded during a representative dense encounter.

Record any failure with browser/native target, resolution, device layout, settings values, run seed, reproduction steps and screenshot/video when visual.
