using System;
using System.Collections.Generic;
using BetterInventory.ItemSearch;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class BetterCrafting : ILoadable {

    public static Configs.Crafting Config => Configs.Crafting.Instance;
    
    public void Load(Mod mod) {
        On_Main.DrawInterface_36_Cursor += DrawCustomCursor;
        On_Main.HoverOverCraftingItemButton += OverrideHover;
        On_Main.TryAllowingToCraftRecipe += TryAllowingToCraftRecipe;

        IL_Main.DrawInventory += ILScrolls;
        IL_Main.CraftItem += ILCraftItem;
        IL_Recipe.Create += ILCreateRecipe;
    }


    public void Unload(){}

    private static void ILScrolls(ILContext il) {
        ILCursor cursor = new(il);

        // ----- Recipe fast scroll -----
        // ...
        // if(<showRecipes>){
        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(Main), "DrawGuideCraftText"));
        cursor.GotoNext(i => i.MatchStsfld(typeof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts), nameof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig)));

        //     for (<recipeIndex>) {
        //         ...
        for (int j = 0; j < 2; j++) { // Up and Down

            //     if(<scrool>) {
            //         if(...) SoundEngine.PlaySound(...);
            //         Main.availableRecipeY[num63] += 6.5f;
            cursor.GotoNext(i => i.MatchCall(typeof(SoundEngine), nameof(SoundEngine.PlaySound)));
            cursor.GotoNext(i => i.MatchLdsfld(typeof(Main), nameof(Main.recFastScroll)));

            // ++ <custom scroll>
            cursor.EmitLdloc(124);
            int s = j == 0 ? -1 : 1;
            cursor.EmitDelegate((int r) => {
                if (!Configs.Crafting.Instance.recipeScroll) return;
                Main.availableRecipeY[r] += s * 6.5f;
                float d = Main.availableRecipeY[r] - (r - Main.focusRecipe) * 65;
                if (Main.recFastScroll && Config.recipeScroll.Value.listScroll) {
                    Main.availableRecipeY[r] += 130000f * s;
                    d *= 3;
                }
                Main.availableRecipeY[r] -= s == 1 ? MathF.Max(s * 6.5f, d / 10) : MathF.Min(s * 6.5f, d / 10);
            });
            //         ...
            //     }
        }
        //         ...
        //     }

        // ----- Material wrapping -----
        //     if (Main.numAvailableRecipes > 0) {
        //         for (<focusRecipeMaterialIndex>) {
        //             ...
        //             int num69 = 80 + num68 * 40;
        //             int num70 = 380 + num51;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "HoverOverCraftingItemButton"));
        cursor.GotoNext(MoveType.Before, i => i.MatchStloc(131));

        //             ++ <wrappingX>
        cursor.EmitLdloc(130);
        cursor.EmitDelegate((int x, int i) => {
            if (!Config.tweeks) return x;
            if (!Main.recBigList) return x - 2 * i;
            x -= i * 40;
            if (i >= MaterialsPerLine[0]) i = MaterialsPerLine[0] - MaterialsPerLine[1] + (i - MaterialsPerLine[0]) % MaterialsPerLine[1];
            return x + 38 * i;
        });

        //             ++ <wrappingY>
        cursor.GotoNext(MoveType.Before, i => i.MatchStloc(132));
        cursor.EmitLdloc(130);
        cursor.EmitDelegate((int y, int i) => {
            if (!Config.tweeks || !Main.recBigList) return y;
            i = i < MaterialsPerLine[0] ? 0 : ((i - MaterialsPerLine[0]) / MaterialsPerLine[1] + 1);
            return y + 38 * i;
        });

        //             ...
        //         }
        //     }
        //     ...
        // }

        // ----- recBigList Scroll Fix ----- 
        // Main.hidePlayerCraftingMenu = false;
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(typeof(Main), nameof(Main.hidePlayerCraftingMenu)));
        // if(<recBigListVisible>) {
        //     ...
        for (int i = 0; i < 2; i++) {

            // if (<upVisible> / <downVisible>) {
            //     if(<hover>) {
            //         Main.player[Main.myPlayer].mouseInterface = true;
            cursor.GotoNext(i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));
            cursor.GotoPrev(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));

            //         ++ <autoScroll>
            cursor.EmitDelegate(() => {
                if (!Config.tweeks || !Main.mouseLeft) return;
                if (Main.mouseLeftRelease || _recDelay == 0) {
                    Main.mouseLeftRelease = true;
                    _recDelay = 1;
                } else _recDelay--;
            });

            //         ...
            //     }
            //     Main.spriteBatch.Draw(...);
            cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));
            // }
        }

        // ----- Cursor override for recBigList -----
        //     ...
        //     while (<showingRecipes>) {
        //         ...
        //         if (<mouseHover>) {
        //             Main.player[Main.myPlayer].mouseInterface = true;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), nameof(Main.LockCraftingForThisCraftClickDuration)));
        cursor.GotoPrev(i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));
        ILLabel? noClick = null;
        cursor.GotoPrev(i => i.MatchBrtrue(out noClick));
        cursor.GotoNext(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));

        //             ++ if(<enabled>) goto noClick;
        cursor.EmitLdloc(153);
        cursor.EmitDelegate((int i) => {
            if (Config.focusRecipe) {
                Main.focusRecipe = i;
                Main.recFastScroll = true;
            }
            if (Config.craftingOnRecList) {
                int f = Main.focusRecipe;
                Reflection.Main.HoverOverCraftingItemButton.Invoke(i);
                if (f != Main.focusRecipe) Main.recFastScroll = true;
                Main.craftingHide = false;
                return true;
            }
            else if (BetterGuide.Enabled) return BetterGuide.NoRecipeListClick(i);
            return false;
        });
        cursor.EmitBrtrue(noClick!);
        //             if(<click>) <scrollList>
        //             ...
        //         }
        //         ++ noClick:
        //         ...
        //     }
        // }
        // ...
    }

    private static void OverrideHover(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (Config.craftOverrides && (!BetterGuide.Enabled || BetterGuide.AvailableRecipes.Contains(Main.availableRecipe[recipeIndex])) && recipeIndex == Main.focusRecipe && ItemSlot.ShiftInUse) Main.cursorOverride = CraftCursorID;
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
        if (Config.craftOverrides) {
            movedAnItemToAllowCrafting = false;
            if (Main.mouseLeft && !Main.mouseLeftRelease) return false;
            if (Main.cursorOverride == CraftCursorID && Main.mouseLeft) return Main.LocalPlayer.ItemSpace(currentRecipe.createItem).CanTakeItem;
        }
        return orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting || Config.tweeks, out movedAnItemToAllowCrafting);
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
        cursor.EmitDelegate((Recipe r) => Config.craftOverrides && Main.cursorOverride == CraftCursorID && Main.mouseLeft);
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
            if (Config.craftOverrides && Main.mouseLeft) {
                int amount = GetMaxCraftAmount(r);
                if (Main.cursorOverride == CraftCursorID) craftMultiplier = Math.Min(amount, GetMaxPickupAmount(r.createItem) / r.createItem.stack);
                else craftMultiplier = Math.Min(amount, (r.createItem.maxStack - Main.mouseItem.stack) / r.createItem.stack);
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
            if (!Config.craftOverrides || Main.cursorOverride != CraftCursorID || !Main.mouseLeft) return false;
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

    public static int craftMultiplier = 1;

    public const int CraftCursorID = 22;
    public static Asset<Texture2D> CursorCraft => ModContent.Request<Texture2D>($"BetterInventory/Assets/Cursor_Craft");

    private static int _recDelay = 0;
    public static readonly int[] MaterialsPerLine = new int[] { 6, 4 };

}
