using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Newtonsoft.Json;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI.Chat;

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

    internal static void ILTextColors(ILContext il) {
        ILCursor cursor = new(il);

        if(!cursor.TryGotoNext(i => i.MatchCall(typeof(ChatManager), nameof(ChatManager.DrawColorCodedStringWithShadow)))
                || !cursor.TryGotoPrev(MoveType.After, i => i.MatchLdloc3())) {
            return; // TODO log info and other ILs
        }
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Color color, ConfigElement self) => {
            if (self is not TextElement textElem) return color;
            Text? text = textElem.Value;
            if (text is null || !text.Label.HasValue || !text.Label.Value.Color.HasValue) return color;
            return color == Color.White ? text.Label.Value.Color.Value : text.Label.Value.Color.Value.MultiplyRGB(color);
        });
    }
}

public sealed class TextElement : ConfigElement<Text?> {

    public new Text? Value => base.Value;

    public override void OnBind() {
        base.OnBind();
        Text? value = Value;
        if (value is null) return;
        if (value.Label.HasValue) Label = Label.FormatTagKeys(value.Label.Value.Tags);
        if (value.Tooltip.HasValue) {
            string tooltip = TooltipFunction().FormatTagKeys(value.Tooltip.Value.Tags);
            TooltipFunction = () => tooltip;
        }
    }

    public override void Recalculate() {
        base.Recalculate();
        Vector2 size = ChatManager.GetStringSize(FontAssets.ItemStack.Value, Label, new Vector2(0.8f), GetDimensions().Width + 1);
        Height.Pixels = size.Y + 30 - FontAssets.ItemStack.Value.LineSpacing;
        if (Parent != null && Parent is UISortableElement) Parent.Height.Set(Height.Pixels, 0f);
    }
}