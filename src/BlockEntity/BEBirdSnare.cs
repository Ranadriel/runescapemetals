using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace RuneScapeForges
{
    // Server-side bird snare. Ticks every 2s scanning a small AABB for chickens.
    // On encounter snare *always* snaps (OSRS semantics). Catch chance is read
    // from the block's tier attributes. On successful catch the snare resolves
    // which OSRS species was caught from local climate, kills the proxy entity,
    // drops the species' loot table at the snare tile, and awards Hunter XP to
    // the placer if known.
    public class BEBirdSnare : BlockEntity
    {
        private const float Radius = 2.5f;
        private const float VertRadius = 2.0f;
        private const int TickIntervalMs = 2000;

        private bool armed = true;
        private string placerUid;
        private Random rand;

        public bool Armed => armed;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            rand = new Random(Pos.GetHashCode());
            if (api.Side == EnumAppSide.Server)
            {
                RegisterGameTickListener(OnTick, TickIntervalMs);
            }
        }

        public void OnPlaced(IPlayer byPlayer)
        {
            placerUid = byPlayer?.PlayerUID;
            MarkDirty(false);
        }

        private void OnTick(float dt)
        {
            if (!armed) return;
            var center = new Vec3d(Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5);
            var candidates = Api.World.GetEntitiesAround(center, Radius, VertRadius, IsPrey);
            if (candidates == null || candidates.Length == 0) return;

            var prey = candidates[rand.Next(candidates.Length)];
            armed = false;
            MarkDirty(true);

            float catchChance = GetTierCatchChance();
            bool caught = rand.NextDouble() < catchChance;
            if (!caught) return;

            var placer = ResolvePlacer();
            int playerLvl = placer != null ? HunterXpSystem.GetLevel(placer) : 1;

            var climate = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.WorldGenValues);
            float tempC = climate?.Temperature ?? 12f;
            float rain = climate?.Rainfall ?? 0.5f;

            var species = HunterSpeciesTable.Resolve(tempC, rain, playerLvl);

            prey.Die(EnumDespawnReason.Death);
            DropSpeciesLoot(species);
            if (placer != null) HunterXpSystem.AddXp(placer, species.XpReward, species.DisplayName);
        }

        private float GetTierCatchChance()
        {
            var block = Api.World.BlockAccessor.GetBlock(Pos);
            string tier = block?.Variant?["tier"] ?? "copper";
            var attrs = block?.Attributes?["catchchance"];
            if (attrs == null || !attrs.Exists) return 0.5f;
            return attrs[tier].AsFloat(0.5f);
        }

        private static bool IsPrey(Entity e)
        {
            if (e == null || !e.Alive) return false;
            var path = e.Code?.Path;
            return path != null && path.StartsWith("chicken-");
        }

        private IPlayer ResolvePlacer()
        {
            if (string.IsNullOrEmpty(placerUid)) return null;
            return Api.World.PlayerByUid(placerUid);
        }

        private void DropSpeciesLoot(HunterSpecies species)
        {
            var pos = new Vec3d(Pos.X + 0.5, Pos.Y + 0.4, Pos.Z + 0.5);
            foreach (var d in species.Drops) DropStack(d.Code, d.Qty, pos);
        }

        private void DropStack(string code, int qty, Vec3d pos)
        {
            var loc = new AssetLocation(code);
            var item = Api.World.GetItem(loc);
            if (item != null) { Api.World.SpawnItemEntity(new ItemStack(item, qty), pos); return; }
            var block = Api.World.GetBlock(loc);
            if (block != null) Api.World.SpawnItemEntity(new ItemStack(block, qty), pos);
        }

        public bool TryRearm()
        {
            if (armed) return false;
            armed = true;
            MarkDirty(true);
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            armed = tree.GetBool("armed", true);
            placerUid = tree.GetString("placer", null);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("armed", armed);
            if (placerUid != null) tree.SetString("placer", placerUid);
        }

        public override void GetBlockInfo(IPlayer forPlayer, System.Text.StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            float chance = GetTierCatchChance();
            dsc.AppendLine(armed
                ? $"Armed — {(int)(chance * 100)}% base catch chance"
                : "Sprung — right-click to reset");
        }
    }
}
