using System;
using System.Collections.Generic;
using System.Linq;
using BetterInventory.Crafting;
using BetterInventory.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.ItemSearch;


public sealed class Guide : ModSystem {
    public static bool Enabled => Configs.ItemSearch.Instance.betterGuide;
    public static Configs.BetterGuide Config => Configs.ItemSearch.Instance.betterGuide.Value;

    public static VisibilityFilters LocalFilters => InventoryManagement.BetterPlayer.LocalPlayer.VisibilityFilters;

    public override void Load() {
        IL_Main.DrawInventory += IlDrawInventory;

        On_Main.DrawGuideCraftText += HookGuideCraftText;
        IL_Main.HoverOverCraftingItemButton += ILOverrideCraftHover;

        On_Recipe.ClearAvailableRecipes += HookClearRecipes;
        IL_Recipe.FindRecipes += ILFindRecipes;
        On_Recipe.CollectGuideRecipes += HookCollectGuideRecipes;
        IL_Recipe.CollectGuideRecipes += ILOverrideGuideRecipes;
        On_Recipe.AddToAvailableRecipes += HookAddToAvailableRecipes;

        On_Player.AdjTiles += HookGuideTileAdj;
        On_ItemSlot.PickItemMovementAction += HookAllowGuideItem;
        On_ItemSlot.OverrideHover_ItemArray_int_int += HookOverrideHover;
        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;

        On_ItemSlot.Draw_SpriteBatch_refItem_int_Vector2_Color += HookHideItemStack;
        On_ItemSlot.DrawItemIcon += HookCustomItemIcom;
        MonoModHooks.Add(typeof(ItemLoader).GetMethod(nameof(ItemLoader.ModifyTooltips)), HookHideTooltip);

        s_inventoryBack4 = TextureAssets.InventoryBack4;
    }

    public override void Unload() {
        s_inventoryBack4 = null!;
        CraftingStationsItems.Clear();
        ConditionItems.Clear();
    }

    public override void PostAddRecipes() {
        SearchItem.HooksBestiaryUI();
        for (int r = 0; r < Recipe.numRecipes; r++) {
            foreach (int tile in Main.recipe[r].requiredTile) CraftingStationsItems[tile] = 0;
        }
        for (int type = 0; type < ItemLoader.ItemCount; type++) {
            Item item = new(type);
            if (CraftingStationsItems.ContainsKey(item.createTile) && CraftingStationsItems[item.createTile] == ItemID.None) CraftingStationsItems[item.createTile] = item.type;
        }

        ConditionItems["Conditions.NearWater"] = ItemID.WaterBucket;
        ConditionItems["Conditions.NearLava"] = ItemID.LavaBucket;
        ConditionItems["Conditions.NearHoney"] = ItemID.HoneyBucket;
        ConditionItems["Conditions.InGraveyard"] = ItemID.Gravestone;
        ConditionItems["Conditions.InSnow"] = ItemID.SnowBlock;
    }


    private static void HookGuideCraftText(On_Main.orig_DrawGuideCraftText orig, int adjY, Color craftingTipColor, out int inventoryX, out int inventoryY) {
        if (!Enabled) {
            orig(adjY, craftingTipColor, out inventoryX, out inventoryY);
            return;
        }
        inventoryX = 73;
        inventoryY = 331 + adjY;

        HandleVisibility(inventoryX, inventoryY);

        Recipe recipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];

        if (s_focusRecipe != (Main.numAvailableRecipes == 0 ? -1 : recipe.RecipeIndex)) UpdateCraftTiles(recipe);

        float minX = inventoryX + TextureAssets.InventoryBack.Width() * Main.inventoryScale * (1 + TileScacingRatio);
        Vector2 delta = new(TextureAssets.InventoryBack.Width() * (TileScale + TileScacingRatio), -TextureAssets.InventoryBack.Height() * (TileScale + TileScacingRatio));
        delta *= Main.inventoryScale;
        Vector2 position = new(minX, inventoryY - delta.Y);
        int slot = 0;
        void MovePosition() {
            if (Crafting.Tweeks.Config.tweeks && ++slot % TilesPerLine == 0) {
                position.X = minX;
                position.Y += delta.Y;
                if (slot == TilesPerLine) MovePosition();
            } else position.X += delta.X;
        }
        Main.inventoryScale *= TileScale;
        for (int i = 0; i < s_craftingTiles.Count; i++) {
            Item tile = s_craftingTiles[i];
            Color inventoryBack = Main.inventoryBack;
            OverrideRecipeTexture(TileTextures, false, tile.createTile == -2 || Main.LocalPlayer.adjTile[tile.createTile]);
            ItemSlot.Draw(Main.spriteBatch, ref tile, ContextID.CraftingMaterial, position);
            TextureAssets.InventoryBack4 = s_inventoryBack4;
            Rectangle hitbox = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
            if (hitbox.Contains(Main.mouseX, Main.mouseY)) {
                Main.LocalPlayer.mouseInterface = true;
                ItemSlot.MouseHover(ref tile, ContextID.CraftingMaterial);
            }
            Main.inventoryBack = inventoryBack;
            MovePosition();
        }

