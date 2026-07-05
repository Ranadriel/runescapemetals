using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    public class BlockMultiblockPart : Block
    {
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
            (world.BlockAccessor.GetBlockEntity(pos) as BEMultiblockPart)?.PingNearbyControllers();
        }
    }
}
