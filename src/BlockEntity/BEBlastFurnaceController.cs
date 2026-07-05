using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace RuneScapeForges
{
    public class BEBlastFurnaceController : BEMultiblockControllerBase
    {
        protected override string StructureDisplayName() => "Blast Furnace";

        // Predicates for slot matching
        private static readonly Predicate<Block> IsT2Wall = b =>
        {
            var c = b?.Code?.ToString();
            if (c == null) return false;
            return c.StartsWith("game:refractorybricks-good-tier2") ||
                   c.StartsWith("game:refractorybricks-damaged-tier2");
        };

        private static readonly Predicate<Block> IsTuyere = b =>
            b?.Code?.Domain == "runescape" &&
            (b?.Code?.Path?.StartsWith("bf_tuyere") ?? false);

        private static readonly Predicate<Block> IsChimney = b =>
            b?.Code?.Domain == "runescape" &&
            (b?.Code?.Path?.StartsWith("bf_chimney") ?? false);

        // Layout — controller at (0,0,0), facing NORTH (out of structure).
        // +Z is INTO the structure (away from controller). +X is right. +Y is up.
        // Footprint 3×3 (x ∈ {-1,0,+1}, z ∈ {0,1,2}). Y stack 0..3.
        private static readonly MultiblockSlot[] Layout = BuildLayout();

        private static MultiblockSlot[] BuildLayout()
        {
            var slots = new List<MultiblockSlot>();

            // Y0..Y2: 3×3 perimeter walls + 1 air center per level. Y0 also skips controller's own cell.
            for (int y = 0; y <= 2; y++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        if (dx == 0 && dz == 1) continue; // air center (hearth/shaft)
                        if (y == 0 && dx == 0 && dz == 0) continue; // controller's own cell
                        if (y == 1 && dx == 0 && dz == 2)
                        {
                            slots.Add(new MultiblockSlot(dx, y, dz, IsTuyere, "tuyere"));
                        }
                        else
                        {
                            slots.Add(new MultiblockSlot(dx, y, dz, IsT2Wall, "wall"));
                        }
                    }
                }
            }

            // Y3: chimney at center (above hearth column)
            slots.Add(new MultiblockSlot(0, 3, 1, IsChimney, "chimney"));

            return slots.ToArray();
        }

        protected override MultiblockSlot[] GetStructureLayout() => Layout;
    }
}
