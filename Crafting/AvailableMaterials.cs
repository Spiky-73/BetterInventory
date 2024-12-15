using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BetterInventory.ItemActions;
using BetterInventory.ItemSearch.BetterGuide;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.IL;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BetterInventory.Crafting;

public sealed class AvailableMaterialsItem : GlobalItem {


    public override void Load() {
        On_Recipe.FindRecipes += HookFindRecipes;
        On_Recipe.CollectItemsToCraftWithFrom += HookCollectItems;
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += static il => {
            if (!il.ApplyTo(ILModifyStackText, Configs.AvailableMaterials.Enabled)) Configs.UnloadedCrafting.Value.availableMaterialsItemSlot = true;
        };
    }

    private static void HookFindRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        if (!canDelayCheck) _collectedRecipes = false;
        orig(canDelayCheck);
    }
    private static void HookCollectItems(On_Recipe.orig_CollectItemsToCraftWithFrom orig, Player player) {
        orig(player);
        if (player.whoAmI == Main.myPlayer) _collectedRecipes = true;
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!Configs.AvailableMaterials.Tooltip || !ShouldDisplayStack(item, item.tooltipContext, out string? text) || text.Length == 0) return;
        if (item.stack != 1) tooltips[0].Text = tooltips[0].Text[0..^(2 + item.stack.ToString().Length)];
        tooltips[0].Text += $" ({text})";
    }
    private static void ILModifyStackText(ILContext il) {
        ILCursor cursor = new(il);
        // if (++[true] || item.stack > 1) {
        //     ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, ++[customStack], position + new Vector2(10f, 26f) * inventoryScale, color, 0f, Vector2.Zero, new Vector2(inventoryScale), -1f, inventoryScale);
        // }
        cursor.GotoNext(i => i.MatchLdfld(Reflection.Item.DD2Summon));
        cursor.GotoPrev(i => i.SaferMatchCall(typeof(ChatManager), nameof(ChatManager.DrawColorCodedStringWithShadow)));
        cursor.GotoPrev(MoveType.After, i => i.MatchCall(Reflection.Int32.ToString) && i.Previous.MatchLdflda(Reflection.Item.stack));
        cursor.EmitLdarg1().EmitLdarg2().EmitLdarg3();
        cursor.EmitDelegate((string stack, Item[] inv, int context, int slot) => {
            Item item = inv[slot];
            return Configs.AvailableMaterials.ItemSlot && ShouldDisplayStack(item, context, out string? text, true) ? text : stack;
        });
        cursor.GotoPrev(i => i.MatchLdflda(Reflection.Item.stack));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdfld(Reflection.Item.stack));
        cursor.EmitLdarg1().EmitLdarg2().EmitLdarg3();
        cursor.EmitDelegate((int stack, Item[] inv, int context, int slot) => Configs.AvailableMaterials.ItemSlot && ShouldDisplayStack(inv[slot], context, out _) ? 2 : stack);
    }

    private static bool ShouldDisplayStack(Item item, int context, [MaybeNullWhen(false)] out string text, bool compact = false) {
        text = null;
        if (!(context == ItemSlot.Context.CraftingMaterial || (Configs.RecipeTooltip.Enabled && context == ItemSlot.Context.ChatItem))) return false;
        if (!_collectedRecipes) return false;

        (Recipe? recipe, Item[] tiles, Item[] conditions) = context == ItemSlot.Context.CraftingMaterial ?
            (Main.recipe[Main.availableRecipe[Main.focusRecipe]], RequiredObjectsDisplay._displayedRecipeTiles, RequiredObjectsDisplay._displayedRecipeConditions) :
            (RecipeTooltipItem.HoveredRecipe, RecipeTooltipItem._displayedTiles, RecipeTooltipItem._displayedConditions);
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
            if(met.HasValue) {
                text = compact ?
                    met.Value ? string.Empty : "0/1" :
                    met.Value ? string.Empty : Language.GetTextValue($"{Localization.Keys.UI}.Unmet");
                return true;
            }
        }
        return false;
    }

    private static bool _collectedRecipes;
}