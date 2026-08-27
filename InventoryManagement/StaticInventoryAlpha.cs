using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.InventoryManagement;

public sealed class StaticInventoryAlpha : ILoadable {
    public void Load(Mod mod) {
        On_Main.DrawInterface_24_InterfaceLogic2 += HookStaticAlpha;
    }
    public void Unload() { }

    private static void HookStaticAlpha(On_Main.orig_DrawInterface_24_InterfaceLogic2 orig) {
        orig();
        if (!Configs.InventoryManagement.StaticInventoryAlpha) return;
        Main.invAlpha = Configs.StaticInventoryAlpha.Value.alpha * 255;
        Main.inventoryBack = new((int)Main.invAlpha, (int)Main.invAlpha, (int)Main.invAlpha, (int)Main.invAlpha);
    }
}