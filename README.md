# RuneScape Metals

Vintage Story 1.22 content mod adding four post-steel metal tiers.

## Tiers

| Metal      | Tier | Hue  | Density | Anvil | Reinforcement |
|------------|------|------|---------|-------|---------------|
| Mithril    | 5    | 220° | 5500    | 5     | 920           |
| Adamantite | 6    | 140° | 8500    | 6     | 1040          |
| Runite     | 7    | 190° | 8800    | 7     | 1200          |
| Dragon     | 8    | 0°   | 10000   | 8     | 1400          |

Numeric stats are tier-scaled from vanilla iron's baseline:
mithril ×1.15, adamantite ×1.30, runite ×1.50, dragon ×1.75. Iron stays untouched.

Vanilla steel sits at tier 4. Each new metal has its own anvil at the matching tier; smithing a tier-N ingot requires the tier-N (or higher) anvil.

## What it adds

- **Ingots**, **nuggets**, **ores** (poor / medium / rich / bountiful grades), **loose surface ores** for all 4 metals
- **Worldgen**: rarer and deeper as tier rises (mithril mid-depth → dragon ultra-rare deep)
- **Tools**: pickaxe, axe, chisel, hammer, prospectingpick, saw, scythe, shears, cleaver, knife, blade-falx, spear, shovel, hoe, helvehammer, arrow (+ all tool heads / blades / arrowheads)
- **Armor**: plate / chain / scale / brigandine, each in helmet / cuirass / leggings
- **Anvil smithing recipes** auto-extended via `allowedVariants` patches
- **Per-metal anvils** at tiers 5/6/7/8 (built via existing anvil-base/anvil-top smithing recipe)
- **Mining tier gating**: tier-N pickaxe required to mine tier-N ore
- **Contraptions**: lanterns (small + large), padlocks, metal nails-and-strips, chests, cabinets, helve-hammer assembly, metal plaques
- **Smithing parts**: bracket, hub-metal, hoop, shield boss, rod, nails, tongs, wrench, crowbar, pounder cap, punch set
- **Shields**: round shields in all three constructions (woodmetal, woodmetalleather, full metal), with tier-scaled durability gains
- **Apiary v2**: white OSRS-style hive assembled from 3 stackable sections (stand/body/roof); scoop-gated weekly harvest of 3–6 honeycombs + beeswax, tamed swarming into nearby empty skeps, and real winter dormancy

## File layout

```
runescapemetals/
├── modinfo.json
├── README.md
├── scripts/
│   ├── recolor_phase2.py     (luminance-preserving tinter — Phase 2 generator)
│   └── gen_patches.py        (deterministic patch emitter)
└── assets/runescape/
    ├── lang/en.json
    ├── patches/
    │   ├── metal_register.json                    Phase 1 — base 4 metals
    │   ├── toolmetal_register.json                Phase 1
    │   ├── tools_register.json                    Phase 1 — blade/pickaxe/axe + heads
    │   ├── smithing_recipes.json                  Phase 1 — pickaxe/axe/blade/knife
    │   ├── ore_nugget_register.json               Phase 2 — nugget worldproperty
    │   ├── ore_graded_worldproperty_register.json Phase 2 — ore-graded type list
    │   ├── ore_blocks_register.json               Phase 2 — block variants
    │   ├── ore_items_register.json                Phase 2 — dropped ore + crystalized
    │   ├── ore_mining_tier_register.json          Phase 2 — tier-gating
    │   ├── ore_metalunits_register.json           Phase 2 — yield per grade
    │   ├── pickaxe_attrs_register.json            Phase 2 — tooltier/durability/speed
    │   ├── nugget_smelting_register.json          Phase 2 — combustibleProps
    │   ├── looseores_register.json                Phase 2 — surface scatter
    │   ├── armor_register.json                    Phase 2b — armor itemtypes
    │   ├── armor_smithing.json                    Phase 2b — anvil intermediates
    │   ├── armor_grid.json                        Phase 2b — grid-craft helmets/etc
    │   ├── ingot_balance.json                     Phase 2b — anvil tier + reinforcement
    │   ├── anvil_register.json                    Phase 3 — per-metal anvils + recipes
    │   ├── fix_bladehead_knifeblade.json          Bugfix — variant-axis index fix
    │   ├── tools_coverage.json                    Bugfix — chisel/hammer/etc + heads
    │   ├── shape_redirects.json                   Bugfix — point new metals at iron/steel shapes
    │   ├── contraptions_register.json             Phase 4 — lantern textures/stacks, padlock + metalnailsandstrips variants
    │   ├── contraptions_grid.json                 Phase 4 — grid recipes: lantern, chest, cabinet, helvehammer, metalplaque
    │   ├── contraptions_smithing.json             Phase 4 — smithing recipes: padlock, bracket, hub-metal, hoop, boss, rod, nails, tongs, wrench, crowbar, poundercap, punchset
    │   ├── shield_register.json                   Phase 4 — roundshield variantGroupsByType + tier-scaled durability gains
    │   └── shield_grid.json                       Phase 4 — roundshield grid recipes (28 sub-recipes × boss/hoop/plate)
    ├── worldgen/deposits/
    │   └── {mithril,adamantite,runite,dragon}.json
    └── textures/
        ├── block/metal/ingot/{metal}.png          ingots
        ├── block/metal/sheet/{metal}{1..5}.png    armor sheets
        ├── block/metal/anvil/{metal}.png          anvils
        ├── block/stone/ore/{metal}{1..3}.png      ore overlays
        ├── item/resource/nugget/{metal}.png       nuggets
        └── entity/humanoid/serapharmor/{plate,chain,scale,brigandine}/{metal}.png
```

