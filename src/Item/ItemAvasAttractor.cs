using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace RuneScapeForges
{
    // Ava's attractor — shoulder-worn ammo recovery device (see docs/DESIGN_avas_attractor.md).
    // This class only handles the Commune toggle and tooltip; the working mechanics live
    // server-side in AvasAttractorSystem.
    public class ItemAvasAttractor : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity?.Controls?.Sneak == true && slot?.Itemstack != null)
            {
                bool wasOff = slot.Itemstack.Attributes.GetBool("communeOff", false);
                slot.Itemstack.Attributes.SetBool("communeOff", !wasOff);
                slot.MarkDirty();

                if (api.Side == EnumAppSide.Server && (byEntity as EntityPlayer)?.Player is IServerPlayer splr)
                {
                    splr.SendMessage(GlobalConstants.GeneralChatGroup,
                        wasOff
                            ? Lang.Get("The device hums to life — junk attraction on.")
                            : Lang.Get("The device quiets down — junk attraction muted."),
                        EnumChatType.Notification);
                }

                handling = EnumHandHandling.PreventDefault;
                return;
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            bool off = inSlot?.Itemstack?.Attributes?.GetBool("communeOff", false) ?? false;
            float chance = Attributes?["avasattractor"]?["recoveryChance"]?.AsFloat(0.6f) ?? 0.6f;

            dsc.AppendLine(Lang.Get("Worn on the shoulders, it draws fired ammunition back to its owner ({0}%).", (int)(chance * 100)));
            dsc.AppendLine(Lang.Get("Periodically attracts stray iron oddments. Currently: {0}", off ? Lang.Get("muted") : Lang.Get("attracting")));
            dsc.AppendLine(Lang.Get("Inert while metal torso armor is worn."));
            dsc.AppendLine(Lang.Get("Sneak + right click while held to commune with the device."));
        }
    }
}
