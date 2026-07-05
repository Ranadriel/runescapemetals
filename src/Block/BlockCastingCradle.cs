using Vintagestory.API.Common;

namespace RuneScapeForges
{
    public class BlockCastingCradle : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BECastingCradle be)
            {
                return be.OnInteractStart(byPlayer);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BECastingCradle be)
            {
                return be.OnInteractStep(secondsUsed, byPlayer);
            }
            return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BECastingCradle be)
            {
                be.OnInteractStop(secondsUsed, byPlayer);
                return;
            }
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BECastingCradle be)
            {
                be.OnInteractStop(secondsUsed, byPlayer);
                return true;
            }
            return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
        }
    }
}
