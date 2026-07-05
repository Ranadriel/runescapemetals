using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    // Ripening brain of the wooden apiary (Path A+ of the plan): own calendar
    // timer, own lightweight flower gate — bees need flowers, but nothing here
    // touches vanilla BlockEntityBeehive internals. Stateless across block
    // swaps by design: the populated phase arms and ripens; the harvestable
    // phase just sits; harvest swaps back to populated which re-arms fresh.
    public class BEApiary : BlockEntity
    {
        // 0 = not armed (waiting for flowers). Game-hours on the calendar.
        double harvestableAtTotalHours;
        int nearbyFlowers;
        int rescanCountdown;

        const int FlowerRadius = 8;     // horizontal scan radius
        const int FlowerYRange = 2;     // vertical scan half-height
        const int MinFlowers = 3;       // below this the colony won't ripen

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side == EnumAppSide.Server)
            {
                RegisterGameTickListener(OnServerTick, 5000);
                if (Block?.Variant["type"] == "populated" && harvestableAtTotalHours <= 0)
                {
                    TryArm();
                }
            }
        }

        void OnServerTick(float dt)
        {
            if (Block?.Variant["type"] != "populated") return;

            if (harvestableAtTotalHours <= 0)
            {
                // Flower-starved: rescan every ~60s (12 ticks of 5s).
                if (--rescanCountdown <= 0) TryArm();
                return;
            }

            if (Api.World.Calendar.TotalHours >= harvestableAtTotalHours)
            {
                Block ripe = Api.World.GetBlock(Block.CodeWithVariant("type", "harvestable"));
                if (ripe != null) Api.World.BlockAccessor.SetBlock(ripe.Id, Pos);
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

            float speed = GameMath.Clamp(nearbyFlowers / 8f, 0.5f, 1.5f);
            double hours = (36.0 + Api.World.Rand.NextDouble() * 24.0) / speed;
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

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("harvestableAtTotalHours", harvestableAtTotalHours);
            tree.SetInt("nearbyFlowers", nearbyFlowers);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            harvestableAtTotalHours = tree.GetDouble("harvestableAtTotalHours", 0.0);
            nearbyFlowers = tree.GetInt("nearbyFlowers", 0);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            string type = Block?.Variant["type"];
            if (type == "harvestable")
            {
                dsc.AppendLine("The combs hang heavy.");
                return;
            }
            if (type != "populated") return;

            if (harvestableAtTotalHours <= 0)
            {
                dsc.AppendLine("The bees find too few flowers here. (" + nearbyFlowers + " nearby, needs " + MinFlowers + "+)");
                return;
            }
            double remaining = harvestableAtTotalHours - Api.World.Calendar.TotalHours;
            if (remaining < 0) remaining = 0;
            dsc.AppendLine("Flowers nearby: " + nearbyFlowers);
            dsc.AppendLine("Combs ready in ~" + ((int)remaining) + " hours");
        }
    }
}
