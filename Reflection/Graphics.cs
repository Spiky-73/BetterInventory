
using TTextureAssets = Terraria.GameContent.TextureAssets;
using TAsset_T2D = ReLogic.Content.Asset<Microsoft.Xna.Framework.Graphics.Texture2D>;
namespace BetterInventory.Reflection;

public static class TextureAssets {
    public static readonly StaticField<TAsset_T2D> CraftUpButton = new(typeof(TTextureAssets), nameof(TTextureAssets.CraftUpButton));
    public static readonly StaticField<TAsset_T2D> CraftDownButton = new(typeof(TTextureAssets), nameof(TTextureAssets.CraftDownButton));
    public static readonly StaticField<TAsset_T2D[]> CraftToggle = new(typeof(TTextureAssets), nameof(TTextureAssets.CraftToggle));
}

public static class Asset<T> where T: class{
    public static readonly Property<ReLogic.Content.Asset<T>, T> Value = new(nameof(ReLogic.Content.Asset<T>.Value));
}