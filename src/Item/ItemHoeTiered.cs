using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuneScapeForges
{
    // Tiered hoe — Harvest Moon shape.
    //
    // Copper..steel:  1 tile of farmland (vanilla).
    // Mithril:        2 tiles of farmland in a line forward from the player.
    // Adamantite:     3 tiles.
    // Runite:         4 tiles.
    // Dragon:         5 tiles.
    //
    // "Forward" = the player's horizontal cardinal facing (BlockFacing.HorizontalFromYaw
    // snaps yaw to one of N/E/S/W). Same convention BlockEntityClayForm.OnUseOver uses
    // for player-facing world actions.
    //
    // Extension mechanic: after the vanilla target tile is tilled, step forward
    // (facing.Normali) up to TierRadius-1 additional times. Each tile is passed through
    // vanilla's ItemHoe.DoTill via a fresh BlockSelection, so soil check, sound,
    // farmland-dry-{type} conversion, BlockEntityFarmland.OnCreatedFromSoil, soil-
    // nutrition transfer, durability drain, and MarkBlockDirty all follow the vanilla
    // semantics unchanged — one soil block converted per tile, one durability point per
    // tile actually converted, no durability spent on non-soil tiles.
    //
    // Canopy blocks (block-above-tile != 0) on any tile past the target skip that tile
    // — vanilla would refuse the swing entirely for the target under canopy, but for
    // the extended line we skip individual covered tiles rather than aborting the whole
    // sweep. This lets the player till up to a wall.
    //
    // No LINQ (VS's in-game Roslyn lacks System.Linq on the mod compiler classpath —
    // see feedback/vs-mod-no-linq).
    public class ItemHoeTiered : ItemHoe
    {
        private int cachedRadius = -1;

        public int TierRadius
        {
            get
            {
                if (cachedRadius > 0) return cachedRadius;
                string path = Code?.Path ?? string.Empty;
                if      (path.EndsWith("-dragon"))     cachedRadius = 5;
                else if (path.EndsWith("-runite"))     cachedRadius = 4;
                else if (path.EndsWith("-adamantite")) cachedRadius = 3;
                else if (path.EndsWith("-mithril"))    cachedRadius = 2;
                else                                    cachedRadius = 1;
                return cachedRadius;
            }
        }

        public override void DoTill(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null) return;

            // Default tiers — vanilla single-tile behavior.
            if (TierRadius <= 1)
            {
                base.DoTill(secondsUsed, slot, byEntity, blockSel, entitySel);
                return;
            }

            // Cardinal-snap the player's yaw to a horizontal facing.
            // BlockFacing.HorizontalFromYaw returns one of NORTH/EAST/SOUTH/WEST — the
            // direction the player is looking, snapped to the nearest cardinal.
            BlockFacing forward = BlockFacing.HorizontalFromYaw(byEntity.Pos.Yaw);
            Vec3i step = forward.Normali;

            // Till the target tile, then step forward TierRadius-1 additional tiles.
            IWorldAccessor world = byEntity.World;
            for (int n = 0; n < TierRadius; n++)
            {
                if (slot.Itemstack == null) break;  // tool broke mid-sweep

                BlockPos pos = blockSel.Position.AddCopy(step.X * n, 0, step.Z * n);

                // Skip tiles that are covered above (vanilla behavior for the target;
                // extended per-tile here so the sweep isn't aborted by a distant canopy).
                if (world.BlockAccessor.GetBlock(pos.UpCopy(1)).Id != 0) continue;

                // Fresh BlockSelection per tile so base.DoTill sees the right Position.
                BlockSelection tile = new BlockSelection
                {
                    Position = pos,
                    Face = blockSel.Face,
                    HitPosition = blockSel.HitPosition,
                };

                // Vanilla ItemHoe.DoTill handles soil check, sound, conversion, damage,
                // soil-nutrition transfer, farmland OnCreatedFromSoil, and MarkBlockDirty.
                // Non-soil positions early-return without draining durability, so tiles
                // past the field's edge don't cost the player anything.
                base.DoTill(secondsUsed, slot, byEntity, tile, entitySel);
            }
        }
    }
}
