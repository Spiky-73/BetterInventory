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

public class Notification : IInGameNotification {

    public static TagKeyFormat DownloadTags => new(null, new());
    public static TagKeyFormat UpdateTags => new(null, new(){
        ($"{Localization.Keys.Configs}.Version.DisplayName", $"c/{Colors.RarityCyan.Hex3()}"),
    });
    public static TagKeyFormat UnloadedMore => new(Colors.RarityAmber, new(){
        ($"{Localization.Keys.Configs}.Compatibility.DisplayName", $"c/{Colors.RarityCyan.Hex3()}")
    });
    public static TagKeyFormat UnloadedLess => new(Colors.RarityGreen, new(){
        ($"{Localization.Keys.Configs}.Compatibility.DisplayName", $"c/{Colors.RarityCyan.Hex3()}")
    });
    public static TagKeyFormat BugsTags => new(null, new(){
        ($"{Localization.Keys.Chat}.Workshop", $"c/{Colors.RarityCyan.Hex3()}"),
        ($"{Localization.Keys.Chat}.Homepage", $"c/{Colors.RarityCyan.Hex3()}")
    });
    public static TagKeyFormat ImportantTags => new(Colors.RarityAmber, new());

    public static void DisplayUpdate() {
        bool download;
        if (Version.Instance.lastPlayedVersion.Length == 0) download = true;
        else if (BetterInventory.Instance.Version > new System.Version(Version.Instance.lastPlayedVersion)) download = false;
        else return;

        Version.Instance.lastPlayedVersion = BetterInventory.Instance.Version.ToString();
        Version.Instance.SaveConfig();

        List<(LocalizedText text, TagKeyFormat format)> lines = new();
        if (download) lines.Add((Language.GetText($"{Localization.Keys.Chat}.Download"), DownloadTags));
        else lines.Add((Language.GetText($"{Localization.Keys.Chat}.Update"), UpdateTags));
        lines.Add((Language.GetText($"{Localization.Keys.Chat}.Bug"), BugsTags));
        LocalizedText important = Language.GetText($"{Localization.Keys.Chat}.Important");
        if (!download && important.Value.Length != 0) lines.Add((important, ImportantTags));
        InGameNotificationsTracker.AddNotification(new Notification(lines, 15*60));
    }

    public static void DisplayCompatibility() {
        bool failled;
        if (Hooks.FailledILs > Compatibility.Instance.failledILs) failled = true;
        else if (Hooks.FailledILs < Compatibility.Instance.failledILs) failled = false;
        else return;
        Compatibility.Instance.failledILs = Hooks.FailledILs;
        Compatibility.Instance.SaveConfig();

        List<(LocalizedText text, TagKeyFormat format)> lines = new() {
            failled ? (Language.GetText($"{Localization.Keys.Chat}.UnloadedMore"), UnloadedMore) : (Language.GetText($"{Localization.Keys.Chat}.UnloadedLess"), UnloadedLess)
        };
        InGameNotificationsTracker.AddNotification(new Notification(lines));
    }


    public Notification(List<(LocalizedText text, TagKeyFormat format)> lines, int lifeSpan = 5 * 60) {
        MaxLifeSpan = LifeSpan = lifeSpan;
        
        _lines = new();
        _textSize = new();
        foreach((LocalizedText text, TagKeyFormat format) in lines) {
            string line = text.Value;
            Vector2 size = FontAssets.MouseText.Value.MeasureString(line);
            if (size.X > _textSize.X) _textSize.X = size.X;
            _textSize.Y += size.Y;
            _lines.Add(new(line.FormatTagKeys(format.Tags), size, format.Color));
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

    public int MaxLifeSpan { get; }
    public int LifeSpan { get; private set; }

    public float Scale {
        get {
            if (LifeSpan < 60 / 2) return MathHelper.Lerp(0f, 1f, LifeSpan / (60 / 2f));
            if (LifeSpan > MaxLifeSpan - 60 / 4) return MathHelper.Lerp(1f, 0f, (LifeSpan - (MaxLifeSpan - 60 / 4)) / (60 / 4f));
            return 1;
        }
    }


    public float Opacity => Scale <= 0.2f ? 0f : (Scale - 0.2f) / 0.8f;


    private readonly List<LineInfo> _lines;
    private Vector2 _textSize;

    public static Asset<Texture2D> ModIcon => ModContent.Request<Texture2D>($"BetterInventory/icon");
    public static readonly Vector2 Padding = new(7, 5);

    private readonly record struct LineInfo(string Text, Vector2 Size, Color? Color);
}
