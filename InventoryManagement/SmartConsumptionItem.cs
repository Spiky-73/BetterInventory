using System;
using System.Collections.Generic;
using BetterInventory.ItemActions;
using MonoMod.Cil;
using SpikysLib.Constants;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.InventoryManagement;

public sealed class SmartConsumptionItem : GlobalItem {

    public override void Load() {
        IL_Player.ItemCheck_CheckFishingBobber_PickAndConsumeBait += static il => {
            if(!il.ApplyTo(ILOnConsumeBait, Configs.SmartConsumption.Baits)) Configs.UnloadedInventoryManagement.Value.baits = true;

        };
        IL_Recipe.ConsumeForCraft += static il => {
            if (!il.ApplyTo(ILOnConsumedMaterial, Configs.SmartConsumption.Materials)) Configs.UnloadedInventoryManagement.Value.materials = true;
        };

    }
    public override void OnConsumeItem(Item item, Player player) {
        if (item.PaintOrCoating) {
            if (Configs.SmartConsumption.Paints) SmartConsume(player, item, () => player.LastStack(item, Configs.SmartConsumption.Mouse));
        } else {
            if (Configs.SmartConsumption.Consumables) SmartConsume(player, item, () => player.SmallestStack(item, Configs.SmartConsumption.Mouse));
        }
    }

    public override void OnConsumedAsAmmo(Item ammo, Item weapon, Player player) {
        if (Configs.SmartConsumption.Ammo) SmartConsume(player, ammo, () => player.LastStack(ammo, Configs.SmartConsumption.Mouse));
    }

    private static void ILOnConsumedMaterial(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int consumed, i => i.Previous.SaferMatchCallvirt(Reflection.Item.Clone), 0);

        cursor.GotoNext(MoveType.Before, i => i.MatchLdsfld(Reflection.RecipeLoader.ConsumedItems));
        cursor.EmitLdarg1();
        cursor.EmitLdloc(consumed);
        cursor.EmitDelegate((Item item, Item consumed) => {
            if (Configs.SmartConsumption.Materials) SmartConsume(Main.LocalPlayer, item, () => Main.LocalPlayer.SmallestStack(item, AllowedItems.Self | Configs.SmartConsumption.Mouse), consumed.stack);
        });
    }

    private static void ILOnConsumeBait(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int i, i => i.Previous.MatchLdcI4(-1), 0);

        cursor.GotoNext(i => i.SaferMatchCall(Reflection.NPC.LadyBugKilled));
        cursor.GotoNext(MoveType.After, i => i.MatchStfld(Reflection.Item.stack));
        cursor.EmitLdarg0();
        cursor.EmitLdloc(i);
        cursor.EmitDelegate((Player self, int i) => {
            if (Configs.SmartConsumption.Baits) SmartConsume(self, self.inventory[i], () => self.LastStack(self.inventory[i], Configs.SmartConsumption.Mouse));
        });
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        Crafting.Crafting.AddAvailableMaterials(item, tooltips);
        QuickMove.AddMoveChainLine(item, tooltips);
        ClickOverrides.AddCraftStackLine(item, tooltips);
        BetterPlayer.AddBagContentTooltips(item, tooltips);
    }

    public static void SmartConsume(Player player, Item item, Func<Item?> stackPicker, int consumed = 1) {
        while (consumed > 0) {
            Item? i = stackPicker();
            if (i == null) return;
            int amount = Math.Min(consumed, i.stack);
            item.stack += amount;
            i.stack -= amount;
            if(player.whoAmI == Main.myPlayer) {
                if (item == player.inventory[InventorySlots.Mouse]) Main.mouseItem.stack += amount;
                if (i == player.inventory[InventorySlots.Mouse]) Main.mouseItem.stack -= amount;
            }
            consumed -= amount;
            if (i.stack == 0) i.TurnToAir();
        }
    }
}
