using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace RuneScapeForges
{
    // The metal crucible machine BE (steel/adamantite/dragon tiers).
    //
    // Responsibilities, in order:
    //   1. Hold the inventory (fuel slot + N ingredient slots, N from the
    //      blocktype's cookingContainerSlots attribute).
    //   2. Drive the cook tick: when fuel is present, raise internal temp
    //      toward maxHeatableTemp; cool when fuel absent. Apply current
    //      temp to all ingredient slots each tick.
    //   3. Detect smelt completion: when every ingredient slot's contents
    //      have reached their melting point AND match a single-metal smelt
    //      output, convert the ingredients into a molten metal payload via
    //      AcceptMolten and clear the inventory slots.
    //   4. Hold the molten metal payload (single metal type, unit count).
    //      Magma scoop withdraws and deposits through AcceptMolten/WithdrawMolten.
    //   5. Run the tip animation while RMB is held on the block.
    //   6. Open the inventory dialog on sneak+RMB.
    public class BECrucibleMachine : BlockEntityOpenableContainer
    {
        // ─── Inventory ────────────────────────────────────────────────────────
        // Slot 0 = fuel; slots 1..N = ingredients.
        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;
        public override string InventoryClassName => "crucible";

        // ─── Mesh + animation ─────────────────────────────────────────────────
        MeshData meshDrum;
        bool tipping;
        AnimationMetaData tipAnimMeta = new AnimationMetaData
        {
            Animation = "tip",
            Code = "tip",
            AnimationSpeed = 1f,
            EaseInSpeed = 2f,
            EaseOutSpeed = 2f
        };
        BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>()?.animUtil;

        // ─── Cook state ───────────────────────────────────────────────────────
        // Internal furnace temperature (°C). Advances toward maxHeatableTemp
        // while fuel slot has burnable contents; decays toward ambient otherwise.
        float currentTemp = 20f;
        // Per-fuel-unit burn budget in seconds. When > 0, consuming the fuel
        // slot is in-flight. When it ticks below zero, pull another unit.
        float fuelBurnSecondsLeft;

        const float HeatRiseDegPerSec = 25f;   // how fast temp climbs with fuel
        const float HeatFallDegPerSec = 6f;    // how fast it cools without
        const float AmbientTemp = 20f;
        const float SecondsPerFuelUnit = 12f;  // a unit of fuel = 12 cook-seconds

        // Server-side cook tick handle, registered in Initialize.
        long tickListenerId;

        // ─── Molten metal storage (closed crucible-to-crucible economy) ──────
        string moltenMetalType = "";
        int moltenUnits;
        public string MoltenMetalType => moltenMetalType;
        public int MoltenUnits => moltenUnits;

        int MoltenCapacityUnits
        {
            get
            {
                int slots = Block?.Attributes?["cookingContainerSlots"]?.AsInt(16) ?? 16;
                return slots * 100;
            }
        }

        int CookingSlotsCount => Block?.Attributes?["cookingContainerSlots"]?.AsInt(16) ?? 16;
        float MaxHeatableTemp => Block?.Attributes?["maxHeatableTemp"]?.AsFloat(1600f) ?? 1600f;

        // Superheater connector: an adjacent block carrying the attribute
        // "crucibleHeatBonus" raises the reachable ceiling. Only the strongest
        // single neighbor counts — superheaters do not stack.
        float SuperheaterBonus
        {
            get
            {
                if (Api == null) return 0f;
                float best = 0f;
                BlockFacing[] faces = BlockFacing.HORIZONTALS;
                for (int i = 0; i < faces.Length; i++)
                {
                    BlockPos npos = Pos.AddCopy(faces[i]);
                    Block nb = Api.World.BlockAccessor.GetBlock(npos);
                    float bonus = nb?.Attributes?["crucibleHeatBonus"]?.AsFloat(0f) ?? 0f;
                    if (bonus > best) best = bonus;
                }
                return best;
            }
        }

        float EffectiveMaxTemp => MaxHeatableTemp + SuperheaterBonus;

        // ─── Ctor / Init ──────────────────────────────────────────────────────

        public BECrucibleMachine()
        {
            // Inventory is initialized to a default 17-slot layout here so
            // FromTreeAttributes (which may run before Initialize the FIRST
            // time the BE is materialized from disk) has a target to populate.
            // Initialize() resizes it to match the blocktype's declared count
            // once Block is available.
            inv = new InventoryGeneric(17, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            // Resize inventory now that we know the tier (slot 0 + N cooking).
            int totalSlots = 1 + CookingSlotsCount;
            if (inv == null || inv.Count != totalSlots)
            {
                var oldInv = inv;
                inv = new InventoryGeneric(totalSlots, null, null);
                if (oldInv != null)
                {
                    int n = System.Math.Min(oldInv.Count, totalSlots);
                    for (int i = 0; i < n; i++) inv[i] = oldInv[i];
                }
            }

            base.Initialize(api);

            // Drum mesh + animator
            Shape shape = Shape.TryGet(api, "runescape:shapes/block/crucible-large-drum.json");
            if (shape != null && api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = (ICoreClientAPI)api;
                capi.Tesselator.TesselateShape(Block, shape, out meshDrum, new Vec3f(0, Block.Shape.rotateY, 0));
                animUtil?.InitializeAnimator("runescapemetals-cruciblemachine", shape, null, new Vec3f(0, Block.Shape.rotateY, 0));
                if (tipping) startClientAnim();
            }

            // Server-side cook driver — 4 ticks per second is plenty.
            if (api.Side == EnumAppSide.Server)
            {
                tickListenerId = RegisterGameTickListener(OnCookTick, 250);
            }
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (tickListenerId != 0) UnregisterGameTickListener(tickListenerId);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (tickListenerId != 0) UnregisterGameTickListener(tickListenerId);
        }

        // ─── Interaction ──────────────────────────────────────────────────────

        // Called by BlockCrucibleMachine on sneak+RMB to open the inventory.
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                toggleInventoryDialogClient(byPlayer, () =>
                {
                    invDialog = new GuiDialogBlockEntityInventory(
                        InventoryClassName, Inventory, Pos,
                        1 + CookingSlotsCount,
                        (ICoreClientAPI)Api);
                    return invDialog;
                });
            }
            return true;
        }

        // Tip animation control (kept from prior implementation).
        public void OnTipStart()
        {
            if (tipping) return;
            tipping = true;
            if (Api.Side == EnumAppSide.Client) startClientAnim();
            else MarkDirty(false);
        }

        public void OnTipStop()
        {
            if (!tipping) return;
            tipping = false;
            if (Api.Side == EnumAppSide.Client) stopClientAnim();
            else MarkDirty(false);
        }

        void startClientAnim()
        {
            if (animUtil?.animator?.GetAnimationState("tip")?.Running != true)
                animUtil?.StartAnimation(tipAnimMeta);
        }

        void stopClientAnim() => animUtil?.StopAnimation("tip");

        // ─── Cook driver ──────────────────────────────────────────────────────

        void OnCookTick(float dt)
        {
            // Heat advancement: fuel slot drives temperature toward max.
            ItemSlot fuelSlot = inv[0];
            bool hasFuel = fuelSlot?.Itemstack != null
                          && (fuelSlot.Itemstack.Collectible.CombustibleProps?.BurnDuration ?? 0f) > 0f;

            float maxTemp = EffectiveMaxTemp;
            if (hasFuel && currentTemp < maxTemp)
            {
                // Burn fuel proportionally to time spent in this tick.
                fuelBurnSecondsLeft -= dt;
                if (fuelBurnSecondsLeft <= 0f)
                {
                    // Consume one fuel item, top up the burn budget.
                    fuelSlot.TakeOut(1);
                    fuelSlot.MarkDirty();
                    fuelBurnSecondsLeft += SecondsPerFuelUnit;
                }
                currentTemp = System.Math.Min(maxTemp,
                    currentTemp + HeatRiseDegPerSec * dt);
                MarkDirty(false);
            }
            else if (currentTemp > AmbientTemp)
            {
                currentTemp = System.Math.Max(AmbientTemp,
                    currentTemp - HeatFallDegPerSec * dt);
                MarkDirty(false);
            }

            // Push temperature onto every ingredient stack so vanilla
            // "is melted" semantics work for cross-mod inspection.
            for (int i = 1; i < inv.Count; i++)
            {
                ItemSlot slot = inv[i];
                if (slot?.Itemstack == null) continue;
                slot.Itemstack.Collectible.SetTemperature(Api.World, slot.Itemstack, currentTemp, false);
            }

            // Smelt-eligibility check + landing.
            TryAdvanceSmelt();

            // Pour pipeline: while the drum is tipping AND has melt AND there's
            // a launder in our lip-direction neighbor, drain into the launder.
            if (tipping && moltenUnits > 0)
            {
                TryDrainToLaunder(dt);
            }
        }

        // ─── Pour pipeline ───────────────────────────────────────────────────
        // Rate-limited drain from the tipper drum into a launder positioned in
        // the lip-direction neighbor. ~5 units per second = 1 ingot every 20s,
        // matching the "watch it pour" cadence the design doc calls for.
        const float PourUnitsPerSec = 5f;
        float pendingPourFractional;

        void TryDrainToLaunder(float dt)
        {
            // Phase 1 assumes the drum's lip faces north (-Z) — matches the
            // crucible-large shape's modeled lip orientation. When per-block
            // placement rotation lands in a later patch, swap this for the
            // block's stored mesh angle.
            var launderPos = Pos.NorthCopy(1);
            var be = Api.World.BlockAccessor.GetBlockEntity(launderPos) as BELaunder;
            if (be == null) return;

            pendingPourFractional += PourUnitsPerSec * dt;
            if (pendingPourFractional < 1f) return;

            int units = (int)pendingPourFractional;
            pendingPourFractional -= units;
            if (units > moltenUnits) units = moltenUnits;

            int accepted = be.AcceptMolten(moltenMetalType, units);
            if (accepted > 0) WithdrawMolten(accepted);
        }

        // Convert ingredients to molten metal when every loaded slot has
        // reached its melting point and a single-metal smelt output exists.
        void TryAdvanceSmelt()
        {
            // Gather ingredient stacks (skipping slot 0 / fuel and empty slots).
            ItemStack[] ingredients = new ItemStack[CookingSlotsCount];
            bool anyIngredient = false;
            bool allReady = true;

            for (int i = 0; i < CookingSlotsCount; i++)
            {
                ItemSlot slot = inv[i + 1];
                ItemStack stack = slot?.Itemstack;
                if (stack == null) continue;

                anyIngredient = true;
                ingredients[i] = stack;

                // Already-cooked-item guard reuses the static helper.
                if (BlockCrucibleMachine.IsAlreadyCooked(stack))
                {
                    return; // refuse cook outright; user has bad input loaded
                }

                var combust = stack.Collectible.CombustibleProps;
                float meltPoint = combust?.MeltingPoint ?? float.MaxValue;
                if (currentTemp < meltPoint) allReady = false;
            }

            if (!anyIngredient || !allReady) return;

            // Collapse ingredients to a single ItemStack[] of non-nulls for
            // vanilla's GetSingleSmeltableStack call.
            var nonNull = new System.Collections.Generic.List<ItemStack>();
            foreach (var s in ingredients) if (s != null) nonNull.Add(s);
            if (nonNull.Count == 0) return;

            MatchedSmeltableStack match = BlockSmeltingContainer.GetSingleSmeltableStack(nonNull.ToArray());
            if (match == null || match.output == null) return;

            // Extract metal type from the output stack (matches vanilla pattern).
            string metalName = BlockSmeltingContainer.GetMetal(match.output);
            if (string.IsNullOrEmpty(metalName)) return;

            // Capacity check before clearing inputs — refuse the land if it
            // would overflow molten storage. The player must scoop some out
            // before the next cook completes.
            int unitsToDeposit = (int)System.Math.Round(match.stackSize * 100.0, 0);
            int wouldExceed = (moltenUnits + unitsToDeposit) - MoltenCapacityUnits;
            if (wouldExceed > 0) return; // try again next tick; storage is full

            int accepted = AcceptMolten(metalName, unitsToDeposit);
            if (accepted <= 0) return; // mismatched metal already inside

            // Clear ingredient slots — they have been transformed.
            for (int i = 1; i < inv.Count; i++)
            {
                inv[i].Itemstack = null;
                inv[i].MarkDirty();
            }
            MarkDirty(true);
        }

        // ─── Molten storage API (called by BlockMagmaScoop + cook driver) ────

        public int AcceptMolten(string metalType, int units)
        {
            if (string.IsNullOrEmpty(metalType) || units <= 0) return 0;
            if (!string.IsNullOrEmpty(moltenMetalType) && moltenMetalType != metalType) return 0;

            int free = MoltenCapacityUnits - moltenUnits;
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

        // ─── Rendering ────────────────────────────────────────────────────────

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (!base.OnTesselation(mesher, tessThreadTesselator) && meshDrum != null)
                mesher.AddMeshData(meshDrum);
            return false;
        }

        // ─── Persistence ──────────────────────────────────────────────────────

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("tipping", tipping);
            tree.SetString("moltenMetalType", moltenMetalType ?? "");
            tree.SetInt("moltenUnits", moltenUnits);
            tree.SetFloat("currentTemp", currentTemp);
            tree.SetFloat("fuelBurnSecondsLeft", fuelBurnSecondsLeft);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            bool was = tipping;
            tipping = tree.GetBool("tipping");
            moltenMetalType = tree.GetString("moltenMetalType", "") ?? "";
            moltenUnits = tree.GetInt("moltenUnits");
            currentTemp = tree.GetFloat("currentTemp", AmbientTemp);
            fuelBurnSecondsLeft = tree.GetFloat("fuelBurnSecondsLeft", 0f);

            if (Api != null && Api.Side == EnumAppSide.Client && was != tipping)
            {
                if (tipping) startClientAnim();
                else stopClientAnim();
            }
        }

        // ─── Block info readout ───────────────────────────────────────────────

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            dsc.AppendLine();
            dsc.Append("Temperature: ");
            dsc.Append(currentTemp.ToString("0"));
            dsc.Append("°C / max ");
            dsc.Append(EffectiveMaxTemp.ToString("0"));
            dsc.AppendLine("°C");

            float shBonus = SuperheaterBonus;
            if (shBonus > 0f)
            {
                dsc.Append("Superheater attached: +");
                dsc.Append(shBonus.ToString("0"));
                dsc.AppendLine("°C");
            }

            if (moltenUnits > 0 && !string.IsNullOrEmpty(moltenMetalType))
            {
                float litres = moltenUnits / 100f;
                float capLitres = MoltenCapacityUnits / 100f;
                dsc.Append("Molten ");
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
