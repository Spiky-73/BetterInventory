using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.Features.RecipeFiltering;

public sealed class RecipeFiltersPlayer {

    public static RecipeFiltersPlayer LocalPlayer => RecipeFilteringPlayer.LocalPlayer.FiltersPlayer;

    public static void Load(Mod mod) {
        List<(IItemEntryFilter, int)> filters = [
            (new ItemFilters.Weapon(), 0),
            (new ItemFilters.Armor(), 2),
            (new ItemFilters.Vanity(), 8),
            (new ItemFilters.BuildingBlock(), 4),
            (new ItemFilters.Furniture(), 7),
            (new ItemFilters.Accessories(), 1),
            (new ItemFilters.MiscAccessories(), 9),
            (new ItemFilters.Consumables(), 3),
            (new ItemFilters.Tools(), 6),
            (new ItemFilters.Materials(), 10)
        ];
        foreach (var (f, i) in filters) AddAvailableFilter(new ItemFilterWrapper(f, i));

        RecipeFilters = mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters");
        RecipeFiltersGray = mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters_Gray");
    }

    public static void PostSetupRecipes() {
        AddAvailableFilter(new RecipeMiscFallback(_availableFilters));
    }

    public void LoadData(TagCompound tag) {
        ClearActiveFilters();
        if (tag.TryGet(FiltersTag, out int filters)) {
            for (int i = 0; i < GetAvailableFilters().Count; i++) if ((filters & (1 << i)) != 0) ToggleFilter(i);
        }
    }
    public void SaveData(TagCompound tag) {
        int filters = 0;
        for (int i = 0; i < GetAvailableFilters().Count; i++) if (IsFilterActive(i)) filters |= 1 << i;
        if (IsActive()) tag[FiltersTag] = filters;

    }
    public const string FiltersTag = "filters";

    public bool IsFilterActive(int index) => IsFilterActive(GetAvailableFilters()[index]);
    public void ToggleFilter(int index) => ToggleFilter(GetAvailableFilters()[index]);
    public bool IsFilterActive(IRecipeFilter filter) => _activeFilters.Contains(filter);
    public void ToggleFilter(IRecipeFilter filter) {
        if (!_activeFilters.Remove(filter)) _activeFilters.Add(filter);
    }
    public void ClearActiveFilters() => _activeFilters.Clear();
    public ReadOnlyCollection<IRecipeFilter> GetActiveFilters() => _activeFilters.AsReadOnly();

    public bool IsActive() => _activeFilters.Count > 0;
    public bool FitsFilters(Recipe recipe) {
        if (_activeFilters.Count == 0) return true;
        return _activeFilters.Exists(f => f.FitsFilter(recipe));
    }

    private readonly List<IRecipeFilter> _activeFilters = [];


    public static void AddAvailableFilter(IRecipeFilter filter) => _availableFilters.Add(filter);
    public static ReadOnlyCollection<IRecipeFilter> GetAvailableFilters() => _availableFilters.AsReadOnly();

    private static readonly List<IRecipeFilter> _availableFilters = [];

    public static Asset<Texture2D> RecipeFilters = null!;
    public static Asset<Texture2D> RecipeFiltersGray = null!;
}

public interface IRecipeFilter : IEntryFilter<Recipe> {
    UIElement GetImageGray();
}

public sealed class ItemFilterWrapper : IRecipeFilter {
    public IItemEntryFilter Filter { get; }
    public int Index { get; }

    public ItemFilterWrapper(IItemEntryFilter filter, int index) {
        Filter = filter;
        Index = index;
    }

    public bool FitsFilter(Recipe entry) => Filter.FitsFilter(entry.createItem);
    public string GetDisplayNameKey() => Filter.GetDisplayNameKey();
    public UIElement GetImage() => new UIImageFramed(RecipeFiltersPlayer.RecipeFilters, GetSourceFrame());
    public UIElement GetImageGray() => new UIImageFramed(RecipeFiltersPlayer.RecipeFiltersGray, GetSourceFrame());
    public Rectangle GetSourceFrame() => RecipeFiltersPlayer.RecipeFilters.Frame(horizontalFrames: 11, frameX: Index, sizeOffsetX: -2);
}

public sealed class RecipeMiscFallback : IRecipeFilter {
    public RecipeMiscFallback(List<IRecipeFilter> otherFilters) {
        _fitsFilterByRecipeIndex = new bool[Recipe.numRecipes];
        for (int i = 1; i < _fitsFilterByRecipeIndex.Length; i++) {
            Recipe entry = Main.recipe[i];
            _fitsFilterByRecipeIndex[i] = !otherFilters.Exists(f => f.FitsFilter(entry));
        }
    }

    public bool FitsFilter(Recipe entry) => _fitsFilterByRecipeIndex.IndexInRange(entry.RecipeIndex) && _fitsFilterByRecipeIndex[entry.RecipeIndex];

    public string GetDisplayNameKey() => "CreativePowers.TabMisc";

    private readonly bool[] _fitsFilterByRecipeIndex;

    public UIElement GetImage() => new UIImageFramed(RecipeFiltersPlayer.RecipeFilters, GetSourceFrame());
    public UIElement GetImageGray() => new UIImageFramed(RecipeFiltersPlayer.RecipeFiltersGray, GetSourceFrame());
    public static Rectangle GetSourceFrame() => RecipeFiltersPlayer.RecipeFilters.Frame(horizontalFrames: 11, frameX: 5, sizeOffsetX: -2);
}