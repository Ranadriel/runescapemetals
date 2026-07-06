using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace RuneScapeForges
{
    // Wooden apiary — the Catherby model: a PERMANENT hive. Harvesting never
    // destroys it; the colony re-matures in place. v2: this block is the
    // BOTTOM (controller + BE host) of a 1x3 column assembled from sections
    // (see BlockApiarySection); harvest is scoop-gated. Seeded by
    // transplanting a populated vanilla skep; the woven skep is handed back
    // empty. v1 single blocks (no body part above) are grandfathered:
    // interaction identical, break behavior untouched.
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
                return TryHarvest(world, byPlayer, blockSel);
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        // --- state mirroring -----------------------------------------------

        // Swap the controller's type variant AND, when assembled, the body
        // part's type variant to match (the visible honeycomb accent lives on
        // the body). CodeWithVariant preserves the side variant. Transitions
        // touching the "empty" state use SetBlock so the BE is spawned or
        // removed with the block; populated<->harvestable use ExchangeBlock,
        // which swaps the block WITHOUT the BE lifecycle — the timer fields
        // (nextSwarmTotalHours etc.) survive the ripen/harvest cycle.
        public static void SetApiaryType(IWorldAccessor world, BlockPos basePos, string newType)
        {
            Block controller = world.BlockAccessor.GetBlock(basePos);
            if (!(controller is BlockApiary)) return;

            string oldType = controller.Variant["type"];
            if (oldType != newType)
            {
                Block swapped = world.GetBlock(controller.CodeWithVariant("type", newType));
                if (swapped != null)
                {
                    if (oldType == "empty" || newType == "empty")
                    {
                        world.BlockAccessor.SetBlock(swapped.BlockId, basePos);
                    }
                    else
                    {
                        world.BlockAccessor.ExchangeBlock(swapped.BlockId, basePos);
                        world.BlockAccessor.MarkBlockDirty(basePos);
                        world.BlockAccessor.GetBlockEntity(basePos)?.MarkDirty(true);
                    }
                }
            }

            BlockPos abovePos = basePos.UpCopy(1);
            Block above = world.BlockAccessor.GetBlock(abovePos);
            if (above is BlockApiaryPart && above.Variant["part"] == "body" && above.Variant["type"] != newType)
            {
                // Parts carry no BE — a pure visual exchange.
                Block swappedPart = world.GetBlock(above.CodeWithVariant("type", newType));
                if (swappedPart != null)
                {
                    world.BlockAccessor.ExchangeBlock(swappedPart.BlockId, abovePos);
                    world.BlockAccessor.MarkBlockDirty(abovePos);
                }
            }
        }

        // --- disassembly ---------------------------------------------------

        // Tear the column back down into its three craftable sections. Runs
        // on both sides (the client clears blocks for prediction); item
        // spawns + chat are server-only. Re-entry safe: every clear is
        // preceded by an identity check, and a second call finds no
        // controller and returns. No beemob on teardown — the colony is tamed.
        public static void Disassemble(IWorldAccessor world, BlockPos basePos, IPlayer byPlayer = null)
        {
            Block controller = world.BlockAccessor.GetBlock(basePos);
            if (!(controller is BlockApiary)) return;
            string type = controller.Variant["type"];

            world.BlockAccessor.SetBlock(0, basePos);
            BlockPos p = basePos.UpCopy(1);
            if (world.BlockAccessor.GetBlock(p) is BlockApiaryPart) world.BlockAccessor.SetBlock(0, p);
            p = basePos.UpCopy(2);
            if (world.BlockAccessor.GetBlock(p) is BlockApiaryPart) world.BlockAccessor.SetBlock(0, p);

            if (world.Side != EnumAppSide.Server) return;

            // Creative demolition yields nothing — same contract as vanilla
            // SpawnDropsAndRemoveBlock, else creative sections dupe into
            // survival-legal items.
            bool creative = byPlayer?.WorldData?.CurrentGameMode == EnumGameMode.Creative;
            string[] parts = new string[] { "base", "body", "roof" };
            foreach (string part in parts)
            {
                if (creative) break;
                Block section = world.GetBlock(new AssetLocation("runescape", "apiarysection-" + part));
                if (section != null)
                {
                    world.SpawnItemEntity(new ItemStack(section, 1), basePos.ToVec3d().Add(0.5, 0.5, 0.5), null);
                }
            }
            world.PlaySoundAt(new AssetLocation("sounds/block/planks"), basePos.X + 0.5, basePos.Y + 0.5, basePos.Z + 0.5, null);

            if (type != "empty")
            {
                IPlayer[] players = world.GetPlayersAround(basePos.ToVec3d(), 16f, 16f);
                foreach (IPlayer nearby in players)
                {
                    (nearby as IServerPlayer)?.SendMessage(GlobalConstants.InfoLogChatGroup,
                        "The colony scatters into the wind.", EnumChatType.Notification, null);
                }
            }
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            // A body part directly above marks an assembled v2 column.
            Block above = world.BlockAccessor.GetBlock(pos.UpCopy(1));
            bool assembled = above is BlockApiaryPart && above.Variant["part"] == "body";
            if (!assembled)
            {
                // v1 legacy single block: existing behavior untouched
                // (JSON drops apiary-empty-east).
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                return;
            }
            Disassemble(world, pos, byPlayer);
        }

        // Explosions bypass OnBlockBroken (the engine calls OnBlockExploded +
        // JSON drops instead) — route assembled columns through the same
        // teardown so no orphaned parts and no off-economy legacy item.
        public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, string ignitedByPlayerUid)
        {
            Block above = world.BlockAccessor.GetBlock(pos.UpCopy(1));
            bool assembled = above is BlockApiaryPart && above.Variant["part"] == "body";
            if (assembled)
            {
                Disassemble(world, pos);
                return;
            }
            base.OnBlockExploded(world, pos, explosionCenter, blastType, ignitedByPlayerUid);
        }

        // --- transplant ------------------------------------------------------

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
                // SetApiaryType also mirrors the body part when assembled.
                SetApiaryType(world, blockSel.Position, "populated");

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
                sp?.SendMessage(GlobalConstants.InfoLogChatGroup,
                    "The colony settles into its new home.", EnumChatType.Notification, null);
            }
            return true;
        }

        // --- harvest ---------------------------------------------------------

        // Scoop-gated harvest in place: 3-6 honeycomb + 1 beeswax, colony
        // persists, timer re-arms. Bare hands (or any other tool) refuse —
        // but the colony is tamed, so no beemob either way.
        private bool TryHarvest(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            bool hasScoop = slot != null && !slot.Empty
                && slot.Itemstack.Collectible?.Code?.Path == "apiaryscoop";

            if (!hasScoop)
            {
                if (world.Side == EnumAppSide.Server)
                {
                    (byPlayer as IServerPlayer)?.SendMessage(GlobalConstants.InfoLogChatGroup,
                        "You need an apiary scoop to work the combs without enraging the colony.",
                        EnumChatType.Notification, null);
                }
                return true;
            }

            if (world.Side != EnumAppSide.Server) return true;

            int combs = 3 + world.Rand.Next(4); // 3-6, the weekly haul
            GiveOrDrop(world, byPlayer, blockSel.Position, new AssetLocation("game", "honeycomb"), combs);
            GiveOrDrop(world, byPlayer, blockSel.Position, new AssetLocation("game", "beeswax"), 1);

            slot.Itemstack.Collectible.DamageItem(world, byPlayer.Entity, slot, 1);
            slot.MarkDirty();

            // Back to populated (controller + body part mirror). The BE
            // survives the swap, so its stale ripen timestamp must be
            // cleared explicitly or the next tick would re-ripen instantly.
            SetApiaryType(world, blockSel.Position, "populated");
            BEApiary be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEApiary;
            be?.OnHarvested();
            return true;
        }

        static void GiveOrDrop(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, AssetLocation itemCode, int quantity)
        {
            Item item = world.GetItem(itemCode);
            if (item == null) return;
            ItemStack give = new ItemStack(item, quantity);
            if (byPlayer == null || !byPlayer.InventoryManager.TryGiveItemstack(give))
            {
                world.SpawnItemEntity(give, pos.ToVec3d().Add(0.5, 1.0, 0.5), null);
            }
        }
    }
}
