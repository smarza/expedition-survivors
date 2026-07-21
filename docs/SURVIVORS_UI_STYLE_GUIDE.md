# Survivors UI Style Guide

> **Status:** Character Select pilot (IMGUI active, UI Toolkit prepared)  
> **Purpose:** migrate Camp, Map Select, Codex and other menu screens to the same visual language  
> **Related:** `docs/UI_LAYOUT_GUIDE.md`, `docs/PRESENTATION_FOUNDATION_0.9.md`, `docs/PROJECT_MASTER_PLAN.md` §9.2

This guide documents the **Survivors-style menu presentation** validated on Character Select. It is inspired by Vampire Survivors information density, but keeps Project Expedition identity (nordic expedition, navy/copper/cyan accents from `PresentationTheme`).

---

## 1. Architecture

Do not copy layout code screen-by-screen. Reuse these layers:

| Layer | File | Responsibility |
|---|---|---|
| Style & primitives | `SurvivorsStylePresentation.cs` | Colors, borders, buttons, badges, screen background |
| Layout tokens | `PresentationLayoutSystem.cs` | Spacing, typography base sizes, `LayoutZone`, `MeasureText` |
| Screen presenter | e.g. `CharacterSelectPresentation.cs` | Screen-specific icons, stat labels, portraits |
| View adapter | `GameHUD.cs` (today) | Zone wiring, input, state sync |
| UI Toolkit (optional) | `Resources/UI/<Screen>/` + `*UiToolkitScreen.cs` | USS/UXML mirror of the same tokens |

**Rule:** gameplay data comes from catalogs and save services; presentation only reads and draws.

---

## 2. Design principles

1. **One logical canvas:** 1920×1080 scaled via `PresentationLayout` — never “fix” density by changing resolution.
2. **Minimal borders:** one flat fill + at most **one** 1px border per container. Avoid nested gold frames.
3. **Measured text:** use `PresentationTextMeasure.MeasureHeight` before allocating row height; no hard caps that truncate copy.
4. **Semantic color:** gold = titles/emphasis, green = confirm, red = back/cancel, blue = filter/toggle, muted gray = secondary copy.
5. **Clip vector art:** portraits and icon groups use `GUI.BeginGroup` so scaled primitives cannot bleed outside their cell.
6. **Normalized sprite scale:** all `RuntimeAssets` diamond/circle draws use a **128px reference** (see §8).

---

## 3. Color palette

Defined as `static readonly Color` on `SurvivorsStylePresentation`. Prefer these over ad-hoc literals in new screens.

| Token | RGB (0–1) | Use |
|---|---|---|
| `BackgroundDeep` | 0.04, 0.08, 0.12 | Full-screen backdrop |
| `BackgroundMid` | 0.06, 0.11, 0.16 | Soft vignette overlay |
| `PanelNavy` | 0.05, 0.10, 0.18 | Primary panels (body, footer shell) |
| `PanelNavyInset` | 0.04, 0.08, 0.14 | Inset columns, tile name band, icon wells |
| `TileBackground` | 0.03, 0.06, 0.11 | Grid tile default |
| `TileSelected` | 0.07, 0.12, 0.20 | Selected tile fill |
| `BorderGold` | 0.86, 0.66, 0.18 | Selected tile border, emphasized actions |
| `BorderGoldBright` | 0.98, 0.82, 0.28 | Coin diamond, selection star |
| `BorderGoldDim` | 0.35, 0.42, 0.48 | Default 1px borders (muted steel-gold) |
| `TextLight` | 0.92, 0.94, 0.96 | Body copy |
| `TextGold` | 0.98, 0.82, 0.28 | Screen title, hero name, coin amount |
| `TextMuted` | 0.62, 0.70, 0.76 | Labels, hints, locked copy |
| `StatPositive` | 0.42, 0.92, 0.48 | Numeric stat values |
| `RowStripe` | 0.02, 0.05, 0.09 @ 55% | Alternating list rows |

**Do not use** the early crimson full-screen background — it was experimental and clashed with Camp/menu navy.

