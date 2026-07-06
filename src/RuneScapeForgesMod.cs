using Vintagestory.API.Common;

namespace RuneScapeForges
{
    public class RuneScapeForgesMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockApiary", typeof(BlockApiary));
            api.RegisterBlockClass("BlockApiarySection", typeof(BlockApiarySection));
            api.RegisterBlockClass("BlockApiaryPart", typeof(BlockApiaryPart));
            api.RegisterBlockClass("BlockBlastFurnaceController", typeof(BlockBlastFurnaceController));
            api.RegisterBlockClass("BlockArcFurnaceController", typeof(BlockArcFurnaceController));
            api.RegisterBlockClass("BlockMultiblockPart", typeof(BlockMultiblockPart));
            api.RegisterBlockClass("BlockArcFurnaceTransformer", typeof(BlockArcFurnaceTransformer));
            api.RegisterBlockClass("BlockCastingCradle", typeof(BlockCastingCradle));
            api.RegisterBlockClass("BlockMagmaScoop", typeof(BlockMagmaScoop));
            api.RegisterBlockClass("BlockCrucibleMachine", typeof(BlockCrucibleMachine));
            api.RegisterBlockClass("BlockBirdSnare", typeof(BlockBirdSnare));
            api.RegisterBlockClass("BlockBoxTrap", typeof(BlockBoxTrap));
            api.RegisterBlockClass("BlockLaunder", typeof(BlockLaunder));

            api.RegisterItemClass("ItemAvasAttractor", typeof(ItemAvasAttractor));
            api.RegisterItemClass("ItemSmithCodex", typeof(ItemSmithCodex));
            api.RegisterItemClass("ItemScytheTiered", typeof(ItemScytheTiered));
            api.RegisterItemClass("ItemHoeTiered", typeof(ItemHoeTiered));
            api.RegisterItemClass("ItemShearsTiered", typeof(ItemShearsTiered));
            api.RegisterItemClass("ItemAxeTiered", typeof(ItemAxeTiered));

            api.RegisterBlockEntityClass("BEApiary", typeof(BEApiary));
            api.RegisterBlockEntityClass("BEBlastFurnaceController", typeof(BEBlastFurnaceController));
            api.RegisterBlockEntityClass("BEArcFurnaceController", typeof(BEArcFurnaceController));
            api.RegisterBlockEntityClass("BEMultiblockPart", typeof(BEMultiblockPart));
            api.RegisterBlockEntityClass("BECastingCradle", typeof(BECastingCradle));
            api.RegisterBlockEntityClass("CrucibleMachine", typeof(BECrucibleMachine));
            api.RegisterBlockEntityClass("BEBirdSnare", typeof(BEBirdSnare));
            api.RegisterBlockEntityClass("BEBoxTrap", typeof(BEBoxTrap));
            api.RegisterBlockEntityClass("BELaunder", typeof(BELaunder));

            // Bind the codex library to this API so the reader can find its
            // pages under assets/runescape/config/codex/.
            CodexLibrary.Bind(api);
        }

        public override void StartServerSide(Vintagestory.API.Server.ICoreServerAPI sapi)
        {
            // /codexreload — force a re-read of assets/runescape/config/codex/
            // so authoring a page during play doesn't need a game restart.
            // (Kept flat instead of a subcommand — the subcommand builder's
            // fluent chain trips the in-game Roslyn compiler.)
            sapi.ChatCommands.Create("codexreload")
                .WithDescription("Reload Smith's Codex pages from disk")
                .RequiresPrivilege(Vintagestory.API.Server.Privilege.controlserver)
                .HandleWith(args =>
                {
                    CodexLibrary.Invalidate();
                    int n = CodexLibrary.Pages == null ? 0 : CodexLibrary.Pages.Count;
                    return Vintagestory.API.Common.TextCommandResult.Success("Codex reloaded: " + n + " pages");
                });
        }
    }
}
