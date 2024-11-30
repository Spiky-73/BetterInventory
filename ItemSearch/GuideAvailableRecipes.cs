using System.Collections.Generic;
using MonoMod.Cil;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;
using SpikysLib;
using SpikysLib.IL;
using SpikysLib.DataStructures;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace BetterInventory.ItemSearch;

public sealed class GuideAvailableRecipes : ILoadable {

    public void Load(Mod mod) {
        On_Recipe.FindRecipes += HookFindRecipes;
        IL_Recipe.FindRecipes += static il => {
            if (!il.ApplyTo(ILFindRecipes, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.guideAvailableRecipes = true;
        };
        On_Recipe.CollectGuideRecipes += HookCollectGuideRecipes;
        On_Recipe.AddToAvailableRecipes += HookAddToAvailableRecipes;
        On_Main.HoverOverCraftingItemButton += HookDisableCraftWhenNonAvailable;
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookDarkenNotAvailable;

        // TODO make independent
        _availableRecipesFilters = new(() => Configs.BetterGuide.AvailableRecipes && !GuideCraftInMenuPlayer.ShowAllRecipes(), r => IsAvailable(r.RecipeIndex), r => GuideFavoritedRecipesPlayer.LocalPlayer.IsFavorited(r.RecipeIndex));
        GuideRecipeFiltering.AddFilter(_availableRecipesFilters);
    }
    public void Unload() { }


    private static void HookFindRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        s_guideRecipes = false;
        orig(canDelayCheck);
    }

    private static void ILFindRecipes(ILContext il) {
        ILCursor cursor = new(il);

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
            if(!Configs.BetterGuide.AvailableRecipes) return false;
            s_guideRecipes = true;
            return true;
        });
        cursor.EmitBrtrue(skipGuide);
        cursor.MarkLabel(skipGuide); // Here in case of exception
        cursor.GotoNext(MoveType.Before, i => i.SaferMatchCall(Reflection.Recipe.CollectItemsToCraftWithFrom));
        cursor.EmitDelegate(() => { s_availableRecipes.Clear(); s_availableCreateItems.Clear(); });
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());
        cursor.MarkLabel(skipGuide);

        // <availableRecipes>
        // ++ <collectGuideRecipes>
        // Recipe.TryRefocusingRecipe(oldRecipe);
        // Recipe.VisuallyRepositionRecipes(focusY);
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchCall(Reflection.Recipe.TryRefocusingRecipe));

        // TODO optimize performances
        cursor.EmitDelegate(() => {
            if (Configs.BetterGuide.AvailableRecipes) Reflection.Recipe.CollectGuideRecipes.Invoke();
        });
    }

    private static void HookCollectGuideRecipes(On_Recipe.orig_CollectGuideRecipes orig) {
        s_collectingGuide = true;
        orig();
        s_collectingGuide = false;
    }

    private static void HookAddToAvailableRecipes(On_Recipe.orig_AddToAvailableRecipes orig, int recipeIndex) {
        if (!Configs.BetterGuide.AvailableRecipes || s_collectingGuide) orig(recipeIndex);
        else {
            s_availableRecipes.Add(recipeIndex);
            s_availableCreateItems.Add(Main.recipe[recipeIndex].createItem);
        }
    }

    private static void HookDisableCraftWhenNonAvailable(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (Configs.BetterGuide.AvailableRecipes && !IsAvailable(Main.availableRecipe[recipeIndex])) Main.LockCraftingForThisCraftClickDuration();
        orig(recipeIndex);
    }

    private void HookDarkenNotAvailable(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        if(!Configs.BetterGuide.AvailableRecipes || context != ItemSlot.Context.CraftingMaterial) {
            orig(spriteBatch, inv, context, slot, position, lightColor);
            return;
        }
        bool available;
        var focusRecipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];
        if(focusRecipe.requiredItem.Contains(inv[slot])) { // Material
            available = s_availableCreateItems.Contains(focusRecipe.createItem) || focusRecipe.GetMaterialCount(inv[slot]) >= inv[slot].stack;
        } else if(inv == GuideRequiredObjectsDisplay._displayedRecipeTiles) { // Required Tile
            available = slot >= focusRecipe.requiredTile.Count || Main.LocalPlayer.adjTile[focusRecipe.requiredTile[slot]];
        } else if(inv == GuideRequiredObjectsDisplay._displayedRecipeConditions) { // Required Condition
            available = focusRecipe.Conditions[slot].Predicate();
        } else { // Created item
            available = s_availableCreateItems.Contains(inv[slot]);
        }
        Color back = Main.inventoryBack;
        if(!available) Main.inventoryBack.ApplyRGB(0.5f);
        orig(spriteBatch, inv, context, slot, position, lightColor);
        Main.inventoryBack = back;
    }

    public static bool IsAvailable(int recipe) => s_availableRecipes.Contains(recipe);
    public static bool IsAvailable(Item createItem) => s_availableCreateItems.Contains(createItem);

    internal static bool s_guideRecipes;
    private static bool s_collectingGuide;
    private static readonly RangeSet s_availableRecipes = [];
    private static readonly HashSet<Item> s_availableCreateItems = [];

    private static GuideRecipeFilterGroup _availableRecipesFilters = null!;
}
