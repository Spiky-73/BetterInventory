using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BetterInventory.InventoryManagement.SmartPickup;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.Constants;
using SpikysLib.IL;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace BetterInventory;

[Flags] public enum AllowedItems : byte { None = 0b00, Self = 0b01, Mouse = 0b10 }

public static class Utility {

    public static int? CompareHandleNullable<T>(T? x, T? y) {
        if (x is null && y is null) return 0;
        if (x is not null && y is null) return 1;
        if (x is null && y is not null) return -1;
        return null;
    }

    public static void ClearMouseText() {
        Main.HoverItem = new();
        Reflection.Main._mouseTextCache.SetValue(Main.instance, Activator.CreateInstance(Reflection.Main.MouseTextCache));
    }

    public static Item GetItem_Inner(Player self, int plr, Item newItem, GetItemSettings settings) {
        SmartPickup.vanillaGetItem = true;
        Item i = self.GetItem(plr, newItem, settings);
        SmartPickup.vanillaGetItem = false;
        return i;
    }

    public static int FailedILs { get; private set; }

    public static Item? LastStack(this Player player, Item item, AllowedItems allowedItems = AllowedItems.None) {
        bool Check(Item i) => item.type == i.type && (allowedItems.HasFlag(AllowedItems.Self) || i != item);

        for (int i = InventorySlots.Items.End - 1; i >= InventorySlots.Items.Start; i--) if (Check(player.inventory[i])) return player.inventory[i];
        for (int i = InventorySlots.Ammo.End - 1; i >= InventorySlots.Coins.Start; i--) if (Check(player.inventory[i])) return player.inventory[i];
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
    public static int GetInventoryFreeSpace(Player player, Item item)
        => InventoryLoader.GetInventories(player)
            .Where(si => PlayerHelper.InventoryContexts.Contains(si.Context))
            .Select(si => GetFreeSpace(si.Items, item))
            .Sum();

    public static int GetFreeSpace(IList<Item> inv, Item item) => inv.Select(i => GetFreeSpace(i, item)).Sum();
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
        str = str[0..Math.Min(str.Length, Math.Max(1, digits - prefix.Length))];
        if (str[^1] == '.') str = str[0..^1];
        return $"{str}{prefix}";
    }

    public static int GetPrioritizedStepIndex<TEntryType, TStepType>(this EntrySorter<TEntryType, TStepType> sorter) where TEntryType : new() where TStepType : IEntrySortStep<TEntryType> {
        return Reflection.EntrySorter<TEntryType, TStepType>._prioritizedStep.GetValue(sorter);
    }

    public static readonly string[] MetricPrefixes = [string.Empty, "k", "M", "G", "T", "P"];
}
