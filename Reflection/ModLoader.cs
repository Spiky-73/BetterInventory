using System.Collections.Generic;
using SpikysLib.Reflection;
using Terraria.ModLoader;
using TItem = Terraria.Item;
using TRecipe = Terraria.Recipe;
using TAccPlayer = Terraria.ModLoader.Default.ModAccessorySlotPlayer;
using TAccLoader = Terraria.ModLoader.AccessorySlotLoader;
using TBuilderLoader = Terraria.ModLoader.BuilderToggleLoader;
using TLoader = Terraria.ModLoader.RecipeLoader;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TColor = Microsoft.Xna.Framework.Color;
using TItemLoader = Terraria.ModLoader.ItemLoader;
using TVector2 = Microsoft.Xna.Framework.Vector2;
using System.Collections.ObjectModel;

namespace BetterInventory.Reflection;

public static class ModAccessorySlotPlayer {
    public static readonly Field<TAccPlayer, TItem[]> exAccessorySlot = new(nameof(exAccessorySlot));
    public static readonly Field<TAccPlayer, TItem[]> exDyesAccessory = new(nameof(exDyesAccessory));
}

public static class AccessorySlotLoader {
    public static readonly Method<TAccLoader, object?> DrawAccSlots = new(nameof(TAccLoader.DrawAccSlots), typeof(int));
    public static readonly Method<TAccLoader, object?> DrawSlotTexture = new(nameof(DrawSlotTexture), typeof(Texture2D), typeof(TVector2), typeof(Rectangle), typeof(TColor), typeof(float), typeof(TVector2), typeof(float), typeof(SpriteEffects), typeof(float), typeof(int), typeof(int));
}

public static class BuilderToggleLoader {
    public static readonly StaticField<List<BuilderToggle>> BuilderToggles = new(typeof(TBuilderLoader), nameof(BuilderToggles));
}

public static class RecipeLoader {
    public static readonly StaticMethod<object?> OnCraft = new(typeof(TLoader), nameof(TLoader.OnCraft), typeof(TItem), typeof(TRecipe), typeof(List<TItem>), typeof(TItem));
    public static readonly StaticField<List<TItem>> ConsumedItems = new(typeof(TLoader), nameof(ConsumedItems));
}

public static class ItemLoader {
    public static readonly StaticMethod<bool> PreDrawTooltip = new(typeof(TItemLoader), nameof(TItemLoader.PreDrawTooltip), typeof(TItem), typeof(ReadOnlyCollection<TooltipLine>), typeof(int).MakeByRefType(), typeof(int).MakeByRefType());
    public static readonly StaticMethod<List<TooltipLine>> ModifyTooltips = new(typeof(TItemLoader), nameof(TItemLoader.ModifyTooltips), typeof(TItem), typeof(int).MakeByRefType(), typeof(string[]), typeof(string[]).MakeByRefType(), typeof(bool[]).MakeByRefType(), typeof(bool[]).MakeByRefType(), typeof(int).MakeByRefType(), typeof(TColor?[]).MakeByRefType(), typeof(int));
    public delegate List<TooltipLine> ModifyTooltipsFn(TItem item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out TColor?[] overrideColor, int prefixlineIndex);
}