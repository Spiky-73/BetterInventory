using BetterInventory.Crafting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpikysLib;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace BetterInventory.UI.Elements;

public class UIRecipeFilterIcon : UIElement {
    public UIRecipeFilterIcon(IRecipeFilter filter, int recipeCount, bool active) {
        bool available = recipeCount > 0;

        UIElement item = available ? filter.GetImage() : filter.GetImageGray();
        item.VAlign = 0.5f;
        item.HAlign = 0.5f;
        Width.Set(item.Width.Pixels+4, 0);
        Height.Set(item.Height.Pixels+4, 0);
        if (!active) {
            Color alpha = new(80, 80, 80, 70);
            if(item is IColorable colorable) colorable.Color = colorable.Color.MultiplyRGBA(alpha);
            else if (item is UIImage image) image.Color = image.Color.MultiplyRGBA(alpha);
        }
        Append(item);

        _hoverText = Language.GetTextValue(filter.GetDisplayNameKey());
        if (available || active) _hoverText = Language.GetTextValue($"{Localization.Keys.UI}.Filter", _hoverText, recipeCount);
        if (!available) _hoverText = $"[c/{Colors.RarityTrash.Hex3()}:{_hoverText}]";
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        base.DrawSelf(spriteBatch);
        if (IsMouseHovering) {
            GraphicsHelper.DrawMouseText(_hoverText);
            spriteBatch.Draw(TextureAssets.InfoIcon[13].Value, GetDimensions().Position(), Main.OurFavoriteColor);
        }
    }

    private string _hoverText;
}