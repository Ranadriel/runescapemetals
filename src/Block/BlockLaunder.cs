using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    // Custom Block class for the launder. The shape was originally authored in
    // NEGATIVE Y space (body hanging one full tile below its block position),
    // which caused buried/invisible models, below-ground hitboxes, and needed
    // an overlap placement guard. On 2026-07-01 the model was lifted +16 into
    // its own tile (y 0..12.25) — normal block semantics; the guard is gone.
    // Placement logging retained for diagnostics.
    public class BlockLaunder : Block
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool ok = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            world.Logger.Notification("[runescapemetals] launder place at {0}: ok={1} failureCode={2}",
                blockSel.Position, ok, failureCode ?? "none");
            return ok;
        }
    }
}
