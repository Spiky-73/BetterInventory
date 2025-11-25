using BetterInventory.Features.RecipeFiltering.UI.States;
using Microsoft.Xna.Framework.Graphics;
using SpikysLib;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace BetterInventory.Features.RecipeFiltering.UI.Elements;

public sealed class UIRecipeSort : UIElement {

    public UIRecipeSort() {
        OnUpdate += UpdateSelf;
        OnLeftClick += LeftClickSelf;
        OnRightClick += RightClickSelf;
    }

    public override void OnActivate() => Rebuild(true);

    private void UpdateSelf(UIElement affectedElement) {
        if (_pendingRebuild) Rebuild(true);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        if (IsMouseHovering) GraphicsHelper.DrawMouseText(_hoverText);
    }

    private void LeftClickSelf(UIMouseEvent evt, UIElement listeningElement) {
        RecipeSortPlayer.LocalPlayer.SelectNextSortStep();
        OnSortChanged();
    }

    private void RightClickSelf(UIMouseEvent evt, UIElement listeningElement) {
        RecipeSortPlayer.LocalPlayer.ResetSortStep();
        OnSortChanged();
    }

    public void Rebuild(bool immediate = false) {
        _pendingRebuild = !immediate;
        if (_pendingRebuild) return;

        RecipeSortPlayer player = RecipeSortPlayer.LocalPlayer;
        IRecipeSortStep sortStep = player.GetActiveSortStep();

        UIElement icon = sortStep.GetImage();
        icon.Left.Set(9 - icon.Width.Pixels / 2, 0f);
        icon.Top.Set(15 - icon.Height.Pixels / 2, 0f);
        _hoverText = Language.GetTextValue(sortStep.GetDisplayNameKey());
        UIImageButton arrow = new(RecipeSortPlayer.RecipeSortToggle) {
            VAlign = 0.5f,
            HAlign = 0.5f,
        };
        arrow.SetHoverImage(RecipeSortPlayer.RecipeSortToggleBorder);
        arrow.SetVisibility(1, 1);

        Width = arrow.Width;
        Height = arrow.Height;
        Append(icon);
        Append(arrow);
        UIRecipeFiltering.NeedsRecalculate();
    }

    private void OnSortChanged() {
        Rebuild();
        Recipe.FindRecipes();
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    private bool _pendingRebuild;
    private string _hoverText = string.Empty;
}