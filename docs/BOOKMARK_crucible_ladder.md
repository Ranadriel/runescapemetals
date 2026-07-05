# Crucible Ladder — Ramshackle v0.1 Bookmark

*Paused 2026-06-06. Steel/Adamantite/Dragon crucibles stubbed but UNTESTED in-game.*

## Design summary

Three-tier bulk-melt crucible family that scales above the vanilla clay crucible. Each shell melts its own tier and below. Mithril and runite are skipped as friction tiers — their bulk option only opens at the next shell up, forcing the player to climb.

| Crucible      | Melts (up to)             | maxHeatableTemp | Slots | Volume |
|---------------|---------------------------|-----------------|-------|--------|
| Vanilla clay  | bronze/iron range          | 1200            | 4     | 1x     |
| Steel         | steel (1502)               | 1600            | 16    | 4x     |
| Adamantite    | adamantite (1953) + mithril| 2100            | 32    | 8x     |
| Dragon        | dragon (2629) + runite     | 2800            | 64    | 16x    |

Friction tier math:
- mithril (1727): steel can't, adamantite can — *mithril gets bulk at adamantite tier*
- runite (2253): adamantite can't, dragon can — *runite gets bulk at dragon tier*

## Recipe (3x3 grid, same shape per tier)

```
[ rod ] [ plate ] [ rod ]
[2 clay cru] [ plate ] [2 clay cru]
[ rod ] [ plate ] [ rod ]
```

= 4 × `rod-{tier}` + 3 × `metalplate-{tier}` + 4 × any fired clay crucible

The grid cross-section literally is the pot: vertical plate-spine = hoops, corner rods = staves, side cavities = the four clay crucibles being bound into one larger vessel.

## Files written

- `assets/runescape/blocktypes/crucible-steel.json`
- `assets/runescape/blocktypes/crucible-adamantite.json`
- `assets/runescape/blocktypes/crucible-dragon.json`
- `assets/runescape/recipes/grid/crucible-steel.json`
- `assets/runescape/recipes/grid/crucible-adamantite.json`
- `assets/runescape/recipes/grid/crucible-dragon.json`
- `assets/runescape/lang/en.json` (display names appended)

## Known fragile spots (verify in playtest)

1. **Wildcard crucible match** — recipes use `game:crucible-*-fired` without `allowedVariants`. May silently fail to match. Fix: enumerate `["blue","fire","black","brown","cream","earthyorange","gray","orange","red","tan"]`.
2. **Class reuse** — `BlockSmeltingContainer` / `SmeltedContainer` reused from vanilla; may assume clay-specific attributes.
3. **GUI slot count** — 16/32/64 slots may overflow vanilla crucible's 4-slot GUI layout.
4. **Placeholder shape and texture** — using vanilla clay crucible shape, just bigger collision; textures pull from `block/metal/ingot/{tier}` for shell sheen.
5. **Adamantite & dragon rod/plate codes** — recipes assume `runescape:rod-adamantite` / `runescape:metalplate-adamantite` / `runescape:rod-dragon` / `runescape:metalplate-dragon` exist. The mod's `parts_register.json` adds these as variants of vanilla `game:rod` and `game:metalplate`, so the actual resolved code is `game:rod-adamantite` etc. *Recipes may need to use `game:` namespace, not `runescape:`.*

## When you resume

Boot-test plan:
1. Launch VS, check log for any block/recipe load errors
2. Creative mode, search "crucible" — confirm three new entries appear
3. Open handbook, find each recipe — confirm 3x3 pattern displays
4. Test-craft each tier (if rod/plate variants are reachable)
5. Place each one, verify GUI opens
6. Test melt of tier-appropriate metal — does it heat? does it pour into mold?
7. Sanity check friction gates: try to melt mithril in steel crucible (should fail — heat ceiling 1600 < 1727)

Next development steps after v0.1 verifies:
- Real barrel-shaped shape file (replace clay crucible placeholder)
- Proper textures (steel hammered finish, adamantite green sheen, dragon iridescent — author per tier)
- Tune capacities if 64 slots breaks the GUI (consider keeping slots at 16 across tiers, scaling only maxContentDimensions)
- Decide whether to also ship the standalone metal-barrel-as-storage item (currently barrel exists only inside the crucible recipe)
- Consider whether the crucibles need their own heat source (forge isn't going to reach 2800°C for dragon)
