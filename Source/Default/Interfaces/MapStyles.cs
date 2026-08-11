using Terraria;

namespace BetterInventory.Default.Interfaces;

public abstract class MapStyleInterface : ModInterface {
    public abstract int Style { get; }
    public sealed override bool Active => !Main.mapFullscreen && Main.mapStyle == Style;
    public sealed override void Activate() {
        if (Active) return;
        if (Main.mapFullscreen) Main.LocalPlayer.ToggleInv();
        Main.mapStyle = Style;
    }
}
public sealed class MapClosed : MapStyleInterface {
    public sealed override int Style => 0;
}
public sealed class MiniMap : MapStyleInterface {
    public sealed override int Style => 1;
}
public sealed class BackgroundMap : MapStyleInterface {
    public sealed override int Style => 2;
}

public sealed class FullScreenMap : ModInterface {
    public override bool Active => Main.mapFullscreen;

    public override void Activate() {
        if (Active) return;
        Main.playerInventory = false;
        Main.LocalPlayer.SetTalkNPC(-1);
        Main.npcChatCornerItem = 0;
        Main.mapFullscreenScale = 2.5f;
        Main.mapFullscreen = true;
        Main.resetMapFull = true;
        Main.mapStyle = 0;
    }
}