using SpikysLib.DataStructures;
using Terraria;
namespace BetterInventory.Default.Inventories;

public abstract class ALoadout : ModSubInventory {
    public abstract int Index { get; }
    public sealed override int? MaxStack => 1;
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.Loadouts[player.CurrentLoadoutIndex <= Index ? (Index + 1) : Index].Armor);
}
public abstract class Loadout1 : ALoadout {
    public sealed override int Index => 1;
}
public abstract class Loadout2 : ALoadout {
    public sealed override int Index => 2;
    public override int ComparePositionTo(ModSubInventory other) => other is Loadout1 ? 1 : 0;
}