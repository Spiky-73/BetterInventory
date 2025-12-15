using BetterInventory.Features.RecipeFiltering.UI.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpikysLib;
using SpikysLib.UI.Elements;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace BetterInventory.Features.RecipeFiltering.UI.Elements;

public sealed class UIRecipeFilters : UIFlexGrid {

    public UIRecipeFilters(): base(1) {
        ListPadding = 0.01f; // Cannot be set to 0 for some reason;
        OnUpdate += UpdateSelf;
    }

    public override void OnActivate() => Rebuild();
    private void UpdateSelf(UIElement affectedElement) {
        if (_pendingRebuild) Rebuild(true);
    }

    public void Rebuild(bool immediate = false) {
        _pendingRebuild = !immediate;
        if (_pendingRebuild) return;

        Clear();
        ItemsPerLine = RecipeFiltersConfig.Instance.filtersPerLine;

        RecipeFiltersPlayer player = RecipeFiltersPlayer.LocalPlayer;
        foreach (var filter in RecipeFiltersPlayer.GetAvailableFilters()) {
            bool active = player.IsFilterActive(filter);
            UIRecipeFilterIcon icon = new(filter, active);
            icon.OnLeftClick += (_, _) => {
                bool keepOn = !active || player.GetActiveFilters().Count > 1;
                player.ClearActiveFilters();
                if (keepOn) player.ToggleFilter(filter);
                OnFilterChange();
            };
            icon.OnRightClick += (_, _) => {
                player.ToggleFilter(filter);
                OnFilterChange();
            };
            Add(icon);
        }
        UIRecipeFiltering.NeedsRecalculate();
    }

    public void OnFilterChange() {
        Rebuild();
        Recipe.FindRecipes();
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    private bool _pendingRebuild;
}

public class UIRecipeFilterIcon : UIElement {
    public UIRecipeFilterIcon(IRecipeFilter filter, bool active) {
        UIElement icon = filter.GetImage();
        icon.VAlign = 0.5f;
        icon.HAlign = 0.5f;
        if (!active) {
            Color alpha = new(80, 80, 80, 70);
            if (icon is IColorable colorable) colorable.Color = colorable.Color.MultiplyRGBA(alpha);
            else if (icon is UIImage image) image.Color = image.Color.MultiplyRGBA(alpha);
        }
        Width.Set(icon.Width.Pixels + 4, 0);
        Height.Set(icon.Height.Pixels + 4, 0);
        Append(icon);

        _hoverText = Language.GetTextValue(filter.GetDisplayNameKey());
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        if (!IsMouseHovering) return;
        GraphicsHelper.DrawMouseText(_hoverText);
        spriteBatch.Draw(TextureAssets.InfoIcon[13].Value, GetDimensions().Position(), Main.OurFavoriteColor);
    }

    private readonly string _hoverText;
}