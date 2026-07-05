# DEBUG — adamant ingot won't place on adamant anvil at work temp

reported 2026-06-11, parked pending in-game readings. do NOT guess-patch before the
three readings below are taken.

## what the decompile established (own-eyes, VSSurvivalMod.dll via ilspycmd)

- placement chain: `BlockEntityAnvil.TryPut` → ingot's `GetRequiredAnvilTier` vs
  `BlockAnvil.MetalTier` → `ItemIngot.TryPlaceOn` converts ingot to `workitem-{metal}`.
- `GetRequiredAnvilTier`: vanilla default = metal tier − 1; our `ingot_balance.json`
  overrides adamantite to **6** (so adamant needs an adamant-tier anvil, not mithril).
- `BlockAnvil.MetalTier`: from `SurvivalCoreSystem.metalsByCode` — built with
  `GetMany("worldproperties/block/metal.json", domain:null)` → **reads ALL domains**,
  so our parallel `runescape:worldproperties/block/metal.json` (adamantite tier 6)
  SHOULD give the anvil tier 6.
- **`ItemIngot.TryPlaceOn` fails SILENTLY (returns null, no error) if
  `game:workitem-adamantite` does not exist.** Matches the symptom if no error toast.
- variant expansion (`loadFromProperties: "block/metal"` in vanilla ingot.json /
  workitem.json): `ModRegistryObjectTypeLoader.GetWorldPropertyByCode` is an EXACT
  domain-keyed lookup → resolves **game: domain only** → the parallel runescape
  metal.json does NOT add variants to vanilla items. The old direct patch that did
  (`metal_register.json.disabled-superseded-by-parallel-metaljson`, addmerge into
  game:worldproperties/block/metal.json) is disabled.
- UNRESOLVED CONTRADICTION: by that reading, `ingot-adamantite` itself shouldn't
  exist either — yet RHO holds one in-game and logs show zero resolve errors.
  Something in the chain is mis-read; hence: instrument, don't patch.
- no patch in the mod adds workitem states; no other mod (runestory checked) registers
  these metals.

## the three in-game readings (10 seconds each)

1. hover the adamant anvil → block info prints "Tier N anvil". Record N.
   (N=6 → tier chain fine. N=0/missing → metalsByCode lookup failed.)
2. click hot adamant ingot on anvil → top-right toast?
   - "Working this metal needs a tier 6 anvil" → tier path broken.
   - nothing at all → TryPlaceOn returned null → workitem missing.
3. `/giveitem workitem-adamantite` → exists or not. Also try `/giveitem ingot-adamantite`
   to learn how ingot got registered (settles the contradiction).

## likely fix (IF reading 2 = silence and reading 3 = no such item)

Mirror `anvil_register.json` pattern — new patch `workitem_register.json`:
- `game:itemtypes/resource/workitem.json` op add `/variantgroups/0/states/-` for
  mithril / adamantite / runite / dragon (vanilla group already has explicit states
  list alongside loadFromProperties — appendable, same as anvil.json was).
- add `/combustiblePropsByType/workitem-{metal}` for each (meltingPoint per
  worldproperty, meltingDuration ~30, smeltedRatio 1, smeltedStack ingot-{metal})
  so abandoned work items re-smelt like vanilla.
- texture base `block/metal/ingot/{metal}` resolves automatically — our ingot
  textures already exist in game-domain texture overlay.
- then ALSO re-check how ingot-adamantite exists (reading 3) — if ingots came from a
  mechanism that bypasses variant expansion, helvehammer/plate paths may hide the
  same silent-null class of bug one tier later.

---

## RESOLVED 2026-06-11 — not a bug: temperature gate + unit mismatch

Full gate chain decompiled and verified coherent on disk (BlockEntityAnvil 1.22):
plain right-click = TryTake (silent no-op); **sneak+right-click** = TryPut →
tier gate (adamant anvil Tier 6 vs ingot requiresAnvilTier 6 — passes, toast if not)
→ ItemIngot.TryPlaceOn → CanWork: temp ≥ meltingPoint/2 — **silent** fail → workitem
exists → places. No mod touches this flow (DLL string-scan: only chiseltools +
AttributeRenderingLibrary reference BlockEntityAnvil, neither alters placement).

Root cause: RHO's ingot was ~1700°F ≈ 927°C; adamantite (meltPoint 1953°C) is
workable from **976.5°C ≈ 1790°F**. Steel works from 751°C ≈ 1384°F, so a forge
heat that handles steel comfortably sits *below* adamantite's silent gate.
FreedomUnits displays °F; all engine thresholds are °C.

Working-temp ladder (°C / °F): mithril 863.5/1586 · adamantite 976.5/1790 ·
runite 1126.5/2060 · dragon 1314.5/2398. The tier-2 blast / tier-3 arc furnaces
are the intended infrastructure for the upper rungs.

Side finding, also closed: the "Requires 2147483647x workitem-adamantite" display
was the creative-given workitem probe — no voxel-data attribute → VoxelCountForHandbook
0 → division by zero → saturating (int) cast = int.MaxValue. Honest math, broken input.
Recover an eaten workitem with plain (no-sneak) right-click = the take gesture.
