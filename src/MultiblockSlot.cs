using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RuneScapeForges
{
    public class MultiblockSlot
    {
        public Vec3i Offset;
        public Predicate<Block> Matcher;
        public string Role;

        public MultiblockSlot(int dx, int dy, int dz, Predicate<Block> matcher, string role)
        {
            Offset = new Vec3i(dx, dy, dz);
            Matcher = matcher;
            Role = role;
        }
    }
}
