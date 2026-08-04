using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib.Constants;
using SpikysLib.IL;
using SpikysLib.UI;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

public sealed class UniversalShiftClick : ModPlayer {
    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.UniversalShiftClick;
    public override void Load() {
        QuickBuyCursor = CursorLoader.RegisterCursor(Mod, TextureAssets.Cursors[CursorOverrideID.QuickSell]);
        On_ItemSlot.LeftClick_ItemArray_int_int += HookShiftBuy;

        QuickCraftCursor = CursorLoader.RegisterCursor(Mod, Mod.Assets.Request<Texture2D>($"Assets/Cursor_Craft"));
        IL_Main.HoverOverCraftingItemButton += il => il.TryEdit(ILShiftCraft, ref UnloadedUniversalShiftClick.Instance.quickCraft);
    }

    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        if (!BetterInventoryManagementConfig.UniversalShiftClick || inventory[slot].IsAir) return false;
        if ((context == ItemSlot.Context.ChestItem || context == ItemSlot.Context.BankItem) && ItemSlot.ControlInUse) {
            Main.cursorOverride = CursorOverrideID.TrashCan;
            return true;
        }
        if (context == ItemSlot.Context.ShopItem && ItemSlot.ShiftInUse && Main.LocalPlayer.ItemSpace(inventory[slot]).CanTakeItemToPersonalInventory) {
            Main.cursorOverride = QuickBuyCursor.Type;
            return true;
        }
        return false;
    }

    private static void HookShiftBuy(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!BetterInventoryManagementConfig.UniversalShiftClick || Main.cursorOverride != QuickBuyCursor.Type || inv[slot].IsAir) {
            orig(inv, context, slot);
            return;
        }
        int fakeStack = inv[slot].maxStack - GetInventoryFreeSpace(Main.LocalPlayer, inv[slot]);
        (var mouse, Main.mouseItem) = (Main.mouseItem, new(inv[slot].type, fakeStack));
        (var cursor, Main.cursorOverride) = (Main.cursorOverride, CursorOverrideID.DefaultCursor);
        orig(inv, context, slot);
        (Main.mouseItem, Item[] inv2) = (mouse, [Main.mouseItem]);
        inv2[0].stack -= fakeStack;
        if (!inv2[0].IsAir) {
            (bool mouseLeft, bool mouseLeftRelease, Main.mouseLeft, Main.mouseLeftRelease) = (Main.mouseLeft, Main.mouseLeftRelease, true, true);
            Main.cursorOverride = CursorOverrideID.ChestToInventory;
            ItemSlot.LeftClick(inv2, ItemSlot.Context.ChestItem, 0);
            (Main.mouseLeft, Main.mouseLeftRelease) = (mouseLeft, mouseLeftRelease);
        }
        Main.cursorOverride = cursor;
    }

    private static void ILShiftCraft(ILContext il) {
        ILCursor cursor = new(il);

        // if (Main.focusRecipe == recipeIndex && ++[Main.guideItem.IsAir || <allowCraft>]) {
        //     <flags*4>
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main._preventCraftingBecauseClickWasUsedToChangeFocusedRecipe));
        cursor.GotoNextLoc(out int flag3, i => true, 3);
        cursor.GotoNextLoc(MoveType.After, out int flag5, i => i.Previous.MatchOr(), 5);

        //     + <overrideHover>
        cursor.EmitLdarg0();
        cursor.EmitLdloc(flag3);
        cursor.EmitLdloc(flag5);
        cursor.EmitDelegate((int recipeIndex, bool canCraft, bool crafting) => {
            if (!BetterInventoryManagementConfig.UniversalShiftClick || !ItemSlot.ShiftInUse || !canCraft || crafting) return;
            var createItem = Main.recipe[Main.availableRecipe[recipeIndex]].createItem;
            if (!Main.LocalPlayer.ItemSpace(createItem).CanTakeItemToPersonalInventory) return;
            Main.cursorOverride = QuickCraftCursor.Type;
            _fakeStack = createItem.maxStack - GetInventoryFreeSpace(Main.LocalPlayer, createItem);
            (_mouse, Main.mouseItem) = (Main.mouseItem, new(createItem.type, _fakeStack));
        });
        //     ...
        // }
        // else ...

        // craftingHide = true;
        // ++ <restore mouse item>
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(() => Main.craftingHide));
        cursor.EmitDelegate(() => {
            if (_mouse is null) return;
            Main.mouseItem.stack -= _fakeStack;
            Main.LocalPlayer.GetItem(Main.myPlayer, Main.mouseItem, GetItemSettings.InventoryUIToInventorySettings);
            (_mouse, Main.mouseItem) = (null, _mouse);
        });
    }
    private static int _fakeStack;
    private static Item? _mouse;

    public static int GetInventoryFreeSpace(Player player, Item item, int max = 9999) {
        if (item.IsAir) return 0;
        int total = 0;
        for (int i = 0; i < InventorySlots.Count; i++) {
            if (i == InventorySlots.Ammo.Start && !item.FitsAmmoSlot()) break;
            if (i == InventorySlots.Coins.Start && !item.IsACoin) break;
            total += GetFreeSpace(player.inventory[i], item); 
            if (total >= max) break;
        }
        if (player.useVoidBag()) {
            for (int i = 0; i < player.bank4.item.Length; i++) {
                total += GetFreeSpace(player.bank4.item[i], item);
                if (total >= max) break;
            }
        }
        return total;
    }

    public static int GetFreeSpace(Item destination, Item item) {
        if (destination.IsAir) return item.maxStack;
        if (destination.type == item.type) return item.maxStack - destination.stack;
        return 0;
    }


    public static ModCursor QuickCraftCursor { get; private set; } = null!;
    public static ModCursor QuickBuyCursor { get; private set; } = null!;
}