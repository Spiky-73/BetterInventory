using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BetterInventory.ItemSearch.BetterGuide;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.IL;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BetterInventory.BetterRecipeList;

public sealed class AvailableMaterialsCountItem : GlobalItem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeListConfig.AvailableMaterialsCount;
    public override void Load() {
        On_Recipe.FindRecipes += HookFindRecipes;
        On_Recipe.CollectItemsToCraftWithFrom += HookCollectItems;
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += il => il.TryEdit(ILModifyStackText, ref UnloadedAvailableMaterialsCountConfig.Instance.itemSlot);
    }

    private static void HookFindRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        if (!canDelayCheck) AvailableMaterialsCount.ResetCollectedMaterials();
        orig(canDelayCheck);
    }
    private static void HookCollectItems(On_Recipe.orig_CollectItemsToCraftWithFrom orig, Player player) {
        orig(player);
        if (player.whoAmI == Main.myPlayer) AvailableMaterialsCount.SetCollectedMaterials();
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (AvailableMaterialsCountConfig.Tooltip) AvailableMaterialsCount.Tooltip_ModifyTooltips(item, tooltips);
    }
    private static void ILModifyStackText(ILContext il) {
        ILCursor cursor = new(il);
        // if (++[true] || item.stack > 1) {
        //     ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, ++[customStack], position + new Vector2(10f, 26f) * inventoryScale, color, 0f, Vector2.Zero, new Vector2(inventoryScale), -1f, inventoryScale);
        // }
        cursor.GotoNext(i => i.MatchLdfld((Item i) => i.DD2Summon));
        cursor.GotoPrev(i => i.SaferMatchCall(typeof(ChatManager), nameof(ChatManager.DrawColorCodedStringWithShadow)));
        cursor.GotoPrev(MoveType.After, i => i.MatchCall((int i) => i.ToString()) && i.Previous.MatchLdflda((Item i) => i.stack));
        cursor.EmitLdarg1().EmitLdarg2().EmitLdarg3();
        cursor.EmitDelegate((string stack, Item[] inv, int context, int slot) => {
            if (!AvailableMaterialsCountConfig.ItemSlot) return stack;
            return AvailableMaterialsCount.ItemStack_ModifyText(inv[slot], context, stack);
        });
        cursor.GotoPrev(i => i.MatchLdflda((Item i) => i.stack));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdfld((Item i) => i.stack));
        cursor.EmitLdarg1().EmitLdarg2().EmitLdarg3();
        cursor.EmitDelegate((int stack, Item[] inv, int context, int slot) => AvailableMaterialsCountConfig.ItemSlot && AvailableMaterialsCount.ShouldDisplayStack(inv[slot], context, out _) ? 2 : stack);
    }
}

public static class AvailableMaterialsCount {

    public static bool ResetCollectedMaterials() => _collectedMaterials = false;
    public static bool SetCollectedMaterials() => _collectedMaterials = true;

    public static bool ShouldDisplayStack(Item item, int context, [MaybeNullWhen(false)] out string text, bool compact = false) {
        text = null;
        if (!(context == ItemSlot.Context.CraftingMaterial || (BetterRecipeListConfig.RecipeTooltip && context == ItemSlot.Context.ChatItem))) return false;
        if (!_collectedMaterials) return false;

        (Recipe? recipe, Item[] tiles, Item[] conditions) = context == ItemSlot.Context.CraftingMaterial ?
            (Main.recipe[Main.availableRecipe[Main.focusRecipe]], RequiredObjectsDisplay._displayedRecipeTiles, RequiredObjectsDisplay._displayedRecipeConditions) :
            RecipeTooltip.GetHoveredRecipeData();
        if (recipe is null) return false;
        var guid = item.UniqueId();
        if (recipe.requiredItem.Exists(i => i.UniqueId() == guid)) {
            long count = recipe.GetMaterialCount(item);
            text = $"{(compact ? Utility.ToMetricString(count) : count)}/{item.stack}";
            return true;
        }
        if (Configs.BetterGuide.RequiredObjectsDisplay) {
            int index;
            bool? met = null;
            if ((index = Array.FindIndex(tiles, t => t.UniqueId() == guid)) >= 0) met = index >= recipe.requiredTile.Count || Main.LocalPlayer.adjTile[recipe.requiredTile[index]];
            if ((index = Array.FindIndex(conditions, c => c.UniqueId() == guid)) >= 0) met = recipe.Conditions[index].Predicate();
            if (met.HasValue) {
                text = compact ?
                    met.Value ? string.Empty : "0/1" :
                    met.Value ? string.Empty : Language.GetTextValue($"{Localization.Keys.UI}.Unmet");
                return true;
            }
        }
        return false;
    }

    private static bool _collectedMaterials;

    public static void Tooltip_ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!ShouldDisplayStack(item, item.tooltipContext, out string? text) || text.Length == 0) return;
        if (item.stack != 1) tooltips[0].Text = tooltips[0].Text[0..^(2 + item.stack.ToString().Length)];
        tooltips[0].Text += $" ({text})";
    }

    public static string ItemStack_ModifyText(Item item, int context, string stack) {
        return ShouldDisplayStack(item, context, out string? text, true) ? text : stack;
    }
}

