using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BetterInventory.BetterMenuNavigation;

public sealed class MenuChainsPlayer : ModPlayer {
    public override bool IsLoadingEnabled(Mod mod) => BetterInventoryConfig.BetterMenuNavigation;
    public override void Load() {
        _keybinds = [.. MenuChainsConfig.Instance.chains.Select((chain, index) => {
            Language.GetOrRegister($"Mods.BetterInventory.Keybinds.MoveChain{index}.DisplayName", () => chain.name); // Needs to be first as RegisterKeybind sets it otherwise
            return KeybindLoader.RegisterKeybind(Mod, $"MoveChain{index}", Keys.None);
        })];
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (!BetterMenuNavigationConfig.MenuChains) return;
        // Breaks the chain if we waited too long
        if (_graceTime == 0) BreakChain();
        if (_graceTime > 0) _graceTime--;

        // Check if we pressed a key
        int index = Array.FindIndex(_keybinds, key => key.JustPressed);
        if (index != -1) {
            // We didn't release the keybind we expected -> new chain
            if (!InChain() || _chainKey != index) {
                _chainKey = index;
                SetupChain(MenuChainsConfig.Instance.chains[index]);
            }
            _graceTime = MenuChainsConfig.Instance.holdTime; // Wait for a release
        }

        // Not in a chain -> nothing to check
        if (!InChain()) return;

        // Check if we released the chain key
        if (!_keybinds[_chainKey].JustReleased) return;

        // Continue the chain
        _graceTime = MenuChainsConfig.Instance.graceTime; // Wait for a press
        ContinueChain();
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public sealed override void UpdateAutopause() {
        if (!BetterMenuNavigationConfig.MenuChains) return;
        ProcessTriggers(PlayerInput.Triggers.Current);
    }

    public static List<ModInterface> GetChain(ModInterface[] interfaces, int current) {
        if (current < 0) return [.. Enumerable.Range(0, interfaces.Length).Select(i => interfaces[i])];
        if (current == 0) return [.. Enumerable.Range(1, interfaces.Length - 1).Select(i => interfaces[i])];
        return [..(MenuChainsConfig.Instance.mode switch {
            MenuChainMode.Toggle => [.. Enumerable.Range(1, interfaces.Length - 1).Select(i => i == current ? 0 : i), current],
            MenuChainMode.Skip => [.. Enumerable.Range(0, interfaces.Length).Where((_, i) => i != current), current],
            MenuChainMode.Continue => Enumerable.Range(current, interfaces.Length - current).Select(i => (i + 1) % interfaces.Length),
            MenuChainMode.Restart or _ => [0],
        }).Select(i => interfaces[i])];
    }

    public static bool InChain() => _chain.Count > 0;
    public static void BreakChain() => _chain.Clear();
    public static void SetupChain(MenuChain chain) {
        ModInterface[] interfaces = [.. chain.interfaces.Select(def => InterfaceLoader.Interfaces[InterfaceLoader.Search.GetId(def.ToString())]).Where(i => i.Available)];
        int current = Array.FindIndex(interfaces, i => i.Active);
        _index = 0;
        _chain = [interfaces[Math.Max(current, 0)], .. GetChain(interfaces, current)];
    }
    private static void ContinueChain() {
        _index++;
        _chain[_index].Activate();
        if (_index == _chain.Count - 1) BreakChain();

    }

    // Chain controls
    private static ModKeybind[] _keybinds = [];
    private static int _chainKey = -1;
    private static int _graceTime;

    // Chain data
    private static List<ModInterface> _chain = [];
    private static int _index = 0;

}