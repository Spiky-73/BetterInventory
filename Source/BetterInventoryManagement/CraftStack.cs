using System;
using System.Collections.Generic;
using BetterInventory.CrossMod;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.IL;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

public sealed class CraftStackItem : GlobalItem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.CraftStack;
    public override void Load() {
        On_ItemSlot.HandleShopSlot += HookBuyStack;
        On_Main.CraftItem += HookCraftStack;
        bool skip = false; // Not saved as unloaded as if this is the case, this mean there is no findrecipes to skip
        IL_Recipe.Create += il => il.TryEdit(ILSkipFindRecipes, ref skip);
    }


    private static void HookBuyStack(On_ItemSlot.orig_HandleShopSlot orig, Item[] inv, int slot, bool rightClickIsValid, bool leftClickIsValid) {
        if (!BetterInventoryManagementConfig.CraftStack) {
            orig(inv, slot, rightClickIsValid, leftClickIsValid);
            return;
        }

        if (!(CraftStackConfig.Instance.invertClicks ? rightClickIsValid : leftClickIsValid) || Main.stackSplit > 1) {
            orig(inv, slot, rightClickIsValid, leftClickIsValid);
            return;
        }

        if (!CraftStackConfig.Instance.repeat) {
            if (CraftStackConfig.Instance.invertClicks) rightClickIsValid &= Main.mouseRightRelease;
            else leftClickIsValid &= Main.mouseLeftRelease;
        }


        var stackSplit = Main.stackSplit;
        (var superFastStack, Main.superFastStack) = (Main.superFastStack, ((Main.superFastStack + 1) * GetShopMultiplier(inv[slot], Main.mouseItem)) - 1);
        orig(inv, slot, rightClickIsValid, leftClickIsValid);
        Main.superFastStack = superFastStack;
        if (Main.stackSplit > 1) {
            Main.stackSplit = stackSplit;
            ItemSlot.RefreshStackSplitCooldown();
        }
    }

    private static void HookCraftStack(On_Main.orig_CraftItem orig, Recipe r) {
        if (!BetterInventoryManagementConfig.CraftStack) {
            orig(r);
            return;
        }

        if (!(CraftStackConfig.Instance.invertClicks ? Main.mouseRight : Main.mouseLeft)) {
            orig(r);
            return;
        }

        if (!CraftStackConfig.Instance.repeat) Main.LockCraftingForThisCraftClickDuration();

        var multiplier = GetCraftMultiplier(r, Main.mouseItem);
        _skipFindRecipes = true;
        for (int i = 0; i < multiplier; i++) {
            orig(r);
        }
        _skipFindRecipes = false;
        Recipe.FindRecipes();
    }
    private static void ILSkipFindRecipes(ILContext il) {
        // ...
        // AchievementsHelper.NotifyItemCraft(this);
        // AchievementsHelper.NotifyItemPickup(Main.player[Main.myPlayer], createItem);
        // ++ if (!<craftStacking>) {
        //     FindRecipes();
        // ++ }
        // return;
        ILCursor cursor = new(il);
        cursor.GotoNext(i => i.MatchCall(() => Recipe.FindRecipes));
        cursor.GotoPrev(MoveType.Before, i => i.MatchLdcI4(0));
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitDelegate(() => _skipFindRecipes);
        cursor.EmitBrtrue(skip);
        cursor.MarkLabel(skip);
        cursor.GotoNext(MoveType.After, i => i.MatchCall(() => Recipe.FindRecipes));
        cursor.MarkLabel(skip);
    }
    private static bool _skipFindRecipes;


    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!BetterInventoryManagementConfig.CraftStack || !CraftStackConfig.Instance.tooltip) return;
        bool recipe;
        if (item.tooltipContext == ItemSlot.Context.CraftingMaterial && item.UniqueId() == Main.recipe[Main.availableRecipe[Main.focusRecipe]].createItem.UniqueId()) recipe = true;
        else if (item.tooltipContext == ItemSlot.Context.ShopItem) recipe = false;
        else return;

        int amount = recipe ?
            GetCraftMultiplier(Main.recipe[Main.availableRecipe[Main.focusRecipe]], Main.mouseItem) * Main.recipe[Main.availableRecipe[Main.focusRecipe]].createItem.stack :
            GetShopMultiplier(item, Main.mouseItem);
        if (amount == 0) return;
        tooltips.Add(new(
            Mod, "CraftStack",
            Language.GetTextValue($"{Localization.Keys.UI}.CraftStackTooltip",
            Lang.SupportGlyphs(CraftStackConfig.Instance.invertClicks ? "<right>" : "<left>"),
            Language.GetTextValue($"{Localization.Keys.UI}.{(recipe ? "Craft" : "Buy")}"), amount))
        );
    }

    public static int GetMaxCraftStackAmount(Item item) {
        if (CraftStackConfig.Instance.maxItems.Key != 0 || !SpysInfiniteConsumablesIntegration.Enabled) return CraftStackConfig.Instance.maxItems.Key.amount;
        if (SpysInfiniteConsumablesIntegration.GetItemRequirement(item) == 0) return 99;
        return SpysInfiniteConsumablesIntegration.GetItemInfinity(Main.LocalPlayer, item) == 0 ?
            (int)SpysInfiniteConsumablesIntegration.GetCountToInfinity(Main.LocalPlayer, item) :
            (int)SpysInfiniteConsumablesIntegration.GetItemRequirement(item);
    }
    public static int GetMaxBuyMultiplier(Item item, long price) {
        if (price == 0) return item.maxStack;
        else return (int)Math.Max(Main.LocalPlayer.CountCurrency(item.shopSpecialCurrency) / price, 1);
    }
    public static int GetMaxCraftMultiplier(Recipe recipe) {
        Dictionary<int, int> groupItems = [];
        foreach (int id in recipe.acceptedGroups) {
            RecipeGroup group = RecipeGroup.recipeGroups[id];
            groupItems.Add(group.IconicItemId, group.GetGroupFakeItemId());
        }

        int amount = 0;
        foreach (Item material in recipe.requiredItem) {
            int a = Recipe._ownedItems.GetValueOrDefault(groupItems.GetValueOrDefault(material.type, material.type), 0) / material.stack;
            if (amount == 0 || a < amount) amount = a;
        }
        return amount;
    }
    public static int GetFreeSpace(Item destination, Item item) {
        if (destination.IsAir) return item.maxStack;
        if (destination.type == item.type) return item.maxStack - destination.stack;
        return 0;
    }

    public static int GetCraftMultiplier(Recipe recipe, Item destinationItem) {
        int ToMultiplier(int amount) => (CraftStackConfig.Instance.maxItems.Value.above ? (amount + recipe.createItem.stack - 1) : amount) / recipe.createItem.stack;

        int craft = Math.Clamp(GetMaxCraftMultiplier(recipe), 0, ToMultiplier(recipe.createItem.maxStack));
        if (craft > 0) craft = Math.Max(1, Math.Min(craft, ToMultiplier(GetMaxCraftStackAmount(recipe.createItem))));

        int mouse = ToMultiplier(GetFreeSpace(destinationItem, recipe.createItem));
        return Math.Min(craft, mouse);
    }
    public static int GetShopMultiplier(Item item, Item destination, long? price = null) {
        long p;
        if (price.HasValue) p = price.Value;
        else Main.LocalPlayer.GetItemExpectedPrice(item, out long _, out p);

        int buy = Math.Clamp(GetMaxBuyMultiplier(item, p), 0, item.buyOnce ? item.stack : item.maxStack);
        if (buy > 0) buy = Math.Clamp(buy, 1, GetMaxCraftStackAmount(item));

        int mouse = GetFreeSpace(destination, item);
        return Math.Min(buy, mouse);
    }
}