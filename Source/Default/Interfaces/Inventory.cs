using Terraria;
using Terraria.GameInput;
using Terraria.Graphics.Capture;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace BetterInventory.Default.Interfaces;

public sealed class GameInterface : ModInterface {

    // Taken for Player.ToggleInv()
    public override bool Active => !Main.mapFullscreen && !PlayerInput.InBuildingMode && !Main.ingameOptionsWindow && !Main.inFancyUI
        && !CaptureManager.Instance.Active && Main.LocalPlayer.talkNPC < 0 && Main.LocalPlayer.sign < 0 && !Main.clothesWindow && !Main.playerInventory;

    public override void Activate() {
        if (Active) return;
        Main.LocalPlayer.ToggleInv();
        if (Main.playerInventory) Main.LocalPlayer.ToggleInv();
    }
}
public sealed class PlayerInventory : ModInterface {
    public override bool Active => Main.playerInventory;

    public override void Activate() {
        if (Active) return;
        Main.LocalPlayer.ToggleInv();
        if (!Main.playerInventory) Main.LocalPlayer.ToggleInv(); // In case we were in another interface (map fullscreen, fancy ui, etc...)
    }
}

public sealed class Settings : ModInterface {
    public override bool Active => Main.ingameOptionsWindow;

    public override void Activate() {
        if (Active) return;
        IngameOptions.Open();
    }
}

public sealed class ModConfigList : ModInterface {
    public override bool Active => Main.InGameUI.CurrentState == Interface.modConfigList;

    public override void Activate() {
        if (Active) return;
        IngameOptions.Close();
        IngameFancyUI.OpenUIState(Interface.modConfigList);
    }
}

public sealed class ControlsMenu : ModInterface {
    public override bool Active => Main.InGameUI.CurrentState == Main.ManageControlsMenu;

    public override void Activate() {
        if (Active) return;
        IngameOptions.Close();
        IngameFancyUI.OpenUIState(Main.ManageControlsMenu);
    }
}