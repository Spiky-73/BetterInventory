using System.Collections.Generic;
using BetterInventory.InventoryManagement.SmartPickup;
using Terraria;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace BetterInventory.IO;

public sealed class InventoryPreviousItemSlotSerializer : TagSerializer<InventoryPreviousItemSlot, TagCompound> {
    public override TagCompound Serialize(InventoryPreviousItemSlot value) {
        TagCompound tag = [];
        foreach ((int slot, TagCompound slotTag) in value._unloadedItems) {
            tag[slot.ToString()] = slotTag;
        }
        foreach ((int slot, Item mark) in value._previousItems) {
            tag[slot.ToString()] = new TagCompound() {
                [ItemTag] = new ItemDefinition(mark.type),
                [FavoritedTag] = mark.favorited,
            };
        }
        return tag;
    }

    public override InventoryPreviousItemSlot Deserialize(TagCompound tag) {
        InventoryPreviousItemSlot previousItems = new();
        foreach ((string key, object value) in tag) {
            TagCompound slotTag = (TagCompound)value;
            int slot = int.Parse(key);
            ItemDefinition item = slotTag.Get<ItemDefinition>(ItemTag);
            bool favorited = slotTag.Get<bool>(FavoritedTag);
            if (item.IsUnloaded) previousItems._unloadedItems[slot] = slotTag;
            else previousItems._previousItems[slot] = new(item.Type, 1) { favorited = favorited };
        }
        return previousItems;
    }

    public const string ItemTag = "item";
    public const string FavoritedTag = "favorited";
}