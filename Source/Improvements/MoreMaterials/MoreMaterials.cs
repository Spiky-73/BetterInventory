using System.Collections.Generic;
using BetterInventory.Default.Inventories;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Improvements.MoreMaterials;

public sealed class MoreMaterialsPlayer : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || ImprovementsConfig.MoreMaterials;
    public override void Load() {
        On_ItemSlot.RecordLoadoutChange += HookSwapLoadout;
    }

    private static void HookSwapLoadout(On_ItemSlot.orig_RecordLoadoutChange orig) {
        orig();
        if (!ImprovementsConfig.MoreMaterials) return;
        if (MoreMaterialsConfig.Equipment) Recipe.FindRecipes();
    }

    public override IEnumerable<Item> AddMaterialsForCrafting(out ItemConsumedCallback? itemConsumedCallback) {
        itemConsumedCallback = null;
        if (!ImprovementsConfig.MoreMaterials) return [];

        List<Item> materials = [];
        itemConsumedCallback = (item, index) => {
            if (item == Main.mouseItem) item.stack -= RecipeLoader.ConsumedItems[^1].stack; // FIXME seems hacky
            return;
        };
        if (MoreMaterialsConfig.Mouse) Mouse_AddMaterials(materials);
        if (MoreMaterialsConfig.Equipment) Equipment_AddMaterials(materials);
        return materials;
    }

    public void Mouse_AddMaterials(List<Item> materials) {
        if (Main.myPlayer == Player.whoAmI) materials.Add(Main.mouseItem);
    }

    public void Equipment_AddMaterials(List<Item> materials) {
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
    }
}
