using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;
public interface IChildValue {
    object Parent { get; set; }
    [Expand(false, false)]
    public object Value { get; set; }
}


[CustomModConfigItem(typeof(UI.ChildValueElement))]
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