Hero **accent strips** (3px top bar on stats column / tile) use the character’s `CharacterDefinition.Color`, desaturated when locked.

---

## 4. Typography (IMGUI)

Create styles through `SurvivorsStylePresentation.CreateLabelStyle` so font resolution and `PresentationTheme.FontSize` stay consistent.

| Style (GameHUD field) | Base size | Weight | Color | Typical use |
|---|---:|---|---|---|
| `_vsDisplay` | 34 | Bold | TextGold | Screen title, footer hero name |
| `_vsHeading` | 22 | Bold | TextGold | Co-op panel title, compact names |
| `_vsBody` | 15 | Normal | TextLight | Passive description, footer body |
| `_vsCaption` | 13 | Bold | TextMuted | Tribe / role line |
| `_vsMicro` | 11 | Bold | TextMuted | Filter rows, weapon caption, hints |
| `_vsStatValue` | 13 | Bold | StatPositive | Stat column values (right-aligned) |
| `_vsHint` | 11 | Bold | TextMuted | Centered control legend under footer |
| `_vsConfirmedLabel` | 24 | Bold | light green | Ready / confirmed button state |

Built-in styles from `SurvivorsStylePresentation`:

- `SectionTitleStyle` — 13px gold, centered (column section headers)
- `TileNameStyle` — 12px light, centered, word wrap (grid tile names)
- `CoinStyle` — 14px gold (renown badge)

**Font:** load `Resources/Fonts/UiPixel` when available; fallback OS monospace (Consolas / Courier).  
**Ui scale:** all sizes pass through `PresentationTheme.FontSize(int baseSize)`.

---

## 5. Spacing tokens

From `PresentationSpacing`:

| Token | px |
|---|---:|
| `Space4` | 4 |
| `Space8` | 8 |
| `Space12` | 12 |
| `Space16` | 16 |
| `Space24` | 24 |

**Character Select metrics** (`CharacterSelectLayoutMetrics`): side columns **280px**, solo footer **200px**, stat row **34px**, tile name band **32px**, screen padding **40px**.

Other screens should define their own `*LayoutMetrics` class beside layout helpers — do not hard-code magic numbers in draw methods.

---

## 6. Component recipes (IMGUI)

### Screen shell

```csharp
SurvivorsStylePresentation.DrawScreenBackground(new Rect(0f, 0f, 1920f, 1080f));
```

### Primary panel (no extra border)

```csharp
DrawPanel(rect, SurvivorsStylePresentation.PanelNavy);
```

### Panel with single border

```csharp
SurvivorsStylePresentation.DrawFlatPanel(rect, SurvivorsStylePresentation.PanelNavy, 1f);
```

### Inset column

```csharp
SurvivorsStylePresentation.DrawInsetPanel(columnRect, optionalHeroAccentColor);
// accentColor == default → fill only, no top strip
```

### Section header

```csharp
SurvivorsStylePresentation.DrawSectionHeader(headerRect, "Section Title");
// dark strip + gold centered label, no nested border
```

### Buttons

```csharp
SurvivorsStylePresentation.DrawButton(rect, label, SurvivorsButtonKind.Green);  // confirm / select
SurvivorsStylePresentation.DrawButton(rect, label, SurvivorsButtonKind.Red);    // back
SurvivorsStylePresentation.DrawButton(rect, label, SurvivorsButtonKind.Blue);   // filter / toggle
SurvivorsStylePresentation.DrawButton(rect, label, SurvivorsButtonKind.Gold);   // secondary emphasis
```

Implementation draws a **solid fill + 2px darker edge**; label uses transparent `GUIStyle` (no tiled 4×4 textures).

Disabled / confirmed footer actions use `DrawFlatPanel` + centered label (`DrawVsFooterButton` pattern in `GameHUD`).

### Renown badge (header)

```csharp
SurvivorsStylePresentation.DrawCoinBadge(headerRect, renownAmount.ToString());
```

### Grid tile

