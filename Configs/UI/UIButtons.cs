using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace BetterInventory.Configs.UI;

public sealed class HoverImage : UIImage {

    public HoverImage(Asset<Texture2D> texture, string hoverText) : base(texture) {
        HoverText = hoverText;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        base.DrawSelf(spriteBatch);
        if (IsMouseHovering) Reflection.UIModConfig.Tooltip.SetValue(HoverText);
    }
    public string HoverText {get; set;}
}

public sealed class HoverImageSplit : UIImage {

    public bool HoveringUp => Main.mouseY < GetDimensions().Y + GetDimensions().Height / 2;

    public HoverImageSplit(Asset<Texture2D> texture, string hoverTextUp, string hoverTextDown) : base(texture) {
        HoverTextUp = hoverTextUp;
        HoverTextDown = hoverTextDown;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        base.DrawSelf(spriteBatch);
        if (IsMouseHovering) Reflection.UIModConfig.Tooltip.SetValue(HoveringUp ? HoverTextUp : HoverTextDown);
    }

    public string HoverTextUp {get; set;}
    public string HoverTextDown {get; set;}
}
