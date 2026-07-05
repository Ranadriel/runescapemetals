using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace RuneScapeForges
{
    public class BEArcFurnaceController : BEMultiblockControllerBase
    {
        protected override string StructureDisplayName() => "Arc Furnace";

        private static readonly Predicate<Block> IsT3Wall = b =>
        {
            var c = b?.Code?.ToString();
            if (c == null) return false;
            return c.StartsWith("game:refractorybricks-good-tier3") ||
                   c.StartsWith("game:refractorybricks-damaged-tier3");
        };

        private static readonly Predicate<Block> IsTransformer = b =>
            b?.Code?.Domain == "runescape" &&
            (b?.Code?.Path?.StartsWith("af_transformer") ?? false);

        private static readonly Predicate<Block> IsElectrode = b =>
            b?.Code?.Domain == "runescape" &&
            (b?.Code?.Path?.StartsWith("af_electrode") ?? false);

        private static readonly Predicate<Block> IsLid = b =>
            b?.Code?.Domain == "runescape" &&
            (b?.Code?.Path?.StartsWith("af_lid") ?? false);

        private static readonly Predicate<Block> IsHatch = b =>
            b?.Code?.Domain == "runescape" &&
            (b?.Code?.Path?.StartsWith("af_charge_hatch") ?? false);

        // Layout — controller at (0,0,0), facing NORTH (out front of structure).
        // +Z = INTO structure. Centered footprint: x ∈ {-2,...,+2}, z ∈ {0,...,4}.
        // Y stack: Y0 foundation = y=-1; Y1 chamber base = y=0 (controller's level);
        // Y2 chamber mid = y=+1; Y3 chamber upper = y=+2; Y4 lid = y=+3.
        private static readonly MultiblockSlot[] Layout = BuildLayout();

        private static MultiblockSlot[] BuildLayout()
        {
            var slots = new List<MultiblockSlot>();

            // Y0 foundation (y=-1): full 5×5 of refractory tier-3
            for (int dx = -2; dx <= 2; dx++)
                for (int dz = 0; dz <= 4; dz++)
                    slots.Add(new MultiblockSlot(dx, -1, dz, IsT3Wall, "foundation"));

            // Y1 (y=0): outer ring only; controller's cell (0,0,0) skipped
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = 0; dz <= 4; dz++)
                {
                    bool isPerim = (dx == -2 || dx == 2 || dz == 0 || dz == 4);
                    if (!isPerim) continue;
                    if (dx == 0 && dz == 0) continue;
                    slots.Add(new MultiblockSlot(dx, 0, dz, IsT3Wall, "wall"));
                }
            }

            // Y2 (y=+1): outer ring + transformer at back-center + 3 electrodes (lower halves)
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = 0; dz <= 4; dz++)
                {
                    bool isPerim = (dx == -2 || dx == 2 || dz == 0 || dz == 4);
                    if (!isPerim) continue;
                    if (dx == 0 && dz == 4)
                        slots.Add(new MultiblockSlot(dx, 1, dz, IsTransformer, "transformer"));
                    else
                        slots.Add(new MultiblockSlot(dx, 1, dz, IsT3Wall, "wall"));
                }
            }
            slots.Add(new MultiblockSlot(-1, 1, 1, IsElectrode, "electrode"));
            slots.Add(new MultiblockSlot(+1, 1, 1, IsElectrode, "electrode"));
            slots.Add(new MultiblockSlot( 0, 1, 3, IsElectrode, "electrode"));

            // Y3 (y=+2): outer ring + 3 electrodes (upper halves)
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = 0; dz <= 4; dz++)
                {
                    bool isPerim = (dx == -2 || dx == 2 || dz == 0 || dz == 4);
                    if (!isPerim) continue;
                    slots.Add(new MultiblockSlot(dx, 2, dz, IsT3Wall, "wall"));
                }
            }
            slots.Add(new MultiblockSlot(-1, 2, 1, IsElectrode, "electrode"));
            slots.Add(new MultiblockSlot(+1, 2, 1, IsElectrode, "electrode"));
            slots.Add(new MultiblockSlot( 0, 2, 3, IsElectrode, "electrode"));

            // Y4 lid plane (y=+3): perimeter walls + inner 3×3 = 3 electrode tops + 5 lid + 1 hatch
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = 0; dz <= 4; dz++)
                {
                    bool isInner3 = (dx >= -1 && dx <= 1) && (dz >= 1 && dz <= 3);
                    if (isInner3) continue;
                    slots.Add(new MultiblockSlot(dx, 3, dz, IsT3Wall, "wall"));
                }
            }
            // Inner 3×3 lid plane (controller-relative x,z)
            slots.Add(new MultiblockSlot(-1, 3, 1, IsElectrode, "electrode")); // front-left electrode top
            slots.Add(new MultiblockSlot( 0, 3, 1, IsLid,        "lid"));
            slots.Add(new MultiblockSlot(+1, 3, 1, IsElectrode, "electrode")); // front-right electrode top
            slots.Add(new MultiblockSlot(-1, 3, 2, IsLid,        "lid"));
            slots.Add(new MultiblockSlot( 0, 3, 2, IsHatch,      "hatch"));
            slots.Add(new MultiblockSlot(+1, 3, 2, IsLid,        "lid"));
            slots.Add(new MultiblockSlot(-1, 3, 3, IsLid,        "lid"));
            slots.Add(new MultiblockSlot( 0, 3, 3, IsElectrode, "electrode")); // back-center electrode top
            slots.Add(new MultiblockSlot(+1, 3, 3, IsLid,        "lid"));

            return slots.ToArray();
        }

        protected override MultiblockSlot[] GetStructureLayout() => Layout;
    }
}
