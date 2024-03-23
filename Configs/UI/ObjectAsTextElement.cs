using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace BetterInventory.Configs.UI;


public sealed class ObjectAsTextElement : ConfigElement<object> {

    public override void OnBind() {
        base.OnBind();

        if (Value is null) {
            object obj = Activator.CreateInstance(MemberInfo.Type, true)!;
            JsonDefaultValueAttribute jsonDefaultValueAttribute2 = JsonDefaultValueAttribute;
            JsonConvert.PopulateObject(jsonDefaultValueAttribute2?.Json ?? "{}", obj, ConfigManager.serializerSettings);
            Value = obj;
        }
        
        _dataList.Top = new(30f, 0f);
        _dataList.Left = new(14f, 0f);
        _dataList.Height = new(-30f, 1f);
        _dataList.Width = new(-14f, 1f);
        _dataList.ListPadding = 5f;
        _dataList.PaddingBottom = -5f;
        SetupList();

        _expandButton = new HoverImage(ExpandedTexture, Language.GetTextValue("tModLoader.ModConfigCollapse"));
        _expandButton.Top.Set(4f, 0f);
        _expandButton.Left.Set(-25f, 1f);
        _expandButton.OnLeftClick += (a, b) => Expand();
        Append(_expandButton);
        Append(_dataList);

        MaxHeight.Pixels = int.MaxValue;
        Recalculate();
    }

    public void Expand(){
        if (_expanded) {
            RemoveChild(_dataList);
            _expandButton.HoverText = Language.GetTextValue("tModLoader.ModConfigExpand");
            _expandButton.SetImage(CollapsedTexture);
        }
        else {
            Append(_dataList);
            _expandButton.HoverText = Language.GetTextValue("tModLoader.ModConfigCollapse");
            _expandButton.SetImage(ExpandedTexture);
        }
        _expanded = !_expanded;
        Recalculate();
    }

    public void SetupList(){
        _dataList.Clear();
        object data = Value;
        int top = 0;

        foreach (PropertyFieldWrapper variable in ConfigManager.GetFieldsAndProperties(data)) {
            if (Attribute.IsDefined(variable.MemberInfo, typeof(JsonIgnoreAttribute)) || Equals(variable.GetValue(data), Activator.CreateInstance(variable.Type))) continue;
            
            _entries.Add(new(new(Reflection.ConfigManager.GetLocalizedLabel.Invoke(variable), Reflection.ConfigManager.GetLocalizedTooltip.Invoke(variable))));
            (UIElement container, UIElement element) = ConfigManager.WrapIt(_dataList, ref top, new(Wrapper<Text>.Member), _entries[^1], 0);
        }
    }

    public override void Recalculate() {
        base.Recalculate();
        float h = (_dataList.Parent != null) ? (_dataList.GetTotalHeight() + 30) : 30;
        Height.Set(h, 0f);
        if (Parent != null && Parent is UISortableElement) Parent.Height.Set(h, 0f);
    }

    private bool _expanded = false;
    private HoverImage _expandButton = null!;
    private readonly UIList _dataList = new();
    private readonly List<Wrapper<Text>> _entries = new();
}

public class Wrapper<T> where T : new() {
    public Wrapper() => Value = new();
    public Wrapper(T value) => Value = value;

    [JsonIgnore] public T Value { get; set; }

    public static implicit operator T(Wrapper<T> wrapper) => wrapper.Value;
    [JsonIgnore] public static Reflection.Property<Wrapper<T>, T> Member => new(nameof(Value));
}