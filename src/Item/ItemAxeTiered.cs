using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuneScapeForges
{
    // Tiered axe — yield-decay resistance per metal tier past steel.
    //
    // Vanilla ItemAxe fells the whole tree in one break: FindTree collects every
    // connected wood + adjacent leaf-cluster block, then OnBlockBrokenWith iterates
    // and breaks each. Two drop multipliers decay as the loop progresses:
    //
    //   leafMult    (initial 1.00) — applied to LEAVES blocks (material 5)
    //   branchyMult (initial 0.80) — applied to BRANCHY blocks (path contains "branchy")
    //
    // Wood logs (material 4) always drop at 1.00 regardless. Vanilla decay factors:
    // leaves 0.85×/block, branchy 0.70×/block. Big trees give diminishing returns on
    // saplings and sticks.
    //
    // This subclass keeps the initial multipliers vanilla-identical but lowers the
    // decay per tier (higher tier = slower decay). At dragon tier, both multipliers
    // are 1.00 — no decay at all, so a huge magic tree gives every sapling and
    // every branchy drop at full quantity.
    //
    //   default (copper..steel): leaves 0.85, branchy 0.70  (vanilla)
    //   mithril:                 leaves 0.88, branchy 0.775
    //   adamantite:              leaves 0.91, branchy 0.85
    //   runite:                  leaves 0.94, branchy 0.925
    //   dragon:                  leaves 1.00, branchy 1.00  (no decay)
    //
    // Two VS-mod-compiler-classpath gotchas navigated:
    //   1. System.Linq is not available (see feedback/vs-mod-no-linq). Use manual
    //      loops.
    //   2. System.Collections.Stack<T> is not available — Stack<> is in the
    //      System.Collections assembly which the mod compiler does not reference,
    //      even though the namespace and forward-target exist at runtime.
    //      Vanilla ItemAxe.FindTree returns Stack<BlockPos>, so we can't call it
    //      directly. We invoke via reflection and iterate the result as a
    //      non-generic System.Collections.IEnumerable (forwarded into
    //      System.Runtime, which IS on the classpath). Stack<T>'s IEnumerable
    //      contract iterates top-to-bottom (LIFO order — same as Pop), so element
    //      order matches vanilla.
    public class ItemAxeTiered : ItemAxe
    {
        private int cachedTier = -1;

        public int TierIndex
        {
            get
            {
                if (cachedTier >= 0) return cachedTier;
                string path = Code?.Path ?? string.Empty;
                if      (path.EndsWith("-dragon"))     cachedTier = 4;
                else if (path.EndsWith("-runite"))     cachedTier = 3;
                else if (path.EndsWith("-adamantite")) cachedTier = 2;
                else if (path.EndsWith("-mithril"))    cachedTier = 1;
                else                                    cachedTier = 0;
                return cachedTier;
            }
        }

        // Index by TierIndex (0 = copper..steel = vanilla, 4 = dragon = no decay).
        private static readonly float[] LeafDecay    = { 0.85f, 0.88f,  0.91f, 0.94f,  1.00f };
        private static readonly float[] BranchyDecay = { 0.70f, 0.775f, 0.85f, 0.925f, 1.00f };

        // Cached reflection lookup — resolved once per Item instance.
        private MethodInfo _findTreeMethod;
        private MethodInfo GetFindTreeMethod()
        {
            if (_findTreeMethod != null) return _findTreeMethod;
            _findTreeMethod = typeof(ItemAxe).GetMethod(
                "FindTree", BindingFlags.Instance | BindingFlags.Public);
            return _findTreeMethod;
        }

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1f)
        {
            // Default tiers — vanilla decay unchanged.
            if (TierIndex <= 0) return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);

            IPlayer plr = null;
            if (byEntity is EntityPlayer ep) plr = world.PlayerByUid(ep.PlayerUID);

            // Invoke vanilla FindTree via reflection to avoid the Stack<T>
            // compile-time dependency. args[2] and args[3] receive the out params.
            MethodInfo findTree = GetFindTreeMethod();
            if (findTree == null)
            {
                return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
            }

            object[] args = new object[] { world, blockSel.Position, 0, 0 };
            object findTreeResult;
            try
            {
                findTreeResult = findTree.Invoke(this, args);
            }
            catch
            {
                return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
            }

            // Materialize the Stack<BlockPos> return into a List<BlockPos> via the
            // non-generic IEnumerable interface. Stack<T>'s enumerator iterates
            // top-to-bottom (matches vanilla's while-Pop order).
            System.Collections.IEnumerable seq = findTreeResult as System.Collections.IEnumerable;
            if (seq == null)
            {
                return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
            }

            List<BlockPos> positions = new List<BlockPos>();
            foreach (object o in seq)
            {
                if (o is BlockPos bp) positions.Add(bp);
            }

            // Not a tree — hand back to vanilla single-block break.
            if (positions.Count == 0)
            {
                return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
            }

            // Does this axe take durability damage when breaking blocks?
            EnumItemDamageSource[] dmgBy = GetDamagedBy(itemslot);
            bool damageOnBreak = false;
            if (dmgBy != null)
            {
                for (int i = 0; i < dmgBy.Length; i++)
                {
                    if (dmgBy[i] == EnumItemDamageSource.BlockBreaking) { damageOnBreak = true; break; }
                }
            }

            float leafMult = 1f;
            float branchyMult = 0.8f;
            float leafDecayRate = LeafDecay[TierIndex];
            float branchyDecayRate = BranchyDecay[TierIndex];
            int blocksBroken = 0;
            bool toolAlive = true;

            for (int idx = 0; idx < positions.Count; idx++)
            {
                BlockPos pos = positions[idx];
                Block block = world.BlockAccessor.GetBlock(pos);

                bool isWood    = (int)block.BlockMaterial == 4;   // EnumBlockMaterial.Wood
                bool isLeaf    = (int)block.BlockMaterial == 5;   // EnumBlockMaterial.Leaves
                bool isBranchy = block.Code.Path.Contains("branchy");

                // Vanilla: skip wood blocks if tool broke (leaves still drop).
                if (isWood && !toolAlive) continue;

                blocksBroken++;
                float dropMult = isLeaf ? leafMult : (isBranchy ? branchyMult : 1f);
                world.BlockAccessor.BreakBlock(pos, plr, dropMult);

                // Wood costs durability, leaves and branchy don't.
                if (damageOnBreak && isWood)
                {
                    DamageItem(world, byEntity, itemslot, 1, true);
                    if (itemslot.Itemstack == null) toolAlive = false;
                }

                // Tier-adjusted decay per block. Floors match vanilla.
                if (isLeaf    && leafMult    > 0.03f)  leafMult    *= leafDecayRate;
                if (isBranchy && branchyMult > 0.015f) branchyMult *= branchyDecayRate;
            }

            // Vanilla treefell sound for a substantial tree.
            if (blocksBroken > 35 && toolAlive)
            {
                world.PlaySoundAt(new AssetLocation("sounds/effect/treefell"),
                    blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z,
                    plr, false, 32f, GameMath.Clamp((float)blocksBroken / 100f, 0.25f, 1f));
            }

            return true;
        }
    }
}
