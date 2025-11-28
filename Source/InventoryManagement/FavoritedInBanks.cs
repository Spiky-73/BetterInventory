using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader.IO;

namespace BetterInventory.InventoryManagement;

public sealed class FavoritedInBanks {

    public FavoritedInBanks(){
        Piggy = new();
        Safe = new();
        Forge = new();
    }
    public FavoritedInBanks(Player player): this(){
        AddInventory(Piggy, player.bank.item);
        AddInventory(Safe, player.bank2.item);
        AddInventory(Forge, player.bank3.item);
    }

    public void Apply(Player player) {
        ApplyInventory(Piggy, player.bank.item);
        ApplyInventory(Safe, player.bank2.item);
        ApplyInventory(Forge, player.bank3.item);
    }

    private static void AddInventory(List<int> favs, Item[] items) {
        for (int i = 0; i < items.Length; i++) if (items[i].favorited) favs.Add(i);
    }
    private static void ApplyInventory(List<int> favs, Item[] items) {
        for (int i = 0; i < favs.Count; i++) items[favs[i]].favorited = true;
    }

    public List<int> Piggy;
    public List<int> Safe;
    public List<int> Forge;
}

public sealed class FavoritedInBanksSerializer : TagSerializer<FavoritedInBanks, TagCompound> {

    public override TagCompound Serialize(FavoritedInBanks value) {
        TagCompound tag = new();
        if (value.Piggy.Count > 0) tag[PiggyTag] = value.Piggy;
        if (value.Safe.Count > 0) tag[SafeTag] = value.Safe;
        if (value.Forge.Count > 0) tag[ForgeTag] = value.Forge;
        return tag;
    }

    public override FavoritedInBanks Deserialize(TagCompound tag) {
        FavoritedInBanks value = new();
        if (tag.TryGet(PiggyTag, out List<int> piggy)) value.Piggy = piggy;
        if (tag.TryGet(SafeTag, out List<int> safe)) value.Safe = safe;
        if (tag.TryGet(ForgeTag, out List<int> forge)) value.Forge = forge;
        return value;
    }

    public const string PiggyTag = "piggy";
    public const string SafeTag = "safe";
    public const string ForgeTag = "forge";
}