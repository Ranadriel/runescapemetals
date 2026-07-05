using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuneScapeForges
{
    // The magma scoop is the ONLY tool that moves molten metal in this mod.
    //
    // Source:  a BlockCrucibleMachine block whose BECrucibleMachine holds
    //          molten metal (filled by the cook driver when it lands).
    // Sink:    another BlockCrucibleMachine. Mixing different metals is
    //          rejected at the destination side; same-metal merges.
    //
    // Everything else — world lava, vanilla crucibles, molds, the ground —
    // is silently refused. No spill, no fallback, no surprise.
    public class BlockMagmaScoop : BlockBucket
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (!firstEvent || blockSel == null || byEntity.Controls.ShiftKey)
            {
                // Shift+RMB and non-first-event passes fall through so the
                // player can still place the scoop in the world as a block.
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }

            var world = byEntity.World;
            var targetBlock = world.BlockAccessor.GetBlock(blockSel.Position);

            // Only react when the target is one of our tipper crucibles OR
            // the launder buffer. Everything else — including vanilla lava
            // blocks — is refused outright. Holding RMB on lava no longer
            // scoops it.
            if (targetBlock is BlockCrucibleMachine)
            {
                var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECrucibleMachine;
                if (be != null)
                {
                    if (TryTransfer(itemslot, byEntity, be))
                    {
                        handHandling = EnumHandHandling.PreventDefault;
                        return;
                    }
                }
                handHandling = EnumHandHandling.PreventDefault;
                return;
            }

            if (targetBlock is BlockLaunder)
            {
                var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BELaunder;
                if (be != null)
                {
                    TryTransferLaunder(itemslot, byEntity, be);
                }
                handHandling = EnumHandHandling.PreventDefault;
                return;
            }

            // Not a crucible or launder — REFUSE silently. The scoop is a
            // closed crucible-economy courier; we do not interact with
            // anything else. No call to base; no liquid drink/place fallback.
            handHandling = EnumHandHandling.PreventDefault;
        }

        // Launder side of the transfer — mirrors TryTransfer but speaks the
        // BELaunder AcceptMolten / WithdrawMolten interface. Same direction
        // rule: empty scoop withdraws, loaded scoop deposits.
        bool TryTransferLaunder(ItemSlot scoopSlot, EntityAgent byEntity, BELaunder launder)
        {
            var world = byEntity.World;
            var player = (byEntity as EntityPlayer)?.Player;
            if (player != null && !world.Claims.TryAccess(player, launder.Pos, EnumBlockAccessFlags.BuildOrBreak)) return false;

            float currentScoopLitres = GetCurrentLitres(scoopSlot.Itemstack);
            var scoopContent = GetContent(scoopSlot.Itemstack);
            bool scoopEmpty = scoopContent == null || currentScoopLitres <= 0;

            if (scoopEmpty)
            {
                return TryWithdrawFromLaunder(scoopSlot, byEntity, launder);
            }

            return TryDepositIntoLaunder(scoopSlot, scoopContent, byEntity, launder);
        }

        bool TryWithdrawFromLaunder(ItemSlot scoopSlot, EntityAgent byEntity, BELaunder launder)
        {
            string metalType = launder.MoltenMetalType;
            int launderUnits = launder.MoltenUnits;
            if (string.IsNullOrEmpty(metalType) || launderUnits <= 0) return false;

            var world = byEntity.World;
            var portionItem = world.GetItem(new AssetLocation("runescape:magmaportion-" + metalType));
            if (portionItem == null) return false;

            int targetCapItems = (int)(CapacityLitres * 100f);
            int wantItems = System.Math.Min(launderUnits, targetCapItems);

            var portionStack = new ItemStack(portionItem, wantItems);
            int movedItems = TryPutLiquid(scoopSlot.Itemstack, portionStack, CapacityLitres);
            if (movedItems <= 0) return false;

            launder.WithdrawMolten(movedItems);
            world.PlaySoundAt(new AssetLocation("sounds/block/lava"), launder.Pos.X + 0.5, launder.Pos.Y + 0.5, launder.Pos.Z + 0.5, (byEntity as EntityPlayer)?.Player);
            scoopSlot.MarkDirty();
            launder.MarkDirty(true);
            return true;
        }

        bool TryDepositIntoLaunder(ItemSlot scoopSlot, ItemStack scoopContent, EntityAgent byEntity, BELaunder launder)
        {
            string scoopMetal = scoopContent.Item?.Code?.Path;
            if (string.IsNullOrEmpty(scoopMetal) || !scoopMetal.StartsWith("magmaportion-")) return false;
            string metalType = scoopMetal.Substring("magmaportion-".Length);

            if (!string.IsNullOrEmpty(launder.MoltenMetalType) && launder.MoltenMetalType != metalType) return false;

            int scoopItems = scoopContent.StackSize;
            if (scoopItems <= 0) return false;

            int accepted = launder.AcceptMolten(metalType, scoopItems);
            if (accepted <= 0) return false;

            ItemStack taken = TryTakeLiquid(scoopSlot.Itemstack, accepted / 100f);
            if (taken == null || taken.StackSize <= 0)
            {
                launder.WithdrawMolten(accepted);
                return false;
            }

            var world = byEntity.World;
            world.PlaySoundAt(new AssetLocation("sounds/block/lava"), launder.Pos.X + 0.5, launder.Pos.Y + 0.5, launder.Pos.Z + 0.5, (byEntity as EntityPlayer)?.Player);
            scoopSlot.MarkDirty();
            launder.MarkDirty(true);
            return true;
        }

        // Attempt the bidirectional transfer. Direction is determined by
        // scoop fill state: empty scoop → withdraw from crucible; loaded
        // scoop → deposit into crucible.
        bool TryTransfer(ItemSlot scoopSlot, EntityAgent byEntity, BECrucibleMachine crucible)
        {
            var world = byEntity.World;
            var player = (byEntity as EntityPlayer)?.Player;
            if (player != null && !world.Claims.TryAccess(player, crucible.Pos, EnumBlockAccessFlags.BuildOrBreak)) return false;

            float currentScoopLitres = GetCurrentLitres(scoopSlot.Itemstack);
            var scoopContent = GetContent(scoopSlot.Itemstack);
            bool scoopEmpty = scoopContent == null || currentScoopLitres <= 0;

            if (scoopEmpty)
            {
                return TryWithdrawFromCrucible(scoopSlot, byEntity, crucible);
            }

            return TryDepositIntoCrucible(scoopSlot, scoopContent, byEntity, crucible);
        }

        // Pull molten metal OUT of a tipper crucible into the empty scoop.
        bool TryWithdrawFromCrucible(ItemSlot scoopSlot, EntityAgent byEntity, BECrucibleMachine crucible)
        {
            string metalType = crucible.MoltenMetalType;
            int crucibleUnits = crucible.MoltenUnits;
            if (string.IsNullOrEmpty(metalType) || crucibleUnits <= 0) return false;

            var world = byEntity.World;
            var portionItem = world.GetItem(new AssetLocation("runescape:magmaportion-" + metalType));
            if (portionItem == null) return false;

            // Vanilla water-tight liquid math: itemsPerLitre = 100, so one
            // unit of molten in the crucible's accounting = one item in
            // the bucket stack. 100 items per litre × capacity litres.
            int targetCapItems = (int)(CapacityLitres * 100f);
            int wantItems = System.Math.Min(crucibleUnits, targetCapItems);

            var portionStack = new ItemStack(portionItem, wantItems);
            int movedItems = TryPutLiquid(scoopSlot.Itemstack, portionStack, CapacityLitres);
            if (movedItems <= 0) return false;

            crucible.WithdrawMolten(movedItems);

            world.PlaySoundAt(new AssetLocation("sounds/block/lava"), crucible.Pos.X + 0.5, crucible.Pos.Y + 0.5, crucible.Pos.Z + 0.5, (byEntity as EntityPlayer)?.Player);
            scoopSlot.MarkDirty();
            crucible.MarkDirty(true);
            return true;
        }

        // Push the scoop's contents INTO a tipper crucible. Rejects when
        // the destination already holds a different metal.
        bool TryDepositIntoCrucible(ItemSlot scoopSlot, ItemStack scoopContent, EntityAgent byEntity, BECrucibleMachine crucible)
        {
            string scoopMetal = scoopContent.Item?.Code?.Path;
            if (string.IsNullOrEmpty(scoopMetal) || !scoopMetal.StartsWith("magmaportion-")) return false;
            string metalType = scoopMetal.Substring("magmaportion-".Length);

            // Cross-metal contamination check — reject mixing.
            if (!string.IsNullOrEmpty(crucible.MoltenMetalType) && crucible.MoltenMetalType != metalType) return false;

            int scoopItems = scoopContent.StackSize;
            if (scoopItems <= 0) return false;

            int accepted = crucible.AcceptMolten(metalType, scoopItems);
            if (accepted <= 0) return false;

            // TryTakeLiquid returns the taken liquid stack (or null/empty if
            // nothing was taken). Convert accepted-units back to litres for
            // the request.
            ItemStack taken = TryTakeLiquid(scoopSlot.Itemstack, accepted / 100f);
            if (taken == null || taken.StackSize <= 0)
            {
                // Roll back the crucible-side accept if the scoop refused
                // to give up the liquid (shouldn't happen, defensive).
                crucible.WithdrawMolten(accepted);
                return false;
            }

            var world = byEntity.World;
            world.PlaySoundAt(new AssetLocation("sounds/block/lava"), crucible.Pos.X + 0.5, crucible.Pos.Y + 0.5, crucible.Pos.Z + 0.5, (byEntity as EntityPlayer)?.Player);
            scoopSlot.MarkDirty();
            crucible.MarkDirty(true);
            return true;
        }
    }
}
