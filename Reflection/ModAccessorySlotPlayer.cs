using TAccPlayer = Terraria.ModLoader.Default.ModAccessorySlotPlayer;

namespace BetterInventory.Reflection;

public static class ModAccessorySlotPlayer {
    public static readonly Field<TAccPlayer, Terraria.Item[]> exAccessorySlot = new(nameof(exAccessorySlot));
    public static readonly Field<TAccPlayer, Terraria.Item[]> exDyesAccessory = new(nameof(exDyesAccessory));
}