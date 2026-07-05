using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    public class BlockBlastFurnaceController : Block
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            // Place oriented so the controller's "front face" points at the player.
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
