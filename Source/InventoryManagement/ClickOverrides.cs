using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.Collections;
using BetterInventory.CrossMod;
using SpikysLib.IL;
using SpikysLib.UI;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.Audio;

namespace BetterInventory.InventoryManagement;

public sealed class ClickOverrides : ModPlayer {

    public override void Load() {
        CraftCursor = CursorLoader.RegisterCursor(Mod, Mod.Assets.Request<Texture2D>($"Assets/Cursor_Craft"));

        On_ItemSlot.LeftClick_ItemArray_int_int += HookShiftLeftCustom;
        On_ItemSlot.RightClick_ItemArray_int_int += HookDepositClick; // Needs to be added before `HookShiftRight` for Shift+Deposit to work 
        On_ItemSlot.RightClick_ItemArray_int_int += HookShiftRight;

        IL_Main.HoverOverCraftingItemButton += static il => {
            if (!il.ApplyTo(ILShiftRightCursorOverride, Configs.BetterShiftClick.UniversalShift)) Configs.UnloadedInventoryManagement.Value.universalShift = true;
        };
        IL_Main.CraftItem += static il => {
            if (!il.ApplyTo(ILShiftCraft, Configs.BetterShiftClick.ShiftRight)) Configs.UnloadedInventoryManagement.Value.shiftRight = true;
        };
    }

