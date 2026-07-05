# DESIGN — Fletching (mod rescope: metals + woods)

**STATUS: v0 BUILT 2026-06-11, server-smoke clean** (throwaway dataPath, 4849 items,
1173 grid recipes, zero resolution failures; chiseltools patch errors are pre-existing
compat noise). Discovery during build: the metal half already existed — arrowheads,
arrow items, projectile entities, and lang for mithril/adamantite/runite/dragon were
registered by earlier patch sweeps (tools_full_coverage, heads_shape_overhaul,
projectile_register, smithing_recipes_sweep), and vanilla's own arrow grid recipe
accepts `arrowhead-*` wildcards — so tipped arrows worked before fletching landed.
v0 therefore ships the wood half: arrowshaft / headlessarrow / bowstring items,
shortbow+longbow ×6 RS-named wood tiers (common/oak/willow/maple/yew/magic) as
`ItemBow` on vanilla shapes with per-wood texture overrides (draw/charge animations
inherited free), unstrung (u) staves on vanilla stave shapes, 18 grid recipes
(shafts/headless/tipped/bowstring/12 stave cuts/2 stringings), RS examine-text
itemdescs, generated magic-wood texture (charcoal + cyan glint, baked not engine-glow),
arrowshaft added to the attractor junk table. Remaining human-eye checks: bow looks
per wood in hand/GUI, magic texture taste, draw animation feel, recipe walk in-game. Rescope requested by RHO ("we need to rescope runescape
metals with + woods. meaning FLETCHING!!"). One mod, one nomenclature — fletching lives
in runescapemetals, no sibling mods. modid stays `runescapemetals` forever (changing it
orphans every item in every save); the display name and description carry the new scope.

## why this marriage works

- The metal ladder already exists → arrowheads per tier come almost free.
- Ava's attractor (built 06-11) recovers fired ammo → fletching supplies the ammo
  economy the attractor exists to serve. The two halves complete each other.
- Knife-on-wood is THE RS fletching gesture, and VS grid recipes support tool
  ingredients natively.

## wood tier ladder — RS → VS stand-ins (RHO to confirm/correct)

VS 1.22 ships: birch, oak, maple, pine, acacia, kapok, baldcypress, larch, redwood,
walnut, ebony, purpleheart. No willow, no yew, no magic. Proposed mapping:

| Tier | RS wood | VS stand-in | Rationale |
|---|---|---|---|
| 1 | Logs (normal) | **any common log** (pine, birch) | the everywood |
| 2 | Oak | **oak** | native, exact |
| 3 | Willow | **baldcypress** | the waterside/swamp tree — willow's silhouette and habitat |
| 4 | Maple | **maple** | native, exact |
| 5 | Yew | **larch** | tough conifer with the longbow heritage |
| 6 | Magic | **purpleheart** | the purple exotic — nearest visual kin to magic logs |

**RHO's correction, 2026-06-11 (table otherwise blessed as proposed):** magic-tier
coloring must be RS-appropriate, not purpleheart-purple. Purpleheart is only the
*source species* (what you chop); every magic-tier product — shafts, unstrung and
strung bows, any future magic log item — wears the RS magic palette: dark charcoal
timber with luminous blue-green/cyan streaks and glint. Implementation notes:
custom textures (not purpleheart recolors), and a modest `glowLevel` on magic-tier
bow shapes so the glint reads in hand and on the back the way magic logs read in RS.

Reserved for later: walnut/ebony/redwood (crossbow stocks follow the RS stock ladder
if/when crossbows happen).

## the fletching chain (v0)

All grid recipes use a knife as a durability-consuming tool ingredient (`isTool: true`),
matching `knife-*` so any metal knife works — RS's knife-in-inventory gesture.

1. **Arrow shafts**: knife + any log → 16× `arrowshaft` (RS gives 15/log; 16 stacks
   cleaner in VS).
2. **Headless arrows**: 4× arrowshaft + 1× feather → 4× `arrow-headless`.
3. **Tipped arrows**: 4× arrow-headless + 4× arrowhead-{metal} → 4× arrows.
   - Vanilla already covers copper/bronzes/iron/steel arrows — we do NOT duplicate.
   - Mod adds `arrowhead-mithril`, `arrowhead-adamantite`, `arrowhead-runite`
     (smithing recipes on the anvil) and matching `arrow-{metal}` items.
   - Dragon arrows: later, special (RS treats dragon ammo as exotic).
4. **Unstrung bows**: knife + log of tier wood → `shortbow-unstrung-{wood}` or
   `longbow-unstrung-{wood}` (shortbow cheaper: 1 log → 1; longbow 2 logs → 1, or
   same log count with longer crafting flavor — tune).
5. **Bowstring**: 3× flax twine → 1× `bowstring` (vanilla flax economy; the
   spinningwheel mod in RHO's modlist makes twine pleasant to mass-produce — RS's
   spin-flax loop recreated by accident).
6. **Stringing**: unstrung bow + bowstring → finished `shortbow-{wood}` /
   `longbow-{wood}`.

## stats ladder (v0 shape, numbers tuned at implementation)

- Bows use vanilla `ItemBow` class — no custom C# for v0.
- Shortbows: faster aim/draw, lower damage. Longbows: slower, higher damage,
  better accuracy at range.
- Tier-1 shortbow anchors just under vanilla `bow-simple`; magic longbow lands a
  touch above vanilla `bow-recurve` (best craftable should beat best vanilla, but
  not embarrass it).
- Arrow damage rides the metal: mithril/adamantite/runite arrowheads scale above
  vanilla steel following the mod's existing tier curve.

## implementation discipline (read BEFORE writing any JSON/C#)

- **Read vanilla `bow-*.json` and `arrow.json` first** — copy the actual attribute
  names (damage, accuracy, drawtime live in attributes the ItemBow code reads;
  do not guess). Cheat sheet rule: the vanilla file is authoritative.
- **Arrowhead smithing recipes ingredient on `ingot-*`** (allowedVariants), NEVER
  on `workitem-*` — the open adamant-anvil bug (DEBUG_adamant_anvil_placement.md)
  implicates exactly that anti-pattern with an int.MaxValue quantity. Resolve or
  at minimum understand that bug before authoring new smithing recipes, so the
  broken pattern doesn't propagate.
- New textures: shafts/headless/arrows can lean on vanilla arrow art recolors;
  bows per wood tint off the wood's plank palette.
- Attractor junkTable: once shafts exist, add `runescape:arrowshaft` as a junk
  entry (low weight) — the attractor pulling fletching supplies is pure RS.
- Smoke-test on throwaway /tmp dataPath, never the live save.

## v0 cut line

IN: shafts, headless, mithril/adamantite/runite arrowheads + arrows, short+long
bows ×6 wood tiers, bowstring, stringing recipes, stats ladder.
OUT (later): crossbows + stock ladder, darts, dragon arrows, brutal arrows,
grand-exchange-style bulk recipes, fletching XP flavor messages.
