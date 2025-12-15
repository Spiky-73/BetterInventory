using System;
using MonoMod.Cil;
using SpikysLib.Constants;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.Improvements.SmartConsumption;

public sealed class SmartConsumptionItem : GlobalItem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || SmartConsumptionConfig.Enabled;
    public override void Load() {
        IL_Player.ItemCheck_CheckFishingBobber_PickAndConsumeBait += il => il.TryEdit(ILOnConsumeBait, ref UnloadedSmartConsumptionConfig.Instance.baits);
        IL_Recipe.ConsumeForCraft += static il => il.TryEdit(ILOnConsumedMaterial, ref UnloadedSmartConsumptionConfig.Instance.materials);
    }

    public override void OnConsumeItem(Item item, Player player) {
        if (!SmartConsumptionConfig.Enabled) return;
        if (item.PaintOrCoating) {
            if (SmartConsumptionConfig.Paints) SmartConsumption.SmartConsume(player, item, SmartConsumption.LastStack);
        } else {
            if (SmartConsumptionConfig.Consumables) SmartConsumption.SmartConsume(player, item, SmartConsumption.SmallestStack);
        }
    }

    public override void OnConsumedAsAmmo(Item ammo, Item weapon, Player player) {
        if (!SmartConsumptionConfig.Enabled) return;
        if (SmartConsumptionConfig.Ammo) SmartConsumption.SmartConsume(player, ammo, SmartConsumption.LastStack);
    }

    private static void ILOnConsumedMaterial(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int consumed, i => i.Previous.SaferMatchCallvirt((Item i) => i.Clone), 0);

        cursor.GotoNext(MoveType.Before, i => i.MatchLdsfld(() => RecipeLoader.ConsumedItems));
        cursor.EmitLdarg1();
        cursor.EmitLdloc(consumed);
        cursor.EmitDelegate((Item item, Item consumed) => {
            if (!SmartConsumptionConfig.Enabled) return;
            if (SmartConsumptionConfig.Materials) SmartConsumption.SmartConsume(Main.LocalPlayer, item, SmartConsumption.SmallestStack, consumed.stack, new(true, SmartConsumptionConfig.Instance.mouse));
        });
    }

    private static void ILOnConsumeBait(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int i, i => i.Previous.MatchLdcI4(-1), 0);

        cursor.GotoNext(i => i.SaferMatchCall(() => NPC.LadyBugKilled));
        cursor.GotoNext(MoveType.After, i => i.MatchStfld((Item i ) => i.stack));
        cursor.EmitLdarg0();
        cursor.EmitLdloc(i);
        cursor.EmitDelegate((Player self, int i) => {
            if (!SmartConsumptionConfig.Enabled) return;
            if (SmartConsumptionConfig.Baits) SmartConsumption.SmartConsume(self, self.inventory[i], SmartConsumption.LastStack);
        });
    }
}

public static class SmartConsumption {
    public static Item? LastStack(Player player, Item item, StackPickerSettings settings) {
        bool Check(Item i) => item.type == i.type && (settings.CanPickArg || i != item);

        for (int i = InventorySlots.Items.End - 1; i >= InventorySlots.Items.Start; i--) if (Check(player.inventory[i])) return player.inventory[i];
        for (int i = InventorySlots.Ammo.End - 1; i >= InventorySlots.Coins.Start; i--) if (Check(player.inventory[i])) return player.inventory[i];
        if (settings.CanPickMouse && Check(player.inventory[InventorySlots.Mouse])) return player.inventory[InventorySlots.Mouse];
        return null;
    }

    public static Item? SmallestStack(Player player, Item item, StackPickerSettings settings) {
        Item? min = null;
        void Check(Item i) {
            if (item.type == i.type && (min is null || i.stack < min.stack) && (settings.CanPickArg || i != item)) min = i;
        }

        for (int i = InventorySlots.Items.End - 1; i >= InventorySlots.Items.Start; i--) Check(player.inventory[i]);
        for (int i = InventorySlots.Ammo.End - 1; i >= InventorySlots.Coins.Start; i--) Check(player.inventory[i]);
        if (settings.CanPickMouse) Check(player.inventory[InventorySlots.Mouse]);
        return min;
    }

    public delegate Item? StackPickerFn(Player player, Item item, StackPickerSettings settings);
    public static void SmartConsume(Player player, Item item, StackPickerFn stackPicker, int consumed = 1, StackPickerSettings? settings = null) {
        if (!SmartConsumptionConfig.Instance.mouse && (item == Main.mouseItem || item == player.inventory[InventorySlots.Mouse])) return;
        settings ??= new(SmartConsumptionConfig.Instance.self, SmartConsumptionConfig.Instance.mouse);
        while (consumed > 0) {
            Item? i = stackPicker(player, item, settings.Value);
            if (i == null) return;
            int amount = Math.Min(consumed, i.stack);
            item.stack += amount;
            i.stack -= amount;
            if (player.whoAmI == Main.myPlayer) {
                if (item == player.inventory[InventorySlots.Mouse]) Main.mouseItem.stack += amount;
                if (i == player.inventory[InventorySlots.Mouse]) Main.mouseItem.stack -= amount;
            }
            consumed -= amount;
            if (i.stack == 0) i.TurnToAir();
        }
    }
}

public record struct StackPickerSettings(bool CanPickArg, bool CanPickMouse);