## Iron flow — by design, not configurable

Iron ore smelts straight to iron ingots (`patches/iron_nuggets_to_ingot.json`); iron
blooms are obsolete in this mod and this is deliberately NOT a config option.

Design rationale: a bloom exists because a bloomery can't reach iron's melting point —
solid-state reduction leaves slag fused to the sponge, hence vanilla's hammering step.
This mod's premise is heat well past that (four tiers above steel); in a true melt the
detritus floats and separates, so you cast clean ingots and the bloom step has no
gameplay-additive reason to exist. Bloomeries are stone-age tech relative to this ladder.

Technical rationale: the crucible machine / molten-unit / casting chain is built on
well-formed ingot outputs. Blooms have no `combustibleProps` (meltingPoint resolves to
float.MaxValue), so a bloom in a cook slot stalls the machine, and making blooms
meltable was already tried and reverted (see the `*.disabled-2026-06-05-bloom-destruction`
patches — generic smelting destroys the bloom's stored unit value). Do not resurrect
the bloom path.

## Testing

In creative:
1. Open inventory, type a metal name in the search bar
2. Expect: ingot + nugget + ore variants + all tools + all armor + tool heads

Via chat (cheats on):
```
/give @s ingot-mithril
/give @s pickaxe-dragon
/give @s armor-body-plate-runite
```

In survival:
- Worldgen deposits only spawn in unexplored chunks. Travel out or start a new world.
- Mithril ore is mineable with a tier-5+ pickaxe (requires mithril pickaxe or better).

## Known limitations

- No nugget shape files for items dropped from ore — may render with fallback shape.
- KCs Dragons / BetterRuins compatibility: BetterRuins has a recipe expecting `metalblock-corroded-sheet-{metal}` (corroded ruin metal sheet) which doesn't exist for new metals. Harmless warnings on load; recipe is unused.

## Patch design notes

- All patches use JSON Patch RFC 6902 (`op` / `path` / `value`).
- File targets use `game:` prefix (vanilla survival assets).
- Strict JSON only — no trailing commas, all keys quoted.
- Variant patches always check the right `variantgroups[N]` index — not all tool itemtypes use the same axis order (bladehead has `type` at 0 and `metal` at 1).
- Texture lookup is path-based, not domain-based: `block/metal/ingot/{metal}` resolves across all loaded mod domains, so this mod's PNGs are picked up by vanilla itemtype texture refs without explicit `runescape:` prefix.
