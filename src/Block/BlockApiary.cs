using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace RuneScapeForges
{
    // Wooden skep apiary — the Catherby model: a PERMANENT hive. Harvesting
    // never destroys it; the colony re-matures in place (berry-bush pattern).
    // Seeded by transplanting a populated vanilla skep (reed or papyrus);
    // the woven skep is handed back empty. See APIARY_PLAN_2026-07-01.md.
    public class BlockApiary : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            string type = Variant["type"];

            if (type == "empty")
            {
                return TryTransplant(world, byPlayer, blockSel);
            }

            if (type == "harvestable")
            {
                Harvest(world, byPlayer, blockSel);
                return true;
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        // Empty apiary + populated vanilla skep in hand -> colony moves house.
        private bool TryTransplant(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            if (slot == null || slot.Empty) return false;

            Block held = slot.Itemstack.Block;
            bool isPopulatedSkep = held != null
                && held.Code != null
                && held.Code.Path.StartsWith("skep-")
                && held.Variant["type"] == "populated";
            if (!isPopulatedSkep) return false;

            if (world.Side == EnumAppSide.Server)
            {
                // The colony settles in; the woven skep survives, emptied.
                Block populated = world.GetBlock(CodeWithVariant("type", "populated"));
                world.BlockAccessor.SetBlock(populated.Id, blockSel.Position);

                Block emptySkep = world.GetBlock(held.CodeWithVariant("type", "empty"));
                slot.TakeOut(1);
                slot.MarkDirty();
                if (emptySkep != null)
                {
                    ItemStack give = new ItemStack(emptySkep, 1);
                    if (!byPlayer.InventoryManager.TryGiveItemstack(give))
                    {
                        world.SpawnItemEntity(give, blockSel.Position.ToVec3d().Add(0.5, 1.0, 0.5), null);
                    }
                }

                IServerPlayer sp = byPlayer as IServerPlayer;
                sp?.SendMessage(Vintagestory.API.Config.GlobalConstants.InfoLogChatGroup,
                    "The colony settles into its new home.", EnumChatType.Notification, null);
            }
            return true;
        }

        // Harvest in place: honeycomb out, colony persists, timer re-arms.
        private void Harvest(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side != EnumAppSide.Server) return;

            int combs = 2 + world.Rand.Next(3); // 2-4, avg 3 (vanilla skep yield)
            Item honeycomb = world.GetItem(new AssetLocation("game", "honeycomb"));
            if (honeycomb != null)
            {
                ItemStack give = new ItemStack(honeycomb, combs);
                if (byPlayer == null || !byPlayer.InventoryManager.TryGiveItemstack(give))
                {
                    world.SpawnItemEntity(give, blockSel.Position.ToVec3d().Add(0.5, 1.0, 0.5), null);
                }
            }

            // Back to populated; the fresh BE re-arms the ripening timer.
            Block populated = world.GetBlock(CodeWithVariant("type", "populated"));
            world.BlockAccessor.SetBlock(populated.Id, blockSel.Position);
        }
    }
}
