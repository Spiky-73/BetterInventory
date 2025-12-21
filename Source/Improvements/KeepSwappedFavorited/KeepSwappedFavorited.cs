using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Improvements.KeepSwappedFavorited;

public sealed class KeepSwappedFavorited : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || ImprovementsConfig.KeepSwappedFavorited;
    public void Load(Mod mod) {
        On_ItemSlot.DyeSwap += HookDyeSwapFavorited;
        On_ItemSlot.ArmorSwap += HookArmorSwapFavorited;
        On_ItemSlot.EquipSwap += HookEquipSwapFavorited;
    }
    public void Unload() { }

    private static Item HookEquipSwapFavorited(On_ItemSlot.orig_EquipSwap orig, Item item, Item[] inv, int slot, out bool success) => EquipSwapFavorited((out bool success) => orig(item, inv, slot, out success), item, out success);
    private static Item HookArmorSwapFavorited(On_ItemSlot.orig_ArmorSwap orig, Item item, out bool success) => EquipSwapFavorited((out bool success) => orig(item, out success), item, out success);
    private static Item HookDyeSwapFavorited(On_ItemSlot.orig_DyeSwap orig, Item item, out bool success) => EquipSwapFavorited((out bool success) => orig(item, out success), item, out success);

    private delegate Item EquipSwapFn(out bool success);
    private static Item EquipSwapFavorited(EquipSwapFn swap, Item item, out bool success) {
        bool favorited = item.favorited;
        Item swapped = swap(out success);
        if (success && favorited && ImprovementsConfig.KeepSwappedFavorited) swapped.favorited = true;
        return swapped;
    }
}