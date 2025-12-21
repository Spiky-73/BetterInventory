using System.Collections.Generic;
using SpikysLib;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.Features.KeybindShortcuts;

public sealed class KeybindShortcutsPlayer : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || FeaturesConfig.KeybindShortcuts;
    public override void Load() {
        FavoritedBuffKb = KeybindLoader.RegisterKeybind(Mod, "FavoritedQuickBuff", Microsoft.Xna.Framework.Input.Keys.B);
        QuickStackKb = KeybindLoader.RegisterKeybind(Mod, "QuickStack", Microsoft.Xna.Framework.Input.Keys.None);
    }
    public override void SetStaticDefaults() {
        foreach (BuilderToggle toggle in Reflection.BuilderToggleLoader.BuilderToggles.GetValue()) {
            if (toggle is WireVisibilityBuilderToggle wv && wv.NumberOfStates == 3) {
                if (WireDisplayToggles.Count == 0) BuilderTogglesKb.Add((null, KeybindLoader.RegisterKeybind(Mod, "WireDisplay", Microsoft.Xna.Framework.Input.Keys.None)));
                WireDisplayToggles.Add(toggle);
                continue;
            }
            BuilderTogglesKb.Add((toggle, KeybindLoader.RegisterKeybind(Mod, toggle.Name.Replace("BuilderToggle", string.Empty), Microsoft.Xna.Framework.Input.Keys.None)));
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (KeybindShortcutsConfig.FavoritedBuff && FavoritedBuffKb.JustPressed) FavoritedQuickBuff();
        if (KeybindShortcutsConfig.QuickStack && QuickStackKb.JustPressed) QuickStack();
        if (KeybindShortcutsConfig.BuilderAccs) BuilderKeys();
    }

    // TODO mods adding a quickbuff from safes
    private void FavoritedQuickBuff() => ItemHelper.RunWithHiddenItems(Player.inventory, Player.QuickBuff, i => !i.favorited);

    private void BuilderKeys() {
        foreach ((BuilderToggle? builder, ModKeybind kb) in BuilderTogglesKb) {
            if (!kb.JustPressed) continue;
            if (builder is null) {
                CycleBuilderState(WireDisplayToggles[0]);
                for (int i = 1; i < WireDisplayToggles.Count; i++) CycleBuilderState(WireDisplayToggles[i], WireDisplayToggles[i].CurrentState);
            } else {
                CycleBuilderState(builder);
            }
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
    }
    private void CycleBuilderState(BuilderToggle toggle, int? state = null) => Player.builderAccStatus[toggle.Type] = (state ?? (Player.builderAccStatus[toggle.Type] + 1)) % toggle.NumberOfStates;

    private void QuickStack() {
        Player.QuickStackAllChests();
        Recipe.FindRecipes();
    }

    public static ModKeybind FavoritedBuffKb { get; private set; } = null!;
    public static ModKeybind QuickStackKb { get; private set; } = null!;
    public static readonly List<(BuilderToggle? toggle, ModKeybind kb)> BuilderTogglesKb = [];
    public static readonly List<BuilderToggle> WireDisplayToggles = [];
}