using System;
using System.Collections.Generic;
using MonoMod.Cil;
using SpikysLib.Extensions;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.InventoryManagement;

public sealed class SmartConsumptionItem : GlobalItem {

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

    internal static void ILOnConsumedMaterial(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.Before, i => i.MatchLdsfld(Reflection.RecipeLoader.ConsumedItems));
        cursor.EmitLdarg1();
        cursor.EmitLdloc0();
        cursor.EmitDelegate((Item item, Item consumed) => {
            if (Configs.SmartConsumption.Materials) SmartConsume(Main.LocalPlayer, item, () => Main.LocalPlayer.SmallestStack(item, AllowedItems.Self | Configs.SmartConsumption.Mouse), consumed.stack);
        });
    }

    internal static void ILOnConsumeBait(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.NPC.LadyBugKilled));
        cursor.GotoNext(MoveType.After, i => i.MatchStfld(Reflection.Item.stack));
        cursor.EmitLdarg0();
        cursor.EmitLdloc1();
        cursor.EmitDelegate((Player self, Item item) => {
            if (Configs.SmartConsumption.Baits) SmartConsume(self, item, () => self.LastStack(item, Configs.SmartConsumption.Mouse));
        });
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        QuickMove.AddMoveChainLine(item, tooltips);
    }

    public static void SmartConsume(Player player, Item item, Func<Item?> stackPicker, int consumed = 1) {
        while (consumed > 0) {
            Item? i = stackPicker();
            if (i == null) return;
            int amount = Math.Min(consumed, i.stack);
            item.stack += amount;
            i.stack -= amount;
            if(player.whoAmI == Main.myPlayer) {
                if (item == player.inventory[58]) Main.mouseItem.stack += amount;
                if (i == player.inventory[58]) Main.mouseItem.stack -= amount;
            }
            consumed -= amount;
            if (i.stack == 0) i.TurnToAir();
        }
    }
}
