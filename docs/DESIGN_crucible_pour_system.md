# Design — Bulk Crucible & Launder Pour System

*Authored 2026-06-10, design session with Ranadriel. This is the canonical spec for the
metal-crucible tier system. Supersedes the docked-pot concept (4 clay crucibles socketed
on the machine frame) — that idea is retired; pots are now the OUTPUT of the system,
not the input inventory.*

## The machine (per metal tier: steel / adamantite / dragon)

- **2 blocks tall**, single-tile footprint. Real-world ancestor: hand-tipped foundry
  crucible rigs (small-cement-mixer scale, trunnion-mounted, lever-tipped — the
  third-world metal-recycling yard machines).
- **Bottom block = firebox.** The fuel gut. Self-contained rig — fire under the drum,
  fueled like a forge. NOT a stand over a vanilla heat source.
- **Top block = drum** on a trunnion axle between two frame posts, pour lip facing
  north, tipping lever on the east axle end.
- The drum's cooking slots (steel 16 / adamantite 32 / dragon 64) are the machine's
  own gut. Load it, heat it, melt in bulk.
- **Tip to pour**: hold right-click → drum rotates ~110° around the trunnion
  (fruitpress animation model: `onActivityStopped: Rewind`, `onAnimationEnd: Hold`).
  Release early = drum swings back. Melt exits the lip only past a threshold angle.

## The pour line

1. **4 clay crucibles ground-stored on one tile** in front of the lip — vanilla
   quadrants layout, zero new code for the pots.
2. **The launder** (foundry term: U-channel that carries molten metal — the word
   Ranadriel was reaching for). A new placeable block: U-trough on heavy metal
   brackets, sits OVER the pot tile, spout holes aligned above the 4 crucible mouths.
3. **Bracket heat gate: iron-tier metal or above required** to craft launder brackets.
   Wood/copper cannot survive the pour heat. The recipe enforces the metallurgy.

## REVISION 2026-06-10 (later) — the sawtooth launder

*Supersedes the flat spout-grid launder below, the same way the docked pots were
retired. Original text preserved in ink.*

Ranadriel's drawing fused the tsunami-defense principle (staged energy sapping —
porous breakwaters, baffle fields, stepped revetments) with the fill problem.
The launder is now **serpentine in plan AND sawtooth in elevation**:

- The channel snakes over the 4-pot tile: enter south over the west column →
  SW valley → cross east → SE valley → run north → NE valley → cross west →
  NW valley (terminal). Each stage's floor is a step LOWER (1.5px per stage).
- Each valley has a drop hole over one pot mouth. Melt pools in the valley,
  drains into the pot; only when that valley is satisfied does it crest the
  weir and descend to the next stage.
- **The wiggle worm stops being code.** Fill order is no longer an algorithm —
  the channel's geometry IS the sequencer, like the first sand mold filling
  first in the yard videos. Gravity does the bookkeeping.
- Real-foundry parity: actual launders use dams/weirs to slow melt and trap
  dross. Same physics, same shape.
- Spectacle is a design goal: the pour visibly walks the switchbacks and pots
  top off one at a time. ("the people like visuals yes? :3")

Tier chaining across multiple tiles (adamantite 2 tiles, dragon 4) needs a
height answer — each subsequent tile's launder must continue LOWER than the
previous exit. Open design question for the C# pass (stepped terrain, lower
variant, or per-block descent reset).

## Fill rule — the wiggle worm (serpentine / square wave) [SUPERSEDED, kept in ink]

NOT strict left-right pairs. NOT equal or simultaneous fill. The melt walks the pots
in a serpentine path where **every overflow step spills into a physically adjacent
pot, preferring forward motion**:

```
 machine
   ↓ pour
  1 → 2     near tile
       ↓
  4 ← 3
  ↓
  5 → 6     next tile (adamantite+)
       ↓
  8 ← 7
```

The order is NOT a hardcoded list. One rule generates it for any launder length:
*current pot full → spill to the unfilled neighbor, preferring forward motion.*
Steel/adamantite/dragon share the same logic; longer launder, same worm.
Ranadriel's verdict: "fluidly repeatable."

## Tier scaling

| Tier       | Drum slots | Pots' worth of melt | Launder length      |
|------------|------------|---------------------|---------------------|
| Steel      | 16         | ~4                  | 1 tile (4 pots)     |
| Adamantite | 32         | ~8                  | 2 tiles, chained    |
| Dragon     | 64         | ~16                 | 4 tiles, chained    |

Launder segments chain end-to-end; the overflow cascade keeps walking down the line.
More metal does NOT mean equal fill — pots top off one at a time, in worm order.

## Build order

1. **Shape blockout** (in progress) — 2-tall machine, art-directed by Ranadriel
   in-game. v0 single-tile/1-block was approved in proportion ("this aint a bad
   shape"); v1 extends to 2-tall with firebox.
2. **Multiblock structure** — fruitpress donor pattern (base + top part variants;
   base holds the BE, top delegates interactions down).
3. **Launder block** — shape + blocktype + bracket recipe (iron+ gate).
4. **C# pass** — animatable BE (tip), firebox fuel/heat, bulk smelt, pour raycast to
   launder, serpentine fill of ground-stored crucibles.

## Status

- 2026-06-10: design locked through fill rule. Steel shape v1 (2-tall) being blocked
  out. Adamantite/dragon blocktypes still wear the vanilla pot until the steel form
  is approved.
