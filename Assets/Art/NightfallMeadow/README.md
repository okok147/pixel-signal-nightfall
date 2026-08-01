# Nightfall Meadow Art Pack

This folder is the hand-authored production target for the Unity and browser versions.

## Included now

- `Reference/sprite_sheet_256.png` — transparent 32 px-cell reference for Meadow Courier, Dusk Slime, Lantern Moth, Wool Sprite, Mushroom Thief, Moonhorn, pickups, weapons and pulse effects.

## Companion pack generated for this redesign

- moonlit top-down clearing background at 480×270 and 1920×1080;
- finished gameplay composition with HUD;
- level-up composition with three upgrade cards;
- 512 px transparent UI atlas;
- 256 px transparent sprite/effect sheet.

## Import settings

- Texture Type: Sprite (2D and UI)
- Filter Mode: Point
- Compression: None
- Generate Mip Maps: Off
- Pixels Per Unit: 32 for entities
- Mesh Type: Full Rect for UI

## Integration order

1. Replace runtime background gradients and decorative geometry with the authored clearing.
2. Replace runtime character/enemy primitives with sprites from dedicated sheets.
3. Rebuild the HUD using sliced panel textures rather than solid rectangles.
4. Preserve simulation code and public state APIs while replacing presentation.
5. Validate at 960×540, 1920×1080 and ultrawide.

See the repository root `ART_PRODUCTION_SPEC.md` for the complete visual and UX contract.
