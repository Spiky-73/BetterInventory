using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

public sealed class CraftWithEquipment : ModPlayer {
    public override bool IsLoadingEnabled(Mod mod) => BetterInventoryConfig.BetterInventoryManagement;
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

        var player = Main.LocalPlayer;
        var loader = LoaderManager.Get<AccessorySlotLoader>();
        var accessoryPlayer = AccessorySlotLoader.ModSlotPlayer(player);

        void AddAccessoryTrio(Item[] armor, Item[] dye, int slot) {
            materials.Add(armor[slot]);
            materials.Add(armor[slot + armor.Length / 2]);
            materials.Add(dye[slot]);
        }
        void AddAccessorySlot(int slot, bool modded) {
            if (modded) AddAccessoryTrio(accessoryPlayer.exAccessorySlot, accessoryPlayer.exDyesAccessory, slot);
            else AddAccessoryTrio(player.armor, player.dye, slot);

            if (!CraftWithEquipmentConfig.Instance.allLoadouts || (modded && accessoryPlayer.IsSharedSlot(slot))) return;
            for (int i = 0; i < player.Loadouts.Length; i++) {
                if (i == player.CurrentLoadoutIndex) continue;
                if (modded) AddAccessoryTrio(accessoryPlayer.exLoadouts[i].ExAccessorySlot, accessoryPlayer.exLoadouts[i].ExDyesAccessory, slot);
                else AddAccessoryTrio(player.Loadouts[i].Armor, player.Loadouts[i].Dye, slot);
            }
        }

        for (int slot = 0; slot < player.armor.Length / 2; slot++) {
            bool unlocked = player.IsItemSlotUnlockedAndUsable(slot);
            bool shown = CanAccessorySlotBeShown(player, slot);
            if (unlocked || shown) AddAccessorySlot(slot, false);
        }

        for (int slot = 0; slot < accessoryPlayer.SlotCount; slot++) {
            bool unlocked = loader.ModdedIsItemSlotUnlockedAndUsable(slot, player);
            bool shown = loader.ModdedCanSlotBeShown(slot);
            if (unlocked || shown) AddAccessorySlot(slot, true);
        }

        materials.AddRange(player.miscEquips);
        materials.AddRange(player.miscDyes);

        return materials;
    }

    public static bool CanAccessorySlotBeShown(Player player, int slot) {
        switch (slot) {
        case 8 or 18:
            return player.CanDemonHeartAccessoryBeShown();
        case 9 or 19:
            return player.CanMasterModeAccessoryBeShown();
        }
        if (player.IsItemSlotUnlockedAndUsable(slot)) return true;
        int count = player.armor.Length / 2;
        return player.armor[slot % count].type > ItemID.None || player.armor[(slot % count) + count].type > ItemID.None || player.dye[slot % count].type > ItemID.None;
    }
}
