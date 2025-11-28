using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.Features.RecipeFiltering;

public sealed class RecipeSortPlayer {

    public static RecipeSortPlayer LocalPlayer => RecipeFilteringPlayer.LocalPlayer.GetSortPlayer();

    public void LoadData(TagCompound tag) {
        ResetSortStep();
        if (tag.TryGet(SortTag, out int sort)) SetActiveSortStep(sort);
    }
    public void SaveData(TagCompound tag) {
        int sort = GetActiveSortStepIndex();
        if (sort != 0) tag[SortTag] = sort;
    }
    public const string SortTag = "sort";

    public void SelectNextSortStep() => SetActiveSortStep((GetActiveSortStepIndex() + 1) % GetAvailableSortStep().Count);
    public void SetActiveSortStep(int sortStep) => SetActiveSortStep(GetAvailableSortStep()[sortStep]);
    public void SetActiveSortStep(IRecipeSortStep sortStep) =>_activeSort = sortStep;
    public void ResetSortStep() => _activeSort = _availableSortSteps[0];
    public IRecipeSortStep GetActiveSortStep() {
        if (_activeSort is null) ResetSortStep();
        return _activeSort!;
    }
    public int GetActiveSortStepIndex() => _availableSortSteps.IndexOf(GetActiveSortStep());

    public bool IsActive() => _activeSort is not RecipeSortStep.ByRecipeId;
    public IComparer<Recipe> Comparer => GetActiveSortStep();

    private IRecipeSortStep? _activeSort;

    public static void Load() {
        AddSortStep(new RecipeSortStep.ByRecipeId());
        AddSortStep(new RecipeSortStep.ByCreateItemName());
        AddSortStep(new RecipeSortStep.ByCreateItemCreativeId());
        AddSortStep(new RecipeSortStep.ByCreateItemValue());

        RecipeSortToggle = BetterInventory.Instance.Assets.Request<Texture2D>($"Assets/Sort_Toggle");
        RecipeSortToggleBorder = BetterInventory.Instance.Assets.Request<Texture2D>($"Assets/Sort_Toggle_Border");
        RecipeSortingSteps = BetterInventory.Instance.Assets.Request<Texture2D>($"Assets/RecipeSortingSteps");
    }

    public static Asset<Texture2D> RecipeSortToggle = null!;
    public static Asset<Texture2D> RecipeSortToggleBorder = null!;
    public static Asset<Texture2D> RecipeSortingSteps = null!;

    public static void AddSortStep(IRecipeSortStep step) => _availableSortSteps.Add(step);
    public static ReadOnlyCollection<IRecipeSortStep> GetAvailableSortStep() => _availableSortSteps.AsReadOnly();

    private static readonly List<IRecipeSortStep> _availableSortSteps = [];
}

public interface IRecipeSortStep : IEntrySortStep<Recipe> {
    public bool HiddenFromSortOptions { get; }
    UIElement GetImage();
}