        for (int i = 0; i < s_craftingConditions.Count; i++) {
            (Item item, Condition condition) = s_craftingConditions[i];
            Color inventoryBack = Main.inventoryBack;
            OverrideRecipeTexture(ConditionTextures, false, condition.Predicate());
            ItemSlot.Draw(Main.spriteBatch, ref item, ContextID.CraftingMaterial, position);
            TextureAssets.InventoryBack4 = s_inventoryBack4;
            Rectangle hitbox = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
            if (hitbox.Contains(Main.mouseX, Main.mouseY)) {
                Main.LocalPlayer.mouseInterface = true;
                ForcedToolip = condition.Description;
                ItemSlot.MouseHover(ref item, ContextID.CraftingMaterial);
            }
            Main.inventoryBack = inventoryBack;
            MovePosition();
        }

        Main.inventoryScale /= TileScale;
        DrawGuideTile(inventoryX, inventoryY);
    }
    private static void UpdateCraftTiles(Recipe recipe) {
        s_craftingTiles.Clear();
        s_craftingConditions.Clear();
        if (Main.numAvailableRecipes != 0 && !IsUnknown(Main.availableRecipe[Main.focusRecipe])) {
            s_focusRecipe = recipe.RecipeIndex;
            if (recipe.requiredTile.Count == 0) s_craftingTiles.Add(new(CraftingItem.type) {createTile = -2});
            else {
                for (int i = 0; i < recipe.requiredTile.Count && recipe.requiredTile[i] != -1; i++) {
                    if (CraftingStationsItems.TryGetValue(recipe.requiredTile[i], out int type) && type != ItemID.None) s_craftingTiles.Add(new(type));
                    else s_craftingTiles.Add(new(CraftingItem.type) { createTile = recipe.requiredTile[i] });
                }
            }

            foreach (Condition condition in recipe.Conditions) {
                Item item;
                if (ConditionItems.TryGetValue(condition.Description.Key, out int type)) item = new(type);
                else item = new(CraftingItem.type) { BestiaryNotes = ConditionMark + condition.Description.Key };
                s_craftingConditions.Add((item, condition));
            }
        } else s_focusRecipe = -1;
    }

    private static void DrawGuideTile(int inventoryX, int inventoryY) {
        if (!Config.guideTile) return;
        float x = inventoryX + TextureAssets.InventoryBack.Width() * Main.inventoryScale * (1 + TileScacingRatio);
        float y = inventoryY;
        Main.inventoryScale *= TileScale;
        Rectangle hitbox = new((int)x, (int)y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
        Item[] items = new Item[] { Main.guideItem, guideTile };
        if (!_ilVisibilityHover && hitbox.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface) {
            Main.player[Main.myPlayer].mouseInterface = true;
            Main.craftingHide = true;
            ItemSlot.OverrideHover(items, 7, 1);
            ItemSlot.LeftClick(items, 7, 1);
            if (Main.mouseLeftRelease && Main.mouseLeft) {
                Recipe.FindRecipes(false);
            }
            ItemSlot.RightClick(items, 7, 1);
            ItemSlot.MouseHover(items, 7, 1);
        }
        ItemSlot.Draw(Main.spriteBatch, items, 7, 1, hitbox.TopLeft());
        (Main.guideItem, guideTile) = (items[0], items[1]);
        Main.inventoryScale /= TileScale;
    }


    private static void IlDrawInventory(ILContext il) {
        ILCursor cursor = new(il);

        // if(Main.InReforgeMenu){
        //     ...
        // } else if(Main.InGuideCraftMenu) {
        //     if(<closeGuideUI>) ...
        //     else {
        ILLabel? endGuide = null;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "DrawGuideCraftText"));
        cursor.GotoPrev(MoveType.After, i => i.MatchBr(out endGuide));

        //         ++ guide:
        ILLabel guide = cursor.DefineLabel();
        cursor.MarkLabel(guide);

        //         Main.DrawGuideCraftText(...);
        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(Main), "DrawGuideCraftText"));

        // ----- Visibility Filters -----
        ILLabel? noHover = null;
        cursor.FindNext(out _, i => i.MatchBlt(out noHover));
        //         ++ if(!<visibilityHover>) {
        cursor.EmitDelegate(() => _ilVisibilityHover);
        cursor.EmitBrtrue(noHover!);

        //             <handle guide item slot>
        //         ++ }
        //         ItemSlot.Draw(Main.spriteBatch, ref Main.guideItem, ...);
        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //         ++ <drawVisibility>
        cursor.EmitDelegate(DrawVisibility);

        cursor.GotoLabel(endGuide!, MoveType.Before);


        // ----- Force GuideItem display -----
        //         ++ if(<alternateGuideDraw>) goto recipe;
        ILLabel recipe = cursor.DefineLabel();
        cursor.EmitDelegate(() => SearchItem.Config.searchRecipes && !Main.InGuideCraftMenu);
        cursor.EmitBrtrue(recipe);
        //     }
        // }

        // ...
        // if(<showRecipes>){
        cursor.GotoNext(i => i.MatchStsfld(typeof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts), nameof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig)));
        cursor.GotoPrev(MoveType.AfterLabel);

        //     ++ if(<alternateGuideDraw>) goto guide;
        cursor.EmitDelegate(() => SearchItem.Config.searchRecipes && !Main.InGuideCraftMenu);
        cursor.EmitBrtrue(guide);

        //     ++ recipe:
        cursor.MarkLabel(recipe);


        // ----- Recipes createItem background -----
        //     for (<recipeIndex>) {
        //         ...
        //         if(<visible>) {
        //             ...
        //             if (Main.numAvailableRecipes > 0) {
        //                 ...
        //                 Main.inventoryBack = ...;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "HoverOverCraftingItemButton"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //                 ++ <overrideBackground>
        cursor.EmitLdloc(124);
        cursor.EmitDelegate((int i) => {
            if (!Enabled) return;
            OverrideRecipeTexture(GetFavoriteState(Main.availableRecipe[i]), ItemSlot.DrawGoldBGForCraftingMaterial, s_availableRecipes.Contains(Main.availableRecipe[i]));
            ItemSlot.DrawGoldBGForCraftingMaterial = false;
            if (IsUnknown(Main.availableRecipe[i])) s_hideNextItem = true;
        });

        //                 ItemSlot.Draw(...);
        cursor.GotoNext(MoveType.After, i => true);

        //                 ++ <restoreBackground>
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = s_inventoryBack4; });
        //             }
        //         }
        //     }

        // ----- Unknown recipes no materials -----
        //     if (++<known> && Main.numAvailableRecipes > 0) {
        cursor.GotoNext(MoveType.After, i => i.MatchLdsfld(Reflection.Main.numAvailableRecipes));
        cursor.EmitDelegate((int num) => num == 0 || IsUnknown(Main.availableRecipe[Main.focusRecipe]) ? 0 : num);

        // ----- Recipe requiredItems background -----
        //         for (<focusRecipeMaterialIndex>) {
        //             ...
        //             Item tempItem = ...;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "SetRecipeMaterialDisplayName"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //             ++ <overrideBackground>
        cursor.EmitLdloc(130);
        cursor.EmitDelegate((int matI) => {
            if (!Enabled) return;
            bool canCraft = s_availableRecipes.Contains(Main.availableRecipe[Main.focusRecipe]);
            if (!canCraft) {
                Item material = Main.recipe[Main.availableRecipe[Main.focusRecipe]].requiredItem[matI];
                canCraft = Utility.OwnedItems.GetValueOrDefault(material.type, 0) >= material.stack;
            }
            FavoriteState state = GetFavoriteState(Main.availableRecipe[Main.focusRecipe]);
            if (state == FavoriteState.Favorited) state = FavoriteState.Default;
            OverrideRecipeTexture(state, false, canCraft);
        });

        //             ItemSlot.Draw(...);
        cursor.GotoNext(MoveType.After, i => true);

        //             ++ <restoreBackground>
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = s_inventoryBack4; });
        //         }
        //     }
        //     ...
        // }

        // ----- Recipe big list background -----
        // Main.hidePlayerCraftingMenu = false;
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(typeof(Main), nameof(Main.hidePlayerCraftingMenu)));

        // if(<recBigListVisible>) {
        //     ...
        //     while (<showingRecipes>) {
        //         ...
        //         if (Main.numAvailableRecipes > 0) {
        //             ...
        //             Main.inventoryBack = ...;
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //             ++ <overrideBackground>
        cursor.EmitLdloc(153);
        cursor.EmitDelegate((int i) => {
            if (Main.focusRecipe == i && Crafting.Tweeks.Config.craftOnList.Parent && !Crafting.Tweeks.Config.craftOnList.Value.focusRecipe) ItemSlot.DrawGoldBGForCraftingMaterial = true;
            if (!Enabled) return;
            OverrideRecipeTexture(GetFavoriteState(Main.availableRecipe[i]), ItemSlot.DrawGoldBGForCraftingMaterial, s_availableRecipes.Contains(Main.availableRecipe[i]));
            ItemSlot.DrawGoldBGForCraftingMaterial = false;
            if (IsUnknown(Main.availableRecipe[i])) s_hideNextItem = true;

        });

        //             ItemSlot.Draw(...);
        cursor.GotoNext(MoveType.After, i => true);

        //             ++ <restoreBackground>
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = s_inventoryBack4; });
        //         }
        //         ...
        //     }
        // }
    }
    public static void HandleVisibility(int x, int y) {
        _ilVisibilityHover = false;
        if (!Enabled || !Config.craftInMenu) return;
        x += (int)((TextureAssets.InventoryBack.Width() - 2) * Main.inventoryScale);
        y += (int)(4 * Main.inventoryScale);
        Vector2 size = InventoryTickBorder.Size() * Main.inventoryScale;
        s_hitBox = new(x - (int)(size.X / 2f), y - (int)(size.Y / 2f), (int)size.X, (int)size.Y);

        if (!(s_hover = s_hitBox.Contains(Main.mouseX, Main.mouseY)) || PlayerInput.IgnoreMouseInterface) return;

        Main.player[Main.myPlayer].mouseInterface = true;
        if (Main.mouseLeft && Main.mouseLeftRelease) {
            LocalFilters.ShowAllRecipes = !LocalFilters.ShowAllRecipes;
            SoundEngine.PlaySound(SoundID.MenuTick);
            Recipe.FindRecipes();
        }
        _ilVisibilityHover = true;
    }
    public static void DrawVisibility() {
        if (!Enabled || !Config.craftInMenu) return;
        VisibilityFilters filters = LocalFilters;
        Asset<Texture2D> tick = filters.ShowAllRecipes ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff;
        Color color = Color.White * 0.7f;
        Main.spriteBatch.Draw(tick.Value, s_hitBox.Center(), null, color, 0f, tick.Value.Size() / 2, 1, 0, 0f);
        if (s_hover) {
            string key = filters.ShowAllRecipes ? "Mods.BetterInventory.UI.ShowAll" : "Mods.BetterInventory.UI.ShowAvailable";
            Main.instance.MouseText(Language.GetTextValue(key));
            Main.spriteBatch.Draw(InventoryTickBorder.Value, s_hitBox.Center(), null, color, 0f, InventoryTickBorder.Value.Size() / 2, 1, 0, 0f);
        }
    }


    public static Item? GetGuideMaterials() =>  Enabled && !SearchItem.Config.searchRecipes ? Main.guideItem : null;

    private static void HookClearRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        s_favRecipes.Clear();
        s_otherRecipes.Clear();
        s_blackRecipes.Clear();
        s_unknownRecipes.Clear();
        orig();
    }
    private static void ILFindRecipes(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // Recipe.ClearAvailableRecipes()
        cursor.GotoNext(MoveType.After, i => i.MatchCall(Reflection.Recipe.ClearAvailableRecipes));

        // ++ if (Enabled) goto skipGuide
        ILLabel skipGuide = cursor.DefineLabel();
        cursor.EmitDelegate(() => Enabled);
        cursor.EmitBrtrue(skipGuide);


        // if(<guideItem>) {
        //     <guideRecipes>
        // }
        // ++ skipGuide:
        // Player localPlayer = Main.LocalPlayer;
        // Recipe.CollectItemsToCraftWithFrom(localPlayer);
        cursor.GotoNext(MoveType.After, i => i.MatchCall(Reflection.Recipe.CollectItemsToCraftWithFrom));

        // ++<setup>
        cursor.EmitDelegate(() => {
            if (!Enabled) return;
            s_availableRecipes.Clear();
            s_collectingAvailable = true;
        });

        cursor.GotoPrev(MoveType.After, i => i.MatchRet());
        cursor.MarkLabel(skipGuide);

        // <availableRecipes>
        cursor.GotoNext(i => i.MatchCall(Reflection.Recipe.TryRefocusingRecipe));

        // ++<process>
        cursor.EmitDelegate(() => {
            if (!Enabled) return;
            s_collectingAvailable = false;
            Reflection.Recipe.CollectGuideRecipes.Invoke();
        });
    }
    private static void HookCollectGuideRecipes(On_Recipe.orig_CollectGuideRecipes orig) {
        if (!Enabled) {
            orig();
            return;
        }
        if (!Main.mouseItem.IsAir) LocalFilters.AddOwnedItems(Main.mouseItem);
        foreach (Item item in Main.LocalPlayer.inventory) if (!item.IsAir) LocalFilters.AddOwnedItems(item);
        if(Main.LocalPlayer.InChest(out Item[]? chest)){
            foreach (Item item in chest) if (!item.IsAir) LocalFilters.AddOwnedItems(item);
        }

        s_collectingGuide = true;
        orig();
        s_collectingGuide = false;
        foreach (int i in s_favRecipes) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
        foreach (int i in s_otherRecipes) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
        foreach (int i in s_blackRecipes) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
        foreach (int i in s_unknownRecipes) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
    }
    private static void ILOverrideGuideRecipes(ILContext il) {
        ILCursor cursor = new(il);
        ILLabel? endLoop = null;

        // for (<recipeIndex>) {
        //     ...
        //     if (recipe.Disabled) continue;
        cursor.GotoNext(i => i.MatchCallvirt(Reflection.Recipe.Disabled.GetMethod!));
        cursor.GotoNext(i => i.MatchBrtrue(out endLoop));
        cursor.GotoNext(MoveType.AfterLabel);

        //     ++ if(<extraRecipe>) {
        //     ++     <addRecipe>
        //     ++     continue;
        //     ++ }
        cursor.EmitLdloc1();
        cursor.EmitDelegate<Func<int, bool>>(recipeIndex => {
            if (!Enabled) return false;

            if (Configs.ItemSearch.Instance.unknownDisplay != Configs.ItemSearch.UnknownDisplay.Known && !LocalFilters.IsKnownRecipe(Main.recipe[recipeIndex])) {
                if (Configs.ItemSearch.Instance.unknownDisplay == Configs.ItemSearch.UnknownDisplay.Hidden || !LocalFilters.ShowAllRecipes) return true;
                _ilRecAdder = r => s_unknownRecipes.Add(r);
            }
            else {
                FavoriteState fav = LocalFilters.GetFavoriteState(recipeIndex);
                if (fav == FavoriteState.Favorited) _ilRecAdder = r => s_favRecipes.Add(r);
                else if (fav == FavoriteState.Default) {
                    if (!LocalFilters.ShowAllRecipes && !s_availableRecipes.Contains(recipeIndex)) return true;
                    _ilRecAdder = r => s_otherRecipes.Add(r);
                }
                else {
                    if (!LocalFilters.ShowAllRecipes) return true;
                    _ilRecAdder = r => s_blackRecipes.Add(r);
                }
            }

            Recipe recipe = Main.recipe[recipeIndex];
            if (Config.guideTile && !guideTile.IsAir) {
                switch (GetPlaceholderType(guideTile)) {
                case PlaceholderType.ByHand:
                    if (recipe.requiredTile.Count != 0) return true;
                    break;
                case PlaceholderType.Tile:
                    if (!recipe.requiredTile.Contains(guideTile.createTile)) return true;
                    break;
                case PlaceholderType.Condition:
                    if (!recipe.Conditions.Exists(c => c.Description.Key == guideTile.BestiaryNotes[ConditionMark.Length..])) return true;
                    break;
                default: // Real Item
                    if (guideTile.createTile != -1) { // Tile
                        if (!recipe.requiredTile.Contains(guideTile.createTile)) return true;
                    } else { // Condition
                        if (!recipe.Conditions.Exists(c => ConditionItems.TryGetValue(c.Description.Key, out int type) && type == guideTile.type)) return true;
                    }
                    break;
                }
            }
            if (Main.guideItem.type == ItemID.None || Main.recipe[recipeIndex].HasResult(Main.guideItem.type)) {
                Reflection.Recipe.AddToAvailableRecipes.Invoke(recipeIndex);
                return true;
            }
            return false;
        });
        cursor.EmitBrtrue(endLoop!);
        //     for(<material>) {
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdsfld(Reflection.Main.availableRecipe));

        //         if(<recipeOk> ++[&& !custom]) {
        cursor.EmitLdloc1();
        cursor.EmitDelegate((int r) => {
            if (!Enabled) return false;
            Reflection.Recipe.AddToAvailableRecipes.Invoke(r);
            return true;
        });
        cursor.EmitBrtrue(endLoop!);

        //             Main.availableRecipe[Main.numAvailableRecipes] = i;
        //             Main.numAvailableRecipes++;
        //             break;
        //         }
        //     }
        // }
    }
    private static void HookAddToAvailableRecipes(On_Recipe.orig_AddToAvailableRecipes orig, int recipeIndex) {
        if (s_collectingAvailable) s_availableRecipes.Add(recipeIndex);
        else if (s_collectingGuide) _ilRecAdder.Invoke(recipeIndex);
        else if(!RecipeFiltering.Enabled || RecipeFiltering.FitsFilters(recipeIndex)) orig(recipeIndex);
    }

    private static void ILOverrideCraftHover(ILContext context) {
        ILCursor cursor = new(context);
        // ...
        // <flags>
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.focusRecipe));

        // ++ if(<favorite>) goto skip;
        cursor.EmitLdarg0();
        cursor.EmitDelegate((int recipeIndex) => {
            if (!Enabled) return false;
            if (IsUnknown(Main.availableRecipe[recipeIndex])) {
                ForcedToolip = Language.GetText("Mods.BetterInventory.UI.Unknown");
                return false;
            }
            if (!Config.favoriteRecipes) return false;
            bool click = Main.mouseLeft && Main.mouseLeftRelease;
            if (Main.keyState.IsKeyDown(Main.FavoriteKey)) {
                Main.cursorOverride = CursorOverrideID.FavoriteStar;
                if (click) {
                    FavoriteState state = GetFavoriteState(Main.availableRecipe[recipeIndex]);
                    LocalFilters.SetFavoriteState(Main.availableRecipe[recipeIndex], state == FavoriteState.Default ? FavoriteState.Favorited : FavoriteState.Default);
                    Recipe.FindRecipes(true);
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    return true;
                }
            } else if (ItemSlot.ControlInUse) {
                FavoriteState state = GetFavoriteState(Main.availableRecipe[recipeIndex]);
                if (state == FavoriteState.Favorited) return click;
                Main.cursorOverride = CursorOverrideID.TrashCan;
                if (click) {
                    LocalFilters.SetFavoriteState(Main.availableRecipe[recipeIndex], state == FavoriteState.Default ? FavoriteState.Blacklisted : FavoriteState.Default);
                    Recipe.FindRecipes(true);
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    return true;
                }
                return false;
            }
            return false;
        });
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrtrue(skip);

        // if (Main.focusRecipe == recipeIndex && ++[Main.guideItem.IsAir || <allowCraft>]) {
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.guideItem));
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(Reflection.Item.IsAir.GetMethod!));
        cursor.EmitDelegate((bool isAir) => isAir || (Enabled && Config.craftInMenu));

        //     bool flag3 = Main.LocalPlayer.ItemTimeIsZero && Main.LocalPlayer.itemAnimation == 0 && !Main.player[Main.myPlayer].HasLockedInventory() && ++[!Main._preventCraftingBecauseClickWasUsedToChangeFocusedRecipe || <cannotCraft>]
        cursor.GotoNext(MoveType.After, i => i.MatchLdsfld(Reflection.Main._preventCraftingBecauseClickWasUsedToChangeFocusedRecipe));
        cursor.EmitLdarg0();
        cursor.EmitDelegate((bool prevent, int recipe) => {
            if (prevent || Enabled && !s_availableRecipes.Contains(Main.availableRecipe[recipe])) return true;
            InventoryManagement.ClickOverride.OverrideCraftHover(recipe);
            return false;
        });
        //     ...
        // }

        cursor.GotoNext(i => i.MatchStsfld(Reflection.Main.craftingHide));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdcI4(1));
        cursor.MarkLabel(skip);
    }

    private static void HookGuideTileAdj(On_Player.orig_AdjTiles orig, Player self) {
        orig(self);
        if (SearchItem.Config.searchRecipes || guideTile.createTile < TileID.Dirt) return;
        self.adjTile[guideTile.createTile] = true;
        Recipe.FindRecipes();
    }
    private static int HookAllowGuideItem(On_ItemSlot.orig_PickItemMovementAction orig, Item[] inv, int context, int slot, Item checkItem) {
        if (!Enabled || !Config.anyItem || context != ContextID.GuideItem) return orig(inv, context, slot, checkItem);
        if (slot == 0 && GetPlaceholderType(Main.mouseItem) == PlaceholderType.None) return 0;
        if (slot == 1 && IsCraftingTileItem(Main.mouseItem)) return 0;
        return -1;
    }
    private static void HookOverrideHover(On_ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (SearchItem.OverrideHover(inv, context, slot)) return;
        if (!inv[slot].favorited && (Enabled ? InventoryContexts.Contains(context) : context == ContextID.InventoryItem) && ItemSlot.ShiftInUse && !ItemSlot.ShiftForcedOn && Main.InGuideCraftMenu && !inv[slot].IsAir && ItemSlot.PickItemMovementAction(inv, ContextID.GuideItem, 0, inv[slot]) == 0) Main.cursorOverride = CursorOverrideID.InventoryToChest;
        else orig(inv, context, slot);
    }
    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if (context == ContextID.GuideItem && !Main.mouseItem.IsAir && ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == 0) Main.recBigList = true;
        if (Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.InventoryToChest) {
            (Item mouse, Main.mouseItem, inv[slot]) = (Main.mouseItem, inv[slot], new());
            (int cursor, Main.cursorOverride) = (Main.cursorOverride, 0);

            Item[] items = new Item[] { Main.guideItem, guideTile };
            ItemSlot.LeftClick(items, ContextID.GuideItem, Config.guideTile && IsCraftingTileItem(Main.mouseItem) ? 1 : 0);
            (Main.guideItem, guideTile) = (items[0], items[1]);

            if (Enabled) Main.LocalPlayer.GetDropItem(ref Main.mouseItem);
            else inv[slot] = Main.mouseItem;
            Main.mouseItem = mouse;
            Main.cursorOverride = cursor;
            return true;
        }
        if (context == ContextID.GuideItem && slot == 1) {
            if (GetPlaceholderType(inv[slot]) != PlaceholderType.None && Main.mouseItem.IsAir) {
                inv[slot].TurnToAir();
                guideTile.TurnToAir();
                Recipe.FindRecipes();
                SoundEngine.PlaySound(SoundID.Grab);
            } else if (inv[slot].IsAir && Main.mouseItem.IsAir) {
                inv[slot] = new(CraftingItem.type) { createTile = -2 };
                guideTile = inv[slot];
                Recipe.FindRecipes();
                SoundEngine.PlaySound(SoundID.Grab);
                return true;
            }
            if(ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) != -1) guideTile = Main.mouseItem;
        }
        if (SearchItem.OverrideLeftClick(inv, context, slot)) return true;
        return orig(inv, context, slot);
    }

    private static void HookHideItemStack(On_ItemSlot.orig_Draw_SpriteBatch_refItem_int_Vector2_Color orig, SpriteBatch spriteBatch, ref Item inv, int context, Vector2 position, Color lightColor) {
        if (s_hideNextItem) {
            Item item = CraftingItem;
            orig(spriteBatch, ref item, context, position, lightColor);
            s_hideNextItem = false;
        } else {
            orig(spriteBatch, ref inv, context, position, lightColor);
        }
    }
    private static float HookCustomItemIcom(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
        if (s_hideNextItem) {
            return DrawTexture(spriteBatch, UnknownTexture.Value, Color.White, screenPositionForItemCenter, ref scale, sizeLimit, environmentColor);
        }
        switch (GetPlaceholderType(item)) {
        case PlaceholderType.ByHand:
            Main.instance.LoadItem(ItemID.BoneGlove);
            return DrawTexture(spriteBatch, TextureAssets.Item[ItemID.BoneGlove].Value, Color.White, screenPositionForItemCenter, ref scale, sizeLimit, environmentColor);
        case PlaceholderType.Tile:
            Utility.DrawTileFrame(spriteBatch, item.createTile, screenPositionForItemCenter, new Vector2(0.5f, 0.5f), scale);
            return scale;
        }
        return orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
    }

    private static float DrawTexture(SpriteBatch spriteBatch, Texture2D value, Color alpha, Vector2 screenPositionForItemCenter, ref float scale, float sizeLimit, Color environmentColor) {
        Rectangle frame = value.Frame(1, 1, 0, 0, 0, 0);
        if (frame.Width > sizeLimit || frame.Height > sizeLimit) scale *= (frame.Width <= frame.Height) ? (sizeLimit / frame.Height) : (sizeLimit / frame.Width);
        spriteBatch.Draw(value, screenPositionForItemCenter, new Rectangle?(frame), alpha, 0f, frame.Size() / 2f, scale, 0, 0f);
        return scale;
    }

    public delegate List<TooltipLine> ModifyTooltipsFn(Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex);
    private static List<TooltipLine> HookHideTooltip(ModifyTooltipsFn orig, Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex) {
        string? name = GetPlaceholderType(item) switch {
            PlaceholderType.ByHand => Language.GetTextValue("Mods.BetterInventory.UI.ByHand"),
            PlaceholderType.Tile => Lang.GetMapObjectName(MapHelper.TileToLookup(item.createTile, item.placeStyle)),
            PlaceholderType.Condition => Language.GetTextValue(item.BestiaryNotes[ConditionMark.Length..]),
            _ => ForcedToolip?.Value,
        };
        if (name is null) return orig.Invoke(item, ref numTooltips, names, ref text, ref modifier, ref badModifier, ref oneDropLogo, out overrideColor, prefixlineIndex);
        List<TooltipLine> tooltips = new() { new(BetterInventory.Instance, names[0], name) };
        numTooltips = 1;
        text = new string[] { tooltips[0].Text };
        modifier = new bool[] { tooltips[0].IsModifier };
        badModifier = new bool[] { tooltips[0].IsModifierBad };
        oneDropLogo = -1;
        overrideColor = new Color?[] { null };
        return tooltips;
    }

    internal static void RecipeListHover(int recipe) {
        if (!Enabled || !IsUnknown(Main.availableRecipe[recipe])) return;
        ForcedToolip = Language.GetText("Mods.BetterInventory.UI.Unknown");
    }


    public static FavoriteState GetFavoriteState(int recipe) => Config.favoriteRecipes ? LocalFilters.GetFavoriteState(recipe) : FavoriteState.Default;
    public static bool IsUnknown(int recipe) => s_unknownRecipes.Contains(recipe);

    public static void OverrideRecipeTexture(FavoriteState state, bool selected, bool available) => OverrideRecipeTexture(state switch {
        FavoriteState.Default => DefaultTextures,
        FavoriteState.Favorited => FavoriteTextures,
        FavoriteState.Blacklisted or _ => BlacklistedTextures,
    }, selected, available);
    public static void OverrideRecipeTexture(Asset<Texture2D>[] textures, bool selected, bool available) {
        TextureAssets.InventoryBack4 = textures[selected ? 1 : 0];
        if (!available) Main.inventoryBack.ApplyRGB(0.5f);
    }

    public static void ToggleRecipeList(bool? enabled = null) {
        if (Main.playerInventory && Main.recBigList && !Main.CreativeMenu.Enabled) {
            if (enabled == true) return;
            Main.recBigList = false;
        } else {
            if (enabled == false) return;
            if (!Main.playerInventory) {
                Main.LocalPlayer.ToggleInv();
                if (!Main.playerInventory) Main.LocalPlayer.ToggleInv();
            } else {
                Main.CreativeMenu.CloseMenu();
                Main.LocalPlayer.tileEntityAnchor.Clear();
                if (!Main.InGuideCraftMenu) Main.LocalPlayer.SetTalkNPC(-1);
            }
            Main.recBigList = Main.numAvailableRecipes > 0;
        }
    }

    public static PlaceholderType GetPlaceholderType(Item item) {
        if (item.type != CraftingItem.type || item.stack != 1) return PlaceholderType.None;
        if (item.createTile == -2) return PlaceholderType.ByHand;
        if (item.createTile != -1) return PlaceholderType.Tile;
        if (item.BestiaryNotes?.StartsWith(ConditionMark) == true) return PlaceholderType.Condition;
        return PlaceholderType.None;
    }

    public static bool IsCraftingTileItem(Item item) => item.IsAir || CraftingStationsItems.ContainsKey(item.createTile) || ConditionItems.ContainsValue(item.type) || GetPlaceholderType(item) != PlaceholderType.None;

    internal static void dropItemCheck(Player self) {
        if (Main.InGuideCraftMenu || guideTile.IsAir) return;
        if (GetPlaceholderType(guideTile) != PlaceholderType.None) guideTile.TurnToAir();
        else self.GetDropItem(ref guideTile);
    }

    public static bool AreSame(Item item, Item other) {
        PlaceholderType a = GetPlaceholderType(item);
        PlaceholderType b = GetPlaceholderType(other);
        if (a != b) return false;
        return a switch {
            PlaceholderType.ByHand => true,
            PlaceholderType.Tile => item.createTile == other.createTile,
            PlaceholderType.Condition => item.BestiaryNotes == other.BestiaryNotes,
            PlaceholderType.None or _ => item.type == other.type,
        };
    }

    public static readonly Item CraftingItem = new(ItemID.Lens);
    public const string ConditionMark = "@BI:";

    public static readonly Dictionary<int, int> CraftingStationsItems = new(); // tile -> item
    public static readonly Dictionary<string, int> ConditionItems = new(); // descrition -> id

    public static Item guideTile = new();

    public static readonly Asset<Texture2D>[] DefaultTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack4, TextureAssets.InventoryBack14 };
    public static readonly Asset<Texture2D>[] FavoriteTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack10, TextureAssets.InventoryBack17 };
    public static readonly Asset<Texture2D>[] BlacklistedTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack5, TextureAssets.InventoryBack11 };
    public static readonly Asset<Texture2D>[] TileTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack3, TextureAssets.InventoryBack6 };
    public static readonly Asset<Texture2D>[] ConditionTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack12, TextureAssets.InventoryBack8 };

    public static Asset<Texture2D> InventoryTickBorder => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Border");
    public static Asset<Texture2D> UnknownTexture => ModContent.Request<Texture2D>($"BetterInventory/Assets/Unknown_Item");

    private static Asset<Texture2D> s_inventoryBack4 = null!;

    private static Action<int> _ilRecAdder = null!;
    private static readonly List<int> s_favRecipes = new();
    private static readonly List<int> s_blackRecipes = new();
    private static readonly List<int> s_otherRecipes = new();
    private static readonly RangeSet s_unknownRecipes = new();
    private static readonly RangeSet s_availableRecipes = new();

    private static bool s_collectingAvailable, s_collectingGuide;

    private static bool s_hover;
    private static Rectangle s_hitBox;

    private static int s_focusRecipe = -1;
    private static readonly List<Item> s_craftingTiles = new();
    private static readonly List<(Item item, Condition condition)> s_craftingConditions = new();

    private static bool s_hideNextItem;
    public static LocalizedText? ForcedToolip;

    public const int TilesPerLine = 7;
    public const float TileScale = 0.46f;
    public const float TileScacingRatio = 0.08f;

    public static readonly int[] InventoryContexts = new int[] { ContextID.InventoryItem, ContextID.InventoryAmmo, ContextID.InventoryCoin };

    private static bool _ilVisibilityHover;
}

public enum PlaceholderType { None, ByHand, Tile, Condition}