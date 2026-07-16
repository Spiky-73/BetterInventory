using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.BetterInventoryManagement;

public sealed class CraftWithMouse : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.CraftWithMouse;

    public override IEnumerable<Item> AddMaterialsForCrafting(out ItemConsumedCallback? itemConsumedCallback) {
        itemConsumedCallback = null;
        if (BetterInventoryManagementConfig.CraftWithMouse || Main.myPlayer != Player.whoAmI) return [];

        List<Item> materials = [Main.mouseItem];
        itemConsumedCallback = (item, index) => {
            if (item == Main.mouseItem) item.stack -= RecipeLoader.ConsumedItems[^1].stack; // FIXME seems hacky
            return;
        };
        return materials;
    }
}
