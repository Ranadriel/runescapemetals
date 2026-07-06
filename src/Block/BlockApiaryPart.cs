using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    // Body + roof of the assembled apiary column. Pure satellite: all
    // interaction forwards to the BlockApiary controller at the column
    // bottom; breaking any part tears the whole column back down into its
    // three craftable sections. drops: [] in the JSON — Disassemble spawns
    // the section items instead.
    public class BlockApiaryPart : Block
    {
        // Controller sits 1 below the body, 2 below the roof.
        BlockPos ControllerPos(BlockPos pos)
        {
            return Variant["part"] == "body" ? pos.DownCopy(1) : pos.DownCopy(2);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos basePos = ControllerPos(blockSel.Position);
            Block controller = world.BlockAccessor.GetBlock(basePos);
            if (controller is BlockApiary)
            {
                BlockSelection sel = blockSel.Clone();
                sel.Position = basePos;
                return controller.OnBlockInteractStart(world, byPlayer, sel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            BlockPos basePos = ControllerPos(pos);
            bool assembled = world.BlockAccessor.GetBlock(basePos) is BlockApiary;

            // Base clears this part (and drops nothing); the disassembly then
            // handles the rest of the column + the section item spawns.
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            if (assembled)
            {
                BlockApiary.Disassemble(world, basePos, byPlayer);
            }
        }

        // Explosions bypass OnBlockBroken — tear the column down properly so
        // the two other blocks aren't orphaned with empty drops.
        public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, string ignitedByPlayerUid)
        {
            BlockPos basePos = ControllerPos(pos);
            if (world.BlockAccessor.GetBlock(basePos) is BlockApiary)
            {
                BlockApiary.Disassemble(world, basePos);
                return;
            }
            base.OnBlockExploded(world, pos, explosionCenter, blastType, ignitedByPlayerUid);
        }
    }
}
