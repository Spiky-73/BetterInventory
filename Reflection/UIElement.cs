using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TElement = Terraria.ModLoader.Config.UI.ConfigElement;
namespace BetterInventory.Reflection;

public static class ConfigElement {
    public static readonly Property<TElement, Func<string>> TextDisplayFunction = new(nameof(TextDisplayFunction));
    public static readonly Property<TElement, Func<string>> TooltipFunction = new(nameof(TooltipFunction));
    public static readonly Property<TElement, bool> DrawLabel = new(nameof(DrawLabel));
    public static readonly Field<TElement, Color> backgroundColor = new(nameof(backgroundColor));
    public static readonly Method<TElement, SpriteBatch, object?> DrawSelf = new(nameof(DrawSelf));
}

public static class UIModConfig {
    public static readonly Type Type = Main.tModLoader.GetType("Terraria.ModLoader.Config.UI.UIModConfig")!;
    public static readonly StaticProperty<string> Tooltip = new(Type, nameof(Tooltip));
}

public static class ObjectElement {
    public static readonly Type Type = Main.tModLoader.GetType("Terraria.ModLoader.Config.UI.ObjectElement")!;
    public static readonly FieldInfo pendingChanges = Type.GetField(nameof(pendingChanges), BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo expandButton = Type.GetField(nameof(expandButton), BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo expanded = Type.GetField(nameof(expanded), BindingFlags.Instance | BindingFlags.NonPublic)!;
}