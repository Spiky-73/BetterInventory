using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;

namespace BetterInventory.Configs.UI;

public readonly record struct TagKeyFormat(Color? Color, List<(string, string)> Tags);

[CustomModConfigItem(typeof(TextElement))]
public sealed class Text {
    public Text() {}
    public Text(TagKeyFormat? label = null, TagKeyFormat? tooltip = null) {
        Label = label;
        Tooltip = tooltip;
    }

    [JsonIgnore] public TagKeyFormat? Label { get; }
    [JsonIgnore] public TagKeyFormat? Tooltip { get; }
}

public sealed class TextElement : ConfigElement<Text?> {

    public override void OnBind() {
        base.OnBind();

        // TODO text colors        
        Text? value = Value;
        if (value?.Label.HasValue == true) {
            Label = Label.FormatTagKeys(value.Label.Value.Tags);
        }
        if (value?.Tooltip.HasValue == true) {
            string tooltip = TooltipFunction().FormatTagKeys(value.Tooltip.Value.Tags);
            TooltipFunction = () => tooltip;
        }
    }

    public override void Recalculate() {
        base.Recalculate();
        Height.Pixels = 30 + Label.Count(c => c == '\n') * FontAssets.ItemStack.Value.LineSpacing * 0.8f;
        if (Parent != null && Parent is UISortableElement) Parent.Height.Set(Height.Pixels, 0f);
    }
}