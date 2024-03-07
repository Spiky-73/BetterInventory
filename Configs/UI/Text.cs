using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Newtonsoft.Json;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI.Chat;

namespace BetterInventory.Configs.UI;

public readonly record struct TagKeyFormat(Color? Color, List<(string, string)> Tags);

[CustomModConfigItem(typeof(TextElement))]
public sealed class Text {
    public Text() {}
    public Text(TagKeyFormat? labelFormat = null, TagKeyFormat? tooltipFormat = null) : this(null, null, labelFormat, tooltipFormat) {}
    public Text(string? label = null, string? tooltip = null, TagKeyFormat? labelFormat = null, TagKeyFormat? tooltipFormat = null) {
        Label = label; Tooltip = tooltip;
        LabelFormat = labelFormat; TooltipFormat = tooltipFormat;
    }

    [JsonIgnore] public string? Label { get; }
    [JsonIgnore] public string? Tooltip { get; }
    [JsonIgnore] public TagKeyFormat? LabelFormat { get; }
    [JsonIgnore] public TagKeyFormat? TooltipFormat { get; }

    internal static void ILTextColors(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(i => i.MatchCall(typeof(ChatManager), nameof(ChatManager.DrawColorCodedStringWithShadow)));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdloc3());
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Color color, ConfigElement self) => {
            if (self is not TextElement textElem) return color;
            Text? text = textElem.Value;
            if (text is null || !text.LabelFormat.HasValue || !text.LabelFormat.Value.Color.HasValue) return color;
            return color == Color.White ? text.LabelFormat.Value.Color.Value : text.LabelFormat.Value.Color.Value.MultiplyRGB(color);
        });
    }
}

public sealed class TextElement : ConfigElement<Text?> {

    public new Text? Value => base.Value;

    public override void OnBind() {
        base.OnBind();
        Text? value = Value;
        if (value is null) return;
        if (value.Label is not null) Label = Language.GetTextValue(value.Label);
        if (value.LabelFormat.HasValue) Label = Label.FormatTagKeys(value.LabelFormat.Value.Tags);

        string tooltip = TooltipFunction();
        if (value.Tooltip is not null) tooltip = Language.GetTextValue(value.Tooltip);
        if (value.TooltipFormat.HasValue) tooltip.FormatTagKeys(value.TooltipFormat.Value.Tags);
        TooltipFunction = () => tooltip;
    }

    public override void Recalculate() {
        base.Recalculate();
        Vector2 size = ChatManager.GetStringSize(FontAssets.ItemStack.Value, Label, new Vector2(0.8f), GetDimensions().Width + 1);
        Height.Pixels = size.Y + 30 - FontAssets.ItemStack.Value.LineSpacing;
        if (Parent != null && Parent is UISortableElement) Parent.Height.Set(Height.Pixels, 0f);
    }
}