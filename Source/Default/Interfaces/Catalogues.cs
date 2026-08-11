using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Default.Interfaces;

public sealed class CataloguesClosed : ModInterface {
    public override bool Active => !ModContent.GetInstance<RecipeList>().Active && !ModContent.GetInstance<Bestiary>().Active && !ModContent.GetInstance<JourneyCatalogue>().Active;

    public override void Activate() {
        if (Active) return;

        Main.recBigList = false;
        IngameFancyUI.Close();
        if (Main.CreativeMenu.Enabled) Main.CreativeMenu.ToggleMenu();
    }
}

public sealed class RecipeList : ModInterface {
    public override bool Active => Main.playerInventory && Main.recBigList && !Main.CreativeMenu.Enabled;

    public override void Activate() {
        if (Active) return;

        if (!Main.playerInventory) {
            Main.LocalPlayer.ToggleInv();
            if (!Main.playerInventory) Main.LocalPlayer.ToggleInv(); // In case we were in another interface (map fullscreen, fancy ui, etc...)
        } else {
            Main.CreativeMenu.CloseMenu();
            Main.LocalPlayer.tileEntityAnchor.Clear();
        }
        Main.recBigList = Main.numAvailableRecipes > 0;
    }
}

public sealed class Bestiary : ModInterface {
    public override bool Active => Main.InGameUI.CurrentState == Main.BestiaryUI;

    public override void Activate() {
        if (Active) return;
        Main.LocalPlayer.SetTalkNPC(-1, false);
        Main.npcChatCornerItem = 0;
        Main.npcChatText = string.Empty;
        IngameFancyUI.OpenUIState(Main.BestiaryUI);
        Main.BestiaryUI.OnOpenPage();
    }
}

public sealed class JourneyCatalogue : ModInterface {
    public sealed override bool Available => Main.LocalPlayer.difficulty == 3;
    public override bool Active => Main.CreativeMenu.Enabled && Main.CreativeMenu._uiState._mainCategory.CurrentOption == (int)UICreativePowersMenu.OpenMainSubCategory.InfiniteItems;

    public override void Activate() {
        if (Active) return;
        Main.LocalPlayer.ToggleCreativeMenu();
        var uiState = Main.CreativeMenu._uiState;
        if (uiState._mainCategory.CurrentOption != (int)UICreativePowersMenu.OpenMainSubCategory.InfiniteItems) {
            Main.CreativeMenu._uiState.ToggleCategory(uiState._mainCategory, (int)UICreativePowersMenu.OpenMainSubCategory.InfiniteItems, UICreativePowersMenu.OpenMainSubCategory.None);
            uiState.RefreshElementsOrder();
        }
    }
}
