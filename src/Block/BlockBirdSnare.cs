using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    public class BlockBirdSnare : Block
    {
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (placed)
            {
                var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBirdSnare;
                be?.OnPlaced(byPlayer);
            }
            return placed;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBirdSnare;
            if (be != null && !be.Armed)
            {
                be.TryRearm();
                return true;
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
