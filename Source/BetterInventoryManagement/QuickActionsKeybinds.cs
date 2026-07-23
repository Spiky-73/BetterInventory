using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

public sealed class QuickActionsKeybinds : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.QuickActionsKeybinds;
    public override void Load() {
        QuickStackKb = KeybindLoader.RegisterKeybind(Mod, "QuickStack", Microsoft.Xna.Framework.Input.Keys.None);
        QuickSortKb = KeybindLoader.RegisterKeybind(Mod, "QuickSort", Microsoft.Xna.Framework.Input.Keys.None);
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (!BetterInventoryManagementConfig.QuickActionsKeybinds) return;
        if (QuickStackKb.JustPressed) {
            Player.QuickStackAllChests();
            Recipe.FindRecipes();
        }
        if (QuickSortKb.JustPressed) {
            ItemSorting.SortInventory();
            Recipe.FindRecipes();
        }
    }

    public static ModKeybind QuickStackKb { get; private set; } = null!;
    public static ModKeybind QuickSortKb { get; private set; } = null!;
}