    private static void HookShiftLeftCustom(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Configs.BetterShiftClick.UniversalShift || !Main.mouseLeft || Main.cursorOverride <= CursorOverrideID.DefaultCursor || context != ItemSlot.Context.ShopItem && context != ItemSlot.Context.CreativeInfinite) orig(inv, context, slot);
        else TwoStepClick(inv, context, slot, (inv, context, slot) => orig(inv, context, slot));
    }
    private static void HookShiftRight(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Configs.BetterShiftClick.ShiftRight || Main.cursorOverride <= CursorOverrideID.DefaultCursor
        || !(Main.mouseRight || Configs.InventoryManagement.DepositClick && Main.mouseMiddle)) {
            orig(inv, context, slot);
        } else TwoStepClick(inv, context, slot, (inv, context, slot) => orig(inv, context, slot));
    }
    private static void TwoStepClick(Item[] inv, int context, int slot, Action<Item[], int, int> click) {
        (Item mouse, Main.mouseItem) = (Main.mouseItem, new());
        click(inv, context, slot);
        (Main.mouseItem, Item[] inv2) = (mouse, new[] { Main.mouseItem });
        if (inv2[0].IsAir) return;
        (bool left, bool leftR, Main.mouseLeft, Main.mouseLeftRelease) = (Main.mouseLeft, Main.mouseLeftRelease, true, true);
        int cursor = Main.cursorOverride;
        if (Array.IndexOf(TransportCursors, Main.cursorOverride) == -1) (context, Main.cursorOverride) = (ItemSlot.Context.ChestItem, CursorOverrideID.ChestToInventory);
        ItemSlot.LeftClick(inv2, context, 0);
        (Main.mouseLeft, Main.mouseLeftRelease) = (left, leftR);
        Main.cursorOverride = cursor;
        if (!inv2[0].IsAir) inv[slot] = ItemHelper.MoveInto(inv[slot], inv2[0], out _);
        if (Main.mouseRight || Main.mouseMiddle) Recipe.FindRecipes();
    }


    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if (!Configs.BetterShiftClick.UniversalShift || inv[slot].IsAir) return false;
        if ((context == ItemSlot.Context.ChestItem || context == ItemSlot.Context.BankItem) && ItemSlot.ControlInUse) {
            Main.cursorOverride = CursorOverrideID.TrashCan;
            return true;
        }
        if (context == ItemSlot.Context.ShopItem && ItemSlot.ShiftInUse && Main.LocalPlayer.ItemSpace(inv[slot]).CanTakeItem) {
            Main.cursorOverride = CursorOverrideID.QuickSell;
            return true;
        }
        return false;
    }

    private static void ILShiftRightCursorOverride(ILContext context) {
        ILCursor cursor = new(context);

        // if (Main.focusRecipe == recipeIndex && ++[Main.guideItem.IsAir || <allowCraft>]) {
        //     <flags*4>
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main._preventCraftingBecauseClickWasUsedToChangeFocusedRecipe));
        cursor.GotoNextLoc(out int flag3, i => true, 3);
        cursor.GotoNextLoc(MoveType.After, out int flag5, i => i.Previous.MatchOr(), 5);

        //     + <overrideHover>
        cursor.EmitLdloc(flag3);
        cursor.EmitLdloc(flag5);
        cursor.EmitDelegate((bool canCraft, bool crafting) => {
            if (!Configs.BetterShiftClick.UniversalShift || !ItemSlot.ShiftInUse) return;
            if (canCraft && Main.LocalPlayer.ItemSpace(Main.recipe[Main.availableRecipe[Main.focusRecipe]].createItem).CanTakeItem && !crafting && Main.stackSplit <= 1) CraftCursor.SetAsCurrent();
        });
        //     ...
        // }
    }
    private static void ILShiftCraft(ILContext il) {
        ILCursor cursor = new(il);

        // ++ if(<Shift>){
        // ++     if(!<canTakeItem>) return;
        // ++     goto skipCheck;
        // ++ }
        ILLabel skipVanillaCheck = cursor.DefineLabel();
        ILLabel vanillaCheck = cursor.DefineLabel();
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => Configs.BetterShiftClick.ShiftRight && CraftCursor.IsCurrent);
        cursor.EmitBrfalse(vanillaCheck);
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => Main.LocalPlayer.ItemSpace(r.createItem).CanTakeItem);
        cursor.EmitBrtrue(skipVanillaCheck);
        cursor.EmitRet();
        cursor.MarkLabel(vanillaCheck);
        cursor.MarkLabel(skipVanillaCheck); // Here in case of exception

        // if (Main.mouseItem.stack > 0 && !ItemLoader.CanStack(Main.mouseItem, r.createItem)) return;
        cursor.GotoNextLoc(out _, i => i.Previous.SaferMatchCallvirt(Reflection.Item.Clone), 0);
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());

        // ++ skipCheck:
        cursor.MarkLabel(skipVanillaCheck);
        // if (<cannotCraft>) return;
    }

    public static ModCursor CraftCursor { get; private set; } = null!;

    public static readonly int[] TransportCursors = [CursorOverrideID.TrashCan, CursorOverrideID.InventoryToChest, CursorOverrideID.ChestToInventory];

    private static void HookDepositClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        orig(inv, context, slot);
        if (ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == -1) return;
        if (!Configs.InventoryManagement.DepositClick) return;

        Player player = Main.LocalPlayer;
        if (player.itemAnimation > 0) return;

        if (!Main.mouseMiddle) {
            if (s_allowResetStackSplit) Main.preventStackSplitReset = s_allowResetStackSplit = false;
            return;
        }
        Main.preventStackSplitReset = s_allowResetStackSplit = true;
        if (Main.stackSplit > 1) return;

        Item testItem = Main.mouseItem.IsAir ? inv[slot] : Main.mouseItem;
        if (testItem.maxStack <= 1 && testItem.stack == 1) return;

        // Pickup all items if the mouse is empty
        if (Main.mouseMiddleRelease && Main.mouseItem.IsAir && context != ItemSlot.Context.CreativeInfinite && context != ItemSlot.Context.ShopItem) {
            Main.mouseItem = ItemLoader.TransferWithLimit(inv[slot], inv[slot].stack);
            ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(inv[slot], context, 21, 0));
        }

        int num = Main.superFastStack + 1;
        if (context == ItemSlot.Context.ShopItem) {
            Item[] toSell = [ItemLoader.TransferWithLimit(Main.mouseItem, num)];
            ItemSlot.SellOrTrash(toSell, ItemSlot.Context.MouseItem, 0);
            ItemSlot.RefreshStackSplitCooldown();
        } else if (inv[slot].type == ItemID.None || (inv[slot].type == Main.mouseItem.type && inv[slot].stack < inv[slot].maxStack && ItemLoader.CanStack(inv[slot], Main.mouseItem))) {
            DepositItemFromMouse(inv, context, slot, player, num);
            SoundEngine.PlaySound(SoundID.MenuTick);
            ItemSlot.RefreshStackSplitCooldown();
        }
    }

    public static void DepositItemFromMouse(Item[] inv, int context, int slot, Player player, int amount) {
        if (inv[slot].type == ItemID.None) {
            inv[slot] = ItemLoader.TransferWithLimit(Main.mouseItem, amount);
            ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(Main.mouseItem, ItemSlot.Context.MouseItem, context, 0));
        } else {
            if (context == ItemSlot.Context.CreativeInfinite) Main.mouseItem.stack -= amount;
            else ItemLoader.StackItems(inv[slot], Main.mouseItem, out _, false, amount);
        }
        if (Main.mouseItem.stack <= 0) Main.mouseItem = new Item();
        Recipe.FindRecipes();
        if (Main.netMode == NetmodeID.MultiplayerClient) {
            if (context == ItemSlot.Context.ChestItem) {
                NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, player.chest, slot, 0f, 0f, 0, 0, 0);
            }
            if (context == ItemSlot.Context.DisplayDollArmor || context == ItemSlot.Context.DisplayDollAccessory) {
                NetMessage.SendData(MessageID.TEDisplayDollItemSync, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot, 0f, 0, 0, 0);
            }
            if (context == ItemSlot.Context.DisplayDollDye) {
                NetMessage.SendData(MessageID.TEDisplayDollItemSync, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot, 1f, 0, 0, 0);
            }
            if (context == ItemSlot.Context.HatRackHat) {
                NetMessage.SendData(MessageID.TEHatRackItemSync, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot, 0f, 0, 0, 0);
            }
            if (context == ItemSlot.Context.HatRackDye) {
                NetMessage.SendData(MessageID.TEHatRackItemSync, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot, 1f, 0, 0, 0);
            }

        }
    }

    private static bool s_allowResetStackSplit = false;
}

public record struct Multipliers(int Mouse, int Inventory);