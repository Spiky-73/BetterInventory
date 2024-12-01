using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.ItemSearch;


public sealed partial class Guide : ModSystem {

    // TODO redo
    public static void FindGuideRecipes() => Recipe.FindRecipes();

    public override void Load() {
        On_Recipe.CollectGuideRecipes += HookCollectGuideRecipes;
                
        IL_Recipe.CollectGuideRecipes += static il => {
            if (!il.ApplyTo(ILGuideRecipeOrder, Configs.BetterGuide.RecipeOrdering)) Configs.UnloadedItemSearch.Value.GuideRecipeOrdering = true;
        };
    }

    public override void PostAddRecipes() {
        Default.Catalogues.Bestiary.HooksBestiaryUI();
        GuideGuideTile.FindCraftingStations();
    }
}