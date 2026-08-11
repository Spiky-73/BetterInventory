using Terraria;

namespace BetterInventory.Default.Interfaces;

public abstract class EquipPage : ModInterface {
    public abstract int Page { get; }

    public sealed override bool Active => Main.EquipPageSelected == Page;
    public sealed override void Activate() => Main.EquipPageSelected = Page;
}

public sealed class ArmorPage : EquipPage {
    public sealed override int Page => 0;
}
public sealed class HousingPage : EquipPage {
    public sealed override int Page => 1;
}
public sealed class MiscEquipPage : EquipPage {
    public sealed override int Page => 2;
}