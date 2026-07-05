using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    // Box trap — OSRS L27 trap (research doc §1). Bigger attraction radius (5x5
    // ground-tile area = ~3.5 VS metres) and *no snap-on-fail*: keeps running
    // until something is caught. Targets small mammals (hares as proxy for
    // ferret / chinchompa).
    public class BEBoxTrap : BlockEntity
    {
        private const float Radius = 3.5f;
        private const float VertRadius = 2.0f;
        private const int TickIntervalMs = 3000;

        private bool armed = true;
        private string placerUid;
        private Random rand;

        public bool Armed => armed;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            rand = new Random(Pos.GetHashCode() ^ 0x5a17);
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

            float catchChance = GetTierCatchChance();
            bool caught = rand.NextDouble() < catchChance;
            if (!caught) return; // box trap stays armed — no snap-on-fail

            armed = false;
            MarkDirty(true);

            var placer = ResolvePlacer();
            int playerLvl = placer != null ? HunterXpSystem.GetLevel(placer) : 1;

            var climate = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.WorldGenValues);
            float tempC = climate?.Temperature ?? 12f;
            float rain = climate?.Rainfall ?? 0.5f;

            var species = BoxTrapSpecies.Resolve(tempC, rain, playerLvl);

            prey.Die(EnumDespawnReason.Death);
            DropSpeciesLoot(species);
            if (placer != null) HunterXpSystem.AddXp(placer, species.XpReward, species.DisplayName);
        }

        private float GetTierCatchChance()
        {
            var block = Api.World.BlockAccessor.GetBlock(Pos);
            string tier = block?.Variant?["tier"] ?? "iron";
            var attrs = block?.Attributes?["catchchance"];
            if (attrs == null || !attrs.Exists) return 0.5f;
            return attrs[tier].AsFloat(0.5f);
        }

        private static bool IsPrey(Entity e)
        {
            if (e == null || !e.Alive) return false;
            var path = e.Code?.Path;
            return path != null && path.StartsWith("hare-");
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
                ? $"Armed — {(int)(chance * 100)}% catch chance per attempt"
                : "Caught — right-click to reset");
        }
    }

    public static class BoxTrapSpecies
    {
        public static readonly HunterSpecies[] Species = new[]
        {
            new HunterSpecies {
                Id = "ferret",       DisplayName = "Ferret",        HunterLevelReq = 27,
                XpReward = 115, FeatherTone = null, Climate = ClimateFilter.TemperateWet,
                Drops = new[] { new SpeciesDrop("game:bone", 1), new SpeciesDrop("game:hide-raw-small", 1) },
            },
            new HunterSpecies {
                Id = "greychinchompa", DisplayName = "Chinchompa",  HunterLevelReq = 53,
                XpReward = 198, FeatherTone = null, Climate = ClimateFilter.TemperateDry,
                Drops = new[] { new SpeciesDrop("game:bone", 1), new SpeciesDrop("game:fat", 1) },
            },
            new HunterSpecies {
                Id = "embertailed",  DisplayName = "Embertailed Jerboa", HunterLevelReq = 39,
                XpReward = 137, FeatherTone = null, Climate = ClimateFilter.Desert,
                Drops = new[] { new SpeciesDrop("game:bone", 1), new SpeciesDrop("game:hide-raw-small", 1) },
            },
            new HunterSpecies {
                Id = "wildkebbit",   DisplayName = "Wild Kebbit",   HunterLevelReq = 1,
                XpReward = 36,  FeatherTone = null, Climate = ClimateFilter.Any,
                Drops = new[] { new SpeciesDrop("game:bone", 1), new SpeciesDrop("game:hide-raw-small", 1) },
            },
        };

        public static HunterSpecies Resolve(float tempC, float rain, int playerLevel)
        {
            HunterSpecies best = null;
            foreach (var s in Species)
            {
                if (s.HunterLevelReq > playerLevel) continue;
                if (!s.Climate.Matches(tempC, rain)) continue;
                if (best == null || s.XpReward > best.XpReward) best = s;
            }
            return best ?? Species[Species.Length - 1];
        }
    }
}
