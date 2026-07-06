# Changelog — RuneScape Metals + Woods

Internal log. Format loosely follows [Keep a Changelog](https://keepachangelog.com/).
`Unreleased` collects work that hasn't been published yet — do not bump the
`modinfo.json` version until this section is ready to ship.

Latest published: **1.0.3**.

---

## 1.0.3 — 2026-07-05

### Added — Apiary v2 rework (3-part column, apiary scoop, winter physics)

The Catherby wooden hive, rebuilt proper. A tall three-part apiary that
assembles itself when the column is complete: Hive Stand, Hive Body, Hive
Roof stacked 1×3.

- Harvest is now gated on the **apiary scoop** (new item, own texture + recipe).
  Bare hands and other tools are refused. Scoop yields 3–6 honeycombs plus a
  scrape of beeswax, roughly weekly.
- **Winter physics**: freezing stalls production; at −10°C the batch is lost
  and the timer resets. A sealed glass-roofed room buys +5°C.
- **Tamed propagation**: a settled colony with 6+ flowers within 8 blocks
  swarms into nearby empty skeps every couple of days — a permanent, tamed
  source of bees; never turns hostile.
- Flower gate unchanged: 3+ bee-feed plants within 8 blocks; more flowers =
  faster combs.
- **Backward compatibility**: apiaries placed before the rework (v1 skeps)
  are grandfathered in and continue to work.
- New classes: `BlockApiarySection` (Stand/Body/Roof parts), `BlockApiaryPart`
  (assembled column members), `BEApiary` (rewritten). New items:
  `apiaryscoop`. New recipes: `apiarysection-base/body/roof`, `apiaryscoop`.

### Added — Smith's Codex v2 (the Super Codex)

Data-driven codex reader: pages now live on disk under
`assets/runescape/config/codex/*.vtml` referenced by `index.json`. Adding a
page = drop a `.vtml` file, list it in `index.json`, done — no code change.

- Search box (case-insensitive across label / category / body).
- Category grouping in the nav column with per-category filter buttons.
- Cross-linked bodies via `<a href="key">label</a>` — VS richtext links.
- Bookmarks (client-side persistence via `capi.Settings.String`).
- Back button + navigation history (up to 32 hops).
- `/codexreload` server command re-reads pages without restart.
- 15 seed pages: overview, quickstart, metals, crucible, superheater,
  smithing, lamellae, scoop, apiary, apiary scoop, fletching, specials,
  hunter, packs, integrations, changelog.

### Added — Tiered special-attack tools (post-steel harvest ability scaling)

New pattern: four metal tiers past steel (mithril / adamantite / runite / dragon)
scale a per-tool special ability by tier. Vanilla behavior preserved for

### Added — Tiered special-attack tools (post-steel harvest ability scaling)

New pattern: four metal tiers past steel (mithril / adamantite / runite / dragon)
scale a per-tool special ability by tier. Vanilla behavior preserved for
copper..steel; higher tiers subclass the vanilla `Item*` class and register via
`classByType` on the vanilla itemtype JSON. All classes fall through to `base` for
default tiers so no lower-tier regressions.

- **`ItemScytheTiered`** (`src/Item/ItemScytheTiered.cs` + `patches/scythe_swing_radius.json`) — swing radius grows 1 block per tier past steel. Steel/copper `3×3×3` scan → 5 blocks; mithril `5×5×5` → 10; adamantite `7×7×7` → 15; runite `9×9×9` → 20; dragon `11×11×11` → 25. Preserves trim-mode, tool-mode selector, sweep animation, sound. Nearest-first sort so smaller patches read naturally.
- **`ItemHoeTiered`** (`src/Item/ItemHoeTiered.cs` + `patches/hoe_line_length.json`) — Harvest Moon convention: cardinal-forward line of tiles from the target. Copper..steel = 1 tile (vanilla). Mithril = 2, adamantite = 3, runite = 4, dragon = 5. Cardinal-snapped from player yaw via `BlockFacing.HorizontalFromYaw`. Vanilla `DoTill` handles each tile — soil→farmland conversion, place sound, soil-nutrition transfer, durability per tile, `MarkBlockDirty`. Non-soil tiles skip without draining durability. Per-tile canopy check so line-tills can stop at a wall.
- **`ItemShearsTiered`** (`src/Item/ItemShearsTiered.cs` + `patches/shears_cube_radius.json`) — same `TierRadius` semantics as scythe applied to shears' native 3D cube scan. Copper..steel `3×3×3` → 5 blocks (vanilla); mithril → 5×5×5 / 10; up through dragon 11×11×11 / 25. Only plant material qualifies (vanilla `ItemShears.CanMultiBreak`). Vanilla `OnBlockBreaking` damage-nearby-blocks left intact — the wider break happens only on the final swing land.
- **`ItemAxeTiered`** (`src/Item/ItemAxeTiered.cs` + `patches/axe_yield_decay.json`) — vanilla trees already fall in one swing; tier scaling operates on the **yield-decay curve** for leaf and branchy blocks during felling. Copper..steel: leaves 0.85×/block, branchy 0.70×/block (vanilla). Mithril: 0.88 / 0.775. Adamantite: 0.91 / 0.85. Runite: 0.94 / 0.925. Dragon: **1.00 / 1.00** — no decay at all, so a huge magic tree gives every sapling and every stick at full quantity. Damage on wood, tool-alive tracking, treefell sound all preserved. Wood-log drops always at 1.00 per vanilla.

### Changed — Axe stat curve rescaled to 1.25ⁿ exponential from steel baseline

Prior curve was the mod's original ×1.15 / ×1.30 / ×1.50 / ×1.75 progression. New curve is `steel × 1.25ⁿ`, giving substantially steeper progression at high tiers.

- **Attack power** (`patches/tools_stats_register.json`) —
  mithril `4.6 → 5.00`,
  adamantite `5.2 → 6.25`,
  runite `5.0 → 7.81` **(also fixes the monotonicity typo — runite was less than adamantite)**,
  dragon `6.0 → 9.77`.
- **Mining speed on wood / plant / leaves** (`patches/tools_stats_register.json`) —
  mithril `12.7 / 6.9 / 4.6 → 13.75 / 7.50 / 5.00`,
  adamantite `14.3 / 7.8 / 5.2 → 17.19 / 9.38 / 6.25`,
  runite `16.5 / 9.0 / 6.0 → 21.48 / 11.72 / 7.81`,
  dragon `19.3 / 10.5 / 7.0 → 26.86 / 14.65 / 9.77`.
- Durability left unchanged (original ×1.15/1.30/1.50/1.75 curve retained). Flag: with dragon attack power now 2.44× steel and dragon durability only 1.75× steel, the durability curve may feel light — candidate for a follow-up rescale if playtest agrees.

### Added — Lamellae production completion

Lamellae variants (mithril / adamantite / runite / dragon) existed as items and armor variants but lacked the full production flow. Completing the loop:

- **`patches/lamellae_smelting_register.json`** — adds `combustiblePropsByType` for the four new metals. Lamellae can now be melted back to ingots (matching-tier crucible required). Melting points: mithril 1727°C, adamantite 1953°C, runite 2253°C, dragon 2629°C. Same pattern as the existing nugget and metal-bit smelting registers.
- **Handbook text** (`assets/runescape/lang/en.json`, +8 keys) — descriptive handbook entries for `item-handbooktext-metallamellae-{mithril|adamantite|runite|dragon}` and `item-handbooktext-armor-*-lamellar-{mithril|adamantite|runite|dragon}`. Explains source, use, tier gating, salvage, and comparison with plate for each tier.
- **SmithCodex "Lamellae" section** (`src/Gui/GuiDialogSmithCodex.cs`) — new nav entry between Smithing and Fletching. Covers making the mold (clay → clayform → kiln), filling the mold (direct pour / scoop pour / launder pour — three methods), tier gating per crucible, assembly (lamellae + strap), salvaging. Also updated the existing Smithing section to correctly note lamellar is CAST (not anvil-smithed) — the previous text lumped lamellar in with plate/chain/scale as if all four were anvil-forged.

### Fixed — Ava's Attractor pulled arrows mid-flight before damage landed

Symptom: arrows fired by a wearer would visually fly toward the target, then teleport back to inventory *before* the target took damage.

Root cause (traced via decompiled vanilla `EntityProjectile.OnGameTick`): vanilla sets `Stuck = true` the moment `Collided` OR `collTester.IsColliding(blocks)` fires. It's persisted to `WatchedAttributes["stuck"]` and stays sticky forever (the OR includes the persisted value on subsequent ticks). Physics-engine quirks in flight (grazing hitbox edges, chunk boundaries, high-velocity partial-clips) can briefly flip `Collided` mid-flight → Stuck locks true → vanilla `IsColliding` runs `pos.Motion.Set(0,0,0)` and freezes the arrow in midair → `TryAttackEntity` finds no in-range target → no damage dealt → arrow hangs. Then `OnRecoveryTick` (every 750 ms) saw Stuck, yanked the arrow. Player sees: arrow zip back with target un-damaged.

Fix (`src/AvasAttractorSystem.cs`): two safeguards before yanking a Stuck arrow.
1. **Time-since-Stuck ≥1000 ms.** First tick that sees Stuck records `avasStuckSince` on WatchedAttributes; won't yank until 1 s has elapsed. This is well past vanilla `IsColliding`'s 500 ms internal cooldown (`msCollide + 500` early-exit), so any legitimate hit's damage flow has fully resolved before we intervene.
2. **Motion-settled check.** `proj.ServerPos.Motion.LengthSq() > 0.001` skips arrows that still have velocity — vanilla `IsColliding` zeros motion when Stuck fires, so a truly-landed arrow has near-zero motion. A mid-flight spurious-Stuck arrow may still have residual motion from the physics tracking.

Vanilla behavior for spurious-Stuck arrows is unchanged — they hang in midair permanently (vanilla design). The attractor no longer removes them prematurely; if the vanilla flow legitimately lodges the arrow (block hit, entity hit that survives), the arrow returns after 1 s + motion settle.

### Internal notes

- Two new VS-mod-compiler-classpath gotchas discovered this session, documented in `memory/feedback_vs_mod_no_linq.md` (renamed internally to `vs-mod-restricted-classpath`):
  - `System.Linq` is not on the mod compiler classpath. Any LINQ extension method fails with `CS1061`. Broke ItemScytheTiered on the first compile — first with `OrderedDictionary + LINQ`, then again after an explicit-cast attempt. Fixed by using `List<T>.Sort(Comparison<T>)`.
  - `System.Collections.Generic.Stack<T>` is not on the classpath either — `Stack<T>` lives in the separate `System.Collections.dll` assembly which the mod compiler doesn't reference (`CS0012` / `CS1069`). Broke ItemAxeTiered because vanilla `ItemAxe.FindTree` returns `Stack<BlockPos>`. Fixed by invoking `FindTree` via `System.Reflection.MethodInfo` and iterating the boxed result as non-generic `System.Collections.IEnumerable` (which IS on the classpath via `System.Runtime`).
- Both cases produced the same cascade symptom: `Successfully compiled N source files` in the log while emitting `CS####` errors above it, dropping the failing file silently. Every class in the mod then produced `no such class registered. Will ignore.` on load, which reads as an apocalypse but has one root cause. Fix rule installed: always grep `\[Error\] \[runescapemetals\] CS` above the compile-success line — that IS the load-bearing error.
- Discipline: before every new .cs edit in this mod, grep for `Stack<`, `System.Linq`, `.OrderBy`, `.Where`, `.Select` — kill on sight. Vanilla decompile patterns are traps for the mod compiler.

### Notes for the eventual publish

- Bump `modinfo.json` version from `1.0.2` → `1.0.3` (or higher if scope grows before publish).
- Update the `docs/CHANGELOG.md` `Unreleased` section header to `1.0.3 — YYYY-MM-DD`.
- Regenerate the README's "What it adds" list to include the tiered-tool abilities and the SmithCodex Lamellae section (the mod-page README is currently multiple sessions behind; see the earlier README audit for the full delta list).
- No JSON schema changes; all patches remain RFC 6902 `add` ops on existing paths.

---

## 1.0.2 — Published

Historical published state. All content prior to the tiered-tool / lamellae-completion
/ Ava's-fix work above. No detailed prior-changelog reconstruction has been attempted;
this log begins tracking from `Unreleased` forward.
