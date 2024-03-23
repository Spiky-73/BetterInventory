using System;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace BetterInventory.Configs.UI;

public interface IChildValue {
    object Parent { get; set; }
    [Expand(false, false)]
    public object Value { get; set; }
}

[CustomModConfigItem(typeof(ChildValueElement))]
public class ChildValue<TParent, TValue> : IChildValue where TParent: struct where TValue: class, new() {
    public ChildValue() : this(default) { } 
    public ChildValue(TParent parent = default, TValue? value = default) {
        Parent = parent;
        Value = value ?? new();
    }

    public TParent Parent { get; set; }

    [Expand(false, false)]
    public TValue Value { get; set; }
    [Expand(false, false)]
    object IChildValue.Parent { get => Parent; set => Parent = (TParent)value!; }
    object IChildValue.Value { get => Value; set => Value = (TValue)value!; }

    public static implicit operator TParent(ChildValue<TParent, TValue> self) => self.Parent;
}

public sealed class Toggle<T> : ChildValue<bool, T> where T: class, new() {
    public Toggle() : this(default) {}
    public Toggle(bool enabled = default, T? value = null) : base(enabled, value) {}
}

public sealed class ChildValueElement : ConfigElement<IChildValue> {
    public override void OnBind() {
        base.OnBind();

        IChildValue value = Value;

        int top = 0;
        (UIElement container, UIElement child) = ConfigManager.WrapIt(this, ref top, new(value.GetType().GetProperty(nameof(IChildValue.Value))), value, 0);
        _child = (ConfigElement)child;
        container.Left.Pixels -= 20;
        container.Width.Pixels += 20;
        DrawLabel = false;
        Reflection.ConfigElement.TextDisplayFunction.SetValue(_child, TextDisplayFunction);
        Reflection.ConfigElement.TooltipFunction.SetValue(_child, TooltipFunction);
        Reflection.ConfigElement.backgroundColor.SetValue(_child, Color.Transparent);
        UIImage expandButton = (UIImage)Reflection.ObjectElement.expandButton.GetValue(_child)!;
        expandButton.Left.Set(-25f, 1f);

        top = 0;
        (UIElement toggleContainer, UIElement toggle) = ConfigManager.WrapIt(this, ref top, new(value.GetType().GetProperty(nameof(IChildValue.Parent))), value, 0);
        _toggle = (ConfigElement)toggle;

        _toggle.OnLeftDoubleClick += (_, _) => Expand();
        toggleContainer.Left.Pixels -= 20;
        toggleContainer.Width.Pixels -= 5;

        DrawLabel = false;
        Func<string> parentText = Reflection.ConfigElement.TextDisplayFunction.GetValue(_toggle);
        Reflection.ConfigElement.TextDisplayFunction.SetValue(_toggle, () => $"{TextDisplayFunction()}{parentText()[nameof(IChildValue.Parent).Length..]}");
        Reflection.ConfigElement.TooltipFunction.SetValue(_toggle, TooltipFunction);
        Reflection.ConfigElement.backgroundColor.SetValue(_toggle, Color.Transparent);
    }

    public void Expand() {
        Reflection.ObjectElement.expanded.SetValue(_child, !(bool)Reflection.ObjectElement.expanded.GetValue(_child)!);
        Reflection.ObjectElement.pendingChanges.SetValue(_child, true);
    }

    public override void Recalculate() {
        base.Recalculate();
        Height = _child.Height;
        if (Parent != null && Parent is UISortableElement) Parent.Height.Set(Height.Pixels, 0f);
    }

    private ConfigElement _toggle = null!;
    private ConfigElement _child = null!;
}