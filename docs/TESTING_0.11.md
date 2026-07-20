# Testing 0.11.0 — Camp and Progression

The automated gate is **80 EditMode + 13 PlayMode tests**. GitHub Actions must also pass static validation, WebGL compilation, GitHub Pages deployment and the Windows milestone build before owner validation.

## Automated contracts

EditMode adds coverage for:

- fresh save starter unlocks (Haldor + Scout only);
- renown purchase validation, insufficient funds and double-purchase rejection;
- save envelope v3 → v4 migration with retroactive Sylva unlock;
- v4 round-trip for `SpentRenown`, mastery fields, unlock/codex arrays and camp onboarding flag;
- per-hero mastery accrual formula and damage multiplier cap;
- codex discover idempotency and evolution recipe hint visibility;
- unlocked-character/map index navigation skipping locked content.

PlayMode adds coverage for:

- fresh save gating (Haldor/Scout only at camp);
- run completion increasing renown and Haldor mastery;
- camp unlock purchase spending available renown via the CODEX.

## Final owner matrix

Use the GitHub Pages preview first, then the Windows artifact for the native device gate.

### Fresh player loop

1. Start with a cleared save (or fresh profile) and confirm camp onboarding appears once across four panels.
2. Confirm the ledger shows **AVAILABLE** renown (zero on first visit) and mastery summary for H/S/M/E.
3. Start **Solo** — confirm only **Haldor Stormborn** is selectable; Sylva, Mara and Eira show lock cost and camp guidance.
4. Complete or fail one Scout run; confirm results show renown earned, available balance and unlock callout when affordable.
5. Return to camp; open **CODEX** → **HEROES** and confirm Sylva appears at 75 renown — a strong first victory should leave you close but usually short until a second run.
6. Purchase Sylva in the CODEX after enough renown accumulates; confirm available renown decreases and Sylva becomes selectable.
7. Start a second run as Sylva (or save toward Eira/Mara/Long Night) with a clear goal.

### Codex catalog and purchases

1. Confirm the CODEX header shows available renown and lifetime earned balance.
2. Confirm every category lists all catalog entries; undiscovered weapons/gear show as **UNKNOWN ENTRY** without details.
3. Confirm locked heroes and expeditions show name and renown cost; confirm purchase with Submit when affordable.
4. Confirm categories navigate with Up/Down, entries with Left/Right, and Back returns to camp.
5. After a run, confirm discovered weapons/gear reveal full details; evolution hints appear when base weapon and catalyst were seen.
6. Confirm relic entries reveal details after Scout victory relic grants.

### Mastery

1. Complete a run as Haldor — confirm Haldor mastery increases in the ledger.
2. After unlocking and playing Sylva or Mara, confirm the corresponding mastery letter increases.

### Regression carryover from 0.10.0

1. Entire Scout expedition matrix from `docs/TESTING_0.10.md` remains required.
2. Settings, rebinding, glyphs, audio, VFX, safe-area layout and reduced-flash behavior from `docs/TESTING_0.9.md` remain required.
3. Save migration from legacy v1/v2/v3 payloads must preserve renown, relics and mastery without charging retroactive unlock costs.

Record any failure with browser/native target, resolution, save state, reproduction steps and screenshot/video when visual.
