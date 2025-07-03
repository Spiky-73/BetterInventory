using SpikysLib.Reflection;
using Terraria.ModLoader;
using TItem = Terraria.Item;
using TRecipe = Terraria.Recipe;
using TAccPlayer = Terraria.ModLoader.Default.ModAccessorySlotPlayer;
using TAccLoader = Terraria.ModLoader.AccessorySlotLoader;
using TBuilderLoader = Terraria.ModLoader.BuilderToggleLoader;
using TRecipeLoader = Terraria.ModLoader.RecipeLoader;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TColor = Microsoft.Xna.Framework.Color;
using TItemLoader = Terraria.ModLoader.ItemLoader;
using TVector2 = Microsoft.Xna.Framework.Vector2;
using System.Collections.ObjectModel;
using System.Reflection;
using System;

namespace BetterInventory.Reflection;

public static class ModAccessorySlotPlayer {
    public static readonly Field<TAccPlayer, TItem[]> exAccessorySlot = new(nameof(exAccessorySlot));
    public static readonly Field<TAccPlayer, TItem[]> exDyesAccessory = new(nameof(exDyesAccessory));
    public static readonly Field<TAccPlayer, bool[]> sharedLoadoutSlotTypes = new(nameof(sharedLoadoutSlotTypes));
    public static readonly FieldInfo exLoadouts = typeof(TAccPlayer).GetField(nameof(exLoadouts), Member<FieldInfo>.InstanceFlags)!;
    public static readonly Method<TAccPlayer, bool> IsSharedSlot = new(nameof(IsSharedSlot), typeof(int));

    public static class ExEquipmentLoadout {
        public static readonly Type Type = typeof(TAccPlayer).GetNestedType(nameof(ExEquipmentLoadout), BindingFlags.NonPublic)!;
        public static readonly Property<object, TItem[]> ExAccessorySlot = new(Type.GetProperty(nameof(ExAccessorySlot))!);
        public static readonly Property<object, TItem[]> ExDyesAccessory = new(Type.GetProperty(nameof(ExDyesAccessory))!);
    }
}

public static class AccessorySlotLoader {
    public static readonly Method<TAccLoader, object?> DrawAccSlots = new(nameof(TAccLoader.DrawAccSlots), typeof(int));
    public static readonly Method<TAccLoader, object?> DrawSlotTexture = new(nameof(DrawSlotTexture), typeof(Texture2D), typeof(TVector2), typeof(Rectangle), typeof(TColor), typeof(float), typeof(TVector2), typeof(float), typeof(SpriteEffects), typeof(float), typeof(int), typeof(int));
}

public static class BuilderToggleLoader {
    public static readonly StaticField<System.Collections.Generic.List<BuilderToggle>> BuilderToggles = new(typeof(TBuilderLoader), nameof(BuilderToggles));
}

public static class RecipeLoader {
    public static readonly StaticMethod<object?> OnCraft = new(typeof(TRecipeLoader), nameof(TRecipeLoader.OnCraft), typeof(TItem), typeof(TRecipe), typeof(System.Collections.Generic.List<TItem>), typeof(TItem));
    public static readonly StaticField<System.Collections.Generic.List<TItem>> ConsumedItems = new(typeof(TRecipeLoader), nameof(ConsumedItems));
}

public static class ItemLoader {
    public static readonly StaticMethod<bool> PreDrawTooltip = new(typeof(TItemLoader), nameof(TItemLoader.PreDrawTooltip), typeof(TItem), typeof(ReadOnlyCollection<TooltipLine>), typeof(int).MakeByRefType(), typeof(int).MakeByRefType());
    public static readonly StaticMethod<System.Collections.Generic.List<TooltipLine>> ModifyTooltips = new(typeof(TItemLoader), nameof(TItemLoader.ModifyTooltips), typeof(TItem), typeof(int).MakeByRefType(), typeof(string[]), typeof(string[]).MakeByRefType(), typeof(bool[]).MakeByRefType(), typeof(bool[]).MakeByRefType(), typeof(int).MakeByRefType(), typeof(TColor?[]).MakeByRefType(), typeof(int));
    public delegate System.Collections.Generic.List<TooltipLine> ModifyTooltipsFn(TItem item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out TColor?[] overrideColor, int prefixlineIndex);
}