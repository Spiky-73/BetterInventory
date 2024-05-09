using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.ItemSearch;

public sealed class BetterNPC : GlobalNPC {
    public override void OnChatButtonClicked(NPC npc, bool firstButton) {
        if (!Configs.BetterGuide.Enabled || npc.type != NPCID.Guide) return;
        Main.InGuideCraftMenu = true;
        Main.recBigList = true;
        Guide.FindGuideRecipes();
    }
}