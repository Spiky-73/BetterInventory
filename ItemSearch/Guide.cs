using System;
using System.Collections.Generic;
using BetterInventory.Crafting;
using BetterInventory.InventoryManagement;
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

    public static VisibilityFilters LocalFilters => BetterPlayer.LocalPlayer.VisibilityFilters;

    public override void Load() {
        IL_Main.DrawInventory += IlDrawInventory;

        On_Main.DrawGuideCraftText += HookGuideCraftText;
        IL_Main.HoverOverCraftingItemButton += ILOverrideCraftHover;

        On_Recipe.ClearAvailableRecipes += HookClearRecipes;
        On_Recipe.FindRecipes += HookFindRecipes;
        IL_Recipe.FindRecipes += ILFindRecipes;
        On_Recipe.CollectGuideRecipes += HookCollectGuideRecipes;
        IL_Recipe.CollectGuideRecipes += ILOverrideGuideRecipes;
        On_Recipe.AddToAvailableRecipes += HookAddToAvailableRecipes;

        On_Player.AdjTiles += HookGuideTileAdj;
        On_ItemSlot.PickItemMovementAction += HookAllowGuideItem;
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
        guideTile = new();
        s_dispGuide = new();
        s_dispTile = new();
    }

    public override void ClearWorld() {
        SmartPickup.ClearMarks();
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

        if (s_textRecipe != (Main.numAvailableRecipes == 0 ? -1 : recipe.RecipeIndex)) UpdateCraftTiles(recipe);

        float minX = inventoryX + TextureAssets.InventoryBack.Width() * Main.inventoryScale * (1 + TileScacingRatio);
        Vector2 delta = new(TextureAssets.InventoryBack.Width() * (TileScale + TileScacingRatio), -TextureAssets.InventoryBack.Height() * (TileScale + TileScacingRatio));
        delta *= Main.inventoryScale;
        Vector2 position = new(minX, inventoryY - delta.Y);
        int slot = 0;
        void MovePosition() {
            if (Tweeks.Config.tweeks && ++slot % TilesPerLine == 0) {
                position.X = minX;
                position.Y += delta.Y;
                if (slot == TilesPerLine) MovePosition();
            } else position.X += delta.X;
        }
        Main.inventoryScale *= TileScale;
        for (int i = 0; i < s_textTiles.Count; i++) {
            Item tile = s_textTiles[i];
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

        for (int i = 0; i < s_textConditions.Count; i++) {
            (Item item, Condition condition) = s_textConditions[i];
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
        s_textTiles.Clear();
        s_textConditions.Clear();
        if (Main.numAvailableRecipes != 0 && !IsUnknown(Main.availableRecipe[Main.focusRecipe])) {
            s_textRecipe = recipe.RecipeIndex;
            if (recipe.requiredTile.Count == 0) s_textTiles.Add(new(CraftingItem.type) {createTile = -2});
            else {
                for (int i = 0; i < recipe.requiredTile.Count && recipe.requiredTile[i] != -1; i++) {
                    if (CraftingStationsItems.TryGetValue(recipe.requiredTile[i], out int type) && type != ItemID.None) s_textTiles.Add(new(type));
                    else s_textTiles.Add(new(CraftingItem.type) { createTile = recipe.requiredTile[i] });
                }
            }

            foreach (Condition condition in recipe.Conditions) {
                Item item;
                if (ConditionItems.TryGetValue(condition.Description.Key, out int type)) item = new(type);
                else item = new(CraftingItem.type) { BestiaryNotes = ConditionMark + condition.Description.Key };
                s_textConditions.Add((item, condition));
            }
        } else s_textRecipe = -1;
    }

    private static void DrawGuideTile(int inventoryX, int inventoryY) {
        if (!Config.guideTile) return;
        float x = inventoryX + TextureAssets.InventoryBack.Width() * Main.inventoryScale * (1 + TileScacingRatio);
        float y = inventoryY;
        Main.inventoryScale *= TileScale;
        Rectangle hitbox = new((int)x, (int)y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
        Item[] items = GuideItems;
        if (!s_visibilityHover && hitbox.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface) {
            Main.player[Main.myPlayer].mouseInterface = true;
            Main.craftingHide = true;
            ItemSlot.OverrideHover(items, 7, 1);
            ItemSlot.LeftClick(items, 7, 1);
            if (Main.mouseLeftRelease && Main.mouseLeft) {
                Recipe.FindRecipes();
            }
            ItemSlot.RightClick(items, 7, 1);
            ItemSlot.MouseHover(items, 7, 1);
        }
        ItemSlot.Draw(Main.spriteBatch, items, 7, 1, hitbox.TopLeft());
        GuideItems = items;
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
        cursor.EmitDelegate(() => s_visibilityHover);
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
            if (Main.focusRecipe == i && Tweeks.Config.craftOnList.Parent && !Tweeks.Config.craftOnList.Value.focusRecipe) ItemSlot.DrawGoldBGForCraftingMaterial = true;
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
        s_visibilityHover = false;
        if (!Enabled || !Config.craftInMenu) return;
        x += (int)((TextureAssets.InventoryBack.Width() - 2) * Main.inventoryScale);
        y += (int)(4 * Main.inventoryScale);
        Vector2 size = InventoryTickBorder.Size() * Main.inventoryScale;
        s_hitBox = new(x - (int)(size.X / 2f), y - (int)(size.Y / 2f), (int)size.X, (int)size.Y);

        if (PlayerInput.IgnoreMouseInterface || !s_hitBox.Contains(Main.mouseX, Main.mouseY)) return;

        Main.player[Main.myPlayer].mouseInterface = true;
        if (Main.mouseLeft && Main.mouseLeftRelease) {
            LocalFilters.ShowAllRecipes = !LocalFilters.ShowAllRecipes;
            SoundEngine.PlaySound(SoundID.MenuTick);
            FindDisplayedRecipes();
        }
        s_visibilityHover = true;
    }
    public static void DrawVisibility() {
        if (!Enabled || !Config.craftInMenu) return;
        VisibilityFilters filters = LocalFilters;
        Asset<Texture2D> tick = filters.ShowAllRecipes ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff;
        Color color = Color.White * 0.7f;
        Main.spriteBatch.Draw(tick.Value, s_hitBox.Center(), null, color, 0f, tick.Value.Size() / 2, 1, 0, 0f);
        if (s_visibilityHover) {
            string key = filters.ShowAllRecipes ? "Mods.BetterInventory.UI.ShowAll" : "Mods.BetterInventory.UI.ShowAvailable";
            Main.instance.MouseText(Language.GetTextValue(key));
            Main.spriteBatch.Draw(InventoryTickBorder.Value, s_hitBox.Center(), null, color, 0f, InventoryTickBorder.Value.Size() / 2, 1, 0, 0f);
        }
    }


    public static Item? GetGuideMaterials() =>  Enabled && !SearchItem.Config.searchRecipes ? Main.guideItem : null;

    public static void FindDisplayedRecipes() => Reflection.Recipe.CollectGuideRecipes.Invoke();

    private static void HookClearRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        if (Enabled && !s_collectingGuide) ClearAvailableRecipes();
        else {
            InventoryLoader.ClearCache();
            RecipeFiltering.ClearFilters();
            ClearUnknownRecipes();
            orig();
        }
    }
    private void HookFindRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck){
        if (!Enabled || canDelayCheck) {
            orig(canDelayCheck);
            return;
        }
        if (!AreSame(Main.guideItem, s_dispGuide) || !AreSame(guideTile, s_dispTile)) FindDisplayedRecipes();
        else orig(canDelayCheck);
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

        // // ++<setup>
        // cursor.EmitDelegate(() => {
        //     if (!Enabled) return;
        //     s_collectingAvailable = true;
        // });

        cursor.GotoPrev(MoveType.After, i => i.MatchRet());
        cursor.MarkLabel(skipGuide);

        // <availableRecipes>
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(Reflection.Recipe.TryRefocusingRecipe));

        // ++<process>
        cursor.EmitDelegate(() => {
            if (!Enabled) return;
            bool added = false;
            if (!Main.mouseItem.IsAir) added |= LocalFilters.AddOwnedItem(Main.mouseItem);
            foreach (Item item in Main.LocalPlayer.inventory) if (!item.IsAir) added |= LocalFilters.AddOwnedItem(item);
            if(Main.LocalPlayer.InChest(out Item[]? chest)){
                foreach (Item item in chest) if (!item.IsAir) added |= LocalFilters.AddOwnedItem(item);
            }

            if(added || !LocalFilters.ShowAllRecipes) FindDisplayedRecipes();
        });
    }
    private static void HookCollectGuideRecipes(On_Recipe.orig_CollectGuideRecipes orig) {
        if (Enabled) {
            s_collectingGuide = true;
            s_ilRecipes = GetDisplayedRecipes().GetEnumerator();
            s_dispGuide = Main.guideItem.Clone();
            s_dispTile = guideTile.Clone();
            Recipe.ClearAvailableRecipes();
        }
        orig();
        s_ilRecipes?.Dispose();
        s_collectingGuide = false;

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
        cursor.EmitLdloc2();
        cursor.EmitDelegate<Func<Recipe, bool>>(recipe => Enabled && CheckGuideFilters(recipe));
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
        cursor.GotoLabel(endLoop!);
        cursor.GotoNext(i => i.MatchStloc1());
        cursor.GotoNext(MoveType.After, i => i.MatchLdloc1());
        cursor.EmitDelegate((int index) => s_ilRecipes!.MoveNext() ? s_ilRecipes.Current : Recipe.numRecipes);
        cursor.EmitDup();
        cursor.EmitStloc1();

        //     }
        // }
    }
    private static IEnumerable<int> GetDisplayedRecipes() {
        static bool Skip(int r) {
            if (Configs.ItemSearch.Instance.unknownDisplay != Configs.ItemSearch.UnknownDisplay.Known && !LocalFilters.IsKnownRecipe(Main.recipe[r])) {
                s_unknownRecipes.Add(r);
                return true;
            }
            if (LocalFilters.FavoritedRecipes.Contains(r)) return true;
            if (LocalFilters.BlacklistedRecipes.Contains(r)) return true;
            return false;
        }

        foreach (int r in LocalFilters.FavoritedRecipes) yield return r;
        if(LocalFilters.ShowAllRecipes) {
            for (int r = 0; r < Recipe.numRecipes; r++) {
                if (!Skip(r)) yield return r;
            }
            foreach (int r in LocalFilters.BlacklistedRecipes) yield return r;
            if (Configs.ItemSearch.Instance.unknownDisplay == Configs.ItemSearch.UnknownDisplay.Known) foreach (int r in s_unknownRecipes) yield return r;
        } else {
            foreach (int r in s_availableRecipes) {
                if (!Skip(r)) yield return r;
            }
            if(!Config.craftInMenu) foreach (int r in LocalFilters.BlacklistedRecipes) yield return r;
        }
    }

    public static bool CheckGuideFilters(Recipe recipe) {
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
                    }
                    else { // Condition
                        if (!recipe.Conditions.Exists(c => ConditionItems.TryGetValue(c.Description.Key, out int type) && type == guideTile.type)) return true;
                    }
                    break;
            }
        }
        if (Main.guideItem.type == ItemID.None || recipe.HasResult(Main.guideItem.type)) {
            Reflection.Recipe.AddToAvailableRecipes.Invoke(recipe.RecipeIndex);
            return true;
        }
        return false;
    }
    private static void HookAddToAvailableRecipes(On_Recipe.orig_AddToAvailableRecipes orig, int recipeIndex) {
        if (Enabled && !s_collectingGuide) s_availableRecipes.Add(recipeIndex);
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
                    LocalFilters.FavoriteRecipe(Main.availableRecipe[recipeIndex]);
                    FindDisplayedRecipes();
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    return true;
                }
            } else if (ItemSlot.ControlInUse) {
                FavoriteState state = GetFavoriteState(Main.availableRecipe[recipeIndex]);
                if (state == FavoriteState.Favorited) return click;
                Main.cursorOverride = CursorOverrideID.TrashCan;
                if (click) {
                    LocalFilters.BlacklistRecipe(Main.availableRecipe[recipeIndex]);
                    FindDisplayedRecipes();
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
            ClickOverride.OverrideCraftHover(recipe);
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
        if (context != ContextID.GuideItem || !Enabled || !Config.anyItem) return orig(inv, context, slot, checkItem);
        if (slot == 0 && GetPlaceholderType(Main.mouseItem) == PlaceholderType.None) return 0;
        if (slot == 1 && IsCraftingTileItem(Main.mouseItem)) return 0;
        return -1;
    }
    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if (!Main.InGuideCraftMenu || !ItemSlot.ShiftInUse || inv[slot].favorited || !Enabled || Array.IndexOf(Utility.InventoryContexts, context) == -1 || inv[slot].IsAir|| ItemSlot.PickItemMovementAction(inv, ContextID.GuideItem, 0, inv[slot]) != 0) return false;
        Main.cursorOverride = CursorOverrideID.InventoryToChest;
        return true;
    }
    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if (Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.InventoryToChest && (Enabled || SearchItem.Enabled)) {
            (Item mouse, Main.mouseItem, inv[slot]) = (Main.mouseItem, inv[slot], new());
            (int cursor, Main.cursorOverride) = (Main.cursorOverride, 0);

            Item[] items = GuideItems;
            ItemSlot.LeftClick(items, ContextID.GuideItem, Enabled && Config.guideTile && IsCraftingTileItem(Main.mouseItem) ? 1 : 0);
            GuideItems = items;

            if (Enabled && !SearchItem.Config.searchRecipes) Main.LocalPlayer.GetDropItem(ref Main.mouseItem);
            else inv[slot] = Main.mouseItem;
            Main.mouseItem = mouse;
            Main.cursorOverride = cursor;
            return true;
        }

        if (context != ContextID.GuideItem || !Enabled) return orig(inv, context, slot);
        if (!Main.mouseItem.IsAir && ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == 0) Main.recBigList = true;
        if (slot == 1) {
            if (GetPlaceholderType(inv[slot]) != PlaceholderType.None) {
                inv[slot].TurnToAir();
                guideTile.TurnToAir();
                Recipe.FindRecipes();
                SoundEngine.PlaySound(SoundID.Grab);
            } else if (inv[slot].IsAir && (Main.mouseItem.IsAir || !IsCraftingTileItem(Main.mouseItem))) {
                inv[slot] = new(CraftingItem.type) { createTile = -2 };
                guideTile = inv[slot];
                Recipe.FindRecipes();
                SoundEngine.PlaySound(SoundID.Grab);
                return true;
            }
            if(ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) != -1) guideTile = Main.mouseItem;
        }
        bool r =  orig(inv, context, slot);
        return r;
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

    public static void ClearAvailableRecipes() => s_availableRecipes.Clear();
    public static void ClearUnknownRecipes() => s_unknownRecipes.Clear();

    public static readonly Item CraftingItem = new(ItemID.Lens);
    public const string ConditionMark = "@BI:";

    public static readonly Dictionary<int, int> CraftingStationsItems = new(); // tile -> item
    public static readonly Dictionary<string, int> ConditionItems = new(); // descrition -> id

    public static Item guideTile = new();
    public static Item[] GuideItems {
        get {
            (s_guideItems[0], s_guideItems[1]) = (Main.guideItem, guideTile);
            return s_guideItems;
        }
        set => (Main.guideItem, guideTile) = (value[0], value[1]);
    }
    private static readonly Item[] s_guideItems = new Item[2];


    public static readonly Asset<Texture2D>[] DefaultTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack4, TextureAssets.InventoryBack14 };
    public static readonly Asset<Texture2D>[] FavoriteTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack10, TextureAssets.InventoryBack17 };
    public static readonly Asset<Texture2D>[] BlacklistedTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack5, TextureAssets.InventoryBack11 };
    public static readonly Asset<Texture2D>[] TileTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack3, TextureAssets.InventoryBack6 };
    public static readonly Asset<Texture2D>[] ConditionTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack12, TextureAssets.InventoryBack8 };

    public static Asset<Texture2D> InventoryTickBorder => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Border");
    public static Asset<Texture2D> UnknownTexture => ModContent.Request<Texture2D>($"BetterInventory/Assets/Unknown_Item");

    private static Asset<Texture2D> s_inventoryBack4 = null!;

    private static bool s_collectingAvailable, s_collectingGuide;
    private static readonly RangeSet s_unknownRecipes = new();
    private static readonly RangeSet s_availableRecipes = new();

    private static Item s_dispGuide = new(), s_dispTile = new();


    private static bool s_visibilityHover;
    private static Rectangle s_hitBox;

    private static int s_textRecipe = -1;
    private static readonly List<Item> s_textTiles = new();
    private static readonly List<(Item item, Condition condition)> s_textConditions = new();

    private static bool s_hideNextItem;
    public static LocalizedText? ForcedToolip;

    public const int TilesPerLine = 7;
    public const float TileScale = 0.46f;
    public const float TileScacingRatio = 0.08f;

    private static IEnumerator<int>? s_ilRecipes;
}

public enum PlaceholderType { None, ByHand, Tile, Condition}