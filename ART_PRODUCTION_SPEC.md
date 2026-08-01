# Nightfall Meadow — Production Art & UX Specification

## Target

The game must read as a finished top-down 2D survivor-like, not a canvas demo or a collection of runtime primitives.

The final visual sentence is:

> A moonlit pastoral clearing becomes dangerously crowded, while the player remains readable at a glance.

## Camera and battlefield

- Use a top-down 16:9 battlefield. Do not use a horizon-line scene or a boxed play area.
- Gameplay must occupy the full screen behind a sparse overlay HUD.
- The main authored environment is a moonlit woodland clearing with:
  - irregular grass tiles and moonlight pools;
  - a winding dirt path;
  - edge trees and bushes that frame the arena without forming visible walls;
  - a small pond, broken stone arch, standing stones, lanterns and an abandoned cart;
  - restrained flowers, grass tufts and stones that never hide enemies or pickups.
- Background brightness remains below enemy, projectile and XP brightness.
- Avoid large empty areas, smooth vector gradients, random ellipses and browser-dashboard framing.

## Logical pixel grid

- Author the composition at `480 × 270`.
- Export presentation images at `1920 × 1080` using 4× nearest-neighbour scaling.
- Use 32 px logical cells for characters and enemies.
- Unity imports: Point filtering, no mipmaps, no compression.
- Web rendering: `image-rendering: pixelated`.

## Player and enemy readability

- Player always contains cream plus lavender/coral accents and a dark grounding shadow.
- Standard enemies use one dominant body colour and one readable facial feature.
- Every enemy family needs a different silhouette and movement problem:
  - Dusk Slime: round crowd filler;
  - Lantern Moth: wide flying silhouette and dive attack;
  - Wool Sprite: pale clustered body and pack movement;
  - Mushroom Thief: narrow cap silhouette and zigzag movement;
  - Moonhorn: wide horned elite with a charge telegraph;
  - Hedge Witch: ranged silhouette with curse-seed projectiles.
- Do not differentiate enemies using health alone.

## HUD contract

- Top-left: portrait, name and one health bar.
- Top-centre: survival timer only.
- Top-right: level, kills and chests.
- Bottom-centre: six compact loadout slots.
- Bottom edge: one thin full-width XP bar.
- No permanent control legend during active gameplay.
- No giant dashboard, debug telemetry or decorative frame around the battlefield.

## Upgrade screen

- Freeze combat and dim the authored battlefield rather than replacing it.
- Present exactly three parchment cards.
- Every card contains category, icon, short name, measurable effect and key number.
- Normal cards use parchment/timber; evolution cards add gold or coral trim.
- Cards must be selectable by mouse, controller and keys `1`, `2`, `3`.

## Visual effects budget

For ordinary hits, use at most three channels: sprite flash, particles, sound, recoil or damage number.

- Common hit: flash + small particles.
- Critical/evolved hit: flash + stronger particles + short sound.
- Player hit: recoil + sound + brief edge vignette.
- Level up: freeze + chord + card rise.
- Boss defeat: short slowdown + large burst + reward reveal.

## Asset structure

```text
Assets/Art/NightfallMeadow/
  Backgrounds/
  Characters/
  Enemies/
  Effects/
  UI/
  Reference/
web/assets/nightfall/
```

The reference sprite sheet in this branch establishes the palette, 32 px entity scale, loadout icon family and effect language. Replace runtime-drawn placeholder geometry with authored PNG sheets without changing gameplay APIs.

## Acceptance criteria

- The player, dangerous enemy, enemy projectile and XP pickup can each be identified within 200 ms.
- UI never covers nearby enemies.
- The battlefield looks intentionally composed when no units are present.
- At 300 active enemies, background decoration remains visually subordinate.
- Menu → run → level-up → pause → result → restart uses one coherent material language.
- All assets remain original and do not copy protected characters, icons, maps or exact layouts from other games.
