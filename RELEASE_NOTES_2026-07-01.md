# RuneScape Metals — release notes, 2026-07-01

Everything shipped this day, held to the green-smoke standard: working
backend + clean player-facing GUI + adventure guide entries + crafting
recipes + this documentation.

## New content

### Superheater (crucible heat connector)
- Block placed against any large crucible's base: **+300°C** to its ceiling.
  Steel crucible reaches mithril; adamantite crucible reaches runite (the
  ladder's previously-missing bulk option); dragon needs nothing.
- Does not stack — strongest single neighbor counts.
- Recipe: adamantite plates + rods around a **magma scoop**.
- Crucible block-info shows "Superheater attached: +300°C". Handbook entry:
  *block-handbooktext-superheater* (the ladder's missing rung).

### Metal-framed pack ladder (bag space)
- Mithril 10 / Adamantite 12 / Runite 14 / Dragon 16 slots (vanilla sturdy
  is 8). Strictly sequential upgrade recipes: previous bag at grid centre,
  plate above, rod below, sturdy leather at sides.
- Buckles textured per metal; rides the vanilla sturdy-backpack shape.
- Handbook: *item-handbooktext-metalframepack-**.

### Wooden Skep Apiary (permanent beekeeping — the Catherby model)
- Permanent hive: harvest NEVER destroys it; breaking always returns the
  empty apiary. Three states: empty → colonised → dripping.
- Seeded by **reed skep transplant** (right-click with a populated skep;
  the woven skep is returned empty). Harvest: right-click when dripping →
  2-4 honeycomb, colony persists, re-ripens ~1.5-5.5 game days.
- Flower gate: needs 3+ bee-feed plants within 8 blocks; rich meadows ripen
  up to 3× faster. Block info shows flower count and time remaining.
- Recipe: planks / 3 honeycombs / planks with iron+ nails-and-strips.
- Design doc: APIARY_PLAN_2026-07-01.md. Handbook: *block-handbooktext-apiary-**.

## Fixes

- **Launder stands up**: the model was authored a full block below its
  position (buried/invisible when placed). All ~52 shape elements lifted
  into the block's own cell; hitboxes now wrap the sawtooth meander in
  normal 0-1 range; overlap placement guard removed (obsolete); placement
  diagnostics retained in the log. Old shape backed up (.before-lift).
  Handbook entry added (*block-handbooktext-launder*).
- **Boxtrap + birdsnare** texture parse exceptions (bare-string textures)
  fixed — 12 boot errors gone.
- **Cross-mod lang delivered properly**: primitivesurvival hooks/lures/
  spikeplates and runestory runic pickaxes now named via domain lang files
  (the old patch-based delivery silently never applied). Ava's attractor
  named in its own domain. Launder/arc/blast descriptions added.
- **Smithcodex** recipe (dead ingredient `cattailcordage` → flax twine) and
  shape (nonexistent book-held → book) fixed.
- **Chiseltools patches** dependency-gated (no more errors when absent).

## Known state (flagged, awaiting design decisions)

- Arc furnace + blast furnace: structures validate but have no smelting
  payload yet (handsome validators). Casting cradle: animation only.
- Mithril tools currently share steel's mining tier (5); bumping the tool
  ladder to 6/7/8/9 is a pending balance call across ~10 patch files.
