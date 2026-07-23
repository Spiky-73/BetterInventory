using Terraria;

namespace BetterInventory.BetterMenuNavigation.Defaults;

public sealed class EquipPageMenuCycle : ModMenuCycle {
    public override int MenusCount => 3;

    public override int CurrentMenu() => Main.EquipPageSelected;

    public override void ShowMenu(int index) => Main.EquipPageSelected = index;
}   