```csharp
SurvivorsStylePresentation.DrawCharacterTileFrame(tileRect, isSelected, heroColor, unlocked);
// name band (PanelNavyInset) at top → optional LastRun pill → portrait → weapon icon corner
SurvivorsStylePresentation.DrawLockBadge(tileRect);      // top-right, "LOCKED" pill
SurvivorsStylePresentation.DrawLastRunBadge(badgeRect);  // below name band only
```

### Footer detail block (solo)

- Portrait **128×128** (`CharacterPortraitSize.Large`), clipped group
- Weapon frame **88×88**, vertically centered with portrait
- Text block: display name → caption (tribe/role) → passive body
- Green **SELECT** / **CONFIRM** on the right
- Centered hint row below footer (`_vsHint`)

### Stat row

Icon 26×26 + label (`CharacterSelectPresentation.*Label` constants) + value right-aligned.  
Use full words for Ultimate stats:

| Label constant | Value format |
|---|---|
| `UltimateCooldownLabel` | `{cooldown:0}s` |
| `UltimateDamageLabel` | `{damage:0}` |
| `UltimateRangeLabel` | `{radius:0.0}` |

Clip column content with `GUI.BeginGroup` after drawing the inset panel.

### List rows (expeditions, filters)

- Measure wrapped height per row
- `DrawAlternatingRowBackground(rowRect, rowIndex)` for zebra striping
- Unlocked rows: `_vsFilterActive` (TextLight); locked: `_vsMicro` (TextMuted)

---

## 7. UI Toolkit mapping (USS)

Pilot assets: `Assets/Resources/UI/CharacterSelect/CharacterSelectScreen.uxml` + `.uss`

| IMGUI token | USS approximate |
|---|---|
| `BackgroundDeep` | `background-color: rgb(10, 20, 31)` on `.character-select-root` |
| `PanelNavy` | `rgb(13, 26, 51)` |
| `PanelNavyInset` | `rgb(10, 20, 41)` |
| `BorderGoldDim` | `border-color: rgb(89, 107, 122)` |
| `TextGold` | `color: rgb(250, 209, 71)` |
| `TextMuted` | `color: rgb(158, 179, 194)` |
| Green button | `background-color: rgb(71, 184, 92)` |
| Red button | `background-color: rgb(184, 46, 36)` |
| Blue button | `background-color: rgb(46, 87, 184)` |

When adding a new screen:

1. Copy Character Select USS variables into a shared `SurvivorsTheme.uss` (future).
2. Mirror layout zones from the IMGUI wireframe before switching `EnabledByDefault`.

---

## 8. Vector icon scaling (critical)

`RuntimeAssets.Circle` and `RuntimeAssets.Diamond` are authored at a **128px logical size**.

| Helper | Scale meaning | Pixel size |
|---|---|---|
| `DrawEmblem(center, radius, color)` | `radius` is normalized; diameter = `radius × 2 × 128` | |
| `DrawDiamond(center, scaleVector, color)` | each component × 128 = pixel width/height | |
| Stat icons | `iconScale = min(w,h) / 128f` | multiply normalized factors by `iconScale` |
| Silhouette (Large) | `minDimension / 128f × 0.9f` | |
| Silhouette (Compact) | `minDimension / 128f × 0.62f` | |
| Weapon icon | `minDimension / 96f` | slightly bolder than stat icons |

**Never pass raw pixel sizes** (e.g. `rect.width * 0.18f`) into normalized helpers — this caused full-screen diamond artifacts.

Always wrap portrait draws:

```csharp
GUI.BeginGroup(rect);
DrawSilhouette(new Rect(0, 0, rect.width, rect.height), ...);
GUI.EndGroup();
```

---

## 9. Screen migration checklist

Use this when porting **Camp**, **Map Select**, **Codex**, **Main Menu**, etc.

### Planning

- [ ] Wireframe with zones (header / body columns / footer / hint band)
- [ ] Add `<Screen>LayoutMetrics` constants next to `PresentationLayoutSystem.cs`
- [ ] List shared components that map to `SurvivorsStylePresentation` recipes (§6)

