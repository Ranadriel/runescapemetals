using System;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace RuneScapeSpecials
{
    [ProtoContract]
    public class SpecActivatePacket { }

    public class RSSpecsMod : ModSystem
    {
        public const string EnergyKey = "rsSpecEnergy";
        public const float MaxEnergy = 100f;
        public const float RegenPerSecond = 100f / 30f;

        ICoreServerAPI sapi;
        IClientNetworkChannel cch;

        public override void Start(ICoreAPI api)
        {
            api.Network.RegisterChannel("rsspecs").RegisterMessageType<SpecActivatePacket>();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            cch = api.Network.GetChannel("rsspecs");
            api.Input.RegisterHotKey("rsspec", "Special Attack", GlKeys.R, HotkeyType.CharacterControls);
            api.Input.SetHotKeyHandler("rsspec", OnSpecKey);
            api.Gui.RegisterDialog(new HudSpecBar(api));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            api.Network.GetChannel("rsspecs").SetMessageHandler<SpecActivatePacket>(OnSpecActivate);
            api.Event.RegisterGameTickListener(RegenTick, 1000);
            api.Event.PlayerJoin += (p) => {
                if (!p.Entity.WatchedAttributes.HasAttribute(EnergyKey))
                    p.Entity.WatchedAttributes.SetFloat(EnergyKey, MaxEnergy);
            };
        }

        bool OnSpecKey(KeyCombination kc)
        {
            cch.SendPacket(new SpecActivatePacket());
            return true;
        }

        void OnSpecActivate(IServerPlayer player, SpecActivatePacket p)
        {
            var slot = player.InventoryManager.ActiveHotbarSlot;
            var code = slot?.Itemstack?.Collectible?.Code;
            if (code == null) return;
            string path = code.Path;
            if (!path.EndsWith("-dragon") && !path.EndsWith("dragon")) return;

            int cost = 0;
            string spec = null;
            if (path.StartsWith("sword-")) { cost = 25; spec = "cleave"; }
            else if (path.StartsWith("metalclaws")) { cost = 50; spec = "sliceanddice"; }
            else if (path.StartsWith("pickaxe-") || path.StartsWith("prospectingpick-")) { cost = 50; spec = "slam"; }
            else if (path.StartsWith("axe-")) { cost = 100; spec = "timber"; }
            else if (path.StartsWith("hoe-")) { cost = 50; spec = "tilling"; }
            else if (path.StartsWith("scythe-")) { cost = 75; spec = "reaping"; }
            else if (path.StartsWith("shovel-")) { cost = 50; spec = "earthworks"; }
            else if (path.StartsWith("scimitar-")) { cost = 25; spec = "sever"; }
            else if (path.StartsWith("mace-")) { cost = 25; spec = "shatter"; }
            else if (path.StartsWith("battleaxe-")) { cost = 100; spec = "rampage"; }
            else if (path.StartsWith("halberd-")) { cost = 30; spec = "sweep"; }
            else return;

            float e = player.Entity.WatchedAttributes.GetFloat(EnergyKey, 0f);
            if (e < cost) {
                player.SendIngameError("nospec", "Not enough special energy");
                return;
            }
            player.Entity.WatchedAttributes.SetFloat(EnergyKey, e - cost);
            DispatchSpec(player, spec);
        }

        void DispatchSpec(IServerPlayer player, string spec)
        {
            var ent = player.Entity;
            var pos = ent.ServerPos.XYZ.AddCopy(0, ent.LocalEyePos.Y * 0.6, 0);

            switch (spec)
            {
                case "cleave":
                    SpecBurst(ent, pos, 220, 255, 200, 60, 30);
                    SpecHit(player, pos, 3.5f, 14f, EnumDamageType.SlashingAttack, 1);
                    BroadcastTitle(player, "Cleave!");
                    break;
                case "sliceanddice":
                    BroadcastTitle(player, "Slice & Dice!");
                    for (int i = 0; i < 4; i++)
                    {
                        int idx = i;
                        sapi.Event.RegisterCallback(_ => {
                            if (!ent.Alive) return;
                            var ppos = ent.ServerPos.XYZ.AddCopy(0, ent.LocalEyePos.Y * 0.6, 0);
                            SpecBurst(ent, ppos, 200, 240, 255, 80, 14);
                            SpecHit(player, ppos, 3.0f, 5f, EnumDamageType.PiercingAttack, 1);
                        }, 120 * idx);
                    }
                    break;
                case "slam":
                    BroadcastTitle(player, "Slam!");
                    SpecBurst(ent, pos, 255, 220, 120, 200, 60);
                    DoSlam(player);
                    break;
                case "timber":
                    BroadcastTitle(player, "Timber!");
                    SpecBurst(ent, pos, 180, 220, 140, 200, 60);
                    DoTimber(player);
                    break;
                case "tilling":
                    BroadcastTitle(player, "Tilling Burst!");
                    SpecBurst(ent, pos, 200, 160, 90, 200, 80);
                    DoTilling(player);
                    break;
                case "reaping":
                    BroadcastTitle(player, "Grim Harvest!");
                    SpecBurst(ent, pos, 220, 220, 220, 200, 100);
                    DoReaping(player);
                    break;
                case "earthworks":
                    BroadcastTitle(player, "Earthworks!");
                    SpecBurst(ent, pos, 180, 140, 90, 200, 80);
                    DoEarthworks(player);
                    break;
                case "sever":
                    BroadcastTitle(player, "Sever!");
                    SpecBurst(ent, pos, 180, 60, 80, 180, 30);
                    SpecHit(player, pos, 3.5f, 18f, EnumDamageType.SlashingAttack, 1);
                    break;
                case "shatter":
                    BroadcastTitle(player, "Shatter!");
                    SpecBurst(ent, pos, 220, 200, 160, 200, 40);
                    SpecHit(player, pos, 3.0f, 16f, EnumDamageType.BluntAttack, 2);
                    break;
                case "rampage":
                    BroadcastTitle(player, "Rampage!");
                    SpecBurst(ent, pos, 230, 80, 50, 220, 90);
                    SpecHit(player, pos, 3.5f, 32f, EnumDamageType.SlashingAttack, 3);
                    break;
                case "sweep":
                    BroadcastTitle(player, "Sweep!");
                    SpecBurst(ent, pos, 200, 230, 220, 200, 80);
                    SpecHit(player, ent.ServerPos.XYZ, 5.5f, 14f, EnumDamageType.SlashingAttack, 1);
                    break;
            }
        }

        void SpecHit(IServerPlayer player, Vec3d center, float range, float damage, EnumDamageType dmgType, int knockback)
        {
            var ent = player.Entity;
            foreach (var e in sapi.World.GetEntitiesAround(center, range, range, x => x != ent && x.Alive && x is EntityAgent))
            {
                e.ReceiveDamage(new DamageSource {
                    Source = EnumDamageSource.Entity,
                    SourceEntity = ent,
                    CauseEntity = ent,
                    Type = dmgType,
                    KnockbackStrength = knockback
                }, damage);
            }
        }

        void SpecBurst(Entity ent, Vec3d pos, int r, int g, int b, int a, float quantity)
        {
            ent.World.SpawnParticles(
                quantity,
                ColorUtil.ToRgba(a, r, g, b),
                pos.SubCopy(0.4, 0.4, 0.4),
                pos.AddCopy(0.4, 0.4, 0.4),
                new Vec3f(-0.6f, 0.1f, -0.6f),
                new Vec3f(0.6f, 0.8f, 0.6f),
                1.0f,
                -0.05f,
                0.25f,
                EnumParticleModel.Quad,
                null);
        }

        void BroadcastTitle(IServerPlayer player, string text)
        {
            player.SendMessage(GlobalConstants.GeneralChatGroup, text, EnumChatType.Notification);
        }

        BlockPos GetTargetPos(IServerPlayer player)
        {
            var sel = player.CurrentBlockSelection;
            if (sel != null) return sel.Position.Copy();
            var ent = player.Entity;
            return new BlockPos((int)ent.ServerPos.X, (int)ent.ServerPos.Y - 1, (int)ent.ServerPos.Z);
        }

        BlockFacing GetTargetFace(IServerPlayer player)
        {
            return player.CurrentBlockSelection?.Face ?? BlockFacing.UP;
        }

        void DoSlam(IServerPlayer player)
        {
            var origin = GetTargetPos(player);
            var face = GetTargetFace(player);
            var ba = sapi.World.BlockAccessor;
            for (int u = -1; u <= 1; u++)
            for (int v = -1; v <= 1; v++)
            {
                var p = origin.Copy();
                if (face == BlockFacing.UP || face == BlockFacing.DOWN) { p.X += u; p.Z += v; }
                else if (face == BlockFacing.NORTH || face == BlockFacing.SOUTH) { p.X += u; p.Y += v; }
                else { p.Z += u; p.Y += v; }
                if (ba.GetBlock(p).BlockId == 0) continue;
                ba.BreakBlock(p, player, 1.0f);
            }
        }

        void DoTimber(IServerPlayer player)
        {
            var origin = GetTargetPos(player);
            var ba = sapi.World.BlockAccessor;
            var startBlock = ba.GetBlock(origin);
            string sCode = startBlock?.Code?.Path ?? "";
            if (!IsLog(sCode))
            {
                var up = origin.UpCopy();
                if (IsLog(ba.GetBlock(up)?.Code?.Path)) origin = up;
                else return;
            }

            var queue = new System.Collections.Generic.Queue<BlockPos>();
            var seen = new System.Collections.Generic.HashSet<long>();
            queue.Enqueue(origin);
            seen.Add(((long)origin.X * 73856093L) ^ ((long)origin.Y * 19349663L) ^ ((long)origin.Z * 83492791L));
            int budget = 256;
            while (queue.Count > 0 && budget-- > 0)
            {
                var p = queue.Dequeue();
                var b = ba.GetBlock(p);
                if (b == null) continue;
                string code = b.Code?.Path ?? "";
                if (!IsLog(code) && !IsLeaves(code)) continue;
                ba.BreakBlock(p, player, IsLog(code) ? 1.0f : 0.5f);
                if (!IsLog(code)) continue;
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;
                    var n = new BlockPos(p.X + dx, p.Y + dy, p.Z + dz);
                    long key = ((long)n.X * 73856093L) ^ ((long)n.Y * 19349663L) ^ ((long)n.Z * 83492791L);
                    if (seen.Contains(key)) continue;
                    seen.Add(key);
                    queue.Enqueue(n);
                }
            }
        }

        bool IsLog(string code) {
            if (string.IsNullOrEmpty(code)) return false;
            return code.StartsWith("log-") || code.StartsWith("logsection-") || code.Contains("-log-") || code.StartsWith("treetrunk-");
        }
        bool IsLeaves(string code) {
            if (string.IsNullOrEmpty(code)) return false;
            return code.StartsWith("leaves-") || code.StartsWith("leavesbranchy-") || code.Contains("-leaves-");
        }

        void DoTilling(IServerPlayer player)
        {
            var origin = GetTargetPos(player);
            var ba = sapi.World.BlockAccessor;
            for (int dx = -2; dx <= 2; dx++)
            for (int dz = -2; dz <= 2; dz++)
            {
                var p = new BlockPos(origin.X + dx, origin.Y, origin.Z + dz);
                var b = ba.GetBlock(p);
                if (b == null) continue;
                string code = b.Code?.Path ?? "";
                if (!code.StartsWith("soil-")) continue;
                if (ba.GetBlock(p.UpCopy()).BlockId != 0) continue;
                var farmBlock = sapi.World.GetBlock(new AssetLocation("farmland-dry-" + ExtractSoilFertility(code)));
                if (farmBlock != null) ba.SetBlock(farmBlock.BlockId, p);
            }
        }

        string ExtractSoilFertility(string soilCode) {
            string[] tiers = { "verylow", "low", "medium", "compost", "high" };
            foreach (var t in tiers) if (soilCode.Contains(t)) return t;
            return "medium";
        }

        void DoReaping(IServerPlayer player)
        {
            var origin = GetTargetPos(player);
            var ba = sapi.World.BlockAccessor;
            int r = 7;
            for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            for (int dy = -1; dy <= 2; dy++)
            {
                if (dx * dx + dz * dz > r * r) continue;
                var p = new BlockPos(origin.X + dx, origin.Y + dy, origin.Z + dz);
                var b = ba.GetBlock(p);
                if (b == null || b.BlockId == 0) continue;
                string code = b.Code?.Path ?? "";
                if (code.StartsWith("tallgrass") || code.StartsWith("tallplant") || code.StartsWith("flower-")
                    || code.StartsWith("crop-") || code.StartsWith("herb-") || code.StartsWith("mushroom-")
                    || code.StartsWith("smallberrybush-") || code.StartsWith("bigberrybush-"))
                {
                    ba.BreakBlock(p, player, 1.0f);
                }
            }
        }

        void DoEarthworks(IServerPlayer player)
        {
            var origin = GetTargetPos(player);
            var face = GetTargetFace(player);
            var ba = sapi.World.BlockAccessor;
            for (int u = -1; u <= 1; u++)
            for (int v = -1; v <= 1; v++)
            {
                var p = origin.Copy();
                if (face == BlockFacing.UP || face == BlockFacing.DOWN) { p.X += u; p.Z += v; }
                else if (face == BlockFacing.NORTH || face == BlockFacing.SOUTH) { p.X += u; p.Y += v; }
                else { p.Z += u; p.Y += v; }
                var b = ba.GetBlock(p);
                if (b == null || b.BlockId == 0) continue;
                string code = b.Code?.Path ?? "";
                if (code.StartsWith("soil-") || code.StartsWith("sand-") || code.StartsWith("gravel-") || code.StartsWith("clay-") || code.StartsWith("peat-"))
                    ba.BreakBlock(p, player, 1.0f);
            }
        }

        void RegenTick(float dt)
        {
            foreach (var ipl in sapi.World.AllOnlinePlayers)
            {
                var ent = (ipl as IServerPlayer)?.Entity;
                if (ent == null) continue;
                float e = ent.WatchedAttributes.GetFloat(EnergyKey, 0f);
                if (e >= MaxEnergy) continue;
                e = Math.Min(MaxEnergy, e + RegenPerSecond * dt);
                ent.WatchedAttributes.SetFloat(EnergyKey, e);
            }
        }
    }

    public class HudSpecBar : HudElement
    {
        public override double DrawOrder => 0.91;
        public override EnumDialogType DialogType => EnumDialogType.HUD;

        const int BarWidth = 350;
        const int BarHeight = 12;
        const int Notches = 10;
        static readonly double[] Teal = { 43 / 255.0, 164 / 255.0, 164 / 255.0, 1.0 };

        float lastEnergy = -1f;

        public HudSpecBar(ICoreClientAPI capi) : base(capi)
        {
            Compose();
            capi.Event.RegisterGameTickListener(OnTick, 100);
        }

        void OnTick(float dt)
        {
            float e = capi.World.Player.Entity.WatchedAttributes.GetFloat(RSSpecsMod.EnergyKey, 0f);
            if (Math.Abs(e - lastEnergy) > 0.1f)
            {
                lastEnergy = e;
                Compose();
                TryOpen();
            }
        }

        void Compose()
        {
            ElementBounds dialog = ElementStdBounds.Statbar(EnumDialogArea.CenterBottom, BarWidth)
                .WithFixedAlignmentOffset(BarWidth - 102, -90)
                .WithFixedSize(BarWidth, BarHeight);

            ElementBounds inner = ElementBounds.Fixed(0, 0, BarWidth, BarHeight);
            float energy = lastEnergy < 0 ? 0 : lastEnergy;

            SingleComposer = capi.Gui
                .CreateCompo("rsspecbar", dialog)
                .AddStaticCustomDraw(inner, (ctx, surface, eb) => DrawBar(ctx, energy))
                .Compose();
        }

        void DrawBar(Cairo.Context ctx, float energy)
        {
            ctx.SetSourceRGBA(0, 0, 0, 0.55);
            ctx.Rectangle(0, 0, BarWidth, BarHeight);
            ctx.Fill();

            double frac = Math.Max(0, Math.Min(1, energy / RSSpecsMod.MaxEnergy));
            ctx.SetSourceRGBA(Teal[0], Teal[1], Teal[2], Teal[3]);
            ctx.Rectangle(1, 1, (BarWidth - 2) * frac, BarHeight - 2);
            ctx.Fill();

            ctx.SetSourceRGBA(0, 0, 0, 0.85);
            ctx.LineWidth = 1.0;
            for (int i = 1; i < Notches; i++)
            {
                double x = BarWidth / (double)Notches * i;
                ctx.MoveTo(x, 0);
                ctx.LineTo(x, BarHeight);
                ctx.Stroke();
            }

            ctx.SetSourceRGBA(0, 0, 0, 1);
            ctx.Rectangle(0.5, 0.5, BarWidth - 1, BarHeight - 1);
            ctx.Stroke();
        }

        public override bool ShouldReceiveKeyboardEvents() { return false; }
    }
}
