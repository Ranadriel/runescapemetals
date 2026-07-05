using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace RuneScapeForges
{
    // Server-side mechanics for Ava's attractor (docs/DESIGN_avas_attractor.md):
    //  1. ammo recovery  — stuck projectiles fired by a wearer roll to return to inventory
    //  2. junk attraction — every junkIntervalSec (if the wearer moved minMoveBlocks) a
    //     weighted iron oddment is drawn in; sneak+rightclick the held item mutes this
    //  3. interference   — metal torso armor renders the device fully inert
    public class AvasAttractorSystem : ModSystem
    {
        ICoreServerAPI sapi;
        readonly Random rand = new Random();

        class JunkState
        {
            public Vec3d LastPos;
            public double MovedBlocks;
            public long LastGrantMs;
        }

        readonly Dictionary<string, JunkState> junkStates = new Dictionary<string, JunkState>();

        // own projectile tracking via spawn events — deliberately avoids touching
        // IServerWorldAccessor.LoadedEntities, whose ConcurrentDictionary type is not
        // on the script-mod compiler's reference list (CS0012)
        readonly List<EntityProjectileBase> trackedProjectiles = new List<EntityProjectileBase>();

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            api.Event.OnEntitySpawn += OnEntitySpawned;
            api.Event.OnEntityLoaded += OnEntitySpawned;
            api.Event.RegisterGameTickListener(OnRecoveryTick, 750);
            api.Event.RegisterGameTickListener(OnJunkTick, 4000);
            api.Event.PlayerDisconnect += plr => junkStates.Remove(plr.PlayerUID);
        }

        private void OnEntitySpawned(Entity entity)
        {
            if (entity is EntityProjectileBase proj && !proj.WatchedAttributes.GetBool("avasJudged", false))
            {
                trackedProjectiles.Add(proj);
            }
        }

        ItemSlot GetWornAttractor(EntityPlayer eplr)
        {
            var inv = eplr?.Player?.InventoryManager?.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inv == null) return null;

            int idx = (int)EnumCharacterDressType.Shoulder;
            if (idx >= inv.Count) return null;

            ItemSlot slot = inv[idx];
            if (slot?.Itemstack?.Collectible?.Code?.Path == "avasattractor") return slot;
            return null;
        }

        bool MetalTorsoInterference(EntityPlayer eplr)
        {
            var inv = eplr?.Player?.InventoryManager?.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (inv == null) return false;

            int idx = (int)EnumCharacterDressType.ArmorBody;
            if (idx >= inv.Count) return false;

            string path = inv[idx]?.Itemstack?.Collectible?.Code?.Path;
            if (path == null) return false;

            return path.Contains("chain") || path.Contains("lamellar") || path.Contains("plate")
                || path.Contains("scale") || path.Contains("brigandine");
        }

        private void OnRecoveryTick(float dt)
        {
            long now = sapi.World.ElapsedMilliseconds;

            for (int i = trackedProjectiles.Count - 1; i >= 0; i--)
            {
                EntityProjectileBase proj = trackedProjectiles[i];

                if (proj == null || !proj.Alive || proj.WatchedAttributes.GetBool("avasJudged", false))
                {
                    trackedProjectiles.RemoveAt(i);
                    continue;
                }

                if (!proj.Stuck || proj.ProjectileStack == null) continue;

                // Bug workaround: vanilla EntityProjectile.OnGameTick sets Stuck = true the moment
                // Collided is true OR collTester.IsColliding(blocks) OR the persisted "stuck" attribute
                // is true. Once true, it's persisted to WatchedAttributes and remains sticky forever.
                // Physics-engine quirks (grazing a hitbox edge, chunk-boundary jitter, etc.) can flip
                // Collided briefly during flight — that permanently locks Stuck true and freezes the
                // arrow in midair via pos.Motion.Set(0,0,0) inside IsColliding, WITHOUT any legitimate
                // target having been damaged.
                //
                // Two safeguards to distinguish "arrow legitimately landed" from "arrow spuriously
                // Stuck in flight":
                //
                // 1. Record the first tick we see Stuck true; wait ≥1000 ms before yanking. This
                //    gives vanilla's IsColliding + TryAttackEntity + damage flow well past their
                //    500 ms internal cooldown (msCollide + 500), so any legitimate impact has fully
                //    resolved before we intervene.
                //
                // 2. Require motion to have settled. Vanilla IsColliding zeros motion, but arrows
                //    embedded in moving hosts (target entity carrying arrow) can still have residual
                //    velocity as the physics system tracks the host. Motion effectively at rest means
                //    the arrow has actually stopped moving — a real land, not a mid-flight freeze.
                long stuckSince = proj.WatchedAttributes.GetLong("avasStuckSince", 0L);
                if (stuckSince == 0L)
                {
                    proj.WatchedAttributes.SetLong("avasStuckSince", now);
                    continue;
                }
                if (now - stuckSince < 1000L) continue;
                if (proj.ServerPos.Motion.LengthSq() > 0.001) continue;

                // every stuck projectile gets judged exactly once; failures keep vanilla behavior
                proj.WatchedAttributes.SetBool("avasJudged", true);
                trackedProjectiles.RemoveAt(i);

                var eplr = proj.FiredBy as EntityPlayer;
                if (eplr == null) continue;

                ItemSlot worn = GetWornAttractor(eplr);
                if (worn == null || MetalTorsoInterference(eplr)) continue;

                float chance = worn.Itemstack.Collectible.Attributes?["avasattractor"]?["recoveryChance"]?.AsFloat(0.6f) ?? 0.6f;
                if (rand.NextDouble() >= chance) continue;

                ItemStack give = proj.ProjectileStack.Clone();
                give.ResolveBlockOrItem(sapi.World);

                if (!eplr.TryGiveItemStack(give))
                {
                    sapi.World.SpawnItemEntity(give, eplr.ServerPos.XYZ);
                }

                sapi.World.PlaySoundAt(new AssetLocation("game:sounds/effect/latch"), eplr, null, true, 12f, 0.5f);
                proj.Die(EnumDespawnReason.Removed, null);
            }
        }

        private void OnJunkTick(float dt)
        {
            long now = sapi.World.ElapsedMilliseconds;

            foreach (IPlayer plr in sapi.World.AllOnlinePlayers)
            {
                var eplr = plr.Entity;
                if (eplr == null) continue;

                if (!junkStates.TryGetValue(plr.PlayerUID, out JunkState st))
                {
                    junkStates[plr.PlayerUID] = new JunkState
                    {
                        LastPos = eplr.ServerPos.XYZ.Clone(),
                        LastGrantMs = now
                    };
                    continue;
                }

                double moved = eplr.ServerPos.XYZ.DistanceTo(st.LastPos);
                if (moved > 0.01 && moved < 200) st.MovedBlocks += moved; // ignore teleports
                st.LastPos = eplr.ServerPos.XYZ.Clone();

                ItemSlot worn = GetWornAttractor(eplr);
                if (worn == null || MetalTorsoInterference(eplr)) continue;
                if (worn.Itemstack.Attributes.GetBool("communeOff", false)) continue;

                JsonObject cfg = worn.Itemstack.Collectible.Attributes?["avasattractor"];
                float intervalSec = cfg?["junkIntervalSec"]?.AsFloat(210f) ?? 210f;
                float minMove = cfg?["minMoveBlocks"]?.AsFloat(3f) ?? 3f;

                if (now - st.LastGrantMs < intervalSec * 1000f || st.MovedBlocks < minMove) continue;

                ItemStack junkStack = PickJunk(cfg?["junkTable"]);
                if (junkStack == null) continue;

                st.LastGrantMs = now;
                st.MovedBlocks = 0;

                if (!eplr.TryGiveItemStack(junkStack))
                {
                    sapi.World.SpawnItemEntity(junkStack, eplr.ServerPos.XYZ);
                }

                (plr as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup,
                    Lang.Get("Your attractor clicks — it has drawn something in."),
                    EnumChatType.Notification);
            }
        }

        ItemStack PickJunk(JsonObject table)
        {
            if (table == null || !table.Exists) return null;

            JsonObject[] entries = table.AsArray();
            if (entries == null || entries.Length == 0) return null;

            // weigh only entries whose item actually resolves, so the table tolerates
            // codes that don't exist in this game version / mod set
            var resolved = new List<(Item item, int weight, int min, int max)>();
            int total = 0;
            foreach (JsonObject entry in entries)
            {
                string code = entry["code"]?.AsString(null);
                if (code == null) continue;

                Item item = sapi.World.GetItem(new AssetLocation(code));
                if (item == null) continue;

                int weight = entry["weight"]?.AsInt(1) ?? 1;
                int min = entry["min"]?.AsInt(1) ?? 1;
                int max = entry["max"]?.AsInt(1) ?? 1;
                resolved.Add((item, weight, min, max));
                total += weight;
            }

            if (total <= 0) return null;

            int roll = rand.Next(total);
            foreach (var (item, weight, min, max) in resolved)
            {
                roll -= weight;
                if (roll < 0)
                {
                    int size = min + rand.Next(Math.Max(1, max - min + 1));
                    return new ItemStack(item, size);
                }
            }

            return null;
        }
    }
}
