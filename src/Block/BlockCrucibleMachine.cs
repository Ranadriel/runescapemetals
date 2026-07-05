using Vintagestory.API.Common;

namespace RuneScapeForges
{
    // The fired metal crucible machine. Hold right-click to tip the drum;
    // release and it swings back (animation easing handles the rewind).
    public class BlockCrucibleMachine : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BECrucibleMachine be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECrucibleMachine;
            if (be != null)
            {
                // Sneak+RMB → open inventory dialog (load fuel + ore).
                // Plain RMB → tip animation (the existing pour gesture).
                if (byPlayer.Entity.Controls.Sneak)
                {
                    return be.OnPlayerRightClick(byPlayer, blockSel);
                }
                be.OnTipStart();
                return true;
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            // keep ticking while the player holds the tip
            return true;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BECrucibleMachine be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECrucibleMachine;
            if (be != null) be.OnTipStop();
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            BECrucibleMachine be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECrucibleMachine;
            if (be != null) be.OnTipStop();
            return true;
        }

        // ─── Ore-loss guard ───────────────────────────────────────────────────
        // Vanilla's smelt pipeline (and any future heater that drives this
        // crucible) calls CanSmelt() to ask whether the contents are eligible
        // to cook. Refuse the cook outright if the contents include items
        // that have already been through a smelt cycle — re-cooking them
        // either produces nothing (lost slot) or actually consumes mass
        // (lost ore).
        //
        // Two states to detect:
        //   (a) Already-melted form  — a *-smelted block variant (held molten
        //       metal from a prior cook). Re-cooking a melted form has no
        //       legitimate output; the slot is consumed for nothing.
        //   (b) Solidified terminal  — an ingot (Code starts with "ingot-")
        //       or any heatable item whose CombustibleProps.SmeltedStack is
        //       null (i.e., it has a melting point but nothing to smelt to).
        //       Re-cooking an ingot is the canonical ore-loss bug.
        public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
        {
            if (cookingSlotsProvider?.Slots == null) return false;

            bool foundSmeltable = false;
            foreach (ItemSlot slot in cookingSlotsProvider.Slots)
            {
                if (slot?.Itemstack == null) continue;

                // (a) Refuse any already-cooked input (ore-loss guard).
                if (IsAlreadyCooked(slot.Itemstack)) return false;

                // (b) Track positive eligibility — at least one slot must
                //     contain an item whose combustion props point to a
                //     real smelt output. Otherwise this is empty/junk.
                var combust = slot.Itemstack.Collectible.CombustibleProps;
                if (combust?.SmeltedStack != null) foundSmeltable = true;
            }

            return foundSmeltable;
        }

        // Static helper kept on the block so a future BE-level cook driver
        // (BECrucibleMachine) can reuse the same check before advancing its
        // own temperature tick.
        public static bool IsAlreadyCooked(ItemStack stack)
        {
            if (stack?.Collectible == null) return false;

            string code = stack.Collectible.Code?.ToShortString();
            if (code == null) return false;

            // (a) Already-melted: any *-smelted crucible block.
            if (code.Contains("-smelted")) return true;

            // (b) Solidified ingot — fast path by code prefix.
            if (code.StartsWith("ingot-")) return true;

            // (b cont.) Solidified-terminal — anything with a melting point
            // but nothing to smelt to. Catches finished metal items beyond
            // ingots: nuggets-already-counted-as-output, plates, etc.
            var combust = stack.Collectible.CombustibleProps;
            if (combust != null && combust.MeltingPoint > 0 && combust.SmeltedStack == null)
            {
                return true;
            }

            return false;
        }
    }
}
