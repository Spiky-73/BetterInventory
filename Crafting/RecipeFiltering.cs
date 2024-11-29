using BetterInventory.ItemSearch;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace BetterInventory.Crafting;

// TODO re add filtering and sorting
public sealed class RecipeFiltering : ILoadable {

    public static RecipeFilters LocalFilters => ItemActions.BetterPlayer.LocalPlayer.RecipeFilters;

    public void Load(Mod mod) {
        On_Recipe.AddToAvailableRecipes += HookFilterAddedRecipe;
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILDrawUI, Configs.Crafting.RecipeUI)) Configs.UnloadedCrafting.Value.RecipeUI = true;
        };
        IL_Recipe.CollectGuideRecipes += static il => {
            if (!il.ApplyTo(ILForceAddToAvailable, Configs.Crafting.RecipeUI)) Configs.UnloadedCrafting.Value.RecipeUI = true;
        };

        recipeFilters = mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters");
        recipeFiltersGray = mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters_Gray");
    }

    public void Unload() {}

    private static void ILDrawUI(ILContext il) {
        ILCursor cursor = new(il);

        // BetterGameUI Compatibility
        int screenY = 13;
        if(cursor.TryGotoNext(i => i.SaferMatchCallvirt(Reflection.AccessorySlotLoader.DrawAccSlots))) {
            cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.screenHeight));
            cursor.GotoNextLoc(out screenY, i => true, 13);
        }

        // ...
        // if(<showRecipes>){
        cursor.GotoRecipeDraw();

        //     ++<drawFilters>
        cursor.EmitLdloc(screenY); // int num54
        cursor.EmitDelegate((int y) => {
            if (Configs.Crafting.RecipeUI && LocalFilters.AllRecipes != 0) DrawRecipeUI(94, 450 + y);
        });

        //     ...
        //     if(Main.numAvailableRecipes == 0) ...
        //     else {
        //         int num73 = 94;
        //         int num74 = 450 + num51;
        //         if (++false && Main.InGuideCraftMenu) num74 -= 150;
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.TextureAssets.CraftToggle));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(Reflection.Main.InGuideCraftMenu));
        cursor.EmitDelegate((bool inGuide) => !Configs.RecipeFilters.Enabled && inGuide);
        //         ...
        //     }
    }

    public static void DrawRecipeUI(int hammerX, int hammerY){
        Guide.recipeUI.container.Top.Pixels = hammerY + TextureAssets.CraftToggle[0].Height() - TextureAssets.InfoIcon[0].Width() / 2;
        Guide.recipeUI.container.Left.Pixels = hammerX - TextureAssets.InfoIcon[0].Width() - 1;

        if (_needsFilterRefresh) {
            Guide.recipeUI.RebuildRecipeGrid();
            _needsFilterRefresh = false;
        }
        Guide.recipeInterface.Draw(Main.spriteBatch, Guide._lastUpdateUiGameTime);
    }

    public static void ClearFilters() {
        LocalFilters.AllRecipes = 0;
        LocalFilters.RecipeInFilter.Clear();
        for (int i = 0; i < LocalFilters.Filterer.AvailableFilters.Count; i++) LocalFilters.RecipeInFilter.Add(0);
    }

    private static void ILForceAddToAvailable(ILContext il) {
        ILCursor cursor = new(il);

        Utility.GotoRecipeDisabled(cursor, out ILLabel endLoop, out int index, out _);

        //     for(<material>) {
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdsfld(Reflection.Main.availableRecipe));

        //         if(<recipeOk> ++[&& !custom]) {
        cursor.EmitLdloc(index);
        cursor.EmitDelegate((int r) => {
            if (!Configs.RecipeFilters.Enabled) return false;
            Reflection.Recipe.AddToAvailableRecipes.Invoke(r);
            return true;
        });
        cursor.EmitBrtrue(endLoop!);

        //             Main.availableRecipe[Main.numAvailableRecipes] = i;
        //             Main.numAvailableRecipes++;
        //             break;
        //         }
        //     }
        // }
    }

    private static void HookFilterAddedRecipe(On_Recipe.orig_AddToAvailableRecipes orig, int recipeIndex) {
        if (Configs.RecipeFilters.Enabled && !FitsFilters(recipeIndex)) return;
        orig(recipeIndex);
    }
    public static bool FitsFilters(int recipe) {
        Item item = Main.recipe[recipe].createItem;
        var filterer = LocalFilters.Filterer;

        LocalFilters.AllRecipes++;
        for (int i = 0; i < filterer.AvailableFilters.Count; i++) {
            if (filterer.AvailableFilters[i].FitsFilter(item)) LocalFilters.RecipeInFilter[i]++;
        }
        _needsFilterRefresh = true;
        
        return filterer.FitsFilter(item);
    }

    private static bool _needsFilterRefresh;

    internal static Asset<Texture2D> recipeFilters = null!;
    internal static Asset<Texture2D> recipeFiltersGray = null!;
}
