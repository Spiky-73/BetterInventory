using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class Actions : ILoadable {

    public static bool Enabled => Configs.ClientConfig.Instance.craftOverride;

    public void Load(Mod mod) {
        On_Main.DrawInterface_36_Cursor += DrawCustomCursor;
        On_Main.HoverOverCraftingItemButton += OverrideHover;
        On_Main.TryAllowingToCraftRecipe += TryAllowingToCraftRecipe;

        IL_Main.CraftItem += ILCraftItem;
        IL_Recipe.Create += ILCreateRecipe;
    }
    public void Unload(){}

    private static void OverrideHover(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (Enabled && (!ItemSearch.BetterGuide.Enabled || ItemSearch.BetterGuide.AvailableRecipes.Contains(Main.availableRecipe[recipeIndex])) && recipeIndex == Main.focusRecipe && ItemSlot.ShiftInUse) Main.cursorOverride = CraftCursorID;
        orig(recipeIndex);
    }
    private static void DrawCustomCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        if (Main.cursorOverride == CraftCursorID) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(0, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
            Main.spriteBatch.Draw(CursorCraft.Value, new Vector2(Main.mouseX, Main.mouseY), null, Color.White, 0, default, Main.cursorScale, 0, 0f);
        } else orig();
    }

    private static bool TryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting) {
        if (Enabled) {
            movedAnItemToAllowCrafting = false;
            if (Main.mouseLeft && !Main.mouseLeftRelease) return false;
            if (Main.cursorOverride == CraftCursorID && Main.mouseLeft) return Main.LocalPlayer.ItemSpace(currentRecipe.createItem).CanTakeItem;
        }
        return orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting || Enabled, out movedAnItemToAllowCrafting);
    }

    private static void ILCraftItem(ILContext il) {
        ILCursor cursor = new(il);

        // ++ if(<Shift+LeftClick>){
        // ++     if(!<canTakeItem>) return;
        // ++     goto skipCheck;
        // ++ }
        // ++ vanillaCheck:
        ILLabel skipCheck = cursor.DefineLabel();
        ILLabel vanillaCheck = cursor.DefineLabel();
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => Enabled && Main.cursorOverride == CraftCursorID && Main.mouseLeft);
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
            craftMultiplier = 1;
            if (Enabled && Main.mouseLeft) {
                int amount = GetMaxCraftAmount(r);
                if (Main.cursorOverride == CraftCursorID) craftMultiplier = System.Math.Min(amount, GetMaxPickupAmount(r.createItem) / r.createItem.stack);
                else craftMultiplier = System.Math.Min(amount, (r.createItem.maxStack - Main.mouseItem.stack) / r.createItem.stack);
            } else craftMultiplier = 1;
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
            crafted.stack *= craftMultiplier;
            if (!Enabled || Main.cursorOverride != CraftCursorID || !Main.mouseLeft) return false;
            craftMultiplier = 1;
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
            stack *= craftMultiplier;
            craftMultiplier = 1;
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
        cursor.EmitDelegate((ref int consumed) => { consumed *= craftMultiplier; });
        //     <consumeItems>
        // }
        // ...
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
        return System.Math.Min(max, free);
    }

    public static int GetFreeSpace(Item[] inv, Item item, params int[] ignored) {
        int free = 0;
        for (int i = 0; i < inv.Length; i++) {
            if (System.Array.IndexOf(ignored, i) != -1) continue;
            Item slot = inv[i];
            if (slot.IsAir) free += item.maxStack;
            if (slot.type == item.type) free += item.maxStack - slot.stack;
        }
        return free;
    }

    public static int craftMultiplier = 1;

    public const int CraftCursorID = 22;
    public static Asset<Texture2D> CursorCraft => ModContent.Request<Texture2D>($"BetterInventory/Assets/Cursor_Craft");
}
