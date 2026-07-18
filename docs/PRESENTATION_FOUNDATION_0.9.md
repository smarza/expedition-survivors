# Presentation Foundation 0.9.0

Version 0.9.0 adds a presentation layer around the shared simulation accepted in 0.8.0. Presentation may observe run state and emit visual/audio feedback, but it cannot calculate damage, select rewards, advance cooldowns or own any other gameplay rule.

## Player-facing result

- A dedicated **Settings** screen is available from the main menu and pause menu.
- UI scale supports 90–120%, with a 100% default.
- High contrast changes the global text/focus palette.
- Reduced flashes shortens and lowers the opacity of transient effects.
- Screen shake can be adjusted from 0–100%.
- Master, music and SFX buses have independent 0–100% volume controls.
- Every P1 keyboard action can be rebound by selecting it and pressing a key.
- Bindings and presentation preferences persist independently from campaign progress.
- Prompts change between keyboard, Xbox, PlayStation, Switch, Steam Deck and generic gamepad families according to the last active device.
- Haldor has a more recognizable mantle, beard, brooch, axe/shield silhouette, idle breathing, movement lean, hit response, attack anticipation and Ultimate charge response.
- Eira receives a distinct cloak/feather silhouette through the same animation component.
- Frost Axe flight has pooled trails and impact bursts.
- Enemy deaths, XP collection, Raven Guard, Ultimates, down/revive and run results use the common presentation-event vocabulary.
- Frostbound Shore has a deterministic ambient snow layer that does not consume gameplay RNG.
- Camera trauma is centralized and respects the screen-shake preference.
- The F3 panel reports active VFX, SFX voices and music state.

## Architecture

| Component | Responsibility |
| --- | --- |
| `PresentationPreferences` | Versioned local accessibility, audio and keyboard-binding preferences. |
| `PresentationLayout` | Reference-canvas and safe-area transform for 16:9 and 16:10 displays. |
| `PresentationTheme` | Scaled typography, high-contrast palette and focus color. |
| `InputGlyphs` | Device-family detection and semantic prompts. |
| `LocalInputRouter` | Existing device ownership plus persistent P1 keyboard bindings and last-device tracking. |
| `PresentationDirector` | Maps run state to music and translates presentation cues to audio, VFX and camera trauma. |
| `PresentationAudioMixer` | Master/music/SFX software buses, procedural prototype score and prioritized SFX voice pool. |
| `PresentationVfxPool` | Prewarmed transient effects with no steady-state creation/destruction. |
| `HeroPresentation` | Character silhouette composition and animation without gameplay authority. |
| `FrostboundAmbience` | Deterministic visual snow independent from the run seed sequence. |
| `GameHUD` | Code-driven view adapter using the shared layout/theme/settings/input-prompt contracts. |

The current visual/audio assets are original runtime-generated production placeholders. The foundation deliberately keeps asset lookup and playback behind services so authored sprites, animation clips, music and SFX can replace them without changing simulation code.

## Rebinding contract

Rebinding currently applies to Player 1 keyboard actions:

1. movement up/down/left/right;
2. Ultimate;
3. menu submit;
4. menu back;
5. pause;
6. Expedition Build.

Local Co-op Player 2 retains arrows plus Enter/Right Ctrl when using a shared keyboard. Gamepad controls remain semantic rather than physically rebound in 0.9.0; this preserves consistent Steam Deck and console-style navigation. Full gamepad remapping can be added later through the same `BindingAction` vocabulary.

## Audio contract

- Music states: Menu, Expedition, Boss, Reward and Result.
- Music starts only after user interaction so WebGL respects browser autoplay rules.
- The mix applies `master² × bus`, giving useful control at lower slider values.
- Ultimate, down, victory and defeat cues have priority over weapon/pickup/UI voices.
- The voice pool is bounded at ten simultaneous SFX sources.
- Low-priority cues are dropped or replaced before important cues are lost.

## Visual-effect contract

- All recurring transient effects are acquired from a prewarmed pool.
- Effects use unscaled time so reward/result feedback completes while simulation time is paused.
- Reduced flashes lowers maximum alpha and duration.
- Projectile trails never play a separate audio voice.
- Ambient snow uses an independent index hash and cannot change deterministic gameplay sequences.

## Supported presentation targets

The 0.9.0 acceptance targets are:

- desktop Chrome and Edge WebGL at 16:9;
- 1280×800 / 16:10 Steam Deck-style layout;
- Windows native build;
- keyboard/mouse, one gamepad and two-gamepad Local Co-op.

Mobile WebGL remains a convenience preview, not a supported controller target. Android browser gamepad exposure varies by browser/device/OS and may not expose an attached controller to Unity WebGL even when desktop browsers do. Touch controls and native Android input require a separately approved platform milestone.

## Future presentation content checklist

Every new visible or audible feature must define:

1. the gameplay event it observes;
2. the presentation cue and priority;
3. pooling/lifetime behavior;
4. reduced-flash and screen-shake behavior;
5. audio bus and maximum voice pressure;
6. keyboard and every supported gamepad prompt;
7. safe-area behavior at 1920×1080 and 1280×800;
8. Solo and Local Co-op ownership;
9. deterministic separation from gameplay RNG;
10. EditMode contract coverage and a PlayMode adapter smoke test.
