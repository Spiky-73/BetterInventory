using System.Collections.Generic;
using SpikysLib.Reflection;
using TPlayerInput = Terraria.GameInput.PlayerInput;

namespace BetterInventory.Reflection;

public static class PlayerInput {
    public static readonly StaticField<List<string>> MouseInModdedUI = new(typeof(TPlayerInput), nameof(MouseInModdedUI));
    public static readonly StaticProperty<bool> IgnoreMouseInterface = new(typeof(TPlayerInput), nameof(TPlayerInput.IgnoreMouseInterface));
}