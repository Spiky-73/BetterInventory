using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib;
using SpikysLib.CrossMod;
using SpikysLib.Extensions;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed class ClickOverrides : ILoadable {

    public void Load(Mod mod) {
        On_Main.DrawInterface_36_Cursor += HookDrawCustomCursor;
        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;

        On_ChestUI.LootAll += HookLootAll;
        On_ChestUI.Restock += HookRestock;

        On_ItemSlot.LeftClick_ItemArray_int_int += HookShiftLeftCustom;
        On_ItemSlot.RightClick_ItemArray_int_int += HookShiftRight;

        On_Chest.AddItemToShop += HookStackSold;

        IL_Player.PayCurrency += il => {
            if(!il.ApplyTo(ILPayStack, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        };

        IL_Recipe.Create += il => {
            if(!il.ApplyTo(ILRecipeConsumeStack, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        };
        IL_ItemSlot.LeftClick_ItemArray_int_int += il => {
            if(!il.ApplyTo(ILKeepFavoriteInBanks, Configs.InventoryManagement.FavoriteInBanks)) Configs.UnloadedInventoryManagement.Value.favoriteInBanks = true;
        };
        IL_ItemSlot.HandleShopSlot += il => {
            if(!il.ApplyTo(ILPreventChainBuy, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
            if(!il.ApplyTo(ILBuyMultiplier, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
            if(!il.ApplyTo(ILBuyStack, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
            if(!il.ApplyTo(ILRestoreShopItem, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        };
        IL_ItemSlot.SellOrTrash += il => {
            if(!il.ApplyTo(ILStackTrash, Configs.InventoryManagement.StackTrash)) Configs.UnloadedInventoryManagement.Value.stackTrash = true;
        };
        IL_Main.HoverOverCraftingItemButton += il => {
            if (!il.ApplyTo(ILShiftRightCursorOverride, Configs.InventoryManagement.ShiftRight)) Configs.UnloadedInventoryManagement.Value.shiftRight = true;
        };
        IL_Main.CraftItem += il => {
            if (!il.ApplyTo(ILShiftCraft, Configs.InventoryManagement.ShiftRight)) Configs.UnloadedInventoryManagement.Value.shiftRight = true;
            if (!il.ApplyTo(ILCraftMultiplier, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
            if (!il.ApplyTo(ILCraftStackAndPickup, Configs.CraftStack.Enabled || Configs.InventoryManagement.ShiftRight)) Configs.UnloadedInventoryManagement.Value.craftStack = Configs.UnloadedInventoryManagement.Value.shiftRight = true;
            if (!il.ApplyTo(ILCraftFixMouseText, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        };
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += il => {
            if (!il.ApplyTo(ILFavoritedBankBackground, Configs.InventoryManagement.FavoriteInBanks)) Configs.UnloadedInventoryManagement.Value.favoriteInBanks = true;
        };
    }
    public void Unload() { }

    public static void AddCraftStackLine(Item item, List<TooltipLine> tooltips) {
        if (!Configs.CraftStack.Tooltip || !(item.tooltipContext == ItemSlot.Context.ShopItem || (item.tooltipContext == ItemSlot.Context.CraftingMaterial && !item.IsNotSameTypePrefixAndStack(Main.recipe[Main.availableRecipe[Main.focusRecipe]].createItem)))) return;
        tooltips.Add(new(
            BetterInventory.Instance, "CraftStack",
            Language.GetTextValue($"{Localization.Keys.UI}.CraftStackTooltip",
                Lang.SupportGlyphs(Configs.CraftStack.Value.invertClicks ? "<right>" : "<left>"),
                Language.GetTextValue($"{Localization.Keys.UI}.{(item.tooltipContext == ItemSlot.Context.ShopItem ? "Buy" : "Craft")}"),
                GetMaxStackAmount(item)
        )));
    }


    private static void HookShiftLeftCustom(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Configs.InventoryManagement.ShiftRight || !Main.mouseLeft || Main.cursorOverride <= CursorOverrideID.DefaultCursor || context != ItemSlot.Context.ShopItem && context != ItemSlot.Context.CreativeInfinite) orig(inv, context, slot);
        else TwoStepClick(inv, context, slot, (inv, context, slot) => orig(inv, context, slot));
    }
    private static void HookShiftRight(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Main.mouseRight || !Configs.InventoryManagement.ShiftRight || Main.cursorOverride <= CursorOverrideID.DefaultCursor) orig(inv, context, slot);
        else TwoStepClick(inv, context, slot, (inv, context, slot) => orig(inv, context, slot));
    }
    private static void TwoStepClick(Item[] inv, int context, int slot, Action<Item[], int, int> click){
        (Item mouse, Main.mouseItem) = (Main.mouseItem, new());
        click(inv, context, slot);
        (Main.mouseItem, Item[] inv2) = (mouse, new[]{Main.mouseItem});
        if (inv2[0].IsAir) return;
        (bool left, bool leftR, Main.mouseLeft, Main.mouseLeftRelease) = (Main.mouseLeft, Main.mouseLeftRelease, true, true);
        int cursor = Main.cursorOverride;
        if (Array.IndexOf(TransportCursors, Main.cursorOverride) == -1) (context, Main.cursorOverride) = (ItemSlot.Context.ChestItem, CursorOverrideID.ChestToInventory);
        ItemSlot.LeftClick(inv2, context, 0);
        (Main.mouseLeft, Main.mouseLeftRelease) = (left, leftR);
        Main.cursorOverride = cursor;
        if (!inv2[0].IsAir) inv[slot] = ItemExtensions.MoveInto(inv[slot], inv2[0], out _);
        if(Main.mouseRight) Recipe.FindRecipes();
    }


    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if (!Configs.InventoryManagement.ShiftRight || inv[slot].IsAir) return false;
        if((context == ItemSlot.Context.ChestItem || context == ItemSlot.Context.BankItem) && ItemSlot.ControlInUse){
            Main.cursorOverride = CursorOverrideID.TrashCan;
            return true;
        }
        if (context == ItemSlot.Context.ShopItem && ItemSlot.ShiftInUse && Main.LocalPlayer.ItemSpace(inv[slot]).CanTakeItem) {
            Main.cursorOverride = CursorOverrideID.QuickSell;
            return true;
        }
        return false;
    }
    private static void ILPreventChainBuy(ILContext il) {
        ILCursor cursor = new(il);

        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitDelegate((bool rightClickIsValid, bool leftClickIsValid) => {
            if (Configs.CraftStack.Enabled && (Configs.CraftStack.Value.invertClicks ? (rightClickIsValid && !Configs.CraftStack.Value.repeat && !Main.mouseRightRelease) : (leftClickIsValid && !Configs.CraftStack.Value.repeat && !Main.mouseLeftRelease))) return true;
            return false;
        });
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrfalse(skip);
        cursor.EmitRet();
        cursor.MarkLabel(skip);
    }
    private static void ILBuyMultiplier(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(Player), nameof(Player.GetItemExpectedPrice)));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdloc(4); // long calcForBuying
        cursor.EmitDelegate((Item[] inv, int slot, bool rightClickIsValid, bool leftClickIsValid, long price) => {
            if (!Configs.CraftStack.Enabled || !(Configs.CraftStack.Value.invertClicks ? rightClickIsValid : leftClickIsValid)) {
                s_ilShopMultiplier = 1;
                return price;
            }
            int buy = GetMaxBuyAmount(inv[slot], price);
            int available = inv[slot].buyOnce ? inv[slot].stack : inv[slot].maxStack;
            int pickup = GetMaxPickupAmount(inv[slot]);
            int stack = Configs.CraftStack.Value.maxItems == 0 && SpysInfiniteConsumables.Enabled ? (int)SpysInfiniteConsumables.GetMixedRequirement(Main.LocalPlayer, inv[slot]) : Configs.CraftStack.Value.maxItems;
            s_ilShopMultiplier = Math.Max(MathX.Min(buy, available, pickup, stack), 1);
            return price;
        });
        cursor.EmitStloc(4);
    }
    private static void ILBuyStack(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.Before, i => i.MatchStfld(typeof(Item), nameof(Item.stack)));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitDelegate((int one, Item[] inv, int slot) => {
            if (!Configs.CraftStack.Enabled || s_ilShopMultiplier == 1) return one;
            if (!inv[slot].buyOnce) inv[slot].stack = s_ilShopMultiplier;
            return s_ilShopMultiplier;
        });

        cursor.GotoNext(MoveType.Before, i => i.SaferMatchCall(typeof(ItemLoader), nameof(ItemLoader.StackItems)));
        cursor.EmitDelegate((int? one) => !Configs.CraftStack.Enabled || s_ilShopMultiplier == 1 ? one : s_ilShopMultiplier );

    }
    private static void ILPayStack(ILContext il) {
        ILCursor cursor = new(il);

        cursor.EmitLdarg1();
        cursor.EmitDelegate((int price) => Configs.CraftStack.Enabled && s_ilShopMultiplier != 1 ? (price * s_ilShopMultiplier) : price);
        cursor.EmitStarg(1);
    }
    private static void ILRestoreShopItem(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(ItemSlot), nameof(ItemSlot.RefreshStackSplitCooldown)));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdloc(4);
        cursor.EmitDelegate((Item[] inv, int slot, long price) => {
            if (!Configs.CraftStack.Enabled || s_ilShopMultiplier == 1) return;
            if (inv[slot].buyOnce) inv[slot].stack -= s_ilShopMultiplier - 1;
            else inv[slot].stack /= s_ilShopMultiplier;
            s_ilShopMultiplier = 1;
        });
    }


    private static void ILShiftRightCursorOverride(ILContext context) {
        ILCursor cursor = new(context);

        // if (Main.focusRecipe == recipeIndex && ++[Main.guideItem.IsAir || <allowCraft>]) {
        //     <flags*4>
        cursor.GotoNext(MoveType.After, i => i.MatchStloc(5));
        
        //     + <overrideHover>
        cursor.EmitLdloc(3); // bool flag3
        cursor.EmitLdloc(5); // bool flag5
        cursor.EmitDelegate((bool canCraft, bool crafting) => {
            if (!Configs.InventoryManagement.ShiftRight || !ItemSlot.ShiftInUse) return;
            if (canCraft && Main.LocalPlayer.ItemSpace(Main.recipe[Main.availableRecipe[Main.focusRecipe]].createItem).CanTakeItem && !crafting && Main.stackSplit <= 1) Main.cursorOverride = CraftCursorID;
        });
        //     ...
        // }
    }
    private static void HookDrawCustomCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        if (Configs.InventoryManagement.ShiftRight && Main.cursorOverride == CraftCursorID) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(0, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
            Main.spriteBatch.Draw(CursorCraft.Value, new Vector2(Main.mouseX, Main.mouseY), null, Color.White, 0, default, Main.cursorScale, 0, 0f);
        } else orig();
    }

    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting) {
        movedAnItemToAllowCrafting = false;
        if (Configs.CraftStack.Enabled && (Configs.CraftStack.Value.invertClicks ? (Main.mouseRight && !Configs.CraftStack.Value.repeat && !Main.mouseRightRelease) : (Main.mouseLeft && !Configs.CraftStack.Value.repeat && !Main.mouseLeftRelease))) return false;
        if (Configs.InventoryManagement.ShiftRight && Main.cursorOverride == CraftCursorID) return Main.LocalPlayer.ItemSpace(currentRecipe.createItem).CanTakeItem;
        return orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting, out movedAnItemToAllowCrafting);
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
        cursor.EmitDelegate((Recipe r) => Configs.InventoryManagement.ShiftRight && Main.cursorOverride == CraftCursorID);
        cursor.EmitBrfalse(vanillaCheck);
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => Main.LocalPlayer.ItemSpace(r.createItem).CanTakeItem);
        cursor.EmitBrtrue(skipVanillaCheck);
        cursor.EmitRet();
        cursor.MarkLabel(vanillaCheck);
        cursor.MarkLabel(skipVanillaCheck); // Here in case of exception

        // if (Main.mouseItem.stack > 0 && !ItemLoader.CanStack(Main.mouseItem, r.createItem)) return;
        cursor.GotoNext(i => i.MatchStloc0());
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());

        // ++ skipCheck:
        cursor.MarkLabel(skipVanillaCheck);
        // if (<cannotCraft>) return;
    }
    private static void ILCraftMultiplier(ILContext il) {
        ILCursor cursor = new(il);
        // if (<cannotCraft>) return;
        // ++ r `*=` <amountOfCrafts>
        cursor.GotoNext(i => i.MatchStloc0());
        cursor.GotoPrev(MoveType.After, i => i.MatchLdarg0());
        cursor.EmitDelegate((Recipe r) => {
            s_ilCraftMultiplier = 1;
            if (!Configs.CraftStack.Enabled || !(Configs.CraftStack.Value.invertClicks ? Main.mouseRight : Main.mouseLeft)) return r;
            int craft = GetMaxCraftAmount(r);
            int pickup = GetMaxPickupAmount(r.createItem);
            int stack = GetMaxStackAmount(r.createItem);
            s_ilCraftMultiplier = Math.Max(MathX.Min(craft, pickup / r.createItem.stack, Configs.CraftStack.Value.maxItems / r.createItem.stack), 1);
            return r;
        });
        // Item crafted = r.createItem.Clone();
        // ...
    }
    private static void ILRecipeConsumeStack(ILContext il) {
        ILCursor cursor = new(il);

        // foreach (<requiredItem>) {
        //     ...
        //     RecipeLoader.ConsumeItem(this, item2.type, ref num);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(RecipeLoader), nameof(RecipeLoader.ConsumeItem)));

        //     ++ <bulkCraftCost>
        cursor.EmitLdloc(4);
        cursor.EmitDelegate((int consumed) => Configs.CraftStack.Enabled ? (consumed * s_ilCraftMultiplier) : consumed);
        cursor.EmitStloc(4);
        //     <consumeItems>
        // }
        // ...
    }
    private static void ILCraftStackAndPickup(ILContext il) {
        ILCursor cursor = new(il);

        // Item crafted = r.createItem.Clone();
        // r.Create();
        // RecipeLoader.OnCraft(crafted, r, Main.mouseItem);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(RecipeLoader), nameof(RecipeLoader.OnCraft)));

        // ++ <restoreRecipe>
        // ++ if(<gotoInventory>) {
        // ++     <getItems>
        // ++     return;
        // ++ }
        cursor.EmitLdarg0();
        cursor.EmitLdloc0();
        cursor.EmitDelegate((Recipe r, Item crafted) => {
            if (Configs.CraftStack.Enabled && s_ilCraftMultiplier != 1) crafted.stack *= s_ilCraftMultiplier;
            if (Configs.InventoryManagement.ShiftRight && Main.cursorOverride == CraftCursorID) {
                Main.LocalPlayer.GetDropItem(ref crafted, GetItemSettings.InventoryUIToInventorySettingsShowAsNew);
                return true;
            }
            return false;
        });
        ILLabel normalCraftItemCode = cursor.DefineLabel();
        cursor.EmitBrfalse(normalCraftItemCode);
        cursor.EmitRet();
        cursor.MarkLabel(normalCraftItemCode);
    }
    private static void ILCraftFixMouseText(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(i => i.SaferMatchCall(typeof(PopupText), nameof(PopupText.NewText)));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdfld(Reflection.Item.stack));
        cursor.EmitDelegate((int stack) => Configs.CraftStack.Enabled ? (stack * s_ilCraftMultiplier) : stack);
        // PopupText.NewText(...);
        // ...
    }

    private static void ILStackTrash(ILContext il) {
        ILCursor cursor = new(il);
        // if (<shop>){
        //     ...
        // }

        // else if (!inv[slot].favorited) {
        //     SoundEngine.PlaySound(7, -1, -1, 1, 1f, 0f);
        cursor.GotoNext(MoveType.Before, i => i.MatchStfld(Reflection.Player.trashItem));

        //     ++<stackTrash>
        cursor.EmitDelegate((Item trash) => {
            if(Configs.InventoryManagement.StackTrash && trash.type == Main.LocalPlayer.trashItem.type) {
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
        int bought = Main.shopSellbackHelper.GetAmount(newItem);
        if (!Configs.InventoryManagement.StackTrash || bought >= newItem.stack) return orig(self, newItem);
        newItem.stack -= Main.shopSellbackHelper.Remove(newItem);
        for (int i = 0; i < self.item.Length; i++) {
            if (self.item[i].IsAir || self.item[i].type != newItem.type || !self.item[i].buyOnce) continue;
            if (!ItemLoader.TryStackItems(self.item[i], newItem, out int transferred)) continue;
            if (newItem.IsAir) return i;
        }

        return orig(self, newItem);
    }


    private static void ILKeepFavoriteInBanks(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.Before, i => i.MatchStfld(Reflection.Item.favorited));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitDelegate((bool fav, Item[] inv, int context, int slot) => {
            if(Configs.InventoryManagement.FavoriteInBanks && context == ItemSlot.Context.BankItem) fav = inv[slot].favorited;
            return fav;
        });
    }
    private static void HookRestock(On_ChestUI.orig_Restock orig) {
        ChestUI.GetContainerUsageInfo(out bool sync, out Item[] items);
        if (!sync && Configs.InventoryManagement.FavoriteInBanks) ItemExtensions.RunWithHiddenItems(items, () => orig(), i => i.favorited);
        else orig();
    }
    private static void HookLootAll(On_ChestUI.orig_LootAll orig) {
        ChestUI.GetContainerUsageInfo(out bool sync, out Item[] items);
        if (!sync && Configs.InventoryManagement.FavoriteInBanks) ItemExtensions.RunWithHiddenItems(items, () => orig(), i => i.favorited);
        else orig();
    }
    private static void ILFavoritedBankBackground(ILContext il) {
        ILCursor cursor = new(il);

        // if (item.type > 0 && item.stack > 0 && item.favorited && context != 13 && context != 21 && context != 22 && context != 14) {
        //     value = TextureAssets.InventoryBack10.Value;
        //     ++ <favorited>
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(Reflection.Asset<Texture2D>.Value.GetMethod!) && i.Previous.MatchLdsfld(Reflection.TextureAssets.InventoryBack10));
        cursor.EmitLdarg1().EmitLdarg2().EmitLdarg3();
        cursor.EmitDelegate((Texture2D texture, Item[] inv, int context, int slot) => Configs.InventoryManagement.FavoriteInBanks && context == ItemSlot.Context.BankItem && inv[slot].favorited ? TextureAssets.InventoryBack19.Value : texture);
        // }
    }

    public static int GetMaxStackAmount(Item item) {
        if (Configs.CraftStack.Value.maxItems != 0 || !SpysInfiniteConsumables.Enabled) return Configs.CraftStack.Value.maxItems.amount;
        return (int)SpysInfiniteConsumables.GetMixedRequirement(Main.LocalPlayer, item);
    }

    public static int GetMaxBuyAmount(Item item, long price) {
        if (price == 0) return item.maxStack;
        else return (int)Math.Clamp(Main.LocalPlayer.CountCurrency(item.shopSpecialCurrency) / price, 1, item.maxStack - Main.mouseItem.stack);
    }
    public static int GetMaxCraftAmount(Recipe recipe) {
        Dictionary<int, int> groupItems = new();
        foreach (int id in recipe.acceptedGroups) {
            RecipeGroup group = RecipeGroup.recipeGroups[id];
            groupItems.Add(group.IconicItemId, group.GetGroupFakeItemId());
        }

        int amount = 0;
        foreach (Item material in recipe.requiredItem) {
            int a = PlayerExtensions.OwnedItems[groupItems.GetValueOrDefault(material.type, material.type)] / material.stack;
            if (amount == 0 || a < amount) amount = a;
        }
        return amount;
    }
    public static int GetMaxPickupAmount(Item item) {
        if (!Configs.InventoryManagement.ShiftRight || Main.cursorOverride != CraftCursorID) return item.maxStack - Main.mouseItem.stack;
        int free = GetFreeSpace(Main.LocalPlayer.inventory, item, 58);
        if (Main.LocalPlayer.InChest(out Item[]? chest)) free += GetFreeSpace(chest, item);
        if (Main.LocalPlayer.useVoidBag() && Main.LocalPlayer.chest != -5) free += GetFreeSpace(Main.LocalPlayer.bank4.item, item);
        return free;
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

    public static readonly int[] TransportCursors = [CursorOverrideID.TrashCan, CursorOverrideID.InventoryToChest, CursorOverrideID.ChestToInventory];

}
