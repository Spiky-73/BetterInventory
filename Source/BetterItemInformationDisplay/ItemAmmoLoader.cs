using System.Collections.Generic;
using System.Collections.ObjectModel;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.BetterItemInformationDisplay;

public sealed class ItemAmmoLoader : ILoadable {

    internal static void Add(ModItemAmmo itemAmmo) => _itemAmmos.Add(itemAmmo);

    internal static IEnumerable<(ModItemAmmo itemAmmo, Item ammo)> GetAmmos(Player player, Item item) {
        // TODO try to cache ?
        foreach (var itemAmmo in ItemAmmos) {
            if (itemAmmo.TryGetAmmo(player, item, out var ammo)) yield return (itemAmmo, ammo);
        }
    }

    public void Load(Mod mod) {}
    public void Unload() => _itemAmmos.Clear();

    public static ReadOnlyCollection<ModItemAmmo> ItemAmmos => _itemAmmos.AsReadOnly();
    private readonly static List<ModItemAmmo> _itemAmmos = [];
}