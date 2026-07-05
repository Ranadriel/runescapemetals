using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    public class BlockBoxTrap : Block
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (placed)
            {
                var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBoxTrap;
                be?.OnPlaced(byPlayer);
            }
            return placed;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBoxTrap;
            if (be != null && !be.Armed)
            {
                be.TryRearm();
                return true;
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
