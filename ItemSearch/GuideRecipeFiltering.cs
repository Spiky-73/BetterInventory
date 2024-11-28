using System;
using System.Collections.Generic;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.ItemSearch;

public class GuideRecipeFiltering : ILoadable {

    public void Load(Mod mod) {
        IL_Recipe.CollectGuideRecipes += static il => {
            if (!il.ApplyTo(ILGuideRecipesFiltering, Configs.BetterGuide.RecipeFiltering)) Configs.UnloadedItemSearch.Value.guideRecipeFiltering = true;
        };
    }

    public void Unload() {
        _filterGroups.Clear();
        _guideItemFilters._filters.Clear();
    }

    private static void ILGuideRecipesFiltering(ILContext il) {
        ILCursor cursor = new(il);

        Utility.GotoRecipeDisabled(cursor, out ILLabel endLoop, out _, out int recipe);

        //     ++ if(<extraRecipe>) {
        //     ++     <addRecipe>
        //     ++     continue;
        //     ++ }
        cursor.EmitLdloc(recipe);
        cursor.EmitDelegate((Recipe recipe) => {
            if(!Configs.BetterGuide.RecipeFiltering) return false;
            foreach (var group in _filterGroups){
                if(group.Enabled && !group.FitsFilter(recipe)) return true;
            }

            if (!_guideItemFilters.Enabled || _guideItemFilters.FitsFilter(recipe)) {
                Main.availableRecipe[Main.numAvailableRecipes++] = recipe.RecipeIndex;
                return true;
            }
            return false;
        });
        cursor.EmitBrtrue(endLoop);
    }

    public static void AddFilter(GuideRecipeFilterGroup group) => _filterGroups.Add(group);
    public static void AddGuideItemFilter(Func<Recipe, bool> filter) => _guideItemFilters.AddFilter(filter);

    private readonly static List<IGuideRecipeFilter> _filterGroups = [];
    private static GuideRecipeFilterGroup _guideItemFilters = new(() => !Main.guideItem.IsAir);
}

public interface IGuideRecipeFilter {
    bool Enabled { get; }
    bool FitsFilter(Recipe recipe);
}

public sealed class GuideRecipeFilterGroup : IGuideRecipeFilter {
    
    public bool Enabled => _enabled();

    public GuideRecipeFilterGroup(Func<bool> enabled, params Func<Recipe, bool>[] filters) {
        _enabled = enabled;
        _filters = new(filters);
    }

    public bool FitsFilter(Recipe recipe) => _filters.Count == 0 || _filters.Exists(f => f(recipe));
    public void AddFilter(Func<Recipe, bool> filter) => _filters.Add(filter);


    private readonly Func<bool> _enabled;
    internal readonly List<Func<Recipe, bool>> _filters;
}