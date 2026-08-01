## Pixel Signal: Nightfall — progress

- 2026-08-01: Unity 6.5 `6000.5.6f1` project created and opened through Unity Hub / Computer Use.
- 2026-08-01: Main scene is `Assets/Scenes/Main.unity`; it is the enabled build scene.
- 2026-08-01: Implemented the original survivor-like loop: automatic Spark Wand attacks, edge spawning and enemy pursuit, signal-shard XP, three-card level-up selection, weapon/passive upgrades, Ember Ring, Cinder Volley evolution, periodic chests, pause, death, victory timer, restart, and HUD.
- 2026-08-01: Revised visual direction to a hybrid scene: generated navy-teal relay-field background with restrained orange/magenta signals, crisp limited-palette pixel characters, separate low-opacity ground shadows, and a dark translucent cyan-led HUD rail.
- 2026-08-01: Unity Play verification completed after the final compile fix: main menu renders, Enter starts a run, automatic bolts and enemies appear, HUD updates, pause/resume works, and level-up cards accept numeric selection. Console showed no errors in the final Play run.
- 2026-08-01: The project uses `activeInputHandler: 2` so the legacy input calls used by the prototype and Unity's current input backend can coexist.
- 2026-08-01: Added `web/` as a browser companion and reskinned the playable loop to match the GitHub Nightfall Meadow direction: meadow field, moonlight, Meadow Courier, Dusk Slime, Moonhorn, parchment/wood HUD, compact loadout, and full-width moon-dew progress bar.
