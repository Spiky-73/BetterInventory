using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.Features.RecipeFiltering;

public sealed class RecipeSortPlayer {

    public static RecipeSortPlayer LocalPlayer => RecipeFilteringPlayer.LocalPlayer.SortPlayer;

    public static void Load(Mod mod) {
        AddSortStep(new RecipeSortStep.ByRecipeId());
        AddSortStep(new RecipeSortStep.ByCreateItemName());
        AddSortStep(new RecipeSortStep.ByCreateItemCreativeId());
        AddSortStep(new RecipeSortStep.ByCreateItemValue());

        RecipeSortToggle = mod.Assets.Request<Texture2D>($"Assets/Sort_Toggle");
        RecipeSortToggleBorder = mod.Assets.Request<Texture2D>($"Assets/Sort_Toggle_Border");
        RecipeSortingSteps = mod.Assets.Request<Texture2D>($"Assets/RecipeSortingSteps");
    }

    public void LoadData(TagCompound tag) {
        ResetSortStep();
        if (tag.TryGet(SortTag, out int sort)) SetActiveSortStep(sort);
    }
    public void SaveData(TagCompound tag) {
        int sort = GetActiveSortStepIndex();
        if (IsActive()) tag[SortTag] = sort;
    }
    public const string SortTag = "sort";

    public void SelectNextSortStep() => SetActiveSortStep((GetActiveSortStepIndex() + 1) % GetAvailableSortStep().Count);
    public IRecipeSortStep GetActiveSortStep() => GetAvailableSortStep()[GetActiveSortStepIndex()];

    public void SetActiveSortStep(int sortStep) => _sortIndex = sortStep;
    public void ResetSortStep() => _sortIndex = 0;
    public int GetActiveSortStepIndex() => _sortIndex;

    public bool IsActive() => _sortIndex != 0;
    public IComparer<Recipe> Comparer => GetActiveSortStep();

    private int _sortIndex = 0;

    public static Asset<Texture2D> RecipeSortToggle = null!;
    public static Asset<Texture2D> RecipeSortToggleBorder = null!;
    public static Asset<Texture2D> RecipeSortingSteps = null!;

    public static void AddSortStep(IRecipeSortStep step) => _availableSortSteps.Add(step);
    public static ReadOnlyCollection<IRecipeSortStep> GetAvailableSortStep() => _availableSortSteps.AsReadOnly();

    private static readonly List<IRecipeSortStep> _availableSortSteps = [];
}

public interface IRecipeSortStep : IEntrySortStep<Recipe> {
    UIElement GetImage();
}
