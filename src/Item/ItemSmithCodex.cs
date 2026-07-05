using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace RuneScapeForges
{
    // The Smith's Codex — a held tome that opens a unified browser into every
    // subsystem the runescapemetals mod ships: metals ladder, crucible system,
    // smithing, fletching, hunter, integrations. Right-click while holding to
    // open; navigate the sections with the left-side buttons.
    public class ItemSmithCodex : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (!firstEvent || byEntity.World.Side != EnumAppSide.Client)
            {
                handHandling = EnumHandHandling.PreventDefault;
                return;
            }

            var capi = byEntity.Api as ICoreClientAPI;
            if (capi != null)
            {
                var dialog = new GuiDialogSmithCodex(capi);
                if (!dialog.TryOpen())
                {
                    // already open — toggle closed
                    dialog.TryClose();
                }
            }
            handHandling = EnumHandHandling.PreventDefault;
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            return Lang.Get("item-smithcodex");
        }
    }
}
