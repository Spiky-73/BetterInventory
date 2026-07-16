using System.Collections.Generic;
using BetterInventory.Default.Inventories;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

public sealed class CraftWithEquipment : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.CraftWithEquipment;
    public override void Load() {
        On_ItemSlot.RecordLoadoutChange += HookSwapLoadout;
    }

    private static void HookSwapLoadout(On_ItemSlot.orig_RecordLoadoutChange orig) {
        orig();
        if (BetterInventoryManagementConfig.CraftWithEquipment) Recipe.FindRecipes();
    }

    public override IEnumerable<Item> AddMaterialsForCrafting(out ItemConsumedCallback? itemConsumedCallback) {
        itemConsumedCallback = null;
        if (!BetterInventoryManagementConfig.CraftWithEquipment) return [];

        List<Item> materials = [];
        void AddSubInventory(ModSubInventory template) {
            var inventories = EquipmentMaterialsConfig.Instance.allLoadouts ? template.GetInventories(Player) : template.GetActiveInventories(Player);
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
        return materials;
    }
}
