# UI Layout Guide — Character Select Pilot

This guide documents the **layout zones and metrics** for the Character Select screen. For colors, typography, components, vector scaling and migration steps for other menus, see **`docs/SURVIVORS_UI_STYLE_GUIDE.md`**.

Complements `docs/PRESENTATION_FOUNDATION_0.9.md` and the readability rules in `docs/PROJECT_MASTER_PLAN.md` §9.2.

## Why the old screen felt cramped

Project Expedition and reference games like Vampire Survivors both target a **1920×1080 logical canvas** scaled through `PresentationLayout`. The previous Character Select screen wasted space with:

- a large title block and subtitle band;
- a fixed **480px** roster grid (3 columns);
- detail text competing with the grid in the same panel;
- hard height caps and abbreviated names (`ShortCharacterName`).

The redesign increases information density through **layout zones**, **compact typography**, and **measured text heights** — not through changing resolution.

## Character Select zones (solo)

Inspired by Vampire Survivors' information density:

```
┌──────────────────────────────────────────────────────────────────┐
│ HEADER: Renown | Title | Back                                    │
├──────────┬───────────────────────────────────────┬───────────────┤
│ STATS    │ ROSTER (4 columns, square-ish tiles)  │ FILTER        │
│ 280px    │ name band + weapon icon per tile      │ + expeditions │
├──────────┴───────────────────────────────────────┴───────────────┤
│ FOOTER: portrait | starter weapon box | passive | SELECT         │
│ CONTROLS centered below footer                                   │
└──────────────────────────────────────────────────────────────────┘
```

Side columns are **280px** each. The roster grid expands to fill remaining width (~1280px at 1080p).

## Anti-clipping rules (Character Select)

1. Do not use fixed row heights for expedition filter labels — measure wrapped height per row.
2. Use short expedition labels (`SCOUT — Frostbound Shore`) instead of full map titles.
3. Stat rows use a cursor layout with 34px row height inside the stats panel.
4. Footer shows passive description only; numeric stats live in the left column.
5. Grid tiles use a fixed 32px name band at the top (Vampire Survivors layout).
6. Ultimate stat labels use full words (`ULT COOLDOWN`, `ULT DAMAGE`, `ULT RANGE`) — see `CharacterSelectPresentation.*Label`.

## Spacing tokens

Defined in `PresentationSpacing`:

| Token | Value | Typical use |
|---|---:|---|
| `Space4` | 4px | tight inner padding |
| `Space8` | 8px | badge padding |
| `Space12` | 12px | panel inset |
| `Space16` | 16px | column gaps, grid gaps |
| `Space24` | 24px | section separation |

## Compact typography tokens

Defined in `PresentationTypography` / `CompactFontToken`:

| Token | Base size | Use |
|---|---:|---|
| `Display` | 36px | screen title |
| `Heading` | 22px | survivor name |
| `Body` | 16px | description, Ultimate |
| `Caption` | 13px | stats, grid names, hints |
| `Micro` | 11px | badges, filter copy |

These sizes are scaled by `PresentationPreferences.Data.UiScale` (90–120%).

## Layout helpers

`PresentationLayoutSystem.cs` provides:

- **`LayoutZone`** — splits parent rects into columns/rows without magic numbers.
- **`PresentationTextMeasure.MeasureHeight`** — computes wrapped label height before drawing.
- **`CharacterSelectLayoutMetrics`** — shared Character Select constants (column widths, minimum tile size).

## Stat and weapon icons

`CharacterSelectPresentation` draws placeholder icons from `RuntimeAssets` primitives:

- stat rows in the left column (health, speed, armor, Ultimate stats);
- starter weapon badge on each grid tile and in the footer.

Final art can replace these with sprite sheets under `Assets/UI/Icons/` during the UI Toolkit migration.

## UI Toolkit migration path

Phase 2 assets live under:

- `Assets/Resources/UI/CharacterSelect/CharacterSelectScreen.uxml`
- `Assets/Resources/UI/CharacterSelect/CharacterSelectScreen.uss`
- `Assets/Scripts/Runtime/CharacterSelectUiToolkitScreen.cs`

IMGUI remains the active renderer. Set `CharacterSelectUiToolkitScreen.EnabledByDefault = true` to switch after validating PanelSettings scaling and input routing.

## Style reference

Visual treatment (palette, buttons, badges, borders, migration checklist) lives in **`docs/SURVIVORS_UI_STYLE_GUIDE.md`**. This file covers Character Select geometry only.

## Manual acceptance checklist

- 1920×1080 and 1280×800: no clipped names, descriptions, or Ultimate text
- UI scale 90%, 100%, 120%: footer and grid remain readable
- Solo: 4-column grid, stats column populated for unlocked heroes
- Co-op: both panels show Ultimate text
- Filter toggle hides locked survivors from the grid when ON
- Gamepad, keyboard, and mouse navigation unchanged
