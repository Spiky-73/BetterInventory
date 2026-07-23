using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.BetterInventoryManagement;

public sealed class BuilderTogglesKeybinds : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.BuilderTogglesKeybinds;
    public override void SetStaticDefaults() {
        foreach (BuilderToggle toggle in BuilderToggleLoader.BuilderToggles) {
            if (toggle is WireVisibilityBuilderToggle wv && wv.NumberOfStates == 3) {
                if (WireDisplayToggles.Count == 0) BuilderTogglesKb.Add((null, KeybindLoader.RegisterKeybind(Mod, "WireDisplay", Microsoft.Xna.Framework.Input.Keys.None)));
                WireDisplayToggles.Add(toggle);
                continue;
            }
            BuilderTogglesKb.Add((toggle, KeybindLoader.RegisterKeybind(Mod, toggle.Name.Replace("BuilderToggle", string.Empty), Microsoft.Xna.Framework.Input.Keys.None)));
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (!BetterInventoryManagementConfig.BuilderTogglesKeybinds) return;
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

    public static readonly List<(BuilderToggle? toggle, ModKeybind kb)> BuilderTogglesKb = [];
    public static readonly List<BuilderToggle> WireDisplayToggles = [];
}