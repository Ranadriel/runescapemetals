using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuneScapeForges
{
    // Tiered scythe: swing radius grows by 1 block per metal tier past steel.
    //
    //   default (copper..steel): 3×3×3  scan,  5 blocks broken (vanilla behavior)
    //   mithril:                 5×5×5  scan, 10 blocks broken
    //   adamantite:              7×7×7  scan, 15 blocks broken
    //   runite:                  9×9×9  scan, 20 blocks broken
    //   dragon:                 11×11×11 scan, 25 blocks broken
    //
    // ItemScythe bypasses ItemShears.OnBlockBreaking (it uses a timed sweep in
    // OnHeldAttack*) and calls OnBlockBrokenWith directly at t=1.05s. So we only
    // need to override OnBlockBrokenWith with a tier-scaled scan; trim-mode is
    // preserved because breakMultiBlock is inherited from ItemScythe unchanged.
    //
    // No LINQ used — VS's in-game Roslyn does not have System.Linq on the mod
    // compiler classpath. Sort is done via List<T>.Sort with a Comparison<T>.
    public class ItemScytheTiered : ItemScythe
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

        public override int MultiBreakQuantity => 5 * TierRadius;

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1f)
        {
            // Default tiers behave exactly as vanilla — do not intercept.
            if (TierRadius <= 1) return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);

            Block block = world.BlockAccessor.GetBlock(blockSel.Position);
            if (!(byEntity is EntityPlayer ep) || itemslot.Itemstack == null) return true;
            IPlayer plr = world.PlayerByUid(ep.PlayerUID);

            // Center target always breaks via the inherited scythe helper (trim-mode aware).
            breakMultiBlock(blockSel.Position, plr);

            if (!CanMultiBreak(block)) return true;

            int r = TierRadius;
            Vec3d hitPos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);

            // Collect nearby multibreakables with squared distance to hit point.
            var scan = new List<KeyValuePair<BlockPos, float>>();
            for (int i = -r; i <= r; i++)
                for (int j = -r; j <= r; j++)
                    for (int k = -r; k <= r; k++)
                    {
                        if (i == 0 && j == 0 && k == 0) continue;
                        BlockPos np = blockSel.Position.AddCopy(i, j, k);
                        if (CanMultiBreak(world.BlockAccessor.GetBlock(np)))
                        {
                            float dSq = (float)hitPos.SquareDistanceTo(np.X + 0.5, np.Y + 0.5, np.Z + 0.5);
                            scan.Add(new KeyValuePair<BlockPos, float>(np, dSq));
                        }
                    }

            // Nearest-first sort. Instance method — no LINQ needed.
            scan.Sort((a, b) => a.Value.CompareTo(b.Value));

            int broken = 0;
            int quota = MultiBreakQuantity;
            for (int idx = 0; idx < scan.Count; idx++)
            {
                KeyValuePair<BlockPos, float> kv = scan[idx];
                if (plr.Entity.World.Claims.TryAccess(plr, kv.Key, EnumBlockAccessFlags.BuildOrBreak))
                {
                    breakMultiBlock(kv.Key, plr);
                    DamageItem(world, byEntity, itemslot, 1, true);
                    broken++;
                    if (broken >= quota || itemslot.Itemstack == null) break;
                }
            }
            return true;
        }
    }
}
