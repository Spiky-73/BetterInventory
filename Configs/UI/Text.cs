using System.Linq;
using Newtonsoft.Json;
using Terraria.GameContent;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;

namespace BetterInventory.Configs.UI;

[CustomModConfigItem(typeof(TextElement))]
public sealed class Text {
    public Text() {}
    public Text(string? label = null, string? tooltip = null) {
        Label = label;
        Tooltip = tooltip;
    }

    [JsonIgnore] public string? Label { get; }
    [JsonIgnore] public string? Tooltip { get; }
}

public sealed class TextElement : ConfigElement<Text?> {

    public override void OnBind() {
        base.OnBind();
        Text? value = Value;
        if(value?.Label is not null) TextDisplayFunction = () => Value!.Label;
        if(value?.Tooltip is not null) TooltipFunction = () => Value!.Tooltip;
        Height.Set(30, 0);
    }

    public override void Recalculate() {
        base.Recalculate();
        Height.Pixels = 30 + Label.Count(c => c == '\n') * FontAssets.ItemStack.Value.LineSpacing * 0.8f;
        if (Parent != null && Parent is UISortableElement) Parent.Height.Set(Height.Pixels, 0f);
    }
}