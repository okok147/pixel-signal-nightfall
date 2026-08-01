# Future Development Prompt — Nightfall Meadow

Use this prompt for the next implementation sessions.

---

You are developing **Nightfall Meadow**, an original Unity 6.5 2D survivor-like game in the repository `okok147/pixel-signal-nightfall`.

## Product goal

Make a compact, polished game that has the instantly readable pressure and progression of the survivor-like genre, but lives inside a warm fantasy-life world. The emotional sentence is:

> A gentle little life continues even when the night becomes crowded.

The game should feel cute, pastoral and handmade: soft Celtic-fantasy scenery, rounded chibi characters, warm parchment/wood UI, flowers, lanterns, sheep-like creatures, music notes and tiny household objects. This is an original direction. Never copy Mabinogi, Vampire Survivors or any other game's protected characters, names, assets, maps, icons, sound, exact interface or layout.

Read `ART_DIRECTION.md`, `GAME_DESIGN.md`, `Assets/Scripts/CutePixelKit.cs` and `Assets/Scripts/CuteNightfallPresentation.cs` before changing anything.

## Non-negotiable UX

- Movement is the primary continuous input; attacks are automatic.
- The player, dangerous enemies, enemy attacks and XP must be readable in under 200 ms.
- Gameplay receives at least 85% of the screen area.
- HUD stays compact: health top-left, timer top-center, run stats top-right, loadout bottom-center, XP along bottom edge.
- Level-up pauses the run and presents exactly three large choices usable by mouse and keys 1/2/3.
- Every upgrade description states a measurable effect.
- Never show a development dashboard, telemetry language or a permanent control legend during active play.
- Maintain 60 FPS with at least 300 active enemies on the target desktop build.

## Architecture task

The current prototype is monolithic. Improve it gradually without breaking the playable build:

1. Preserve `PixelSurvivorGame` as a fallback until extracted systems are verified.
2. Replace reflection-based presentation with explicit public read-only run state interfaces.
3. Extract these systems:
   - `RunDirector` — timer, difficulty curve, win/loss;
   - `PlayerMotor` — movement and contact safety;
   - `WeaponController` — cooldowns, targeting, projectile patterns;
   - `EnemyDirector` — spawn budget and enemy families;
   - `PickupSystem` — XP, magnet movement and chests;
   - `UpgradeSystem` — weighted choices, levels and evolutions;
   - `RunHUD` — presentation only;
   - `AudioDirector` — music layers and one-shot priorities.
4. Use ScriptableObjects for weapons, passives, enemies and evolution recipes.
5. Pool enemies, projectiles, hit effects and pickups. Do not instantiate/destroy them every hit.
6. Keep simulation code independent from skin and UI code.

## Next playable milestone

Build a polished 10-minute run with:

- one player, Meadow Courier;
- four normal enemy families and two elites;
- six weapons;
- six passive blessings;
- three evolutions;
- treasure chests;
- a mini-boss at 5:00 and boss at 10:00;
- controller, keyboard and mouse support;
- title, pause, settings and results screens;
- basic sound and music;
- saved best time, best kills and discovered evolutions.

## Enemy families

Use behaviours that create different movement problems:

- Dusk Slime — slow crowd filler, short hop acceleration.
- Lantern Moth — circles briefly, then dives.
- Wool Sprite — travels in loose packs and blocks lanes.
- Mushroom Thief — fast zigzag, low health.
- Moonhorn elite — telegraphed straight charge.
- Hedge Witch elite — keeps range and sends slow curse seeds.

Never differentiate enemies with health alone. Each family needs a silhouette, movement pattern and sound cue.

## Weapons

Start with these original weapon concepts:

- Star Wand — nearest-target bolts.
- Hearth Notes — musical notes pulse around the player.
- Shepherd Crook — sweeping close-range arc.
- Berry Basket — lobbed berries split on landing.
- Firefly Jar — orbiting lights periodically dash outward.
- Sewing Needle — fast piercing line attack.

Each weapon must show a distinct rhythm and occupy a distinct tactical role. At level 1 it should already feel useful. Level changes must be visible, not just numerical.

## Progression quality

- First level-up within 20–30 seconds.
- First meaningful build identity by minute 2.
- First evolution opportunity by minute 5–6.
- Difficulty rises through density, formation, speed and attack patterns, not only enemy HP.
- Avoid dead upgrades. A choice that cannot benefit the current build should not appear.
- Show evolution recipe hints after the player owns one component.

## Feedback budget

For every successful hit, choose at most three of: sprite flash, recoil, particles, number, sound, camera impulse. Do not trigger all five for common attacks.

- Common hit: flash + small particles.
- Critical/evolved hit: flash + stronger particles + short sound.
- Player hit: recoil + sound + brief vignette.
- Level up: freeze + chord + card entrance.
- Boss defeat: short slowdown + large burst + reward reveal.

## Art implementation

Until final hand-drawn sprite sheets exist, use `CutePixelKit` to make dependency-free placeholder assets that follow `ART_DIRECTION.md`. Runtime sprites must use point filtering and a consistent pixels-per-unit value. Replace placeholders with original commissioned/exported PNG sheets later without changing gameplay APIs.

## Working method

For every development session:

1. Inspect the current branch and existing implementation.
2. State the single player-facing improvement being targeted.
3. Make the smallest coherent set of changes.
4. Validate compilation and play flow: menu → run → level-up → pause → win/loss → restart.
5. Check 16:9 at 960×600, 1920×1080 and one ultrawide resolution.
6. Check that UI does not cover nearby enemies.
7. Commit to an `agent/...` branch with a concise message.
8. Open or update a draft PR explaining the player-visible result and any known risk.

Do not claim a feature works unless it has been compiled or directly verified. When Unity cannot be run, clearly label validation as static inspection only.
