using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Configs.UI;

public readonly record struct NotificationLine(string Text, Color? Color, Dictionary<string, string>? Tags){

    public static NotificationLine Download => new(Language.GetTextValue("Mods.BetterInventory.Chat.Download"), null, null);
    public static NotificationLine Update => new(Language.GetTextValue($"Mods.BetterInventory.Chat.Update", BetterInventory.Instance.Version), null, new(){
        { $"Version { BetterInventory.Instance.Version}", $"c/{Colors.RarityCyan.Hex3()}" }
    });
    public static NotificationLine Bug => new(Language.GetTextValue("Mods.BetterInventory.Chat.Bug"), null, new(){
        { "Steam Workshop page", $"c/{Colors.RarityCyan.Hex3()}"},
        { "GitHub", $"c/{Colors.RarityCyan.Hex3()}"}
    });
    public static NotificationLine Important => new(Language.GetTextValue("Mods.BetterInventory.Chat.Important"), Colors.RarityAmber, new(){
        // {"I highly advise you to look at the configs and adject them", $"c/ff0000"}
    });
}


public class UpdateNotification : IInGameNotification {


    public UpdateNotification(List<NotificationLine> lines) {
        LifeSpan = MaxLifeSpan;
        
        _lines = new();
        _textSize = new(0, FontAssets.MouseText.Value.LineSpacing * lines.Count);
        foreach(NotificationLine line in lines) {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(line.Text);
            if (size.X > _textSize.X) _textSize.X = size.X;

            string text = line.Text;
            if (line.Tags is not null) {
                foreach ((string word, string tag) in line.Tags) text = text.Replace(word, $"[{tag}:{word}]");
            }
            _lines.Add(new(text, size, line.Color));
        }
    }

    public void Update() {
        if (LifeSpan > 0) LifeSpan--;
    }

    public void DrawInGame(SpriteBatch spriteBatch, Vector2 bottomAnchorPosition) {
        if (Opacity <= 0f) return;

        float iconScale = 0.3f;
        Vector2 size = _textSize + Padding*2;
        size.X += iconScale * 80 + Padding.X;
        size *= Scale;

        Rectangle panelSize = Utils.CenteredRectangle(bottomAnchorPosition + new Vector2(0f, (0f - size.Y) * 0.5f), size);
        bool hovering = panelSize.Contains(Main.MouseScreen.ToPoint());
        Vector2 position = panelSize.Right() - new Vector2(Padding.X, 0);

        Utils.DrawInvBG(spriteBatch, panelSize, new Color(64, 109, 164) * (hovering ? 0.75f : 0.5f));
        spriteBatch.Draw(ModIcon.Value, position, null, Color.White * Opacity, 0f, new Vector2(80, 80 / 2f), iconScale * Scale, SpriteEffects.None, 0f);
        position = panelSize.TopLeft() + Padding * Scale;
        foreach (var line in _lines) {
            Utils.DrawBorderString(spriteBatch, line.Text, position, line.Color ?? new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor / 5, Main.mouseTextColor) * Opacity, Scale, 0, -0.1f);
            position.Y += line.Size.Y * Scale;
        }
        if (hovering) OnMouseOver();
    }

    private void OnMouseOver() {
        if (PlayerInput.IgnoreMouseInterface || LifeSpan <= 30) return;
        Main.LocalPlayer.mouseInterface = true;
        if(LifeSpan < 60 + 60/2) LifeSpan = 60 + 60 / 2;
        
        if (!Main.mouseLeft || !Main.mouseLeftRelease) return;
        Main.mouseLeftRelease = false;
        LifeSpan = 30;
    }

    public void PushAnchor(ref Vector2 positionAnchorBottom) => positionAnchorBottom.Y -= (_textSize.Y + 2*Padding.Y)  * Opacity;


    public bool ShouldBeRemoved => LifeSpan <= 0;

    public int LifeSpan { get; private set; }

    public float Scale => LifeSpan switch {
        < 60 / 2 => MathHelper.Lerp(0f, 1f, LifeSpan / (60 / 2f)),
        > MaxLifeSpan - 60 / 4 => MathHelper.Lerp(1f, 0f, (LifeSpan - (MaxLifeSpan - 60 / 4)) / (60 / 4f)),
        _ => 1f,
    };
    public float Opacity => Scale <= 0.2f ? 0f : (Scale - 0.2f) / 0.8f;


    private readonly List<LineInfo> _lines;
    private Vector2 _textSize;

    public const int MaxLifeSpan = 15 * 60;
    public static Asset<Texture2D> ModIcon => ModContent.Request<Texture2D>($"BetterInventory/icon");
    public static readonly Vector2 Padding = new(7, 5);

    private readonly record struct LineInfo(string Text, Vector2 Size, Color? Color);
}
