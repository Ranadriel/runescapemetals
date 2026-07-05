using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    // Lightweight part-block entity. On neighbour change, walks a small radius
    // looking for any controller and re-pings its validation. Cheap.
    public class BEMultiblockPart : BlockEntity
    {
        private const int SearchRadius = 5;

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            PingNearbyControllers();
        }

        public void PingNearbyControllers()
        {
            if (Api?.Side != EnumAppSide.Server) return;
            for (int dx = -SearchRadius; dx <= SearchRadius; dx++)
            {
                for (int dy = -SearchRadius; dy <= SearchRadius; dy++)
                {
                    for (int dz = -SearchRadius; dz <= SearchRadius; dz++)
                    {
                        var p = Pos.AddCopy(dx, dy, dz);
                        var be = Api.World.BlockAccessor.GetBlockEntity(p) as BEMultiblockControllerBase;
                        if (be != null)
                        {
                            be.RevalidateStructure();
                        }
                    }
                }
            }
        }
    }
}
