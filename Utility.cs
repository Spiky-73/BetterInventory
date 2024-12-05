using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BetterInventory.ItemSearch;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.Constants;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory;

[Flags] public enum AllowedItems : byte { None = 0b00, Self = 0b01, Mouse = 0b10}

public static class Utility {

    public static int FailedILs { get; private set; }

    public static Item? LastStack(this Player player, Item item, AllowedItems allowedItems = AllowedItems.None) {
        bool Check(Item i) => item.type == i.type && (allowedItems.HasFlag(AllowedItems.Self) || i != item);

        for (int i = InventorySlots.Items.End - 1; i >= InventorySlots.Items.Start; i--) if (Check(player.inventory[i])) return player.inventory[i];
        for (int i = InventorySlots.Ammo.End-1; i >= InventorySlots.Coins.Start; i--) if (Check(player.inventory[i])) return player.inventory[i];
        if (allowedItems.HasFlag(AllowedItems.Mouse) && Check(player.inventory[InventorySlots.Mouse])) return player.inventory[InventorySlots.Mouse];
        return null;
    }

   public static Item? SmallestStack(this Player player, Item item, AllowedItems allowedItems = AllowedItems.None) {
        Item? min = null;
        void Check(Item i) {
            if (item.type == i.type && (min is null || i.stack < min.stack) && (allowedItems.HasFlag(AllowedItems.Self) || i != item)) min = i;
        }

        for (int i = InventorySlots.Items.End - 1; i >= InventorySlots.Items.Start; i--) Check(player.inventory[i]);
        for (int i = InventorySlots.Ammo.End - 1; i >= InventorySlots.Coins.Start; i--) Check(player.inventory[i]);
        if (allowedItems.HasFlag(AllowedItems.Mouse)) Check(player.inventory[InventorySlots.Mouse]);
        return min;
    }

    public static bool ApplyTo(this ILContext context, Action<ILContext> ilEdit, bool enabled, [CallerArgumentExpression(nameof(ilEdit))] string name = "") {
        Mod mod = ModContent.GetInstance<BetterInventory>();
        if (Configs.Compatibility.CompatibilityMode && !enabled) {
            mod.Logger.Info($"ILHook {name} was not loaded. Related features will be disabled until reload");
            return false;
        }
        try {
            ilEdit(context);
        } catch {
            FailedILs++;
            mod.Logger.Warn($"ILHook {name} failed to load. Related features will be disabled until reload");
            MonoModHooks.DumpIL(mod, context);
            return false;
        }
        return true;
    }

    public static int GetMouseFreeSpace(Item item) => GetFreeSpace(Main.mouseItem, item);
    public static int GetInventoryFreeSpace(Player player, Item item) {
        int free = 0;
        foreach(ModSubInventory inv in InventoryLoader.SubInventories) {
            if (PlayerHelper.InventoryContexts.Contains(inv.Context)) free += GetFreeSpace(inv.Items(player), item);
        }
        return free;
    }
    public static int GetFreeSpace(IList<Item> inv, Item item, params int[] ignored) {
        int free = 0;
        for (int i = 0; i < inv.Count; i++) {
            if (Array.IndexOf(ignored, i) == -1) free += GetFreeSpace(inv[i], item);
        }
        return free;
    }
    public static int GetFreeSpace(Item test, Item item) {
        if (test.IsAir) return item.maxStack;
        if (test.type == item.type) return item.maxStack - test.stack;
        return 0;
    }

    public static ILCursor GotoRecipeDraw(this ILCursor cursor) {
        // bool flag10 = Main.CreativeMenu.Enabled && !Main.CreativeMenu.Blocked;
        // flag10 |= Main.hidePlayerCraftingMenu;
        // if (!Main.InReforgeMenu && !Main.LocalPlayer.tileEntityAnchor.InUse && !flag10) {
        //     UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = -1;
        //     UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = -1;
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.hidePlayerCraftingMenu));
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentRecipeSmall));
        return cursor;
    }

    public static ILCursor GotoRecipeDisabled(ILCursor cursor, out ILLabel endLoop, out int index, out int recipe) {
        cursor.GotoNextLoc(out index, i => i.Previous.MatchLdcI4(0), 1);
        // for (<recipeIndex>) {
        //     ...
        //     if (recipe.Disabled) continue;
        cursor.GotoNext(i => i.MatchCallvirt(Reflection.Recipe.Disabled.GetMethod!));
        int r = 0;
        recipe = cursor.TryFindPrev(out _, i => i.MatchLdloc(out r)) ? r : 2;
        ILLabel end = null!;
        cursor.GotoNext(i => i.MatchBrtrue(out end!));
        cursor.GotoNext(MoveType.AfterLabel);
        endLoop = end;
        return cursor;
    }

    public static long GetMaterialCount(this Recipe recipe, Item item) {
        int group = recipe.acceptedGroups.FindIndex(g => RecipeGroup.recipeGroups[g].IconicItemId == item.type);
        return PlayerHelper.OwnedItems.GetValueOrDefault(group == -1 ? item.type : RecipeGroup.recipeGroups[recipe.acceptedGroups[group]].GetGroupFakeItemId());
    }

    public static string ToMetricString(this double number, int digits = 4) {
        int power = number == 0 ? 0 : (int)Math.Log10(number);
        if (power < digits) return number.ToString();
        string prefix;
        if (power <= MetricPrefixes.Length * 3 - 1) {
            prefix = MetricPrefixes[power / 3];
            power = power / 3 * 3;
        } else {
            prefix = $"e{power}";
        }
        if (power > 0) number /= Math.Pow(10, power);

        string str = number.ToString();
        str = str[0..Math.Min(str.Length, Math.Max(1, digits-prefix.Length))];
        if (str[^1] == '.') str = str[0..^1];
        return $"{str}{prefix}";
    }

    public static readonly string[] MetricPrefixes = [string.Empty, "k", "M", "G", "T", "P"];
}
