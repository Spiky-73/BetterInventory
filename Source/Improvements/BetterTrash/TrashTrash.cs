using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.Audio;

namespace BetterInventory.Improvements.BetterTrash;

public sealed class TrashTrash : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterTrashConfig.TrashTrash;
    public void Load(Mod mod) {
        On_ItemSlot.LeftClick_ItemArray_int_int += HookHoverTrashSlot;
        On_ItemSlot.LeftClick_SellOrTrash += HookTrashTrash;
    }
    public void Unload() { }

    private static void HookHoverTrashSlot(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (BetterTrashConfig.TrashTrash && context == ItemSlot.Context.TrashItem
                && !ItemSlot.Options.DisableQuickTrash && (ItemSlot.Options.DisableLeftShiftTrashCan ? ItemSlot.ControlInUse : ItemSlot.ShiftInUse)) {
            Main.cursorOverride = CursorOverrideID.TrashCan;
        }
        orig(inv, context, slot);
    }

    private static bool HookTrashTrash(On_ItemSlot.orig_LeftClick_SellOrTrash orig, Item[] inv, int context, int slot) {
        if (BetterTrashConfig.TrashTrash && context == ItemSlot.Context.TrashItem
                && Main.cursorOverride == CursorOverrideID.TrashCan) {
            inv[slot].TurnToAir();
            SoundEngine.PlaySound(SoundID.Grab);
            return true;
        }

        return orig(inv, context, slot);
    }
}