# Credits

## Ranadriel
Mod author. Mithril/adamantite/runite/dragon tier ladder, multiblock furnaces, dragon-tier specials.

## LenKagamine
Collaboration grant (2026-06-05, forum):

> Granted, even if it wasn't needed. Go nuts! If you need, you can contact my same @ tag on discord. All sourcecode is avail on my Github, reference and steal as you need.

Source: github.com/LenKagamine — referenced and adapted under his open grant.

## Assimilation log
- **2026-06-06** — runescapemetals 0.3.0: full Rune-Story integration for high-tier metals. All 15 patch ops gated on `runestory` mod being loaded.
  - **runicpickaxe variants** (`runestory_runicpickaxe_assimilate.json`, 7 ops): added mithril, adamantite, runite, dragon to the runic pickaxe variantgroup; `tooltierByType` 5→8, `miningspeedByType` 22→36, `texturesByType` resolving ingot art from the runescape domain.
  - **rune altar recipes** (`runestory_runealtar_recipes.json`, 4 ops): added altar transmutation entries `game:pickaxe-{metal}` + `runestory:rune-blank` → `runestory:runicpickaxe-{metal}` for all four metals, matching Len Kagamine's existing pattern.
  - **lang** (`runestory_lang.json`, 4 ops): English item-name entries for the four new variants.
  - **Decompile findings**: runestory.dll ships zero active Harmony patches (string-table reference only). No risk of compat collision with vanilla forge/firepit/smelting; runescapemetals multiblock furnaces are untouched. Rune-drop chance for our metals falls through `RunePickaxe.OnBlockBrokenWith`'s default branch at 2.5% — acceptable for first pass; a small Harmony postfix could raise this to a proper tier-8 curve in a future revision.
