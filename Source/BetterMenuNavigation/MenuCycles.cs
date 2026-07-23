using System.Collections.Generic;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace BetterInventory.BetterMenuNavigation;

public sealed class MenuCyclesPlayer : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterMenuNavigationConfig.MenuCycles;

    public override void SetStaticDefaults() {
        _keybinds = new ModKeybind[MenuCycleLoader.Cycles.Count + 1];
        _timers = new int[MenuCycleLoader.Cycles.Count + 1];
        _indices = new int[MenuCycleLoader.Cycles.Count + 1];
        _chains = new List<int>[MenuCycleLoader.Cycles.Count + 1];
        foreach (var cycle in MenuCycleLoader.Cycles) {
            _keybinds[cycle.Type] = KeybindLoader.RegisterKeybind(Mod, cycle.Name, cycle.DefaultKeybind);
            _chains[cycle.Type] = [];
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        foreach (var cycle in MenuCycleLoader.Cycles) ProcessTriggers(cycle);
    }

    private static void ProcessTriggers(ModMenuCycle cycle) {
        int type = cycle.Type;

        if (InChain(type)) _timers[type]++;

        // Waiting
        if (_keybinds[type].JustPressed) {
            if (_timers[type] > MenuCyclesConfig.Instance.delay) { // Waited for too long -> reset chain
                BreakChain(type);
            }
            _timers[type] = 0; // Reset the timer

            if (!InChain(cycle.Type)) SetupChain(cycle);
        }

        // Holding
        if (_keybinds[type].JustReleased) {
            if (_timers[type] > MenuCyclesConfig.Instance.tap) { // Held for too long -> reset chain
                BreakChain(type);
                return;
            }
            _timers[type] = 0; // Reset the timer
            ContinueChain(cycle);
        }

    }

    public static bool InChain(int cycle) => _chains[cycle].Count > 0;
    public static void BreakChain(int cycle) {
        _timers[cycle] = 0;
        _chains[cycle].Clear();
    }

    public static void SetupChain(ModMenuCycle cycle) {
        int type = cycle.Type;
        int current = cycle.CurrentMenu();
        _indices[type] = 0;
        _chains[type].Clear();
        if (current == 0) {
            for (int i = 0; i < cycle.MenusCount; i++) _chains[type].Add((i + 1) % cycle.MenusCount);
            return;
        }
        switch (MenuCyclesConfig.Instance.mode) {
        case MenuCycleMode.Restart:
            for (int i = 0; i < cycle.MenusCount; i++) _chains[type].Add(i);
            break;
        case MenuCycleMode.Continue:
            for (int i = 0; i < cycle.MenusCount; i++) _chains[type].Add((i + current + 1) % cycle.MenusCount);
            break;
        case MenuCycleMode.Skip:
            for (int i = 0; i < cycle.MenusCount - 1; i++) _chains[type].Add(i < current ? i : i + 1);
            _chains[type].Add(current);
            break;
        case MenuCycleMode.Toggle:
            for (int i = 1; i < cycle.MenusCount; i++) _chains[type].Add(i == current ? 0 : i);
            _chains[type].Add(current);
            break;
        }
    }
    private static void ContinueChain(ModMenuCycle cycle) {
        int type = cycle.Type;
        cycle.ShowMenu(_chains[type][_indices[type]]);
        _indices[type]++;
        if (_indices[type] >= _chains[type].Count) BreakChain(type);
    }

    private static ModKeybind[] _keybinds = [];
    private static int[] _timers = [];
    private static int[] _indices = [];
    private static List<int>[] _chains = [];
}