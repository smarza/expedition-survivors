# Art direction — UI asset catalog

All UI art loads through `UiArtCatalog` with procedural fallbacks when PNGs are absent.

## Path conventions (`Assets/Resources/Art/`)

| Category | Path pattern | Master size |
|---|---|---|
| Title | `Title/ProjectExpedition_TitleArt.png` | 1920×1080 |
| Character portrait | `Characters/{id}_Portrait.png` | 512×512 |
| Map key art | `Maps/{id}_KeyArt.png` | 1280×720 or 1920×1080 |
| Item icon | `Items/{id}_Icon.png` | 128×128 |
| Relic icon | `Relics/{id}_Icon.png` | 128×128 |

Stable IDs use dots in code (`weapon.frost_axe`) and underscores in filenames (`weapon_frost_axe_Icon.png`).

## Readability gates

- Portraits: legible at 128px and 64px tile sizes.
- Item icons: legible at 32px (gameplay bottom bar) and 48px (Level Up cards).
- Map/title art: lower-third vignette reserved for buttons and labels.

---

# Haldor key-art direction

The included selection-screen key art was generated with the built-in image generator from this production prompt:

> Create original square 2D game key art for Haldor Stormborn, the iconic favorite hero of a new survivors-like. Waist-up charismatic Viking expedition leader, broad-shouldered, copper-blond braided beard, eyebrow scar, confident half-smile, undercut hair, dark navy wool and leather armor, fur mantle, iron raven brooch, round shield edge, one-handed rune axe glowing icy cyan. Nordic dusk, fjord cliffs, snow and subtle aurora. Premium hand-painted indie game concept art with crisp readable silhouette and dramatic cold rim light plus warm firelight. Heroic, loyal and adventurous, not grim. No horned helmet, no gore, no skull motifs, no text, no UI, no logo, no watermark, no copyrighted character resemblance.

## Production notes

- Preserve Haldor's face, beard, undercut, raven brooch, navy/copper/cyan palette and asymmetrical axe/shield silhouette.
- The key art is suitable for character selection and marketing exploration, not yet a final in-game animation sheet.
- In-game prototype actors are deliberately generated from simple shapes. A later milestone will create directional animation, attack poses, VFX and damage readability at gameplay scale.