### IMGUI pass

- [ ] Replace screen background with `DrawScreenBackground`
- [ ] Replace `DrawPanel` + manual borders with `DrawFlatPanel` / `DrawInsetPanel`
- [ ] Replace action buttons with `SurvivorsButtonKind` semantics
- [ ] Replace abbreviated copy with full words; centralize labels in a presenter static class
- [ ] Measure text heights; remove `Short*` string helpers
- [ ] Clip scrollable columns with `GUI.BeginGroup`
- [ ] Audit primitive draws for 128px scale convention

### Validation

- [ ] 1920×1080 and 1280×800 — no clipped text
- [ ] Ui scale 90%, 100%, 120%
- [ ] **Keyboard-only** — complete flow without mouse
- [ ] **Gamepad-only** — focus ring visible on all selectable controls
- [ ] **Touch-only** — all targets ≥ 44×44 px; gameplay overlay move/ult/pause/details
- [ ] High contrast preference still respected where `PresentationTheme` applies

### UI Toolkit pass (optional, later)

- [ ] UXML zone tree matches IMGUI wireframe
- [ ] USS uses palette from §7
- [ ] Feature flag `EnabledByDefault = false` until parity verified

---

## 10. Suggested migration order

| Priority | Screen | Notes |
|---:|---|---|
| 1 | **Map Select** | Similar header + grid + detail footer; reuse tile frame and footer block |
| 2 | **Camp hub** | Ledger / renown badge already exist — restyle panels and buttons only |
| 3 | **Codex** | List + detail split; reuse stat labels and lock badge |
| 4 | **Main menu** | Simplest layout; validates buttons and background on one screen |
| 5 | **Pause / Settings** | Keep existing settings logic; apply typography and button styles |

---

## 11. Anti-patterns (learned from Character Select)

| Problem | Cause | Fix |
|---|---|---|
| Giant yellow diamonds | Pixel size passed as normalized scale | §8 scale rules |
| Yellow stats column | `DrawEmblem` with pixel radius | Use `/128f` normalization |
| Button vertical stripes | 4×4 tiled button texture | Solid fill + transparent button style |
| LAST RUN over name | Badge Y overlapped name band | Place badge at `nameBand.yMax + 2` |
| Footer portrait overflow | Silhouette scale > 1 on Large | `minDimension/128*0.9` + `BeginGroup` |
| Cramped UI | Nested ornate borders | Single border or fill only |
| Cryptic stats | `ULT CD` / `ULT RAD` | Full labels in `CharacterSelectPresentation` constants |

---

## 12. Source reference (Character Select)

| Concern | Primary file |
|---|---|
| Style primitives | `Assets/Scripts/Runtime/SurvivorsStylePresentation.cs` |
| Layout tokens | `Assets/Scripts/Runtime/PresentationLayoutSystem.cs` |
| Stat labels & icons | `Assets/Scripts/Runtime/CharacterSelectPresentation.cs` |
| Solo/co-op layout | `Assets/Scripts/Runtime/GameHUD.cs` → `DrawCharacterSelect*` |
| UI Toolkit pilot | `Assets/Scripts/Runtime/CharacterSelectUiToolkitScreen.cs` |
| Layout wireframe doc | `docs/UI_LAYOUT_GUIDE.md` |

---

## 13. Copy & label conventions

- **ALL CAPS** for titles, buttons, tile names and section headers (VS-style density).
- **Full words** over abbreviations in stat and tutorial copy (`ULT COOLDOWN`, not `ULT CD`).
- **Expedition filter** short form: `[x] SCOUT — Frostbound Shore` (checkbox + duration + region nickname).
- **Control hint** format: `P1 • ARROWS CHOOSE • [SPACE] SELECT • KEYBOARD + ACTIVE GAMEPAD` (centered, `_vsHint`).

Maintain label constants in the screen presenter (`CharacterSelectPresentation.*Label`) so IMGUI and UI Toolkit stay aligned.
