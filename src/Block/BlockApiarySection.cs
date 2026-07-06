using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace RuneScapeForges
{
    // Craftable third of the apiary column (variant part = base|body|roof).
    // Sections place like normal blocks anywhere; the moment a complete
    // base/body/roof stack exists the column self-assembles into the real
    // apiary blocks (BlockApiary controller at the bottom + 2 BlockApiaryPart
    // satellites), entrance facing the placer. See APIARY_V2_DESIGN §Assembly.
    public class BlockApiarySection : Block
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (placed && world.Side == EnumAppSide.Server)
            {
                CheckAssembly(world, blockSel.Position, byPlayer);
            }
            return placed;
        }

        // The just-placed section can be any of the three slots, so the
        // candidate column bottom is one of {pos, pos-1, pos-2}.
        void CheckAssembly(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            for (int down = 0; down <= 2; down++)
            {
                BlockPos b = pos.DownCopy(down);
                if (IsSection(world, b, "base")
                    && IsSection(world, b.UpCopy(1), "body")
                    && IsSection(world, b.UpCopy(2), "roof"))
                {
                    Assemble(world, b, byPlayer);
                    return;
                }
            }
        }

        static bool IsSection(IWorldAccessor world, BlockPos pos, string part)
        {
            Block b = world.BlockAccessor.GetBlock(pos);
            return b is BlockApiarySection && b.Variant["part"] == part;
        }

        static void Assemble(IWorldAccessor world, BlockPos basePos, IPlayer byPlayer)
        {
            // Entrance toward the player: the horizontal facing OPPOSITE the
            // placer's yaw. HorizontalFromYaw, not HorizontalFromAngle — the
            // latter is 0=east and lands one quadrant off for entity yaw.
            float yaw = byPlayer?.Entity == null ? 0f : byPlayer.Entity.Pos.Yaw;
            string side = BlockFacing.HorizontalFromYaw(yaw).Opposite.Code;

            Block bottom = world.GetBlock(new AssetLocation("runescape", "apiary-empty-" + side));
            Block body = world.GetBlock(new AssetLocation("runescape", "apiarypart-body-empty-" + side));
            Block roof = world.GetBlock(new AssetLocation("runescape", "apiarypart-roof-empty-" + side));
            // All three or nothing — a partial swap would strand section items.
            if (bottom == null || body == null || roof == null) return;

            world.BlockAccessor.SetBlock(bottom.BlockId, basePos);
            world.BlockAccessor.SetBlock(body.BlockId, basePos.UpCopy(1));
            world.BlockAccessor.SetBlock(roof.BlockId, basePos.UpCopy(2));

            (byPlayer as IServerPlayer)?.SendMessage(GlobalConstants.InfoLogChatGroup,
                "The hive stands ready. 'Bee' good.", EnumChatType.Notification, null);
        }
    }
}
