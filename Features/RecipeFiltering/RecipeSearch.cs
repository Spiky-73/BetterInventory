using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ModLoader.IO;

namespace BetterInventory.Features.RecipeFiltering;

public sealed class RecipeSearchPlayer {

    public static RecipeSearchPlayer LocalPlayer => RecipeFilteringPlayer.LocalPlayer.GetSearchPlayer();

    public void LoadData(TagCompound tag) {
        ClearSearch();
        if (tag.TryGet(SearchTag, out string search)) SetSearch(search);
    }
    public void SaveData(TagCompound tag) {
        string? search = GetSearch();
        if (!string.IsNullOrEmpty(search)) tag[SearchTag] = search;
    }
    public const string SearchTag = "search";

    public void SetSearch(string? search) => _search = search;
    public void ClearSearch() => _search = null;
    public string? GetSearch() => _search;

    public bool FitsFilters(Recipe recipe) {
        if (!Configs.RecipeSearchBar.Instance.simpleSearch) return _filter.FitsFilter(recipe.createItem);
        return string.IsNullOrEmpty(_search) || recipe.createItem.HoverName.ToLower().Contains(_search, System.StringComparison.OrdinalIgnoreCase);
    }

    private string? _search;
    private readonly ItemFilters.BySearch _filter = new();

    public static void Load() {
        ImageSearchCancel = Main.Assets.Request<Texture2D>("Images/UI/SearchCancel");
    }

    public static Asset<Texture2D> ImageSearchCancel = null!;

}

