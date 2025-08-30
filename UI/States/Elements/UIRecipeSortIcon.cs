using BetterInventory.Crafting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpikysLib;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace BetterInventory.UI.Elements;

public class UIRecipeSortIcon : UIElement {

    public IRecipeSortStep SortStep {
        get => _sortStep!;
        set {
            _icon?.Remove();
            _sortStep = value;
            _hoverText = Language.GetTextValue(value.GetDisplayNameKey());
            _icon = value.GetImage();
            _icon.Left.Set(9 - _icon.Width.Pixels / 2, 0f);
            _icon.Top.Set(15 - _icon.Height.Pixels / 2, 0f);
            _arrow.Remove();
            Append(_icon);
            Append(_arrow);
        }
    }

    public UIRecipeSortIcon() {
        _arrow = new(RecipeUI.recipeSortToggle) {
            VAlign = 0.5f,
            HAlign = 0.5f
        };
        Width.Set(_arrow.Width.Pixels, 0);
        Height.Set(_arrow.Height.Pixels, 0);
        Append(_arrow);
    }

    protected override void DrawChildren(SpriteBatch spriteBatch) {
        base.DrawChildren(spriteBatch);
        if (IsMouseHovering) {
            spriteBatch.Draw(RecipeUI.recipeSortToggleBorder.Value, GetDimensions().Position(), Color.White);
            GraphicsHelper.DrawMouseText(_hoverText);
        }
    }

    private UIImage _arrow;
    private IRecipeSortStep? _sortStep;
    private string? _hoverText;
    private UIElement? _icon;
}