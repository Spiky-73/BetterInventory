using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
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

    internal static void ILColors(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(i => i.MatchCall(typeof(Terraria.UI.Chat.ChatManager), nameof(Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow)));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdloc3());
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
        Height.Pixels = 30 + Label.Count(c => c == '\n') * FontAssets.ItemStack.Value.LineSpacing * 0.8f;
        if (Parent != null && Parent is UISortableElement) Parent.Height.Set(Height.Pixels, 0f);
    }
}