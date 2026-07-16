using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterInventory.BetterRecipeList.RecipeSortSteps;

public sealed class ByRecipeId : IRecipeSortStep {
    public int Compare(Recipe? x, Recipe? y) => Utility.CompareHandleNullable(x, y) ?? x!.RecipeIndex.CompareTo(y!.RecipeIndex);

    public string GetDisplayNameKey() => $"{Localization.Keys.UI}.RecipeSort.ByRecipeId";
    public UIElement GetImage() => new UIImageFramed(RecipeFilteringUISystem.RecipeSortingSteps, GetSourceFrame());
    public static Rectangle GetSourceFrame() => RecipeFilteringUISystem.RecipeSortingSteps.Frame(horizontalFrames: 4, frameX: 0, sizeOffsetX: -2);
}

public sealed class ByCreateItemName : IRecipeSortStep {
    public int Compare(Recipe? x, Recipe? y) => Utility.CompareHandleNullable(x, y) ?? x!.createItem.Name.CompareTo(y!.createItem.Name);

    public string GetDisplayNameKey() => $"{Localization.Keys.UI}.RecipeSort.ByCreateItemName";
    public UIElement GetImage() => new UIImageFramed(RecipeFilteringUISystem.RecipeSortingSteps, GetSourceFrame());
    public static Rectangle GetSourceFrame() => RecipeFilteringUISystem.RecipeSortingSteps.Frame(horizontalFrames: 4, frameX: 1, sizeOffsetX: -2);
}

public sealed class ByCreateItemValue : IRecipeSortStep {
    public int Compare(Recipe? x, Recipe? y) {
        int? nullCompare = Utility.CompareHandleNullable(x, y);
        if (nullCompare.HasValue) return nullCompare.Value;
        return x!.createItem.value.CompareTo(y!.createItem.value);
    }

    public string GetDisplayNameKey() => $"{Localization.Keys.UI}.RecipeSort.ByCreateItemValue";
    public UIElement GetImage() => new UIImageFramed(RecipeFilteringUISystem.RecipeSortingSteps, GetSourceFrame());
    public static Rectangle GetSourceFrame() => RecipeFilteringUISystem.RecipeSortingSteps.Frame(horizontalFrames: 4, frameX: 2, sizeOffsetX: -2);
}

public sealed class ByCreateItemCreativeId : IRecipeSortStep {
    private readonly SortingSteps.ByCreativeSortingId _creativeSorter = new();
    private readonly SortingSteps.Alphabetical _azSorter = new();

    public int Compare(Recipe? x, Recipe? y) {
        int? nullCompare = Utility.CompareHandleNullable(x, y);
        if (nullCompare.HasValue) return nullCompare.Value;
        int creativeCompare = _creativeSorter.Compare(x!.createItem, y!.createItem);
        if (nullCompare != 0) return creativeCompare;
        return _azSorter.Compare(x.createItem, y.createItem);
    }

    public string GetDisplayNameKey() => $"{Localization.Keys.UI}.RecipeSort.ByCreateItemCreativeId";
    public UIElement GetImage() => new UIImageFramed(RecipeFilteringUISystem.RecipeSortingSteps, GetSourceFrame());
    public static Rectangle GetSourceFrame() => RecipeFilteringUISystem.RecipeSortingSteps.Frame(horizontalFrames: 4, frameX: 3, sizeOffsetX: -2);
}
