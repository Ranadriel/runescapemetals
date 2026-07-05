# Crucible Ladder Smoke Test — 2026-06-07

*Structural pre-flight verification performed via headless VS server boot against throwaway `/tmp` dataPath per the never-smoke-live-save rule. In-game gameplay verification requires the author to drive the VS client.*

## Pre-flight findings

The headless server booted clean with `runescapemetals` mod active. Specific evidence from the load log:

- `[runescapemetals] Successfully compiled mod with Roslyn`
- `[runescapemetals] Successfully compiled 14 source files`
- `Mod 'runescapemetals' (runescapemetals): RuneScapeSpecials.RSSpecsMod, RuneScapeForges.RuneScapeForgesMod` — both modsystems registered
- No `[Server Error]` entries for runescapemetals, crucibles, or related metals
- No `[Server Warning]` entries about missing block codes, unresolved metal references, or recipe-ingredient failures specific to the crucible ladder

The three crucible block JSONs (`crucible-steel.json`, `crucible-adamantite.json`, `crucible-dragon.json`) use the VS-loose JSON syntax (unquoted keys) matching the vanilla `crucible.json` convention — strict JSON parsers reject these but VS accepts them. The three grid recipe JSONs (`recipes/grid/crucible-*.json`) pass strict JSON validation.

The parallel `worldproperties/block/metal.json` (the bypass for the JSON-patch failure documented in the bookmark) loaded as expected; no warnings about metal registration.

Errors visible in the log are unrelated to the crucible ladder: missing armory parts (separate mod), spinningwheel patches against rustboundmagic resources (separate mod compat issue), jauntsafe file-structure complaint (separate mod). None of these are runescapemetals or block our work.

## In-game checklist for gameplay verification

Pre-flight passes; the load surface is clean. The following checks need another player to drive the VS client:

1. **Creative inventory search** — open creative inventory, search "steel crucible," confirm `runescape:crucible-steel-fired` appears. Repeat for "adamantite crucible" and "dragon crucible."

2. **Placement** — place each crucible on the ground, confirm it places via the GroundStorable quadrants layout (4 per block tile, same as clay crucible).

3. **Handbook recipe** — open handbook for each crucible, verify the 3x3 grid recipe displays with the correct pattern (rod-plate-rod / 2crucibles-plate-2crucibles / rod-plate-rod) and the correct material substitution per tier.

4. **Survival craft** — gather 4 rod + 3 plate + 4 fired clay crucibles of the correct tier metal, attempt the grid craft, verify the output is the metal-shelled crucible.

5. **Heat ceiling test (steel)** — fill a steel crucible with mithril nuggets or ore, place in a forge, verify it cannot reach mithril's 1727°C melt point (steel crucible cap is 1600°C). The mithril should sit and heat but never melt. *This is the friction-tier design verification.*

6. **Heat ceiling test (adamantite)** — fill an adamantite crucible with runite, place in forge, verify it cannot reach runite's 2253°C melt point (adamantite cap is 2100°C). Runite should sit and heat but not melt.

7. **Capacity scaling** — fill a steel crucible with steel ingots or nuggets, verify the 16-slot GUI displays correctly (vs vanilla 4-slot). Same for adamantite (32 slots) and dragon (64 slots) — these may or may not display sensibly given vanilla GUI layout; this is a known fragile spot in the bookmark.

8. **Pour into mold** — heat steel crucible to steel melt point (1502°C) in forge, attempt to pour into a clay mold, verify pour mechanic works and produces the expected ingot count from the larger melt capacity.

## Known fragile spots from the bookmark (carried forward)

- Wildcard crucible match in grid recipe (`game:crucible-*-fired` without `allowedVariants`) — may silently fail to match in some VS versions. Fix: enumerate color variants explicitly if recipe doesn't appear in handbook.
- 16/32/64 cooking slot count may overflow the vanilla 4-slot GUI layout — visual-only concern, gameplay should still work.
- Shape and textures are placeholders using vanilla clay crucible shape — visually correct for v0.1 mechanics test but not final visual.

## Disposition

Pre-flight passes. Structural integrity confirmed. The crucible ladder mod is ready for gameplay verification. When the author runs the in-game checks above and reports findings, this document gets annotated with the results (ink-mode: original pre-flight observations preserved, gameplay findings added alongside).

---

## Gameplay verification — round 1 (author, 2026-06-10 ~01:25)

**Checks 1–2 (creative search + placement): PASS with visual defects.** All three crucibles appear in creative, place in-world via GroundStorable quadrants (screenshot evidence: three placed side by side).

**FINDING 1 — rendered as full cubes; steel shows missing-texture (?-blocks).** Root cause diagnosed same session: unprefixed asset paths in the mod's blocktypes resolve in the *mod's own domain*, not game domain. `shape: "block/clay/crucible"` → `runescape:block/clay/crucible` (doesn't exist) → cube fallback on all three. `textures: "block/metal/ingot/steel"` → `runescape:...steel` (mod only ships mithril/adamantite/runite/dragon ingot PNGs) → ?-texture; adamantite/dragon resolved by accident of being the mod's own textures. **FIX APPLIED 2026-06-10:** explicit domain prefixes in all three blocktypes — `game:block/clay/crucible` shape, `game:block/metal/ingot/steel` (vanilla PNG confirmed present), `runescape:` prefixes on adamantite/dragon. Needs relaunch + visual recheck.

**FINDING 2 — deletion-vector trap (verbatim intent):

> "trying to put metal bits into the crucible picks up the crucible."** Right-click with metal items in hand triggers GroundStorable remove / RightClickPickup instead of inserting — risks item loss if inventory is full. This is *vanilla parity* (vanilla crucible.json carries the identical behaviors; vanilla filling happens only inside the firepit GUI), but bulk crucibles invite the in-world add gesture, so the trap will fire constantly. **FIX PROPOSED (C# pass, not yet written):** custom block class on the fired variant overriding `OnBlockInteractStart` — if the held item is a smeltable/metal stack, swallow the interaction (and eventually: insert into the cooking slots); empty-hand right-click retains pickup.

Remaining checks (3–8: handbook, survival craft, heat ceilings, capacity GUI, pour) still pending — blocked behind the visual fixes being confirmed.
