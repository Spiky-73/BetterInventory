using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.ItemSearch;

public class BetterNPC : GlobalNPC {
    public override void OnChatButtonClicked(NPC npc, bool firstButton) {
        if (!Guide.Enabled || npc.type != NPCID.Guide) return;
        Main.InGuideCraftMenu = true;
        Main.recBigList = true;
        Recipe.FindRecipes();
    }
}