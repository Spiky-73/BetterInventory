using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BetterInventory.ItemActions;
using BetterInventory.ItemSearch;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BetterInventory.Crafting;

public sealed class Crafting : ILoadable {

    public void Load(Mod mod) {

        On_Recipe.ClearAvailableRecipes += HookClearAvailableRecipes;
        On_Recipe.CollectItemsToCraftWithFrom += HookCollectItems;
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILCraftOnList, Configs.CraftOnList.Enabled)) Configs.UnloadedCrafting.Value.craftOnList = true;
        };
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += static il => {
            if (!il.ApplyTo(ILAvailableMaterial, Configs.AvailableMaterials.Enabled)) Configs.UnloadedCrafting.Value.availableMaterialsItemSlot = true;
        };
    }
    public void Unload() { }

    private static void HookClearAvailableRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        orig();
        _collectedRecipes = false;
    }

    private static void HookCollectItems(On_Recipe.orig_CollectItemsToCraftWithFrom orig, Player player) {
        orig(player);
        if(player.whoAmI == Main.myPlayer) _collectedRecipes = true;
    }


    private static void ILCraftOnList(ILContext il) {

        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int recipeListIndex, i => i.Previous.MatchLdsfld(Reflection.Main.recStart), 153);
        
        // if(<recBigListVisible>) {
        //     ...
        //     while (<showingRecipes>) {
        //         ...
        //         if (<mouseHover>) {
        //             Main.player[Main.myPlayer].mouseInterface = true;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Main.LockCraftingForThisCraftClickDuration));
        cursor.GotoPrev(MoveType.After, i => i.MatchStfld(Reflection.Player.mouseInterface));

        ILLabel skipVanillaHover = null!;
        cursor.FindPrev(out _, i => i.MatchBrtrue(out skipVanillaHover!));

        //             if(++[!craftInList] &&<click>) {
        cursor.EmitLdloc(recipeListIndex);
        cursor.EmitDelegate((int i) => {
            if (!Configs.CraftOnList.Enabled) return false;
            int f = Main.focusRecipe;
            if (Configs.CraftOnList.Value.focusHovered) Main.focusRecipe = i;
            Reflection.Main.HoverOverCraftingItemButton.Invoke(i);
            if (f != Main.focusRecipe) Main.recFastScroll = true;
            Main.craftingHide = false;
            return true;
        });
        cursor.EmitBrtrue(skipVanillaHover);
        //                 <scrollList>
        //                 ...
        //             }
        //             ...
        //         }

        cursor.GotoLabel(skipVanillaHover, MoveType.AfterLabel);
        cursor.EmitLdloc(recipeListIndex);
        cursor.EmitDelegate((int i) => {
            if (!Configs.CraftOnList.Enabled) return;
            if (Main.numAvailableRecipes > 0 && Main.focusRecipe == i && !Configs.CraftOnList.Value.focusHovered) ItemSlot.DrawGoldBGForCraftingMaterial = true;
        });
        //     }
        // }
    }

    public static Item? GetMouseMaterial() => Configs.Crafting.MouseMaterial ? Main.mouseItem : null;

    public static void AddAvailableMaterials(Item item, List<TooltipLine> tooltips) {
        if (!Configs.AvailableMaterials.Tooltip || !ShouldDisplayStack(item, item.tooltipContext, out Recipe? recipe)) return;
        if (item.stack != 1) tooltips[0].Text = tooltips[0].Text[0..^(2 + item.stack.ToString().Length)];
        tooltips[0].Text += $" ({recipe.GetMaterialCount(item)}/{item.stack})";
    }
    private static void ILAvailableMaterial(ILContext il) {
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
            if (!Configs.AvailableMaterials.ItemSlot || !ShouldDisplayStack(item, context, out Recipe? recipe)) return stack;
            return $"{Utility.ToMetricString(recipe.GetMaterialCount(item))}/{item.stack}";
        });
        cursor.GotoPrev(i => i.MatchLdflda(Reflection.Item.stack));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdfld(Reflection.Item.stack));
        cursor.EmitLdarg1().EmitLdarg2().EmitLdarg3();
        cursor.EmitDelegate((int stack, Item[] inv, int context, int slot) => Configs.AvailableMaterials.ItemSlot && ShouldDisplayStack(inv[slot], context, out _) ? 2 : stack);
    }

    private static bool ShouldDisplayStack(Item item, int context, [MaybeNullWhen(false)] out Recipe recipe) {
        recipe = null;
        if (!(context == ItemSlot.Context.CraftingMaterial || (Configs.RecipeTooltip.Enabled && context == ItemSlot.Context.ChatItem))) return false;
        if (!_collectedRecipes) return false;
        
        recipe = context == ItemSlot.Context.CraftingMaterial ? Main.recipe[Main.availableRecipe[Main.focusRecipe]] : RequiredTooltipItem.HoveredRecipe;
        if (recipe is null) return false;
        var guid = item.UniqueId();
        if (!recipe.requiredItem.Exists(i => i.UniqueId() == guid)) return false;
        return true;
    }

    private static bool _collectedRecipes;
}
