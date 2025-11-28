using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using SpikysLib.Reflection;
using TDynamicSpriteFontExtensionMethods = ReLogic.Graphics.DynamicSpriteFontExtensionMethods;
using TVector2 = Microsoft.Xna.Framework.Vector2;
using TColor = Microsoft.Xna.Framework.Color;

namespace BetterInventory.Reflection;

public static class DynamicSpriteFontExtensionMethods {
    public static readonly StaticMethod<object?> DrawString_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_float_SpriteEffects_float = new(typeof(TDynamicSpriteFontExtensionMethods), nameof(TDynamicSpriteFontExtensionMethods.DrawString),
    typeof(SpriteBatch),
    typeof(DynamicSpriteFont),
    typeof(string),
    typeof(TVector2),
    typeof(TColor),
    typeof(float),
    typeof(TVector2),
    typeof(float),
    typeof(SpriteEffects),
    typeof(float));
}