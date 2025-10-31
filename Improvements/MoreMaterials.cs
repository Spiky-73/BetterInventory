using System.Collections.Generic;
using BetterInventory.Default.Inventories;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Improvements;

public sealed class MoreMaterialsPlayer : ModPlayer {

    public override void Load() {
        On_ItemSlot.RecordLoadoutChange += HookSwapLoadout;
    }

    private void HookSwapLoadout(On_ItemSlot.orig_RecordLoadoutChange orig) {
        orig();
        if (Configs.MoreMaterials.Equipment) Recipe.FindRecipes();
    }

    public override IEnumerable<Item> AddMaterialsForCrafting(out ItemConsumedCallback? itemConsumedCallback) {
        itemConsumedCallback = (item, index) => {
            if (item == Main.mouseItem) item.stack -= Reflection.RecipeLoader.ConsumedItems.GetValue()[^1].stack; // FIXME seems hacky
            return;
        };
        List<Item> materials = [];
        if (Configs.MoreMaterials.Mouse && Main.myPlayer == Player.whoAmI) materials.Add(Main.mouseItem);
        if (Configs.MoreMaterials.Equipment) {
            void AddSubInventory(ModSubInventory template) {
                var inventories = Configs.EquipmentMaterials.Instance.allLoadouts ? template.GetInventories(Player) : template.GetActiveInventories(Player);
                foreach (var subInventory in inventories) materials.AddRange(subInventory.Items);
            }

            AddSubInventory(ModContent.GetInstance<HeadArmor>());
            AddSubInventory(ModContent.GetInstance<BodyArmor>());
            AddSubInventory(ModContent.GetInstance<LegArmor>());
            AddSubInventory(ModContent.GetInstance<HeadVanity>());
            AddSubInventory(ModContent.GetInstance<BodyVanity>());
            AddSubInventory(ModContent.GetInstance<LegVanity>());
            AddSubInventory(ModContent.GetInstance<Accessories>());
            AddSubInventory(ModContent.GetInstance<VanityAccessories>());
            AddSubInventory(ModContent.GetInstance<SharedAccessories>());
            AddSubInventory(ModContent.GetInstance<SharedVanityAccessories>());
            AddSubInventory(ModContent.GetInstance<ArmorDyes>());
            AddSubInventory(ModContent.GetInstance<AccessoryDyes>());
            AddSubInventory(ModContent.GetInstance<SharedAccessoryDyes>());
            AddSubInventory(ModContent.GetInstance<EquipmentDyes>());
        }
        return materials;
    }
}
