using System.ComponentModel;
using Terraria;

namespace BetterInventory.Configs;

public class SmartItems {

    public SmartItems() {}
    [DefaultValue(true)] public bool consumption = true;
    [DefaultValue(true)] public bool ammo = true;
    [DefaultValue(SmartPickupLevel.AllItems)] public SmartPickupLevel pickup = SmartPickupLevel.AllItems;
    [DefaultValue(false)] public bool autoEquip = false;

    public bool SmartPickupEnabled(Item item) => pickup switch {
        SmartPickupLevel.AllItems => true,
        SmartPickupLevel.FavoriteOnly => item.favorited,
        SmartPickupLevel.Off or _ => false
    };
}