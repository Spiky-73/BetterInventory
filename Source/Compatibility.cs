using System;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Terraria.ModLoader;

namespace BetterInventory;

public static class Compatibility {
    public static bool LoadDisabledFeatures => BetterInventoryConfig.Instance.loadDisabledModule;

    public static bool TryEdit(this ILContext context, Action<ILContext> ilEdit, ref bool unloaded, [CallerArgumentExpression(nameof(ilEdit))] string name = "") {
        Mod mod = ModContent.GetInstance<BetterInventory>();
        if (unloaded) return false;
        try {
            ilEdit(context);
            return true;
        } catch {
            mod.Logger.Warn($"ILHook {name} failed to load. Related features will be disabled until reload");
            Utility.FailedILs++;
            MonoModHooks.DumpIL(mod, context);
            unloaded = true;
            return false;
        }
    }
}