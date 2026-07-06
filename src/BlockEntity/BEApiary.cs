using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuneScapeForges
{
    // Ripening brain of the apiary column. Lives on the CONTROLLER (bottom
    // block) and SURVIVES populated<->harvestable swaps (same entity class),
    // so harvest must reset the timer via OnHarvested(). Own calendar timer +
    // lightweight flower gate; winter physics are vanilla-parity (freeze at
    // <=0C pushes timers by elapsed, hard reset at <=-10C, greenhouse
    // roomness +5C). Ticking while harvestable only serves propagation; the
    // ripen logic guards on populated.
    public class BEApiary : BlockEntity
    {
        // 0 = not armed (waiting for flowers). Game-hours on the calendar.
        double harvestableAtTotalHours;
        double lastCheckedTotalHours;
        double nextSwarmTotalHours;
        int nearbyFlowers;
        int rescanCountdown;        // ticks; not persisted — a rescan on reload is fine
        int roomnessCountdown;      // ticks; roomness rechecked ~every 60s
        float roomness;             // 1 = greenhouse (+5C), vanilla parity
        RoomRegistry roomreg;

        const int FlowerRadius = 8;     // horizontal scan radius
        const int FlowerYRange = 2;     // vertical scan half-height
        const int MinFlowers = 3;       // below this the colony won't ripen
        const int SwarmMinFlowers = 6;  // below this the colony won't propagate

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;

            roomreg = api.ModLoader.GetModSystem<RoomRegistry>();

            // First-tick sanity: a fresh BE (transplant, or legacy save) must
            // not see a bogus "elapsed" spanning the whole calendar, nor try
            // to swarm five seconds after being seeded.
            if (lastCheckedTotalHours <= 0) lastCheckedTotalHours = api.World.Calendar.TotalHours;
            if (nextSwarmTotalHours <= 0) nextSwarmTotalHours = api.World.Calendar.TotalHours + 48.0;

            RegisterGameTickListener(OnServerTick, 5000);
            if (Block?.Variant["type"] == "populated" && harvestableAtTotalHours <= 0)
            {
                TryArm();
            }
        }

        // Called by BlockApiary right after a scoop harvest swaps the column
        // back to populated: clear the stale ripen timestamp and re-enter the
        // flower gate on the next tick.
        public void OnHarvested()
        {
            harvestableAtTotalHours = 0;
            rescanCountdown = 0;
            MarkDirty(false);
        }

        void OnServerTick(float dt)
        {
            // Read the block fresh from the world: this BE survives the
            // populated<->harvestable swaps, and the cached Block property is
            // not guaranteed to refresh across them.
            Block current = Api.World.BlockAccessor.GetBlock(Pos);
            string type = current?.Variant["type"];
            if (type != "populated" && type != "harvestable") return;

            // Greenhouse roomness, refreshed on the slow cadence (vanilla
            // checks it in its skep scan, not every temperature test).
            if (--roomnessCountdown <= 0)
            {
                roomnessCountdown = 12;
                Room room = roomreg?.GetRoomForPosition(Pos);
                float newRoomness = (room != null && room.SkylightCount > room.NonSkylightCount && room.ExitCount == 0) ? 1f : 0f;
                if (newRoomness != roomness)
                {
                    roomness = newRoomness;
                    MarkDirty(false);
                }
            }

            ClimateCondition climate = Api.World.BlockAccessor.GetClimateAt(Pos,
                EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays);
            if (climate == null) return;
            float temp = climate.Temperature + (roomness > 0f ? 5f : 0f);

            double now = Api.World.Calendar.TotalHours;
            double elapsed = now - lastCheckedTotalHours;

            // Frozen colony: push the timers forward by exactly the frozen
            // span so no progress accrues. The armed guard protects the
            // 0-means-not-armed sentinel of the ripen timer.
            if (temp <= 0f)
            {
                if (harvestableAtTotalHours > 0) harvestableAtTotalHours += elapsed;
                nextSwarmTotalHours += elapsed;
            }
            lastCheckedTotalHours = now;

            // True winter: progress is LOST, vanilla-parity reset.
            if (temp <= -10f)
            {
                if (harvestableAtTotalHours > 0)
                {
                    harvestableAtTotalHours = now + 12.0 * (3.0 + Api.World.Rand.NextDouble() * 8.0);
                }
                nextSwarmTotalHours = now + 48.0;
            }

            if (type == "populated")
            {
                if (harvestableAtTotalHours <= 0)
                {
                    // Flower-starved: rescan every ~60s (12 ticks of 5s).
                    if (--rescanCountdown <= 0) TryArm();
                }
                else if (now >= harvestableAtTotalHours && temp > 0f)
                {
                    BlockApiary.SetApiaryType(Api.World, Pos, "harvestable");
                    return; // block swapped under us
                }
            }

            // Tamed propagation: a rich meadow lets the colony populate the
            // nearest empty skep. Runs while populated OR harvestable.
            if (temp > 0f && nearbyFlowers >= SwarmMinFlowers && now >= nextSwarmTotalHours)
            {
                TrySwarm(now);
            }
        }

        // Count beeFeed flora around the hive; arm the ripening timer if the
        // meadow can sustain the colony. Richer meadow = faster combs.
        void TryArm()
        {
            rescanCountdown = 12;
            nearbyFlowers = CountFlowers();
            if (nearbyFlowers < MinFlowers)
            {
                harvestableAtTotalHours = 0;
                MarkDirty(false);
                return;
            }

            // ~1 in-game week; 115-256 game-hour envelope across the clamp.
            float speed = GameMath.Clamp(nearbyFlowers / 8f, 0.75f, 1.25f);
            double hours = (144.0 + Api.World.Rand.NextDouble() * 48.0) / speed;
            harvestableAtTotalHours = Api.World.Calendar.TotalHours + hours;
            MarkDirty(false);
        }

        int CountFlowers()
        {
            int count = 0;
            BlockPos scan = new BlockPos();
            for (int dx = -FlowerRadius; dx <= FlowerRadius; dx++)
            {
                for (int dz = -FlowerRadius; dz <= FlowerRadius; dz++)
                {
                    for (int dy = -FlowerYRange; dy <= FlowerYRange; dy++)
                    {
                        scan.Set(Pos.X + dx, Pos.Y + dy, Pos.Z + dz);
                        Block b = Api.World.BlockAccessor.GetBlock(scan);
                        if (b?.Attributes != null && b.Attributes["beeFeed"].AsBool(false)) count++;
                    }
                }
            }
            return count;
        }

        // Populate the NEAREST empty vanilla skep in the vanilla scan
        // envelope (x/z -8..+7, y -7..+4). Never spawns a beemob — tamed.
        void TrySwarm(double now)
        {
            BlockPos best = null;
            float bestDist = float.MaxValue;
            BlockPos scan = new BlockPos();
            for (int dx = -8; dx <= 7; dx++)
            {
                for (int dy = -7; dy <= 4; dy++)
                {
                    for (int dz = -8; dz <= 7; dz++)
                    {
                        scan.Set(Pos.X + dx, Pos.Y + dy, Pos.Z + dz);
                        Block b = Api.World.BlockAccessor.GetBlock(scan);
                        if (b is BlockSkep && b.Variant["type"] == "empty")
                        {
                            float dist = Pos.DistanceTo(scan);
                            if (dist < bestDist)
                            {
                                bestDist = dist;
                                best = scan.Copy();
                            }
                        }
                    }
                }
            }

            if (best == null)
            {
                // No empty skep in range: recheck in 12 game-hours.
                nextSwarmTotalHours = now + 12.0;
                MarkDirty(false);
                return;
            }

            Block emptySkep = Api.World.BlockAccessor.GetBlock(best);
            Block populated = Api.World.GetBlock(emptySkep.CodeWithVariant("type", "populated"));
            if (populated != null)
            {
                Api.World.BlockAccessor.SetBlock(populated.BlockId, best);
                Api.World.BlockAccessor.MarkBlockDirty(best);
            }
            nextSwarmTotalHours = now + 48.0;
            MarkDirty(false);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("harvestableAtTotalHours", harvestableAtTotalHours);
            tree.SetDouble("lastCheckedTotalHours", lastCheckedTotalHours);
            tree.SetDouble("nextSwarmTotalHours", nextSwarmTotalHours);
            tree.SetInt("nearbyFlowers", nearbyFlowers);
            tree.SetFloat("roomness", roomness);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            harvestableAtTotalHours = tree.GetDouble("harvestableAtTotalHours", 0.0);
            lastCheckedTotalHours = tree.GetDouble("lastCheckedTotalHours", 0.0);
            nextSwarmTotalHours = tree.GetDouble("nextSwarmTotalHours", 0.0);
            nearbyFlowers = tree.GetInt("nearbyFlowers", 0);
            roomness = tree.GetFloat("roomness", 0f);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            Block current = Api.World.BlockAccessor.GetBlock(Pos);
            string type = current?.Variant["type"];
            if (type == "harvestable")
            {
                dsc.AppendLine("The combs hang heavy. Bring the scoop.");
                return;
            }
            if (type != "populated") return;

            // roomness is synced in the BE tree so the client-side info text
            // doesn't claim winter inside a working greenhouse.
            ClimateCondition climate = Api.World.BlockAccessor.GetClimateAt(Pos,
                EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays);
            float temp = (climate == null ? 20f : climate.Temperature) + (roomness > 0f ? 5f : 0f);
            if (temp <= 0f)
            {
                dsc.AppendLine("The colony clusters for warmth. Production is halted.");
                return;
            }

            if (harvestableAtTotalHours <= 0)
            {
                dsc.AppendLine("The bees find too few flowers here. (" + nearbyFlowers + " nearby, needs " + MinFlowers + "+)");
                return;
            }
            double remaining = harvestableAtTotalHours - Api.World.Calendar.TotalHours;
            if (remaining < 0) remaining = 0;
            dsc.AppendLine("Flowers nearby: " + nearbyFlowers);
            dsc.AppendLine("Combs ready in ~" + ((int)remaining) + "h");
        }
    }
}
