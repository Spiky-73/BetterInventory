using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed class SmartPickup : ILoadable {

    public static Configs.InventoryManagement.SmartPickupLevel Config => Configs.InventoryManagement.Instance.smartPickup;

    public void Load(Mod mod) {
        ItemSlot.OnItemTransferred += MarkSlot;
    }
    public void Unload() { }

    public static void MarkSlot(ItemSlot.ItemTransferInfo info) {
        // TODO impl
    }

    public static void MarkSlot(InventorySlots items, int slot, int type) {
        _markedSlots[type] = (items, slot);
    }

    public static Item SmartGetItem(Player player, Item item, GetItemSettings settings) {
        if(_markedSlots.TryGetValue(item.type, out var mark)){
            _markedSlots.Remove(item.type);
            item = mark.items.GetItem(player, item, settings, mark.slot);
        }
        return item;
    }

    public static bool SmartPickupEnabled(Item item) => Config switch {
        Configs.InventoryManagement.SmartPickupLevel.AllItems => true,
        Configs.InventoryManagement.SmartPickupLevel.FavoriteOnly => item.favorited,
        Configs.InventoryManagement.SmartPickupLevel.Off or _ => false
    };

    private static readonly Dictionary<int, (InventorySlots items, int slot)> _markedSlots = new();
}