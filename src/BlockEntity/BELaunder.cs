using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace RuneScapeForges
{
    // The launder block entity. Holds a buffer of molten metal that the
    // tipper above pours into when it's tipping. Magma scoop withdraws from
    // it through the same AcceptMolten / WithdrawMolten contract used by
    // BECrucibleMachine — that's the deliberate symmetry of the closed
    // crucible-economy: every node that holds melt speaks the same protocol.
    //
    // Phase 1 of the launder dance:
    //   - Tipper detects launder in its lip-direction neighbor while tipping
    //   - Tipper drains ~5 units/sec into launder
    //   - Launder buffers up to BufferCapacityUnits (16 L)
    //   - Magma scoop can withdraw from it (existing scoop logic will be
    //     extended in a follow-up to accept BELaunder as a source)
    //
    // Phase 2 will add the launder → pots flow (deposit into ground-stored
    // vanilla clay crucibles via BlockSmeltingContainer's contents API).
    public class BELaunder : BlockEntity
    {
        // 16 L buffer — matches the steel tipper's molten capacity at the
        // low end. Plenty for a single-tipper pour cycle without overflow.
        const int BufferCapacityUnits = 1600;

        string moltenMetalType = "";
        int moltenUnits;

        public string MoltenMetalType => moltenMetalType;
        public int MoltenUnits => moltenUnits;
        public int CapacityUnits => BufferCapacityUnits;

        public int AcceptMolten(string metalType, int units)
        {
            if (string.IsNullOrEmpty(metalType) || units <= 0) return 0;
            if (!string.IsNullOrEmpty(moltenMetalType) && moltenMetalType != metalType) return 0;

            int free = BufferCapacityUnits - moltenUnits;
            if (free <= 0) return 0;

            int accepted = System.Math.Min(units, free);
            moltenMetalType = metalType;
            moltenUnits += accepted;
            MarkDirty(true);
            return accepted;
        }

        public int WithdrawMolten(int units)
        {
            if (units <= 0 || moltenUnits <= 0) return 0;
            int taken = System.Math.Min(units, moltenUnits);
            moltenUnits -= taken;
            if (moltenUnits == 0) moltenMetalType = "";
            MarkDirty(true);
            return taken;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("moltenMetalType", moltenMetalType ?? "");
            tree.SetInt("moltenUnits", moltenUnits);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            moltenMetalType = tree.GetString("moltenMetalType", "") ?? "";
            moltenUnits = tree.GetInt("moltenUnits");
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (moltenUnits > 0 && !string.IsNullOrEmpty(moltenMetalType))
            {
                float litres = moltenUnits / 100f;
                float capLitres = BufferCapacityUnits / 100f;
                dsc.Append("Holds molten ");
                dsc.Append(char.ToUpper(moltenMetalType[0]));
                dsc.Append(moltenMetalType.Substring(1));
                dsc.Append(": ");
                dsc.Append(litres.ToString("0.##"));
                dsc.Append(" / ");
                dsc.Append(capLitres.ToString("0.##"));
                dsc.AppendLine(" L");
            }
        }
    }
}
