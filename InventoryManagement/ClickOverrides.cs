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

public sealed class ClickOverrides : ILoadable {

    public static Configs.InventoryManagement Config => Configs.InventoryManagement.Instance;

    public void Load(Mod mod) {
        On_Main.DrawInterface_36_Cursor += HookDrawCustomCursor;
        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;

        // IL_Recipe.Create += ILCreateRecipe;

        On_ChestUI.LootAll += HookLootAll;
        On_ChestUI.Restock += HookRestock;

        On_ItemSlot.LeftClick_ItemArray_int_int += HookShiftLeftCustom;
        On_ItemSlot.RightClick_ItemArray_int_int += HookShiftRight;

        On_Chest.AddItemToShop += HookStackSold;

    }
    public void Unload() { }


    private static void HookShiftLeftCustom(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Config.shiftRight || !Main.mouseLeft || Main.cursorOverride == -1 || context != ItemSlot.Context.ShopItem && context != ItemSlot.Context.CreativeInfinite) orig(inv, context, slot);
        else TwoStepClick(inv, context, slot, (inv, context, slot) => orig(inv, context, slot));
    }
    private static void HookShiftRight(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Main.mouseRight || !Config.shiftRight || Main.cursorOverride == -1) orig(inv, context, slot);
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


    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if (!Config.shiftRight || context != ItemSlot.Context.ShopItem || !ItemSlot.ShiftInUse || inv[slot].IsAir || !Main.LocalPlayer.ItemSpace(inv[slot]).CanTakeItem) return false;
        Main.cursorOverride = CursorOverrideID.QuickSell;
        return true;
    }
    internal static void ILPreventChainBuy(ILContext il) {
        ILCursor cursor = new(il);

        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitDelegate((bool rightClickIsValid, bool leftClickIsValid) => {
            if (Config.craftStack && (Config.craftStack.Value.invertClicks ? (rightClickIsValid && !Main.mouseRightRelease) : (leftClickIsValid && !Main.mouseLeftRelease))) return true;
            return false;
        });
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrfalse(skip);
        cursor.EmitRet();
        cursor.MarkLabel(skip);
    }
    internal static void ILBuyStack(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(Player), nameof(Player.GetItemExpectedPrice)));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdloc(4); // long calcForBuying
        cursor.EmitDelegate((Item[] inv, int slot, bool rightClickIsValid, bool leftClickIsValid, long price) => {
            if (!Config.craftStack || !(Config.craftStack.Value.invertClicks ? rightClickIsValid  : leftClickIsValid)) {
                s_ilShopMultiplier = 1;
                return price;
            }
            int amount = GetMaxBuyAmount(inv[slot], price);
            int available = inv[slot].buyOnce ? inv[slot].stack : inv[slot].maxStack;
            int maxPickup = Config.shiftRight && Main.cursorOverride == CursorOverrideID.QuickSell ? GetMaxPickupAmount(inv[slot]) : (inv[slot].maxStack - Main.mouseItem.stack);
            s_ilShopMultiplier = Math.Min(Math.Min(Math.Min(amount, available), maxPickup), Config.craftStack.Value.maxAmount);
            if (!inv[slot].buyOnce) inv[slot].stack = s_ilShopMultiplier;
            return price * s_ilShopMultiplier;
        });
        cursor.EmitStloc(4);

        cursor.GotoNext(MoveType.Before, i => i.MatchStfld(typeof(Item), nameof(Item.stack)));
        cursor.EmitDelegate((int one) => s_ilShopMultiplier == 1 ? one : s_ilShopMultiplier);

        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemLoader), nameof(ItemLoader.StackItems)));
        cursor.EmitDelegate((int? one) => s_ilShopMultiplier == 1 ? one : s_ilShopMultiplier );

        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.RefreshStackSplitCooldown)));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitDelegate((Item[] inv, int slot) => {
            if (inv[slot].buyOnce) inv[slot].stack -= s_ilShopMultiplier - 1;
            else inv[slot].stack /= s_ilShopMultiplier;
            s_ilShopMultiplier = 1;
        });
    }


    internal static void ILCraftCursorOverride(ILContext context) {
        ILCursor cursor = new(context);

        // if (Main.focusRecipe == recipeIndex && ++[Main.guideItem.IsAir || <allowCraft>]) {
        //     <flags*4>
        cursor.GotoNext(MoveType.After, i => i.MatchStloc(5));
        
        //     + <overrideHover>
        cursor.EmitLdloc(3); // bool flag3
        cursor.EmitLdloc(5); // bool flag5
        cursor.EmitDelegate((bool canCraft, bool crafting) => {
            if (!Config.shiftRight || !ItemSlot.ShiftInUse) return;
            if (canCraft && !crafting && Main.stackSplit <= 1) Main.cursorOverride = CraftCursorID;
        });
        //     ...
        // }
    }
    private static void HookDrawCustomCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        if (Config.shiftRight && Main.cursorOverride == CraftCursorID) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(0, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
            Main.spriteBatch.Draw(CursorCraft.Value, new Vector2(Main.mouseX, Main.mouseY), null, Color.White, 0, default, Main.cursorScale, 0, 0f);
        } else orig();
    }

    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting) {
        movedAnItemToAllowCrafting = false;
        if (Config.craftStack && (Config.craftStack.Value.invertClicks ? (Main.mouseRight && !Main.mouseRightRelease) : (Main.mouseLeft && !Main.mouseLeftRelease))) return false;
        if (Config.shiftRight && Main.cursorOverride == CraftCursorID) return Main.LocalPlayer.ItemSpace(currentRecipe.createItem).CanTakeItem;
        return orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting, out movedAnItemToAllowCrafting);
    }
    internal static void ILShiftCraft(ILContext il) {
        ILCursor cursor = new(il);

        // ++ if(<Shift>){
        // ++     if(!<canTakeItem>) return;
        // ++     goto skipCheck;
        // ++ }
        ILLabel skipVanillaCheck = cursor.DefineLabel();
        ILLabel vanillaCheck = cursor.DefineLabel();
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => Config.shiftRight && Main.cursorOverride == CraftCursorID);
        cursor.EmitBrfalse(vanillaCheck);
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => Main.LocalPlayer.ItemSpace(r.createItem).CanTakeItem);
        cursor.EmitBrtrue(skipVanillaCheck);
        cursor.EmitRet();
        cursor.MarkLabel(vanillaCheck);

        // if (Main.mouseItem.stack > 0 && !ItemLoader.CanStack(Main.mouseItem, r.createItem)) return;
        cursor.GotoNext(i => i.MatchStloc0());
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());

        // ++ skipCheck:
        cursor.MarkLabel(skipVanillaCheck);
        // if (<cannotCraft>) return;
    }
    internal static void ILCraftStack(ILContext il) {
        ILCursor cursor = new(il);
        // if (<cannotCraft>) return;
        // ++ r `*=` <amountOfCrafts>
        cursor.GotoNext(i => i.MatchStloc0());
        cursor.GotoPrev(MoveType.After, i => i.MatchLdarg0());
        cursor.EmitDelegate((Recipe r) => {
            s_ilCraftMultiplier = 1;
            if (!Config.craftStack || !(Config.craftStack.Value.invertClicks ? Main.mouseRight : Main.mouseLeft)) return r;
            int amount = GetMaxCraftAmount(r);
            int maxPickup = Config.shiftRight && Main.cursorOverride == CraftCursorID ? GetMaxPickupAmount(r.createItem) : (r.createItem.maxStack - Main.mouseItem.stack);
            s_ilCraftMultiplier = Math.Min(Math.Min(amount, maxPickup / r.createItem.stack), Config.craftStack.Value.maxAmount / r.createItem.stack);
            r.createItem.stack *= s_ilCraftMultiplier;
            foreach (Item i in r.requiredItem) i.stack *= s_ilCraftMultiplier;
            return r;
        });
        // Item crafted = r.createItem.Clone();
        // ...

    }
    internal static void ILRestoreRecipe(ILContext il) {
        ILCursor cursor = new(il);

        // Item crafted = r.createItem.Clone();
        // r.Create();
        // RecipeLoader.OnCraft(crafted, r, Main.mouseItem);
        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(RecipeLoader), nameof(RecipeLoader.OnCraft)));

        // ++ <restoreRecipe>
        // ++ if(<gotoInventory>) {
        // ++     <getItems>
        // ++     return;
        // ++ }
        cursor.EmitLdarg0();
        cursor.EmitLdloc0();
        cursor.EmitDelegate((Recipe r, Item crafted) => {
            if (s_ilCraftMultiplier != 1) {
                r.createItem.stack /= s_ilCraftMultiplier;
                foreach (Item i in r.requiredItem) i.stack /= s_ilCraftMultiplier;
            }
            if (Config.shiftRight && Main.cursorOverride == CraftCursorID) {
                Main.LocalPlayer.GetItem(Main.myPlayer, crafted, GetItemSettings.InventoryUIToInventorySettingsShowAsNew);
                return true;
            }
            return false;
        });
        ILLabel normalCraftItemCode = cursor.DefineLabel();
        cursor.EmitBrfalse(normalCraftItemCode);
        cursor.EmitRet();
        cursor.MarkLabel(normalCraftItemCode); // TODO test can turn mouseItem to air or lose crafted item
    }
    internal static void ILFixCraftMouseText(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(i => i.MatchCall(typeof(PopupText), nameof(PopupText.NewText)));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdfld(Reflection.Item.stack));
        cursor.EmitDelegate((int stack) => stack * s_ilCraftMultiplier);
        // PopupText.NewText(...);
        // ...
    }
    // private static void ILCreateRecipe(ILContext il) {
    //     ILCursor cursor = new(il);

    //     // foreach (<requiredItem>) {
    //     //     ...
    //     //     RecipeLoader.ConsumeItem(this, item2.type, ref num);
    //     cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(RecipeLoader), nameof(RecipeLoader.ConsumeItem)));

    //     //     ++ <bulkCraftCost>
    //     cursor.EmitLdloca(4);
    //     cursor.EmitDelegate((ref int consumed) => { consumed *= s_ilCraftMultiplier; });
    //     //     <consumeItems>
    //     // }
    //     // ...
    // }


    internal static void ILStackStrash(ILContext il) {
        ILCursor cursor = new(il);
        // if (<shop>){
        //     ...
        // }

        // else if (!inv[slot].favorited) {
        //     SoundEngine.PlaySound(7, -1, -1, 1, 1f, 0f);
        cursor.GotoNext(MoveType.Before, i => i.MatchStfld(Reflection.Player.trashItem));

        //     ++<stackTrash>
        cursor.EmitDelegate((Item trash) => {
            if(Config.stackTrash && trash.type == Main.LocalPlayer.trashItem.type) {
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
        if (!Config.stackTrash || baught >= newItem.stack) return orig(self, newItem);
        newItem.stack -= Main.shopSellbackHelper.Remove(newItem);
        for (int i = 0; i < self.item.Length; i++) {
            if (self.item[i].IsAir || self.item[i].type != newItem.type || !self.item[i].buyOnce) continue;
            if (!ItemLoader.TryStackItems(self.item[i], newItem, out int transfered)) continue;
            if (newItem.IsAir) return i;
        }

        return orig(self, newItem);
    }


    internal static void ILKeepFavoriteInChest(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(i => i.MatchStfld(Reflection.Item.favorited));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitDelegate((bool fav, Item[] inv, int context, int slot) => {
            if(Config.favoriteInBanks && context == ItemSlot.Context.BankItem) fav = inv[slot].favorited;
            return fav;
        });
    }
    private static void HookRestock(On_ChestUI.orig_Restock orig) {
        ChestUI.GetContainerUsageInfo(out bool sync, out Item[] items);
        if (!sync && Config.favoriteInBanks) Utility.RunWithHiddenItems(items, i => i.favorited, () => orig());
        else orig();
    }
    private static void HookLootAll(On_ChestUI.orig_LootAll orig) {
        ChestUI.GetContainerUsageInfo(out bool sync, out Item[] items);
        if (!sync && Config.favoriteInBanks) Utility.RunWithHiddenItems(items, i => i.favorited, () => orig());
        else orig();
    }


    public static int GetMaxBuyAmount(Item item, long price) {
        if (price == 0) return item.maxStack;
        else return (int)Math.Clamp(Utility.CountCurrency(Main.LocalPlayer, item.shopSpecialCurrency) / price, 1, item.maxStack - Main.mouseItem.stack);
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

    private static int s_ilCraftMultiplier = 1;
    private static int s_ilShopMultiplier = 1;

    public const int CraftCursorID = 22;
    public static Asset<Texture2D> CursorCraft => ModContent.Request<Texture2D>($"BetterInventory/Assets/Cursor_Craft");
}
