using System;
using MonoMod.Cil;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;
using SpikysLib;
using SpikysLib.IL;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace BetterInventory.ItemSearch.BetterGuide;

public sealed class AvailableRecipes : ModSystem {

    public override void Load() {
        IL_Recipe.FindRecipes += static il => {
            if (!il.ApplyTo(ILSkipGuide, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
        };
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookDarkenNotAvailable;

        _availableRecipesFilters = new(() => Configs.BetterGuide.AvailableRecipes && !CraftInMenuPlayer.ShowAllRecipes(), r => DisplayedRecipes.IsAvailable(r.RecipeIndex), r => FavoritedRecipesPlayer.LocalPlayer.IsFavorited(r.RecipeIndex));
        RecipeFiltering.AddFilter(_availableRecipesFilters);
    }

    private static void ILSkipGuide(ILContext il) {
        ILCursor cursor = new(il);

        // ++ goto noCLear
        // Recipe.ClearAvailableRecipes();
        // ++ noCLear:
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchCall(Reflection.Recipe.ClearAvailableRecipes));
        cursor.EmitDelegate<Action>(() => s_guideRecipes = false);

        // if (<displayGuideRecipes>) {
        //     ++ goto <skip>
        //     <guideRecipes>
        //     return;
        // }
        // ++ skip:
        // ++ <clearAvailableRecipes>
        // Player localPlayer = Main.LocalPlayer;
        // Recipe.CollectItemsToCraftWithFrom(localPlayer);
        cursor.GotoNext(MoveType.Before, i => i.SaferMatchCall(Reflection.Recipe.CollectGuideRecipes));
        ILLabel skipGuide = cursor.DefineLabel();
        cursor.EmitDelegate(() => {
            if (!Configs.BetterGuide.AvailableRecipes) return false;
            s_guideRecipes = true;
            return true;
        });
        cursor.EmitBrtrue(skipGuide);
        cursor.MarkLabel(skipGuide); // Here in case of exception
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Recipe.CollectItemsToCraftWithFrom));
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());
        cursor.MarkLabel(skipGuide);
    }

    private void HookDarkenNotAvailable(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        if (!Configs.BetterGuide.AvailableRecipes || context != ItemSlot.Context.CraftingMaterial) {
            orig(spriteBatch, inv, context, slot, position, lightColor);
            return;
        }
        bool available;
        var focusRecipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];
        if (focusRecipe.requiredItem.Contains(inv[slot])) { // Material
            available = DisplayedRecipes.IsAvailable(focusRecipe.RecipeIndex) || focusRecipe.GetMaterialCount(inv[slot]) >= inv[slot].stack;
        } else if (inv == RequiredObjectsDisplay._displayedRecipeTiles) { // Required Tile
            available = slot >= focusRecipe.requiredTile.Count || Main.LocalPlayer.adjTile[focusRecipe.requiredTile[slot]];
        } else if (inv == RequiredObjectsDisplay._displayedRecipeConditions) { // Required Condition
            available = focusRecipe.Conditions[slot].Predicate();
        } else { // Created item
            available = DisplayedRecipes.IsAvailable(inv[slot]);
        }
        Color back = Main.inventoryBack;
        if (!available) Main.inventoryBack.ApplyRGB(0.5f);
        orig(spriteBatch, inv, context, slot, position, lightColor);
        Main.inventoryBack = back;
    }

    internal static bool s_guideRecipes;

    private static GuideRecipeFilterGroup _availableRecipesFilters = null!;
}
