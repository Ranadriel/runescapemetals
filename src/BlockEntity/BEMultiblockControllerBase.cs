using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    public abstract class BEMultiblockControllerBase : BlockEntity
    {
        public bool IsValid { get; protected set; }
        public string InvalidReason { get; protected set; } = "";
        public Vec3i InvalidOffset { get; protected set; } = new Vec3i(0, 0, 0);

        protected abstract MultiblockSlot[] GetStructureLayout();
        protected abstract string StructureDisplayName();

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side == EnumAppSide.Server)
            {
                RegisterGameTickListener(OnTick, 1000);
            }
        }

        private void OnTick(float dt)
        {
            RevalidateStructure();
        }

        public bool RevalidateStructure()
        {
            var facing = GetControllerFacing();
            foreach (var slot in GetStructureLayout())
            {
                var rotated = RotateOffset(slot.Offset, facing);
                var worldPos = Pos.AddCopy(rotated.X, rotated.Y, rotated.Z);
                var b = Api.World.BlockAccessor.GetBlock(worldPos);
                if (!slot.Matcher(b))
                {
                    bool changed = IsValid || InvalidReason != slot.Role;
                    IsValid = false;
                    InvalidReason = slot.Role;
                    InvalidOffset = rotated;
                    if (changed) MarkDirty(true);
                    return false;
                }
            }
            bool wasInvalid = !IsValid;
            IsValid = true;
            InvalidReason = "";
            if (wasInvalid) MarkDirty(true);
            return true;
        }

        protected BlockFacing GetControllerFacing()
        {
            var side = Block?.Variant["side"];
            if (side == null) return BlockFacing.NORTH;
            var f = BlockFacing.FromCode(side);
            return f ?? BlockFacing.NORTH;
        }

        // Offsets are written assuming controller faces NORTH (looking -Z, with +Z = into structure).
        // Rotate them to match the controller's actual facing.
        protected static Vec3i RotateOffset(Vec3i offset, BlockFacing facing)
        {
            int x = offset.X, y = offset.Y, z = offset.Z;
            if (facing == BlockFacing.NORTH) return new Vec3i(x, y, z);
            if (facing == BlockFacing.EAST)  return new Vec3i(-z, y, x);
            if (facing == BlockFacing.SOUTH) return new Vec3i(-x, y, -z);
            if (facing == BlockFacing.WEST)  return new Vec3i(z, y, -x);
            return new Vec3i(x, y, z);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("isValid", IsValid);
            tree.SetString("invalidReason", InvalidReason);
            tree.SetInt("invalidOffsetX", InvalidOffset.X);
            tree.SetInt("invalidOffsetY", InvalidOffset.Y);
            tree.SetInt("invalidOffsetZ", InvalidOffset.Z);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            IsValid = tree.GetBool("isValid");
            InvalidReason = tree.GetString("invalidReason", "");
            InvalidOffset = new Vec3i(
                tree.GetInt("invalidOffsetX"),
                tree.GetInt("invalidOffsetY"),
                tree.GetInt("invalidOffsetZ"));
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (IsValid)
            {
                dsc.AppendLine($"{StructureDisplayName()}: structure VALID");
            }
            else
            {
                dsc.AppendLine($"{StructureDisplayName()}: incomplete — missing {InvalidReason} at offset {InvalidOffset}");
            }
        }
    }
}
