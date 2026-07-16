using Microsoft.Xna.Framework.Graphics;
using SpikysLib;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace BetterInventory.BetterRecipeList.UI.Elements;

public sealed class UIRecipeSort : UIElement {

    public override void OnInitialize() {
        OnLeftClick += LeftClickSelf;
        OnRightClick += RightClickSelf;
        _arrow = new(RecipeFilteringUISystem.RecipeSortToggle) { VAlign = 0.5f, HAlign = 0.5f };
        _arrow.SetHoverImage(RecipeFilteringUISystem.RecipeSortToggleBorder);
        _arrow.SetVisibility(1, 1);
        Width = _arrow.Width;
        Height = _arrow.Height;
    }

    public override void OnActivate() => UpdateImage();

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        if (IsMouseHovering) GraphicsHelper.DrawMouseText(_hoverText);
    }

    private void LeftClickSelf(UIMouseEvent evt, UIElement listeningElement) {
        RecipeFilteringPlayer.LocalPlayer.Sorter.SelectNextSortStep();
        OnSortChanged();
    }

    private void RightClickSelf(UIMouseEvent evt, UIElement listeningElement) {
        RecipeFilteringPlayer.LocalPlayer.Sorter.ResetSortStep();
        OnSortChanged();
    }

    public void UpdateImage() {
        RemoveAllChildren();

        var sorter = RecipeFilteringPlayer.LocalPlayer.Sorter;
        IRecipeSortStep sortStep = sorter.GetActiveSortStep();

        UIElement icon = sortStep.GetImage();
        icon.Left.Set(9 - icon.Width.Pixels / 2, 0f);
        icon.Top.Set(15 - icon.Height.Pixels / 2, 0f);
        _hoverText = Language.GetTextValue(sortStep.GetDisplayNameKey());
        Append(icon);
        Append(_arrow);
    }

    private void OnSortChanged() {
        UpdateImage();
        Recipe.FindRecipes();
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    private UIImageButton _arrow = null!;
    private string _hoverText = string.Empty;
}