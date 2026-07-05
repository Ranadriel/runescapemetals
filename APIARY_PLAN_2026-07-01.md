# Wooden Skep Apiary — design plan (2026-07-01)

RHO's directive: research RS beekeeping (beekeeper, "yanille" apiary), then
plan a built wooden skep that does NOT destroy on hive collection — permanent
placement object; recipe gated on iron-or-above nails-and-strips + 3
honeycombs; propagated by reed skep transplant.

Research inputs: RS lore brief (wiki-sourced) + VS 1.22.3 mechanics brief
(decompiled BlockSkep / BlockEntityBeehive / wild hive). Both 2026-07-01.

---

## 1. The lore ground

- **Two RS beekeepers**: the 2006 random-event Beekeeper (the drag-the-parts
  hive assembly puzzle, six attempts, "I'm covered in Bees!" on failure) and
  the Catherby apiary keeper ("He loves bees!", farewell pun *"'Bee' good."*).
- **The "yanille" memory** resolves to the RS3 Manor Farm apiary — the
  seven-hive colony cluster north of East Ardougne (the region above
  Yanille). Nearest true "apiary colony" in RS. Mechanics there: feed
  flowers, honeycombs mature on a timer, repellent required to collect.
- **The load-bearing precedent — Catherby (OSRS, 2002)**: beehives are
  PERMANENT world scenery, harvested forever in place (repellent or
  beekeeper's outfit → bucket → wax; bees return between harvests; careless
  harvest = a sting). RS never destroys these hives.
  **Our wooden skep = the Catherby model in VS.** RS has no player-buildable
  free-standing hive — this block is our own invention on RS's foundation.

## 2. The vanilla-VS ground (what we're fixing)

- Vanilla skeps: harvest = BREAK the block. Colony destroyed, skep item not
  even returned, 40% angry bee-mob. Wild hives: one payout, gone forever.
- The non-destructive pattern vanilla DOES have: **berry bushes** —
  right-click when ripe → yield → state reset → calendar re-grow timer.
- Populated skep item carries its colony as the block VARIANT
  (`skep-reed-populated-*`, + optional `harvestable` bool). Enough to detect
  a live colony at transplant; no richer payload exists to lose.
- Verified codes: `metalnailsandstrips-{iron,meteoriciron,steel}`,
  `honeycomb`, `plank-{wood}`, skep variants `skep-{reed,papyrus}-{empty,
  populated}-{n/e/s/w}`.

## 3. The block — `runescape:apiary` (wooden skep)

- **Permanent placement object.** Never consumed by any interaction. Break
  (pickaxe/axe deliberate demolition) returns the EMPTY apiary block — the
  colony is lost but the structure survives even that.
- **Three states** (type variant, like vanilla): `empty` → `populated` →
  `harvestable`. Four horizontal orientations (HorizontalOrientable).
- Shape: wooden box-hive (Langstroth-adjacent silhouette — planked body,
  landing lip); distinct at a glance from the vanilla woven reed dome.
  Placeholder v1 shape can derive from a plank-textured skep; art pass later.

## 4. Recipe (RHO's gate, verbatim)

Grid 3×3, output 1× apiary (empty):

```
P P P        P = plank (any wood, game:plank-*)
H H H        H = honeycomb ×3 (game:honeycomb)
P N P        N = nails and strips, iron or above:
                 game:metalnailsandstrips-iron / -meteoriciron / -steel
```

The three honeycombs are the wax-primer that makes the box bee-worthy; the
iron gate keeps it a mid-game structure (matches the mod's tier ladder).

## 5. Propagation — the reed skep transplant

Right-click an EMPTY apiary holding a POPULATED vanilla skep
(reed or papyrus):
- apiary → `populated` state, ripening timer starts
- the skep item returned to the player as its EMPTY variant — the colony
  moved house; the woven skep survives to be reused. Non-destructive on
  both ends. (Detection: skep block item with variant `type=populated`.)
- flavor note in chat: *"The colony settles into its new home."*

## 6. Harvest — the Catherby loop, berry-bush machinery

Right-click a HARVESTABLE apiary (empty hand or any time):
- yields honeycomb (avg 3, same as vanilla skep drop)
- state resets to `populated`; calendar timer re-arms
  (~1.5–5.5 game days, mirroring vanilla `harvestableAtTotalHours`)
- block persists. Forever. That is the whole point.
- v1 has NO sting/repellent tax. Future flavor hooks (post-v1, RHO's call):
  bee-mob chance on bare-hand harvest unless holding a torch (smoke) —
  the RS repellent/sting loop; beekeeper's outfit as wearables.

## 7. The one real fork — ecology depth (RHO decides)

| Path | What | Cost/Risk |
|---|---|---|
| **A — own BE, pure timer** | Own BlockEntityApiary: `Harvestable` + calendar timer + temperature gate (~40 lines). No flowers needed, no swarming. | Low / update-proof |
| **A+ — own BE + light flower gate (RECOMMENDED)** | As A, plus a cheap flower count (blocks with `beeFeed:true` in ~8-block radius, checked at timer-arm) scaling ripen speed and/or yield: no flowers → won't ripen; rich meadow → faster/fuller. ~30 extra lines, all ours. | Low-Med / update-proof, keeps beekeeping's soul (bees need flowers) |
| **B — subclass vanilla BlockEntityBeehive** | Inherit the full ecology (flower scan, pop size, swarming out to other skeps). Harvest overridden at Block layer. | Med / tied to vanilla private internals; inherits quirks |

A+ is the recommendation: the apiary stays honest to bees (flowers matter)
without marrying vanilla's private fields. B only if swarming OUT of the
apiary into nearby empty skeps is wanted (a bee farm that seeds itself).

## 8. Names & flavor (lang plan)

- `block-apiary-empty`: **Wooden Skep** — *"A home for your bees."* (RS3)
- `block-apiary-populated`: **Wooden Skep (colonised)** — *"Where bees
  live."* (OSRS)
- `block-apiary-harvestable`: **Wooden Skep (dripping)** — *"The combs hang
  heavy. The keeper would be proud."*
- handbook: the Catherby story in one paragraph — permanent hives, calm
  harvests, *"'Bee' good."* as the closing line.

## 9. Build estimate (when RHO says code)

- blocktype JSON (3 types × 4 sides), grid recipe, lang: bench work
- BlockApiary + BlockEntityApiary (Path A+): ~150 lines total, all house
  classpath rules apply; berry-bush interact pattern; skep-transplant check
- shape: v1 placeholder from plank-retextured box; art pass later
- smoke: throwaway dataPath, per discipline
- agent budget if dispatched: fits ONE ≤30k crew with this plan pre-digested
  into the prompt; or bench-built by my hand like the POV system

---
*Researched and planned 2026-07-01. Lore: RS wikis (Beekeeper, Beehive,
Bee keeper, Insect repellent, POF Beehive, Yanille). Mechanics: decompiled
VS 1.22.3 BlockSkep/BlockEntityBeehive/BlockBeehive. The plan stands ready;
no code cut until the word.*
