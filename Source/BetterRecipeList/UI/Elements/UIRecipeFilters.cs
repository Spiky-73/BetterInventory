using System.Collections.Generic;
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

namespace BetterInventory.BetterRecipeList.UI.Elements;

public sealed class UIRecipeFilters : UIFlexGrid {

    public UIRecipeFilters() {
        ListPadding = 0; // FIXME Cannot be set to 0 for some reason;
    }

    public override void OnInitialize() {
        var player = RecipeFilteringPlayer.LocalPlayer.Filterer;
        foreach (var filter in player.AvailableFilters()) {
            bool active = player.IsFilterActive(filter);
            UIRecipeFilterIcon icon = new(filter);
            icon.OnLeftClick += (_, _) => {
                bool keepOn = !active || player.ActiveFilters().Count > 1;
                player.ClearActiveFilters();
                if (keepOn) player.ToggleFilter(filter);
                OnFiltersChange();
            };
            icon.OnRightClick += (_, _) => {
                player.ToggleFilter(filter);
                OnFiltersChange();
            };
            _filterIcons.Add(icon);
            Add(icon);
        }
    }

    public override void OnActivate() => UpdateFilters();

    public void UpdateFilters() {
        var player = RecipeFilteringPlayer.LocalPlayer.Filterer;
        foreach (var filter in _filterIcons) filter.SetActive(player.IsFilterActive(filter.Filter));
    }

    public void OnFiltersChange() {
        UpdateFilters();
        Recipe.FindRecipes();
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    private readonly List<UIRecipeFilterIcon> _filterIcons = [];
}

public class UIRecipeFilterIcon : UIElement {
    public UIRecipeFilterIcon(IRecipeFilter filter) {
        Filter = filter;
        _icon = Filter.GetImage();
        _icon.VAlign = 0.5f;
        _icon.HAlign = 0.5f;
        if (!_active) {
            if (_icon is IColorable colorable) _originalColor = colorable.Color;
            else if (_icon is UIImage image) _originalColor = image.Color;
        }
        _hoverText = Language.GetTextValue(Filter.GetDisplayNameKey());
        Append(_icon);
        Width.Set(_icon.Width.Pixels + 4, 0);
        Height.Set(_icon.Height.Pixels + 4, 0);
    }

    public void SetActive(bool active) {
        _active = active;
        UpdateIcon();
    }

    private void UpdateIcon() {
        Color color = _originalColor;
        if (!_active) color = color.MultiplyRGBA(new(80, 80, 80, 70));
        if (_icon is IColorable colorable) colorable.Color = color;
        else if (_icon is UIImage image) image.Color = color;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        if (!IsMouseHovering) return;
        GraphicsHelper.DrawMouseText(_hoverText);
        spriteBatch.Draw(TextureAssets.InfoIcon[13].Value, GetDimensions().Position(), Main.OurFavoriteColor);
    }

    public IRecipeFilter Filter { get; private set; }
    
    private readonly UIElement _icon;
    private readonly string _hoverText = string.Empty;
    private readonly Color _originalColor;
    private bool _active;
}