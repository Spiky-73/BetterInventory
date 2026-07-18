using Terraria;
using BetterInventory.InventoryManagement;
using Terraria.ID;

namespace BetterInventory.Default.PickupUpgraders;

public sealed class Recovery : ModPickupUpgrader {
    public override bool AppliesTo(Item item) => (item.potion && item.healLife > 0) || item.healMana > 0;

    public override void CheckLockedItems(Player player) {
        Item? heal = player.QuickHeal_GetItemToUse();
        if (_upgradedLife != ItemID.None && !heal.IsAir && _upgradedLife == heal.type) {
            Configs.UpgradeItems.Value.Lock(new(_upgradedLife));
            _upgradedLife = new();
        }
        Item? mana = player.QuickMana_GetItemToUse();
        if (_upgradedMana != ItemID.None && !mana.IsAir && _upgradedMana == mana.type) {
            Configs.UpgradeItems.Value.Lock(new(_upgradedMana));
            _upgradedMana = new();
        }
    }

    public static bool IsAnUpgrade(Player player, Item a, Item b) {
        bool greater = false;
        if (b.healLife > 0) {
            if (a.healLife <= 0) return false; // b heals but a does not
            int heal = player.GetHealLife(a).CompareTo(player.GetHealLife(b));
            if (heal < 0) return false; // a is a worth heal than b
            greater |= heal > 0;
        }
        if (b.healMana > 0) {
            if (a.healMana <= 0) return false; // b heals but a does not
            int heal = player.GetHealMana(a).CompareTo(player.GetHealMana(b));
            if (heal < 0) return false; // a is a worth heal than b
            greater |= heal > 0;
        }
        return greater; // upgrade if one of the comp is better
    }

    public override Item AttemptUpgrade(Player player, Item item) {
        if (item.potion) item = CheckItem(player, item, player.QuickHeal_GetItemToUse(), ref _upgradedLife);
        if (item.healMana > 0) item = CheckItem(player, item, player.QuickMana_GetItemToUse(), ref _upgradedMana);
        return item;
    }

    public static Item CheckItem(Player player, Item item, Item equipped, ref int upgraded) {
        if (equipped is null || Configs.UpgradeItems.Value.importantOnly && !equipped.favorited) return item;
        if (Configs.UpgradeItems.Value.IsLocked(new(equipped.type))) return item;
        if (!IsAnUpgrade(player, item, equipped)) return item;

        InventorySlot? possibleSlot = InventoryLoader.FindItem(player, i => i == item);
        if (!possibleSlot.HasValue) return item;
        var slot = possibleSlot.Value;
        (slot.Item, item) = (item, slot.Item);
        upgraded = item.type;
        (slot.Item.favorited, item.favorited) = (item.favorited && Reflection.ItemSlot.canFavoriteAt.GetValue()[slot.Inventory.Context], slot.Item.favorited);
        return item;
    }

    private static int _upgradedLife;
    private static int _upgradedMana;
}