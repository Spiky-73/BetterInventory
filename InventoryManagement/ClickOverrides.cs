using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed class ClickOverride : ILoadable {

    public static bool Enabled => Configs.InventoryManagement.Instance.clickOverrides;
    public static Configs.ClickOverride Config => Configs.InventoryManagement.Instance.clickOverrides.Value;

    public void Load(Mod mod) {
        On_Main.DrawInterface_36_Cursor += HookDrawCustomCursor;
        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;

        IL_Main.CraftItem += ILCraftItem;
        IL_Recipe.Create += ILCreateRecipe;

        IL_ItemSlot.HandleShopSlot += ILHandleShopSlot;
        On_ItemSlot.LeftClick_ItemArray_int_int += HookShiftLeftCustom;
        On_ItemSlot.RightClick_ItemArray_int_int += HookShiftRight;

        IL_ItemSlot.SellOrTrash += ILStackStrash;
        On_Chest.AddItemToShop += HookStackSold;

    }
    public void Unload() { }

    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if (!Enabled || !Config.shops || context != ItemSlot.Context.ShopItem || !ItemSlot.ShiftInUse || inv[slot].IsAir || !Main.LocalPlayer.ItemSpace(inv[slot]).CanTakeItem) return false;
        Main.cursorOverride = CursorOverrideID.QuickSell;
        return true;
    }
    private static void HookShiftLeftCustom(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Enabled || !Main.mouseLeft || Main.cursorOverride == -1 || context != ItemSlot.Context.ShopItem && context != ItemSlot.Context.CreativeInfinite) orig(inv, context, slot);
        else TwoStepClick(inv, context, slot, (inv, context, slot) => orig(inv, context, slot));
    }
    private static void HookShiftRight(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Enabled || !Main.mouseRight || !Config.shiftRight || Main.cursorOverride == -1) orig(inv, context, slot);
        else TwoStepClick(inv, context, slot, (inv, context, slot) => orig(inv, context, slot));
    }
    private static void TwoStepClick(Item[] inv, int context, int slot, Action<Item[], int, int> click){
        (Item mouse, Main.mouseItem) = (Main.mouseItem, new());
        click(inv, context, slot);
        (Main.mouseItem, Item[] inv2) = (mouse, new[]{Main.mouseItem});
        if (inv2[0].IsAir) return;
        (bool left, bool leftR, Main.mouseLeft, Main.mouseLeftRelease) = (Main.mouseLeft, Main.mouseLeftRelease, true, true);
        int cursor = Main.cursorOverride;
        if (Array.IndexOf(Utility.InventoryContexts, context) == -1) (context, Main.cursorOverride) = (ItemSlot.Context.ChestItem, CursorOverrideID.ChestToInventory);
        ItemSlot.LeftClick(inv2, context, 0);
        (Main.mouseLeft, Main.mouseLeftRelease) = (left, leftR);
        Main.cursorOverride = cursor;
        if (!inv2[0].IsAir) inv[slot] = Utility.MoveInto(inv[slot], inv2[0], out _);
        if(Main.mouseRight) Recipe.FindRecipes();
    }

    private static void ILHandleShopSlot(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(Player), nameof(Player.GetItemExpectedPrice)));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdloc(4);
        cursor.EmitDelegate((Item[] inv, int slot, bool rightClickIsValid, bool leftClickIsValid, long price) => {
            if (!Enabled || !Config.shops || !(Config.invertClicks ? (rightClickIsValid && Main.mouseRightRelease) : (leftClickIsValid && Main.mouseLeftRelease))) {
                s_shopMultiplier = 1;
                return price;
            }
            s_shopMultiplier = (int)Math.Clamp(Utility.CountCurrency(Main.LocalPlayer, inv[slot].shopSpecialCurrency) / price, 1, inv[slot].maxStack - Main.mouseItem.stack);
            if (inv[slot].buyOnce) s_shopMultiplier = Math.Min(s_shopMultiplier, inv[slot].stack);
            else inv[slot].stack = s_shopMultiplier;
            return price * s_shopMultiplier;
        });
        cursor.EmitStloc(4);

        cursor.GotoNext(MoveType.Before, i => i.MatchStfld(typeof(Item), nameof(Item.stack)));
        cursor.EmitDelegate((int stack) => s_shopMultiplier == 1 ? stack : s_shopMultiplier);

        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemLoader), nameof(ItemLoader.StackItems)));
        cursor.EmitDelegate((int? amount) => s_shopMultiplier == 1 ? amount : s_shopMultiplier );

        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.RefreshStackSplitCooldown)));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitDelegate((Item[] inv, int slot) => {
            if (!inv[slot].buyOnce) inv[slot].stack /= s_shopMultiplier;
            else inv[slot].stack -= s_shopMultiplier-1;
            s_shopMultiplier = 1;
        });
    }

    public static void OverrideCraftHover(int recipeIndex) {
        if (Enabled && Config.crafting && recipeIndex == Main.focusRecipe && ItemSlot.ShiftInUse) Main.cursorOverride = CraftCursorID;
    }
    private static void HookDrawCustomCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        if (Main.cursorOverride == CraftCursorID) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(0, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
            Main.spriteBatch.Draw(CursorCraft.Value, new Vector2(Main.mouseX, Main.mouseY), null, Color.White, 0, default, Main.cursorScale, 0, 0f);
        } else orig();
    }

    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting) {
        if (Enabled && Config.crafting) {
            movedAnItemToAllowCrafting = false;
            if (Config.invertClicks ? (Main.mouseRight && !Main.mouseRightRelease) : (Main.mouseLeft && !Main.mouseLeftRelease)) return false;
            if (Main.cursorOverride == CraftCursorID) return Main.LocalPlayer.ItemSpace(currentRecipe.createItem).CanTakeItem;
        }
        return orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting, out movedAnItemToAllowCrafting);
    }

    private static void ILCraftItem(ILContext il) {
        ILCursor cursor = new(il);

        // ++ if(<Shift>){
        // ++     if(!<canTakeItem>) return;
        // ++     goto skipCheck;
        // ++ }
        // ++ vanillaCheck:
        ILLabel skipCheck = cursor.DefineLabel();
        ILLabel vanillaCheck = cursor.DefineLabel();
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => Enabled && Config.crafting && Main.cursorOverride == CraftCursorID);
        cursor.EmitBrfalse(vanillaCheck);
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => Main.LocalPlayer.ItemSpace(r.createItem).CanTakeItem);
        cursor.EmitBrtrue(skipCheck);
        cursor.EmitRet();
        cursor.MarkLabel(vanillaCheck);

        // <vanillaCheck>
        cursor.GotoNext(i => i.MatchCallOrCallvirt(typeof(Recipe), nameof(Recipe.Create)));
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());

        // ++ skipCheck:
        cursor.MarkLabel(skipCheck);

        // ...
        // ++ if(<bulkCraft>) craftMultiplier = <maxAmountOfCrafts>;
        // ++ else craftMultiplier = 1;
        cursor.GotoNext(i => i.MatchCallOrCallvirt(typeof(Recipe), nameof(Recipe.Create)));
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => {
            s_craftMultiplier = 1;
            if (Enabled && Config.crafting && (Enabled && Config.invertClicks ? Main.mouseRight : Main.mouseLeft)) {
                int amount = GetMaxCraftAmount(r);
                if (Main.cursorOverride == CraftCursorID) s_craftMultiplier = Math.Min(amount, GetMaxPickupAmount(r.createItem) / r.createItem.stack);
                else s_craftMultiplier = Math.Min(amount, (r.createItem.maxStack - Main.mouseItem.stack) / r.createItem.stack);
            } else s_craftMultiplier = 1;
        });

        // r.Create();
        // RecipeLoader.OnCraft(crafted, r, Main.mouseItem);
        ILLabel normalCraftItemCode = cursor.DefineLabel();
        cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt(typeof(RecipeLoader), nameof(RecipeLoader.OnCraft)));

        // ++ <multiplyCraftAmount>
        // ++ if(<gotoInventory>) {
        // ++     <getItems>
        // ++     return;
        // ++ }
        cursor.EmitLdloc0();
        cursor.EmitDelegate((Item crafted) => {
            crafted.stack *= s_craftMultiplier;
            if (!Enabled && Config.crafting || Main.cursorOverride != CraftCursorID) return false;
            s_craftMultiplier = 1;
            Main.LocalPlayer.GetItem(Main.myPlayer, crafted, GetItemSettings.InventoryUIToInventorySettingsShowAsNew);
            return true;
        });
        cursor.EmitBrfalse(normalCraftItemCode);
        cursor.EmitRet();
        cursor.MarkLabel(normalCraftItemCode);

        // Mouse text correction
        cursor.GotoNext(i => i.MatchCall(typeof(PopupText), nameof(PopupText.NewText)));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdfld(typeof(Item), nameof(Item.stack)));
        cursor.EmitDelegate((int stack) => {
            stack *= s_craftMultiplier;
            s_craftMultiplier = 1;
            return stack;
        });
        // PopupText.NewText(...);
        // ...
    }
    private static void ILCreateRecipe(ILContext il) {
        ILCursor cursor = new(il);

        // foreach (<requiredItem>) {
        //     ...
        //     RecipeLoader.ConsumeItem(this, item2.type, ref num);
        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(RecipeLoader), nameof(RecipeLoader.ConsumeItem)));

        //     ++ <bulkCraftCost>
        cursor.EmitLdloca(4);
        cursor.EmitDelegate((ref int consumed) => { consumed *= s_craftMultiplier; });
        //     <consumeItems>
        // }
        // ...
    }


    private static void ILStackStrash(ILContext il) {
        ILCursor cursor = new(il);
        // if (<shop>){
        //     ...
        // }

        // else if (!inv[slot].favorited) {
        //     SoundEngine.PlaySound(7, -1, -1, 1, 1f, 0f);
        cursor.GotoNext(MoveType.Before, i => i.MatchStfld(Reflection.Player.trashItem));

        //     ++<stackTrash>
        cursor.EmitDelegate((Item trash) => {
            if(Enabled && Config.stacking && trash.type == Main.LocalPlayer.trashItem.type) {
                if (ItemLoader.TryStackItems(Main.LocalPlayer.trashItem, trash, out int transfered)) return Main.LocalPlayer.trashItem;
            }
            return trash;
        });


        //     player.trashItem = inv[slot].Clone();
        //     ...
        // }
        // ...
    }

    private static int HookStackSold(On_Chest.orig_AddItemToShop orig, Chest self, Item newItem) {
        int baught = Main.shopSellbackHelper.GetAmount(newItem);
        if (!Enabled || !Config.stacking || baught >= newItem.stack) return orig(self, newItem);
        newItem.stack -= Main.shopSellbackHelper.Remove(newItem);
        for (int i = 0; i < self.item.Length; i++) {
            if (self.item[i].IsAir || self.item[i].type != newItem.type || !self.item[i].buyOnce) continue;
            if (!ItemLoader.TryStackItems(self.item[i], newItem, out int transfered)) continue;
            if (newItem.IsAir) return i;
        }

        return orig(self, newItem);
    }



    public static int GetMaxCraftAmount(Recipe recipe) {
        Dictionary<int, int> groupItems = new();
        foreach (int id in recipe.acceptedGroups) {
            RecipeGroup group = RecipeGroup.recipeGroups[id];
            groupItems.Add(group.IconicItemId, group.GetGroupFakeItemId());
        }

        int amount = 0;
        foreach (Item material in recipe.requiredItem) {
            int a = Utility.OwnedItems[groupItems.GetValueOrDefault(material.type, material.type)] / material.stack;
            if (amount == 0 || a < amount) amount = a;
        }
        return amount;
    }

    public static int GetMaxPickupAmount(Item item, int max = -1) {
        if (max == -1) max = item.maxStack;
        int free = GetFreeSpace(Main.LocalPlayer.inventory, item, 58);
        if (Main.LocalPlayer.InChest(out Item[]? chest)) free += GetFreeSpace(chest, item);
        if (Main.LocalPlayer.useVoidBag() && Main.LocalPlayer.chest != -5) free += GetFreeSpace(Main.LocalPlayer.bank4.item, item);
        return Math.Min(max, free);
    }

    public static int GetFreeSpace(Item[] inv, Item item, params int[] ignored) {
        int free = 0;
        for (int i = 0; i < inv.Length; i++) {
            if (Array.IndexOf(ignored, i) != -1) continue;
            Item slot = inv[i];
            if (slot.IsAir) free += item.maxStack;
            if (slot.type == item.type) free += item.maxStack - slot.stack;
        }
        return free;
    }

    private static int s_craftMultiplier = 1;
    private static int s_shopMultiplier = 1;

    public const int CraftCursorID = 22;
    public static Asset<Texture2D> CursorCraft => ModContent.Request<Texture2D>($"BetterInventory/Assets/Cursor_Craft");

    public static readonly int[] MaterialsPerLine = new int[] { 6, 4 };

    public const int VanillaMaterialSpcacing = 40;
    public const int VanillaCurrection = -2;

}
