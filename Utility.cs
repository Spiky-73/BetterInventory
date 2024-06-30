using System;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory;

[Flags] public enum AllowedItems : byte { None = 0b00, Self = 0b01, Mouse = 0b10}

public static class Utility {

    public static int FailedILs { get; private set; }

    public static Item? LastStack(this Player player, Item item, AllowedItems allowedItems = AllowedItems.None) {
        bool Check(Item i) => item.type == i.type && (allowedItems.HasFlag(AllowedItems.Self) || i != item);

        for (int i = 49; i >= 0; i--) if (Check(player.inventory[i])) return player.inventory[i];
        for (int i = 57; i >= 50; i--) if (Check(player.inventory[i])) return player.inventory[i];
        if (allowedItems.HasFlag(AllowedItems.Mouse) && Check(player.inventory[58])) return player.inventory[58];
        return null;
    }

   public static Item? SmallestStack(this Player player, Item item, AllowedItems allowedItems = AllowedItems.None) {
        Item? min = null;
        void Check(Item i) {
            if (item.type == i.type && (min is null || i.stack < min.stack) && (allowedItems.HasFlag(AllowedItems.Self) || i != item)) min = i;
        }

        for (int i = 49; i >= 0; i--) Check(player.inventory[i]);
        for (int i = 57; i >= 50; i--) Check(player.inventory[i]);
        if (allowedItems.HasFlag(AllowedItems.Mouse)) Check(player.inventory[58]);
        return min;
    }

    public static bool ApplyTo(this ILContext context, Action<ILContext> ilEdit, bool enabled, [CallerArgumentExpression("ilEdit")] string name = "") {
        Mod mod = ModContent.GetInstance<BetterInventory>();
        if (Configs.Compatibility.CompatibilityMode && !enabled) {
            mod.Logger.Info($"ILHook {name} was not loaded. Related features will be disabled until reload");
            return false;
        }
        try {
            ilEdit(context);
        } catch {
            FailedILs++;
            mod.Logger.Warn($"ILHook {name} failed to load. Related features will be disabled until reload");
            MonoModHooks.DumpIL(mod, context);
            return false;
        }
        return true;
    }

    public static ILCursor GotoRecipeDraw(this ILCursor cursor) {
        // bool flag10 = Main.CreativeMenu.Enabled && !Main.CreativeMenu.Blocked;
        // flag10 |= Main.hidePlayerCraftingMenu;
        // if (!Main.InReforgeMenu && !Main.LocalPlayer.tileEntityAnchor.InUse && !flag10) {
        //     UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = -1;
        //     UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = -1;
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.hidePlayerCraftingMenu));
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentRecipeSmall));
        return cursor;
    }    
}
