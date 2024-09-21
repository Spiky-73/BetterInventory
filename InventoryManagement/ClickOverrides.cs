using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.Collections;
using SpikysLib.CrossMod;
using SpikysLib.IL;
using SpikysLib.UI;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed class ClickOverrides : ILoadable {

    public void Load(Mod mod) {
        CraftCursor = CursorLoader.RegisterCursor(mod, ModContent.Request<Texture2D>($"BetterInventory/Assets/Cursor_Craft"));

        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;

        On_ChestUI.LootAll += HookLootAll;
        On_ChestUI.Restock += HookRestock;

        On_ItemSlot.LeftClick_ItemArray_int_int += HookShiftLeftCustom;
        On_ItemSlot.RightClick_ItemArray_int_int += HookShiftRight;

        On_Chest.AddItemToShop += HookStackSold;

        On_Recipe.FindRecipes += HookFindRecipes;

        IL_Player.PayCurrency += static il => {
            if(!il.ApplyTo(ILPayStack, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        };
        IL_Recipe.Create += static il => {
            if(!il.ApplyTo(ILRecipeConsumeStack, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        };
        IL_ItemSlot.LeftClick_ItemArray_int_int += static il => {
            if(!il.ApplyTo(ILKeepFavoriteInBanks, Configs.InventoryManagement.FavoriteInBanks)) Configs.UnloadedInventoryManagement.Value.favoriteInBanks = true;
        };
        IL_ItemSlot.HandleShopSlot += static il => {
            if(!il.ApplyTo(ILPreventChainBuy, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
            if(!il.ApplyTo(ILBuyMultiplier, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
            if(!il.ApplyTo(ILBuyStack, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
            if(!il.ApplyTo(ILRestoreShopItem, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        };
        IL_ItemSlot.SellOrTrash += static il => {
            if(!il.ApplyTo(ILStackTrash, Configs.InventoryManagement.StackTrash)) Configs.UnloadedInventoryManagement.Value.stackTrash = true;
        };
        IL_Main.HoverOverCraftingItemButton += static il => {
            if (!il.ApplyTo(ILShiftRightCursorOverride, Configs.BetterShiftClick.UniversalShift)) Configs.UnloadedInventoryManagement.Value.universalShift = true;
        };
        IL_Main.CraftItem += static il => {
            if (!il.ApplyTo(ILShiftCraft, Configs.BetterShiftClick.ShiftRight)) Configs.UnloadedInventoryManagement.Value.shiftRight = true;
            if (!il.ApplyTo(ILCraftMultiplier, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
            if (!il.ApplyTo(ILCraftStackAndPickup, Configs.CraftStack.Enabled || Configs.BetterShiftClick.UniversalShift)) Configs.UnloadedInventoryManagement.Value.craftStack = Configs.UnloadedInventoryManagement.Value.universalShift = true;
            if (!il.ApplyTo(ILCraftFixMouseText, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        };
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += static il => {
            if (!il.ApplyTo(ILFavoritedBankBackground, Configs.InventoryManagement.FavoriteInBanks)) Configs.UnloadedInventoryManagement.Value.favoriteInBanks = true;
        };
    }

    public void Unload() { }

    private static void HookFindRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        s_craftMultipliers.Clear();
        s_shopMultipliers.Clear();
        orig(canDelayCheck);
    }

    public static void AddCraftStackLine(Item item, List<TooltipLine> tooltips) {
        if (!Configs.CraftStack.Tooltip) return;
        bool recipe;
        if (item.tooltipContext == ItemSlot.Context.CraftingMaterial && !item.IsNotSameTypePrefixAndStack(Main.recipe[Main.availableRecipe[Main.focusRecipe]].createItem)) recipe = true;
        else if (item.tooltipContext == ItemSlot.Context.ShopItem) recipe = false;
        else return;

        Multipliers multipliers = recipe ? GetCraftMultipliers(Main.recipe[Main.availableRecipe[Main.focusRecipe]]) : GetShopMultipliers(item, null);
        if (multipliers.Mouse == 0) return;
        int perClick = recipe ? Main.recipe[Main.availableRecipe[Main.focusRecipe]].createItem.stack : 1;
        tooltips.Add(new(
            BetterInventory.Instance, "CraftStack",
            Language.GetTextValue($"{Localization.Keys.UI}.CraftStackTooltip",
            Lang.SupportGlyphs(Configs.CraftStack.Value.invertClicks ? "<right>" : "<left>"),
            Language.GetTextValue($"{Localization.Keys.UI}.{(recipe ? "Craft" : "Buy")}"), multipliers.Mouse * perClick))
        );
    }


    private static void HookShiftLeftCustom(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Configs.BetterShiftClick.UniversalShift || !Main.mouseLeft || Main.cursorOverride <= CursorOverrideID.DefaultCursor || context != ItemSlot.Context.ShopItem && context != ItemSlot.Context.CreativeInfinite) orig(inv, context, slot);
        else TwoStepClick(inv, context, slot, (inv, context, slot) => orig(inv, context, slot));
    }
    private static void HookShiftRight(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Configs.BetterShiftClick.ShiftRight || !Main.mouseRight || Main.cursorOverride <= CursorOverrideID.DefaultCursor) orig(inv, context, slot);
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
        if (!inv2[0].IsAir) inv[slot] = ItemHelper.MoveInto(inv[slot], inv2[0], out _);
        if(Main.mouseRight) Recipe.FindRecipes();
    }


    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if (!Configs.BetterShiftClick.UniversalShift || inv[slot].IsAir) return false;
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

        int calcForBuying = 4;
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(Player), nameof(Player.GetItemExpectedPrice)));
        cursor.FindPrev(out _, i => i.MatchLdloca(out calcForBuying));

        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdloc(calcForBuying); // long calcForBuying
        cursor.EmitDelegate((Item[] inv, int slot, bool rightClickIsValid, bool leftClickIsValid, long price) => {
            s_ilShopMultiplier = !Configs.CraftStack.Enabled || !(Configs.CraftStack.Value.invertClicks ? rightClickIsValid : leftClickIsValid) ?
                1 :
                Configs.BetterShiftClick.UniversalShift && CraftCursor.IsCurrent ?
                    GetShopMultipliers(inv[slot], price).Inventory :
                    GetShopMultipliers(inv[slot], price).Mouse;
            return price;
        });
        cursor.EmitStloc(calcForBuying);
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

        int calcForBuying = 4;
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(Player), nameof(Player.GetItemExpectedPrice)));
        cursor.FindPrev(out _, i => i.MatchLdloca(out calcForBuying));

        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(ItemSlot), nameof(ItemSlot.RefreshStackSplitCooldown)));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdloc(calcForBuying);
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
    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting) {
        movedAnItemToAllowCrafting = false;
        if (Configs.CraftStack.Enabled && (Configs.CraftStack.Value.invertClicks ? (Main.mouseRight && !Configs.CraftStack.Value.repeat && !Main.mouseRightRelease) : (Main.mouseLeft && !Configs.CraftStack.Value.repeat && !Main.mouseLeftRelease))) return false;
        if (Configs.BetterShiftClick.UniversalShift && CraftCursor.IsCurrent) return Main.LocalPlayer.ItemSpace(currentRecipe.createItem).CanTakeItem;
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
    private static void ILCraftMultiplier(ILContext il) {
        ILCursor cursor = new(il);
        // if (<cannotCraft>) return;
        // ++ r `*=` <amountOfCrafts>
        cursor.GotoNextLoc(out _, i => i.Previous.SaferMatchCallvirt(Reflection.Item.Clone), 0);
        cursor.GotoPrev(MoveType.After, i => i.MatchLdarg0());
        cursor.EmitDelegate((Recipe r) => {
            s_ilCraftMultiplier = !Configs.CraftStack.Enabled || !(Configs.CraftStack.Value.invertClicks ? Main.mouseRight : Main.mouseLeft) ?
                1 :
                Configs.BetterShiftClick.UniversalShift && CraftCursor.IsCurrent ?
                    GetCraftMultipliers(r).Inventory:
                    GetCraftMultipliers(r).Mouse;
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
        int consumed = 4;
        cursor.FindPrev(out _, i => i.MatchLdloca(out consumed));

        //     ++ <bulkCraftCost>
        cursor.EmitLdloc(consumed);
        cursor.EmitDelegate((int consumed) => Configs.CraftStack.Enabled ? (consumed * s_ilCraftMultiplier) : consumed);
        cursor.EmitStloc(consumed);
        //     <consumeItems>
        // }
        // ...
    }
    private static void ILCraftStackAndPickup(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int crafted, i => i.Previous.SaferMatchCallvirt(Reflection.Item.Clone), 0);


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
        cursor.EmitLdloc(crafted);
        cursor.EmitDelegate((Recipe r, Item crafted) => {
            if (Configs.CraftStack.Enabled && s_ilCraftMultiplier != 1) crafted.stack *= s_ilCraftMultiplier;
            if (Configs.BetterShiftClick.UniversalShift && CraftCursor.IsCurrent) {
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
        if (!sync && Configs.InventoryManagement.FavoriteInBanks) ItemHelper.RunWithHiddenItems(items, () => orig(), i => i.favorited);
        else orig();
    }
    private static void HookLootAll(On_ChestUI.orig_LootAll orig) {
        ChestUI.GetContainerUsageInfo(out bool sync, out Item[] items);
        if (!sync && Configs.InventoryManagement.FavoriteInBanks) ItemHelper.RunWithHiddenItems(items, () => orig(), i => i.favorited);
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
        if (Configs.CraftStack.Value.maxItems.Key != 0 || !SpysInfiniteConsumables.Enabled) return Configs.CraftStack.Value.maxItems.Key.amount;
        return SpysInfiniteConsumables.GetItemInfinity(Main.LocalPlayer, item) == 0 ?
            (int)SpysInfiniteConsumables.GetCountToInfinity(Main.LocalPlayer, item) :
            (int)SpysInfiniteConsumables.GetItemRequirement(item);
    }

    public static int GetMaxBuyAmount(Item item, long price) {
        if (price == 0) return item.maxStack;
        else return (int)Math.Clamp(Main.LocalPlayer.CountCurrency(item.shopSpecialCurrency) / price, 1, item.maxStack - Main.mouseItem.stack);
    }
    public static int GetMaxCraftMultiplier(Recipe recipe) {
        Dictionary<int, int> groupItems = new();
        foreach (int id in recipe.acceptedGroups) {
            RecipeGroup group = RecipeGroup.recipeGroups[id];
            groupItems.Add(group.IconicItemId, group.GetGroupFakeItemId());
        }

        int amount = 0;
        foreach (Item material in recipe.requiredItem) {
            int a = PlayerHelper.OwnedItems.GetValueOrDefault(groupItems.GetValueOrDefault(material.type, material.type), 0) / material.stack;
            if (amount == 0 || a < amount) amount = a;
        }
        return amount;
    }

    public static Multipliers GetCraftMultipliers(Recipe recipe) => s_craftMultipliers.GetOrAdd(recipe.RecipeIndex, _ => {
        int ToMultiplier(int amount) => (Configs.CraftStack.Value.maxItems.Value.above ? (amount + recipe.createItem.stack-1) : amount) / recipe.createItem.stack; 
        int craft = GetMaxCraftMultiplier(recipe);
        if (craft > 0) craft = Math.Max(1, Math.Min(craft, ToMultiplier(GetMaxStackAmount(recipe.createItem))));
        int mouse = ToMultiplier(Utility.GetMouseFreeSpace(recipe.createItem));
        int inventory = ToMultiplier(Utility.GetInventoryFreeSpace(Main.LocalPlayer, recipe.createItem));
        return new(Math.Min(craft, mouse), Math.Min(craft, inventory));
    });

    public static Multipliers GetShopMultipliers(Item item, long? price) => s_shopMultipliers.GetOrAdd(item.type, _ => {
        long p;
        if (price.HasValue) p = price.Value;
        else Main.LocalPlayer.GetItemExpectedPrice(item, out long _, out p);
        int buy = Math.Min(GetMaxBuyAmount(item, p), item.buyOnce ? item.stack : item.maxStack);
        if (buy > 0) buy = Math.Max(1, Math.Min(buy, GetMaxStackAmount(item)));
        int mouse = Utility.GetMouseFreeSpace(item);
        int inventory = Utility.GetInventoryFreeSpace(Main.LocalPlayer, item);
        return new(Math.Min(buy, mouse), Math.Min(buy, inventory));
    });
    
    private readonly static Dictionary<int, Multipliers> s_craftMultipliers = [];
    private readonly static Dictionary<int, Multipliers> s_shopMultipliers = [];

    private static int s_ilCraftMultiplier = 1;
    private static int s_ilShopMultiplier = 1;

    public static ModCursor CraftCursor { get; private set; } = null!;

    public static readonly int[] TransportCursors = [CursorOverrideID.TrashCan, CursorOverrideID.InventoryToChest, CursorOverrideID.ChestToInventory];
}

public record struct Multipliers(int Mouse, int Inventory);