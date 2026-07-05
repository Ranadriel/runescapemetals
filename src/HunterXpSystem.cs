using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace RuneScapeForges
{
    // Tracks per-player Hunter XP. Level curve follows the OSRS XP table
    // (Runelite-style): level L requires sum_{i=1..L-1} floor(i + 300 * 2^(i/7)) / 4.
    // Stored on player's WatchedAttributes so it persists across save/load and
    // is visible to client for UI later.
    public class HunterXpSystem : ModSystem
    {
        public const string XpKey = "rsf_hunterxp";
        public const int MaxLevel = 99;
        private static int[] _xpAtLevel;

        public override void Start(ICoreAPI api)
        {
            BuildLevelTable();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.ChatCommands
                .Create("hunter")
                .WithDescription("Show your Hunter level and XP.")
                .RequiresPlayer()
                .HandleWith(args =>
                {
                    var p = args.Caller.Player;
                    int xp = GetXp(p);
                    int lvl = LevelForXp(xp);
                    int next = lvl < MaxLevel ? _xpAtLevel[lvl + 1] : xp;
                    int toNext = Math.Max(0, next - xp);
                    return Vintagestory.API.Common.TextCommandResult.Success(
                        $"Hunter level {lvl} — {xp} XP (next: {toNext} XP to L{lvl + 1})");
                });
        }

        private static void BuildLevelTable()
        {
            _xpAtLevel = new int[MaxLevel + 2];
            _xpAtLevel[1] = 0;
            double points = 0;
            for (int L = 1; L < MaxLevel; L++)
            {
                points += Math.Floor(L + 300.0 * Math.Pow(2.0, L / 7.0));
                _xpAtLevel[L + 1] = (int)Math.Floor(points / 4.0);
            }
            _xpAtLevel[MaxLevel + 1] = int.MaxValue;
        }

        public static int LevelForXp(int xp)
        {
            for (int L = MaxLevel; L >= 1; L--)
                if (xp >= _xpAtLevel[L]) return L;
            return 1;
        }

        public static int GetXp(IPlayer player)
        {
            return player?.Entity?.WatchedAttributes?.GetInt(XpKey, 0) ?? 0;
        }

        public static int GetLevel(IPlayer player) => LevelForXp(GetXp(player));

        public static void AddXp(IPlayer player, int amount, string speciesName)
        {
            if (player?.Entity == null || amount <= 0) return;
            int before = GetXp(player);
            int prevLevel = LevelForXp(before);
            int after = Math.Min(before + amount, _xpAtLevel[MaxLevel] + 200_000_000);
            player.Entity.WatchedAttributes.SetInt(XpKey, after);
            player.Entity.WatchedAttributes.MarkPathDirty(XpKey);
            int newLevel = LevelForXp(after);

            if (player is IServerPlayer sp)
            {
                sp.SendMessage(0, $"You caught a {speciesName}. +{amount} Hunter XP.", Vintagestory.API.Common.EnumChatType.Notification);
                if (newLevel > prevLevel)
                {
                    sp.SendMessage(0, $"Hunter level up! You are now level {newLevel}.", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
        }
    }
}
