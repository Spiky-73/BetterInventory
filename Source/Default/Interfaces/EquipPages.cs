using Terraria;

namespace BetterInventory.Default.Interfaces;

public abstract class EquipPageInterface : ModInterface {
    public abstract int Page { get; }

    public sealed override bool Active => Main.EquipPageSelected == Page;
    public sealed override void Activate() => Main.EquipPageSelected = Page;
}

public sealed class ArmorInterface : EquipPageInterface {
    public sealed override int Page => 0;
}
public sealed class HousingInterface : EquipPageInterface {
    public sealed override int Page => 1;
}
public sealed class MiscEquipInterface : EquipPageInterface {
    public sealed override int Page => 2;
}