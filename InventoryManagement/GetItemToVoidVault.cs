using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.InventoryManagement;

public sealed class GetItemToVoidVault : ILoadable {
    
    public void Load(Mod mod) {}
    public void Unload() => ResetValues();

    public static void UpdateValues(bool enabled) {
        if (!enabled) ResetValues();
        else SetValues();
    }

    public static void SetValues() {
        if (s_set) return;
        s_inventoryEntityToPlayerInventorySettings = GetItemSettings.InventoryEntityToPlayerInventorySettings;
        s_npcEntityToPlayerInventorySettings = GetItemSettings.NPCEntityToPlayerInventorySettings;
        s_lootAllSettingsRegularChest = GetItemSettings.LootAllSettingsRegularChest;
        s_pickupItemFromWorld = GetItemSettings.PickupItemFromWorld;
        s_getItemInDropItemCheck = GetItemSettings.GetItemInDropItemCheck;
        s_inventoryUIToInventorySettings = GetItemSettings.InventoryUIToInventorySettings;
        s_inventoryUIToInventorySettingsShowAsNew = GetItemSettings.InventoryUIToInventorySettingsShowAsNew;
        s_itemCreatedFromItemUsage = GetItemSettings.ItemCreatedFromItemUsage;
        GetItemSettings.InventoryEntityToPlayerInventorySettings = EnableVoidBag(GetItemSettings.InventoryEntityToPlayerInventorySettings);
        GetItemSettings.NPCEntityToPlayerInventorySettings = EnableVoidBag(GetItemSettings.NPCEntityToPlayerInventorySettings);
        GetItemSettings.LootAllSettingsRegularChest = EnableVoidBag(GetItemSettings.LootAllSettingsRegularChest);
        GetItemSettings.PickupItemFromWorld = EnableVoidBag(GetItemSettings.PickupItemFromWorld);
        GetItemSettings.GetItemInDropItemCheck = EnableVoidBag(GetItemSettings.GetItemInDropItemCheck);
        GetItemSettings.InventoryUIToInventorySettings = EnableVoidBag(GetItemSettings.InventoryUIToInventorySettings);
        GetItemSettings.InventoryUIToInventorySettingsShowAsNew = EnableVoidBag(GetItemSettings.InventoryUIToInventorySettingsShowAsNew);
        GetItemSettings.ItemCreatedFromItemUsage = EnableVoidBag(GetItemSettings.ItemCreatedFromItemUsage);
        s_set = true;
    }

    public static void ResetValues() {
        if (!s_set) return;
        GetItemSettings.InventoryEntityToPlayerInventorySettings = s_inventoryEntityToPlayerInventorySettings;
        GetItemSettings.NPCEntityToPlayerInventorySettings = s_npcEntityToPlayerInventorySettings;
        GetItemSettings.LootAllSettingsRegularChest = s_lootAllSettingsRegularChest;
        GetItemSettings.PickupItemFromWorld = s_pickupItemFromWorld;
        GetItemSettings.GetItemInDropItemCheck = s_getItemInDropItemCheck;
        GetItemSettings.InventoryUIToInventorySettings = s_inventoryUIToInventorySettings;
        GetItemSettings.InventoryUIToInventorySettingsShowAsNew = s_inventoryUIToInventorySettingsShowAsNew;
        GetItemSettings.ItemCreatedFromItemUsage = s_itemCreatedFromItemUsage;
        s_set = false;
    }

    public static GetItemSettings EnableVoidBag(GetItemSettings settings) => new(settings.LongText, settings.NoText, true, settings.HandlePostAction);

    private static bool s_set = false;
    private static GetItemSettings s_inventoryEntityToPlayerInventorySettings;
    private static GetItemSettings s_npcEntityToPlayerInventorySettings;
    private static GetItemSettings s_lootAllSettingsRegularChest;
    private static GetItemSettings s_pickupItemFromWorld;
    private static GetItemSettings s_getItemInDropItemCheck;
    private static GetItemSettings s_inventoryUIToInventorySettings;
    private static GetItemSettings s_inventoryUIToInventorySettingsShowAsNew;
    private static GetItemSettings s_itemCreatedFromItemUsage;
}