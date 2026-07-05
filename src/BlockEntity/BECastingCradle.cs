using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RuneScapeForges
{
    public class BECastingCradle : BlockEntity
    {
        // animUtil is supplied by the Animatable BE behavior declared in the
        // blocktype JSON. Its constructor runs in BEBehaviorAnimatable.Initialize.
        BlockEntityAnimationUtil animUtil =>
            GetBehavior<BEBehaviorAnimatable>()?.animUtil;

        AnimationMetaData tipAnimMeta = new AnimationMetaData
        {
            Animation = "tip",
            Code = "tip",
            AnimationSpeed = 1f,
            EaseInSpeed = 1f,
            EaseOutSpeed = 1f
        };

        bool isTipping;

        // Packet ids for client→server (start/stop request) and server→client
        // (state broadcast). Match the fruitpress numbering convention.
        const int PacketReqStart = 1010;
        const int PacketReqStop  = 1011;
        const int PacketDoStart  = 1012;
        const int PacketDoStop   = 1013;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            // Load the shape's animator on both sides so animations resolve.
            // BEBehaviorAnimatable already constructed animUtil; we just kick
            // off the actual animator with the block's own shape.
            float rotY = Block?.Shape?.rotateY ?? 0f;
            if (api.Side == EnumAppSide.Client)
            {
                animUtil?.InitializeAnimator("rsf-castingcradle", null, null, new Vec3f(0f, rotY, 0f));
            }
            else
            {
                ((AnimationUtil)animUtil)?.InitializeAnimatorServer("rsf-castingcradle", null);
            }
        }

        // ─── Interaction lifecycle ────────────────────────────────────────────

        public bool OnInteractStart(IPlayer byPlayer)
        {
            if (!isTipping)
            {
                if (Api.Side == EnumAppSide.Client)
                {
                    // Ask server to authorize the start. Server will broadcast back.
                    ((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos, PacketReqStart);
                }
                // Optimistic local start so the player sees no latency.
                StartTipAnimLocal();
            }
            return true;
        }

        public bool OnInteractStep(float secondsUsed, IPlayer byPlayer)
        {
            // Keep ticking while RMB is held.
            return isTipping;
        }

        public void OnInteractStop(float secondsUsed, IPlayer byPlayer)
        {
            if (isTipping)
            {
                if (Api.Side == EnumAppSide.Client)
                {
                    ((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos, PacketReqStop);
                }
                StopTipAnimLocal();
            }
        }

        // ─── Animation control helpers ────────────────────────────────────────

        void StartTipAnimLocal()
        {
            isTipping = true;
            animUtil?.StartAnimation(tipAnimMeta);
        }

        void StopTipAnimLocal()
        {
            isTipping = false;
            // Shape's onActivityStopped = "Rewind" handles the return motion
            // smoothly without an explicit reverse-keyframe sequence.
            animUtil?.StopAnimation("tip");
        }

        // ─── Network sync ─────────────────────────────────────────────────────

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(fromPlayer, packetid, data);

            if (packetid == PacketReqStart)
            {
                StartTipAnimLocal();
                MarkDirty(true);
                BroadcastToOtherClients(PacketDoStart, fromPlayer);
            }
            else if (packetid == PacketReqStop)
            {
                StopTipAnimLocal();
                MarkDirty(true);
                BroadcastToOtherClients(PacketDoStop, fromPlayer);
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);

            if (packetid == PacketDoStart) StartTipAnimLocal();
            else if (packetid == PacketDoStop) StopTipAnimLocal();
        }

        void BroadcastToOtherClients(int packetid, IPlayer excludePlayer)
        {
            var sapi = Api as Vintagestory.API.Server.ICoreServerAPI;
            if (sapi == null) return;
            foreach (var player in sapi.World.AllOnlinePlayers)
            {
                if (player == excludePlayer) continue;
                var sp = player as Vintagestory.API.Server.IServerPlayer;
                if (sp != null) sapi.Network.SendBlockEntityPacket(sp, Pos, packetid);
            }
        }

        // ─── Persistence ──────────────────────────────────────────────────────

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("isTipping", isTipping);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            bool was = isTipping;
            isTipping = tree.GetBool("isTipping");

            // Client just rejoined / chunk loaded with the BE mid-tip — sync the
            // visual animation to the persisted state.
            if (Api?.Side == EnumAppSide.Client && was != isTipping)
            {
                if (isTipping) animUtil?.StartAnimation(tipAnimMeta);
                else animUtil?.StopAnimation("tip");
            }
        }
    }
}
