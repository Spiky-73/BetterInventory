using BetterInventory.Default.Catalogues;
using BetterInventory.Features.RecipeFiltering.UI.States;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace BetterInventory.Features.RecipeFiltering.UI.Elements;

public sealed class UIRecipeSearch : UIPanel {

    public UIRecipeSearch() {
        OnUpdate += UpdateSelf;

        Height = new StyleDimension(24f, 0);
        BackgroundColor = new Color(35, 40, 83);
        BorderColor = new Color(15, 20, 40);
        SetPadding(0f);
    }

    private void OnSearchContentChange(string? content) {
        if (Main.gameMenu) return;
        RecipeSearchPlayer.LocalPlayer.SetSearch(content);
        Recipe.FindRecipes();
    }
    public override void OnInitialize() {
        _searchBar = new(Language.GetText("UI.PlayerNameSlot"), 0.8f) {
            Width = new StyleDimension(0f, 1f),
            Height = new StyleDimension(0f, 1f),
            HAlign = 0f,
            VAlign = 0.5f,
            Left = new StyleDimension(-4, 0f),
            IgnoresMouseInteraction = true
        };

        _searchBar.OnStartTakingInput += () => BorderColor = Main.OurFavoriteColor;
        _searchBar.OnEndTakingInput += () => BorderColor = new Color(15, 20, 40);
        OnLeftClick += (evt, _) => {
            if (evt.Target.Parent != this) _searchBar.ToggleTakingText();
        };
        _searchBar.OnContentsChanged += OnSearchContentChange;
        _searchBar.OnCanceledTakingInput += () => OnSearchContentChange(null);
        _searchBar.SetContents(null, true);
        Append(_searchBar);

        _cancelButton = new(RecipeSearchPlayer.ImageSearchCancel) {
            HAlign = 1f,
            VAlign = 0.5f,
            Left = new StyleDimension(-2f, 0f)
        };
        _cancelButton.OnMouseOver += (_, _) => SoundEngine.PlaySound(SoundID.MenuTick);
        _cancelButton.OnLeftClick += (_, _) => {
            RecipeList.HookSearchRecipe_Cancel(_searchBar);
            _searchBar.SetContents(null, true);
            SoundEngine.PlaySound(SoundID.MenuTick);
        };
        Append(_cancelButton);
        RecipeList.OnSearchBarInit(_searchBar);
    }

    public override void OnActivate() => Rebuild();
    private void UpdateSelf(UIElement affectedElement) {
        if (Main.recBigList != _expanded) {
            _expanded = Main.recBigList;
            UIRecipeFiltering.NeedsRecalculate();
        }
        if (_pendingRebuild) Rebuild(true);
    }

    public override void Recalculate() {
        if (RecipeSearchBarConfig.Instance.expand && _expanded) Width.Pixels = 220;
        else Width.Pixels = RecipeSearchBarConfig.Instance.minWidth;
        base.Recalculate();
    }

    public void Rebuild(bool immediate = false) {
        _pendingRebuild = !immediate;
        if (_pendingRebuild) return;

        _searchBar.SetContents(RecipeSearchPlayer.LocalPlayer.GetSearch());
    }

    public static void OnSearchChange(string? search) {
        if (Main.gameMenu) return;
        RecipeSearchPlayer.LocalPlayer.SetSearch(search);
        Recipe.FindRecipes();
    }

    private bool _pendingRebuild;
    private bool _expanded;
    private UIImageButton _cancelButton = null!;
    private UISearchBar _searchBar = null!;
}