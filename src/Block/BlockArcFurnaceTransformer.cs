using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    // Tier 3 power-input block. v0 = visual-only; mechanical-network integration with
    // millwright will be added later. Horizontally orientable so its rear face
    // (axle socket) can be aimed away from the chamber.
    public class BlockArcFurnaceTransformer : Block
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            BlockFacing facing = SuggestedHVOrientation(byPlayer, blockSel)[0].Opposite;
            var oriented = world.GetBlock(CodeWithVariant("side", facing.Code));
            if (oriented == null)
            {
                failureCode = "noorientation";
                return false;
            }
            if (!world.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(this))
            {
                failureCode = "occupied";
                return false;
            }
            world.BlockAccessor.SetBlock(oriented.BlockId, blockSel.Position);
            return true;
        }
    }
}
