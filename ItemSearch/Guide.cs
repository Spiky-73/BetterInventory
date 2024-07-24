using System;
using System.Collections.Generic;
using BetterInventory.Crafting;
using BetterInventory.InventoryManagement;
using SpikysLib.DataStructures;
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
using BetterInventory.ItemActions;
using SpikysLib.Extensions;
using SpikysLib;
using BetterInventory.Default.Catalogues;

namespace BetterInventory.ItemSearch;


public sealed class Guide : ModSystem {

    public static VisibilityFilters LocalFilters => BetterPlayer.LocalPlayer.VisibilityFilters;

    public override void Load() {
        On_Main.DrawGuideCraftText += HookGuideCraftText;

        On_Recipe.ClearAvailableRecipes += HookClearRecipes;
        On_Recipe.FindRecipes += HookFindRecipes;
        On_Recipe.CollectGuideRecipes += HookCollectGuideRecipes;
        On_Recipe.AddToAvailableRecipes += HookAddToAvailableRecipes;

        On_Player.AdjTiles += HookGuideTileAdj;
        On_ItemSlot.PickItemMovementAction += HookAllowGuideItem;
        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;
        On_Main.HoverOverCraftingItemButton += HookGuideTileAir;

        On_ItemSlot.Draw_SpriteBatch_refItem_int_Vector2_Color += HookHideItem;
        On_ItemSlot.DrawItemIcon += HookDrawPlaceholder;
        MonoModHooks.Add(typeof(ItemLoader).GetMethod(nameof(ItemLoader.ModifyTooltips)), HookHideTooltip);

        IL_Main.DrawInventory += static il => {
            if(!il.ApplyTo(ILDrawVisibility, Configs.BetterGuide.CraftInMenu)) Configs.UnloadedItemSearch.Value.guideCraftInMenu = true;
            if(!il.ApplyTo(ILCustomDrawCreateItem, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
            if(!il.ApplyTo(ILCustomDrawMaterials, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
            if(!il.ApplyTo(ILCustomDrawRecipeList, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
        };
        IL_Main.CraftItem += static il => {
            if(!il.ApplyTo(IlUnfavoriteOnCraft, Configs.FavoritedRecipes.UnfavoriteOnCraft)) Configs.UnloadedItemSearch.Value.guideUnfavoriteOnCraft = true;
        };
        IL_Main.HoverOverCraftingItemButton += static il => {
            if(!il.ApplyTo(ILFavoriteRecipe, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
            if(!il.ApplyTo(ILCraftInGuideMenu, Configs.BetterGuide.CraftInMenu)) Configs.UnloadedItemSearch.Value.guideCraftInMenu = true;
        };
        IL_Recipe.FindRecipes += static il => {
            if(!il.ApplyTo(ILSkipGuideRecipes, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
            if(!il.ApplyTo(ILUpdateOwnedItems, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
        };
        IL_Recipe.CollectGuideRecipes += static il => {
            if(!il.ApplyTo(ILMoreGuideRecipes, Configs.BetterGuide.MoreRecipes)) Configs.UnloadedItemSearch.Value.guideMoreRecipes = true;
            if(!il.ApplyTo(ILForceAddToAvailable, Configs.BetterGuide.AvailableRecipes || Configs.BetterGuide.CraftingStation || Configs.RecipeFilters.Enabled)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = Configs.UnloadedItemSearch.Value.guideCraftingStation = Configs.UnloadedCrafting.Value.recipeFilters = true;
            if(!il.ApplyTo(ILGuideRecipeOrder, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
        };


        s_inventoryBack4 = TextureAssets.InventoryBack4;
    }
    
    private void HookGuideTileAir(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex){
        if (!Configs.BetterGuide.CraftingStation || guideTile.IsAir || !Main.guideItem.IsAir) {
            orig(recipeIndex);
            return;
        }
        (Item item, Main.guideItem) = (Main.guideItem, guideTile);
        orig(recipeIndex);
        Main.guideItem = item;
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
        Default.Catalogues.Bestiary.HooksBestiaryUI();
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
        inventoryX = 73;
        inventoryY = 331 + adjY;

        if (Configs.BetterGuide.CraftInMenu) HandleVisibility(inventoryX, inventoryY);
        
        if (Configs.BetterGuide.ConditionsDisplay) DrawGuideItems(inventoryX, inventoryY);
        else orig(adjY, craftingTipColor, out inventoryX, out inventoryY);

        if (Configs.BetterGuide.CraftingStation) DrawGuideTile(inventoryX, inventoryY);
    }

    private static void DrawGuideItems(int inventoryX, int inventoryY) {
        Recipe recipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];

        if (s_textRecipe != (Main.numAvailableRecipes == 0 ? -1 : recipe.RecipeIndex)) UpdateCraftTiles(recipe);

        float minX = inventoryX + TextureAssets.InventoryBack.Width() * Main.inventoryScale * (1 + TileSpacingRatio);
        Vector2 delta = new(TextureAssets.InventoryBack.Width() * (TileScale + TileSpacingRatio), -TextureAssets.InventoryBack.Height() * (TileScale + TileSpacingRatio));
        delta *= Main.inventoryScale;
        Vector2 position = new(minX, inventoryY - delta.Y);
        int slot = 0;
        void MovePosition() {
            if (Configs.FixedUI.Wrapping && ++slot % TilesPerLine == 0) {
                position.X = minX;
                position.Y += delta.Y;
                if (slot == TilesPerLine) MovePosition();
            }
            else position.X += delta.X;
        }
        Main.inventoryScale *= TileScale;
        for (int i = 0; i < textTiles.Count; i++) {
            Item tile = textTiles[i];
            Color inventoryBack = Main.inventoryBack;
            OverrideRecipeTexture(TileTextures, false, GetPlaceholderType(tile) == PlaceholderType.ByHand || Main.LocalPlayer.adjTile[tile.createTile]);
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

        for (int i = 0; i < textConditions.Count; i++) {
            (Item item, Condition condition) = textConditions[i];
            Color inventoryBack = Main.inventoryBack;
            OverrideRecipeTexture(ConditionTextures, false, condition.Predicate());
            ItemSlot.Draw(Main.spriteBatch, ref item, ContextID.CraftingMaterial, position);
            TextureAssets.InventoryBack4 = s_inventoryBack4;
            Rectangle hitbox = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
            if (hitbox.Contains(Main.mouseX, Main.mouseY)) {
                Main.LocalPlayer.mouseInterface = true;
                forcedTooltip = condition.Description;
                ItemSlot.MouseHover(ref item, ContextID.CraftingMaterial);
            }
            Main.inventoryBack = inventoryBack;
            MovePosition();
        }

        Main.inventoryScale /= TileScale;
    }
    private static void UpdateCraftTiles(Recipe recipe) {
        textTiles.Clear();
        textConditions.Clear();
        if (Main.numAvailableRecipes != 0 && !IsUnknown(Main.availableRecipe[Main.focusRecipe])) {
            s_textRecipe = recipe.RecipeIndex;
            if (recipe.requiredTile.Count == 0) textTiles.Add(ByHandPlaceholder);
            else {
                for (int i = 0; i < recipe.requiredTile.Count && recipe.requiredTile[i] != -1; i++) {
                    if (CraftingStationsItems.TryGetValue(recipe.requiredTile[i], out int type) && type != ItemID.None) textTiles.Add(new(type));
                    // else s_textTiles.Add(new(CraftingItem.type) { createTile = recipe.requiredTile[i] });
                    else textTiles.Add(TilePlaceholder(recipe.requiredTile[i]));
                }
            }

            foreach (Condition condition in recipe.Conditions) {
                Item item;
                if (ConditionItems.TryGetValue(condition.Description.Key, out int type)) item = new(type);
                else item = ConditionPlaceholder(condition);
                textConditions.Add((item, condition));
            }
        } else s_textRecipe = -1;
    }

    private static void DrawGuideTile(int inventoryX, int inventoryY) {
        float x = inventoryX + TextureAssets.InventoryBack.Width() * Main.inventoryScale * (1 + TileSpacingRatio);
        float y = inventoryY;
        Main.inventoryScale *= TileScale;
        Rectangle hitbox = new((int)x, (int)y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
        Item[] items = GuideItems;
        if (!s_visibilityHover && hitbox.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface) {
            Main.player[Main.myPlayer].mouseInterface = true;
            Main.craftingHide = true;
            ItemSlot.OverrideHover(items, ContextID.GuideItem, 1);
            ItemSlot.LeftClick(items, ContextID.GuideItem, 1);
            if (Main.mouseLeftRelease && Main.mouseLeft) {
                Recipe.FindRecipes();
            }
            ItemSlot.RightClick(items, ContextID.GuideItem, 1);
            ItemSlot.MouseHover(items, ContextID.GuideItem, 1);
        }
        ItemSlot.Draw(Main.spriteBatch, items, ContextID.GuideItem, 1, hitbox.TopLeft());
        GuideItems = items;
        Main.inventoryScale /= TileScale;
    }

    private static void ILDrawVisibility(ILContext il) {
        ILCursor cursor = new(il);

        //         Main.DrawGuideCraftText(...);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(Main), "DrawGuideCraftText"));
        ILLabel? noHover = null;
        cursor.FindNext(out _, i => i.MatchBlt(out noHover));
        //         ++ if(!<visibilityHover>) {
        cursor.EmitDelegate(() => s_visibilityHover);
        cursor.EmitBrtrue(noHover!);

        //             <handle guide item slot>
        //         ++ }
        //         ItemSlot.Draw(Main.spriteBatch, ref Main.guideItem, ...);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //         ++ <drawVisibility>
        cursor.EmitDelegate(() => {
            if(Configs.BetterGuide.CraftInMenu) DrawVisibility();
        });
    }
    private static void ILCustomDrawCreateItem(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoRecipeDraw();
        
        cursor.GotoNextLoc(out int recipeIndex, i => i.Next.MatchBr(out _), 124);

        //     for (<recipeIndex>) {
        //         ...
        //         if(<visible>) {
        //             ...
        //             if (Main.numAvailableRecipes > 0) {
        //                 ...
        //                 Main.inventoryBack = ...;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Main.HoverOverCraftingItemButton));
        cursor.GotoNext(MoveType.Before, i => i.SaferMatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //                 ++ <overrideBackground>
        cursor.EmitLdloc(recipeIndex); // int num63
        cursor.EmitDelegate((int i) => {
            if (!Configs.BetterGuide.AvailableRecipes) return;
            OverrideRecipeTexture(GetFavoriteState(Main.availableRecipe[i]), ItemSlot.DrawGoldBGForCraftingMaterial, IsAvailable(Main.availableRecipe[i]));
            ItemSlot.DrawGoldBGForCraftingMaterial = false;
            if (Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.availableRecipe[i])) s_hideNextItem = true;
        });

        //                 ItemSlot.Draw(...);
        cursor.GotoNext(MoveType.After, i => true);

        //                 ++ <restoreBackground>
        cursor.EmitDelegate(RestoreBack4);
        //             }
        //         }
        //     }

    }
    private static void ILCustomDrawMaterials(ILContext il) {
        ILCursor cursor = new(il);

        //     if (++<known> && Main.numAvailableRecipes > 0) {
        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentIngredientsCount));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(Reflection.Main.numAvailableRecipes));
        cursor.EmitDelegate((int num) => num == 0 || Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.availableRecipe[Main.focusRecipe]) ? 0 : num);

        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentRecipeSmall));
        cursor.GotoNextLoc(out int materialIndex, i => i.Previous.MatchLdcI4(0), 130);

        //         for (<focusRecipeMaterialIndex>) {
        //             ...
        //             Item tempItem = ...;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Main.SetRecipeMaterialDisplayName));
        cursor.GotoNext(MoveType.Before, i => i.SaferMatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //             ++ <overrideBackground>
        cursor.EmitLdloc(materialIndex);
        cursor.EmitDelegate((int matI) => {
            if (!Configs.BetterGuide.AvailableRecipes) return;
            Recipe recipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];
            bool canCraft = IsAvailable(recipe.RecipeIndex);
            if (!canCraft) {
                Item material = recipe.requiredItem[matI];
                canCraft = PlayerExtensions.OwnedItems.GetValueOrDefault(material.type, 0) >= material.stack;
                if(!canCraft) {
                    int g = recipe.acceptedGroups.FindIndex(g => RecipeGroup.recipeGroups[g].IconicItemId == material.type);
                    if (g != -1) {
                        RecipeGroup group = RecipeGroup.recipeGroups[recipe.acceptedGroups[g]];
                        canCraft = PlayerExtensions.OwnedItems.GetValueOrDefault(group.GetGroupFakeItemId(), 0) >= material.stack;
                    }
                }
            }
            OverrideRecipeTexture(FavoriteState.Default, false, canCraft);
        });

        //             ItemSlot.Draw(...);
        cursor.GotoNext(MoveType.After, i => true);

        //             ++ <restoreBackground>
        cursor.EmitDelegate(RestoreBack4);
        //         }
        //     }
        //     ...
        // }

    }
    private static void ILCustomDrawRecipeList(ILContext il) {
        ILCursor cursor = new(il);

        // ----- Recipe big list background and ??? -----
        // Main.hidePlayerCraftingMenu = false;
        // if(<recBigListVisible>) {
        //     ...
        //     while (<showingRecipes>) {
        cursor.GotoNextLoc(out int recipeListIndex, i => i.Previous.MatchLdsfld(Reflection.Main.recStart), 153);

        //         ...
        //         if (<mouseHover>) {
        //             if (<click>) ...
        //             Main.craftingHide = true;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Main.LockCraftingForThisCraftClickDuration));
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(Reflection.Main.craftingHide));

        //             ++ <GuideHover>
        cursor.EmitLdloc(recipeListIndex); // int num87
        cursor.EmitDelegate((int r) => {
            if (Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.availableRecipe[r])) forcedTooltip = Language.GetText($"{Localization.Keys.UI}.Unknown");
        });
        //             ...
        //         }

        //         if (Main.numAvailableRecipes > 0) {
        //             ...
        //             Main.inventoryBack = ...;
        cursor.GotoNext(MoveType.Before, i => i.SaferMatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //             ++ <overrideBackground>
        cursor.EmitLdloc(recipeListIndex);
        cursor.EmitDelegate((int i) => {
            if (!Configs.BetterGuide.AvailableRecipes) return;
            OverrideRecipeTexture(GetFavoriteState(Main.availableRecipe[i]), ItemSlot.DrawGoldBGForCraftingMaterial, s_availableRecipes.Contains(Main.availableRecipe[i]));
            ItemSlot.DrawGoldBGForCraftingMaterial = false;
            if (Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.availableRecipe[i])) s_hideNextItem = true;

        });

        //             ItemSlot.Draw(...);
        cursor.GotoNext(MoveType.After, i => true);

        //             ++ <restoreBackground>
        cursor.EmitDelegate(RestoreBack4);
        //         }
        //         ...
        //     }
        // }
    }
    private static void RestoreBack4() => TextureAssets.InventoryBack4 = s_inventoryBack4;

    public static void HandleVisibility(int x, int y) {
        s_visibilityHover = false;
        x += (int)((TextureAssets.InventoryBack.Width() - 2) * Main.inventoryScale);
        y += (int)(4 * Main.inventoryScale);
        Vector2 size = InventoryTickBorder.Size() * Main.inventoryScale;
        s_hitBox = new(x - (int)(size.X / 2f), y - (int)(size.Y / 2f), (int)size.X, (int)size.Y);

        if (PlayerInput.IgnoreMouseInterface || !s_hitBox.Contains(Main.mouseX, Main.mouseY)) return;

        Main.player[Main.myPlayer].mouseInterface = true;
        if (Main.mouseLeft && Main.mouseLeftRelease) {
            LocalFilters.ShowAllRecipes = !LocalFilters.ShowAllRecipes;
            SoundEngine.PlaySound(SoundID.MenuTick);
            FindGuideRecipes();
        }
        s_visibilityHover = true;
    }
    public static void DrawVisibility() {
        VisibilityFilters filters = LocalFilters;
        Asset<Texture2D> tick = filters.ShowAllRecipes ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff;
        Color color = Color.White * 0.7f;
        Main.spriteBatch.Draw(tick.Value, s_hitBox.Center(), null, color, 0f, tick.Value.Size() / 2, 1, 0, 0f);
        if (s_visibilityHover) {
            string key = filters.ShowAllRecipes ? $"{Localization.Keys.UI}.ShowAll" : $"{Localization.Keys.UI}.ShowAvailable";
            Main.instance.MouseText(Language.GetTextValue(key));
            Main.spriteBatch.Draw(InventoryTickBorder.Value, s_hitBox.Center(), null, color, 0f, InventoryTickBorder.Value.Size() / 2, 1, 0, 0f);
        }
    }


    public static Item? GetGuideMaterials() => Configs.BetterGuide.AvailableRecipes && !RecipeList.Instance.Enabled ? Main.guideItem : null;

    public static void FindGuideRecipes() {
        s_collectingGuide = true;
        int oldRecipe = Main.availableRecipe[Main.focusRecipe];
        float focusY = Main.availableRecipeY[Main.focusRecipe];
        Recipe.ClearAvailableRecipes();
        Reflection.Recipe.CollectGuideRecipes.Invoke();
        Reflection.Recipe.TryRefocusingRecipe.Invoke(oldRecipe);
        Reflection.Recipe.VisuallyRepositionRecipes.Invoke(focusY);
    }

    private static void HookClearRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        if (Configs.BetterGuide.AvailableRecipes && !s_collectingGuide) {
            ClearAvailableRecipes();
            return;
        }
        InventoryLoader.ClearCache();
        RecipeFiltering.ClearFilters();
        ClearUnknownRecipes();
        orig();
    }
    private void HookFindRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck){
        if (canDelayCheck) {
            orig(canDelayCheck);
            return;
        }
        if (!Configs.BetterGuide.AvailableRecipes) {
            if (Configs.BetterGuide.CraftingStation && !guideTile.IsAir) FindGuideRecipes();
            else orig(canDelayCheck);
            return;
        }
        if (!AreSame(Main.guideItem, s_dispGuide) || !AreSame(guideTile, s_dispTile)) FindGuideRecipes();
        else orig(canDelayCheck);
    }

    private static void ILSkipGuideRecipes(ILContext il) {
        ILCursor cursor = new(il);

        // Recipe.ClearAvailableRecipes()
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(Reflection.Recipe.ClearAvailableRecipes));

        // ++ if (Enabled) goto skipGuide
        ILLabel skipGuide = cursor.DefineLabel();
        cursor.EmitDelegate(() => Configs.BetterGuide.AvailableRecipes);
        cursor.EmitBrtrue(skipGuide);
        cursor.MarkLabel(skipGuide); // Here in case of exception


        // if(<guideItem>) {
        //     <guideRecipes>
        // }
        // ++ skipGuide:
        // Player localPlayer = Main.LocalPlayer;
        // Recipe.CollectItemsToCraftWithFrom(localPlayer);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(Reflection.Recipe.CollectItemsToCraftWithFrom));

        // // ++<setup>
        // cursor.EmitDelegate(() => {
        //     if (!Enabled) return;
        //     s_collectingAvailable = true;
        // });

        cursor.GotoPrev(MoveType.After, i => i.MatchRet());
        cursor.MarkLabel(skipGuide);
    }
    private static void ILUpdateOwnedItems(ILContext il) {
        ILCursor cursor = new(il);

        // <availableRecipes>
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Recipe.CollectItemsToCraftWithFrom));
        cursor.GotoNext(MoveType.AfterLabel, i => i.SaferMatchCall(Reflection.Recipe.TryRefocusingRecipe));

        // ++<updateDisplay>
        cursor.EmitDelegate(() => {
            if (!Configs.BetterGuide.AvailableRecipes) return;
            bool added = false;
            if (!Main.mouseItem.IsAir) added |= LocalFilters.AddOwnedItem(Main.mouseItem);
            foreach (Item item in Main.LocalPlayer.inventory) if (!item.IsAir) added |= LocalFilters.AddOwnedItem(item);
            if(Main.LocalPlayer.InChest(out Item[]? chest)){
                foreach (Item item in chest) if (!item.IsAir) added |= LocalFilters.AddOwnedItem(item);
            }

            if(added || !LocalFilters.ShowAllRecipes) FindGuideRecipes();
        });
        // Recipe.TryRefocusingRecipe(oldRecipe);
        // Recipe.VisuallyRepositionRecipes(focusY);
    }
    private static void HookCollectGuideRecipes(On_Recipe.orig_CollectGuideRecipes orig) {
        if (Configs.BetterGuide.AvailableRecipes) {
            s_collectingGuide = true;
            s_ilRecipes = GetDisplayedRecipes().GetEnumerator();
            s_dispGuide = Main.guideItem.Clone();
            s_dispTile = guideTile.Clone();
        }
        orig();
        s_ilRecipes = null;
        s_collectingGuide = false;

    }

    private static ILCursor GotoRecipeDisabled(ILCursor cursor, out ILLabel endLoop, out int index, out int recipe) {
        cursor.GotoNextLoc(out index, i => i.Previous.MatchLdcI4(0), 1);
        // for (<recipeIndex>) {
        //     ...
        //     if (recipe.Disabled) continue;
        cursor.GotoNext(i => i.MatchCallvirt(Reflection.Recipe.Disabled.GetMethod!));
        int r = 0;
        recipe = cursor.TryFindPrev(out _, i => i.MatchLdloc(out r)) ? r : 2;
        ILLabel end = null!;
        cursor.GotoNext(i => i.MatchBrtrue(out end!));
        cursor.GotoNext(MoveType.AfterLabel);
        endLoop = end;
        return cursor;
    }
    private static void ILMoreGuideRecipes(ILContext il) {
        ILCursor cursor = new(il);

        GotoRecipeDisabled(cursor, out ILLabel endLoop, out _, out int recipe);
        
        //     ++ if(<extraRecipe>) {
        //     ++     <addRecipe>
        //     ++     continue;
        //     ++ }
        cursor.EmitLdloc(recipe);
        cursor.EmitDelegate((Recipe recipe) => {
            if (Configs.BetterGuide.MoreRecipes && Main.guideItem.type == ItemID.None || recipe.HasResult(Main.guideItem.type)) {
                Reflection.Recipe.AddToAvailableRecipes.Invoke(recipe.RecipeIndex);
                return true;
            }
            return false;
        });
        cursor.EmitBrtrue(endLoop);
    }
    private static void ILForceAddToAvailable(ILContext il) {
        ILCursor cursor = new(il);

        GotoRecipeDisabled(cursor, out ILLabel endLoop, out int index, out _);

        //     for(<material>) {
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdsfld(Reflection.Main.availableRecipe));

        //         if(<recipeOk> ++[&& !custom]) {
        cursor.EmitLdloc(index);
        cursor.EmitDelegate((int r) => {
        if (!(Configs.BetterGuide.AvailableRecipes || Configs.BetterGuide.CraftingStation || Configs.RecipeFilters.Enabled)) return false;
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
    private static void ILGuideRecipeOrder(ILContext il) {
        ILCursor cursor = new(il);

        GotoRecipeDisabled(cursor, out ILLabel endLoop, out int index, out _);

        cursor.GotoLabel(endLoop!);
        cursor.GotoNext(i => i.MatchStloc(index));
        cursor.GotoNext(MoveType.After, i => i.MatchLdloc(index));
        cursor.EmitDelegate((int index) => {
            if (!Configs.BetterGuide.AvailableRecipes) return index;
            return s_ilRecipes!.MoveNext() ? s_ilRecipes.Current : Recipe.numRecipes;
        });
        cursor.EmitDup();
        cursor.EmitStloc(index);

        //     }
        // }
    }
    private static IEnumerable<int> GetDisplayedRecipes() {
        static bool Skip(int r) {
            if (Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay != Configs.UnknownDisplay.Known && !LocalFilters.IsKnownRecipe(Main.recipe[r])) {
                s_unknownRecipes.Add(r);
                return true;
            }
            if (!Configs.BetterGuide.FavoritedRecipes) return false;
            if (LocalFilters.FavoritedRecipes.Contains(r)) return true;
            if (LocalFilters.BlacklistedRecipes.Contains(r)) return true;
            return false;
        }

        if (Configs.BetterGuide.FavoritedRecipes) foreach (int r in LocalFilters.FavoritedRecipes) yield return r;
        if (LocalFilters.ShowAllRecipes) {
            for (int r = 0; r < Recipe.numRecipes; r++) {
                if (!Skip(r)) yield return r;
            }
            if (Configs.BetterGuide.FavoritedRecipes) foreach (int r in LocalFilters.BlacklistedRecipes) yield return r;
            if (Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay == Configs.UnknownDisplay.Unknown) foreach (int r in s_unknownRecipes) yield return r;
        } else {
            foreach (int r in s_availableRecipes) {
                if (!Skip(r)) yield return r;
            }
            if(Configs.BetterGuide.FavoritedRecipes && !Configs.BetterGuide.CraftInMenu) foreach (int r in LocalFilters.BlacklistedRecipes) yield return r;
        }
    }

    private static void HookAddToAvailableRecipes(On_Recipe.orig_AddToAvailableRecipes orig, int recipeIndex) {
        if (Configs.BetterGuide.AvailableRecipes && !s_collectingGuide){
            s_availableRecipes.Add(recipeIndex);
            return;
        }
        if ((Configs.BetterGuide.CraftingStation && !CheckGuideFilters(recipeIndex)) || (Configs.RecipeFilters.Enabled && !RecipeFiltering.FitsFilters(recipeIndex))) return;
        orig(recipeIndex);
    }

    public static bool CheckGuideFilters(int recipeIndex) {
        Recipe recipe = Main.recipe[recipeIndex];
        return guideTile.IsAir || GetPlaceholderType(guideTile) switch {
            PlaceholderType.ByHand => recipe.requiredTile.Count == 0,
            PlaceholderType.Tile => recipe.requiredTile.Contains(guideTile.createTile),
            PlaceholderType.Condition => recipe.Conditions.Exists(c => c.Description.Key == guideTile.BestiaryNotes[ConditionMark.Length..]),
            _ => guideTile.createTile != -1 ? // Real Item
                recipe.requiredTile.Contains(guideTile.createTile) :
                recipe.Conditions.Exists(c => ConditionItems.TryGetValue(c.Description.Key, out int type) && type == guideTile.type),
        };
    }

    private static void ILFavoriteRecipe(ILContext il) {
        ILCursor cursor = new(il);

        // <flags>
        cursor.GotoNext(MoveType.Before, i => i.MatchLdsfld(Reflection.Main.focusRecipe));

        // ++ if(<favorite>) goto skip;
        cursor.EmitLdarg0();
        cursor.EmitDelegate((int recipeIndex) => {
            if (!Configs.BetterGuide.AvailableRecipes) return false;

            if (!IsAvailable(Main.availableRecipe[recipeIndex])) Main.LockCraftingForThisCraftClickDuration();

            if (Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.availableRecipe[recipeIndex])) {
                forcedTooltip = Language.GetText($"{Localization.Keys.UI}.Unknown");
                return false;
            }
            if (Configs.BetterGuide.FavoritedRecipes) {
                bool click = Main.mouseLeft && Main.mouseLeftRelease;
                if (Main.keyState.IsKeyDown(Main.FavoriteKey)) {
                    Main.cursorOverride = CursorOverrideID.FavoriteStar;
                    if (click) {
                        LocalFilters.ToggleFavorited(Main.availableRecipe[recipeIndex]);
                        FindGuideRecipes();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        return true;
                    }
                } else if (ItemSlot.ControlInUse && !LocalFilters.IsFavorited(Main.availableRecipe[recipeIndex])) {
                    Main.cursorOverride = CursorOverrideID.TrashCan;
                    if (click) {
                        LocalFilters.ToggleBlacklisted(Main.availableRecipe[recipeIndex]);
                        FindGuideRecipes();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        return true;
                    }
                    return false;
                }
            }
            return false;
        });
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrtrue(skip);
        cursor.MarkLabel(skip); // Here in case of exception

        // if (Main.focusRecipe == recipeIndex && Main.guideItem.IsAir) ...
        // else ...
        // ++ skip:
        // throw new Exception();
        cursor.GotoNext(i => i.MatchStsfld(Reflection.Main.craftingHide));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdcI4(1));
        cursor.MarkLabel(skip);
        // Main.craftingHide = true;
    }

    private static void IlUnfavoriteOnCraft(ILContext il) {
        ILCursor cursor = new(il);

        // Item crafted = r.createItem.Clone();
        // r.Create();
        // RecipeLoader.OnCraft(crafted, r, Main.mouseItem);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(RecipeLoader), nameof(RecipeLoader.OnCraft)));

        // ++ <unFavorite>
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => {
            if (!Configs.FavoritedRecipes.UnfavoriteOnCraft) return;
            if (!(GetFavoriteState(r.RecipeIndex) switch {
                FavoriteState.Favorited => Configs.FavoritedRecipes.Value.unfavoriteOnCraft.HasFlag(Configs.UnfavoriteOnCraft.Favorited),
                FavoriteState.Blacklisted => Configs.FavoritedRecipes.Value.unfavoriteOnCraft.HasFlag(Configs.UnfavoriteOnCraft.Blacklisted),
                FavoriteState.Default or _ => false,
            })) return;
            LocalFilters.ResetRecipeState(r.RecipeIndex);
            FindGuideRecipes();
        });
    }
    private static void ILCraftInGuideMenu(ILContext il) {
        ILCursor cursor = new(il);

        // if (Main.focusRecipe == recipeIndex && ++[Main.guideItem.IsAir || <craftInMenu>]) {
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.guideItem));
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(Reflection.Item.IsAir.GetMethod!));
        cursor.EmitDelegate((bool isAir) => isAir || Configs.BetterGuide.CraftInMenu);

        //     <craft>
        // }
    }
    
    private static void HookGuideTileAdj(On_Player.orig_AdjTiles orig, Player self) {
        orig(self);
        if (!Configs.BetterGuide.AvailableRecipes || !Configs.BetterGuide.CraftingStation || RecipeList.Instance.Enabled || guideTile.createTile < TileID.Dirt) return;
        self.adjTile[guideTile.createTile] = true;
        Recipe.FindRecipes();
    }
    private static int HookAllowGuideItem(On_ItemSlot.orig_PickItemMovementAction orig, Item[] inv, int context, int slot, Item checkItem) {
        if (context != ContextID.GuideItem) return orig(inv, context, slot, checkItem);
        if (slot == 0 && Configs.BetterGuide.MoreRecipes && GetPlaceholderType(Main.mouseItem) == PlaceholderType.None) return 0;
        if (slot == 1 && Configs.BetterGuide.CraftingStation && (checkItem.IsAir || FitsCraftingTile(Main.mouseItem))) return 0;
        return -1;
    }
    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if (GetPlaceholderType(inv[slot]) != PlaceholderType.None
                && (ItemSlot.ShiftInUse || ItemSlot.ControlInUse || Main.mouseItem.IsAir || ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == -1)){
            Main.cursorOverride = CursorOverrideID.TrashCan;
            return true;
        }
        if (Configs.BetterGuide.MoreRecipes && ItemSlot.ShiftInUse && !inv[slot].favorited
                && Main.InGuideCraftMenu && Array.IndexOf(PlayerExtensions.InventoryContexts, context) != -1 && !inv[slot].IsAir
                && ItemSlot.PickItemMovementAction(inv, ContextID.GuideItem, 0, inv[slot]) == 0) {
            Main.cursorOverride = CursorOverrideID.InventoryToChest;
            return true;
        }
        return false;
    }
    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if ((Configs.BetterGuide.Enabled || RecipeList.Instance.Enabled) && Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.InventoryToChest) {
            (Item mouse, Main.mouseItem, inv[slot]) = (Main.mouseItem, inv[slot], new());
            (int cursor, Main.cursorOverride) = (Main.cursorOverride, 0);

            Item[] items = GuideItems;
            ItemSlot.LeftClick(items, ContextID.GuideItem, Configs.BetterGuide.CraftingStation && IsCraftingStation(Main.mouseItem) ? 1 : 0);
            GuideItems = items;

            if (Configs.BetterGuide.Enabled && !RecipeList.Instance.Enabled) Main.LocalPlayer.GetDropItem(ref Main.mouseItem);
            else inv[slot] = Main.mouseItem;
            Main.mouseItem = mouse;
            Main.cursorOverride = cursor;
            return true;
        }

        if (context != ContextID.GuideItem) return orig(inv, context, slot);
        
        if ((Configs.BetterGuide.Enabled || RecipeList.Instance.Enabled) && Main.cursorOverride == CursorOverrideID.TrashCan) {
            inv[slot].TurnToAir();
            Recipe.FindRecipes();
            SoundEngine.PlaySound(SoundID.Grab);
            return true;
        }

        if (!Configs.BetterGuide.Enabled) return orig(inv, context, slot);
        if (!Main.mouseItem.IsAir && ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == 0) Main.recBigList = true;
        
        if (GetPlaceholderType(inv[slot]) != PlaceholderType.None) inv[slot].TurnToAir();
        else if (slot == 1 && inv[slot].IsAir && !FitsCraftingTile(Main.mouseItem)) {
            inv[slot] = ByHandPlaceholder;
            Recipe.FindRecipes();
            SoundEngine.PlaySound(SoundID.Grab);
            return true;
        }
        return orig(inv, context, slot);
    }

    private static void HookHideItem(On_ItemSlot.orig_Draw_SpriteBatch_refItem_int_Vector2_Color orig, SpriteBatch spriteBatch, ref Item inv, int context, Vector2 position, Color lightColor) {
        if (s_hideNextItem) {
            Item item = Placeholder;
            orig(spriteBatch, ref item, context, position, lightColor);
            s_hideNextItem = false;
        } else {
            orig(spriteBatch, ref inv, context, position, lightColor);
        }
    }
    private static float HookDrawPlaceholder(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
        if (s_hideNextItem) {
            return DrawTexture(spriteBatch, UnknownTexture.Value, Color.White, screenPositionForItemCenter, ref scale, sizeLimit, environmentColor);
        }
        switch (GetPlaceholderType(item)) {
        case PlaceholderType.ByHand:
            Main.instance.LoadItem(ItemID.BoneGlove);
            return DrawTexture(spriteBatch, TextureAssets.Item[ItemID.BoneGlove].Value, Color.White, screenPositionForItemCenter, ref scale, sizeLimit, environmentColor);
        case PlaceholderType.Tile:
            Graphics.DrawTileFrame(spriteBatch, item.createTile, screenPositionForItemCenter, new Vector2(0.5f, 0.5f), scale);
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
            PlaceholderType.ByHand => Language.GetTextValue($"{Localization.Keys.UI}.ByHand"),
            PlaceholderType.Tile => Lang.GetMapObjectName(MapHelper.TileToLookup(item.createTile, item.placeStyle)),
            PlaceholderType.Condition => Language.GetTextValue(item.BestiaryNotes[ConditionMark.Length..]),
            _ => forcedTooltip?.Value,
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

    public static FavoriteState GetFavoriteState(int recipe) {
        if (!Configs.BetterGuide.FavoritedRecipes) return FavoriteState.Default;
        if (LocalFilters.IsFavorited(recipe)) return FavoriteState.Favorited;
        if (LocalFilters.IsBlacklisted(recipe)) return FavoriteState.Blacklisted;
        return FavoriteState.Default;
    }

    public static bool IsAvailable(int recipe) => s_availableRecipes.Contains(recipe);
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

    public static readonly Item Placeholder = new(ItemID.Lens);
    public const string ConditionMark = "@BI:";
    public const int ByHandCreateTile = -2;
    public static Item ByHandPlaceholder => new(Placeholder.type) { createTile = ByHandCreateTile };
    public static Item ConditionPlaceholder(Condition condition) => new(Placeholder.type) { BestiaryNotes = ConditionMark + condition.Description.Key };
    public static Item TilePlaceholder(int type) => new(Placeholder.type) { createTile = type };
    public static PlaceholderType GetPlaceholderType(Item item) {
        if (item.type != Placeholder.type || item.stack != 1) return PlaceholderType.None;
        if (item.createTile == ByHandCreateTile) return PlaceholderType.ByHand;
        if (item.createTile != -1) return PlaceholderType.Tile;
        if (item.BestiaryNotes?.StartsWith(ConditionMark) == true) return PlaceholderType.Condition;
        return PlaceholderType.None;
    }

    public static bool FitsCraftingTile(Item item) => IsCraftingStation(item) || ConditionItems.ContainsValue(item.type);
    public static bool IsCraftingStation(Item item) => CraftingStationsItems.ContainsKey(item.createTile) || GetPlaceholderType(item) != PlaceholderType.None;

    public static void dropItemCheck(Player self) {
        if (Main.InGuideCraftMenu || guideTile.IsAir) return;
        if (GetPlaceholderType(guideTile) != PlaceholderType.None) guideTile.TurnToAir();
        else self.GetDropItem(ref guideTile);
    }

    public static bool AreSame(Item item, Item other) {
        PlaceholderType a = GetPlaceholderType(item);
        PlaceholderType b = GetPlaceholderType(other);
        return a == b && (a switch {
            PlaceholderType.ByHand => true,
            PlaceholderType.Tile => item.createTile == other.createTile,
            PlaceholderType.Condition => item.BestiaryNotes == other.BestiaryNotes,
            PlaceholderType.None or _ => item.type == other.type,
        });
    }

    public static void ClearAvailableRecipes() => s_availableRecipes.Clear();
    public static void ClearUnknownRecipes() => s_unknownRecipes.Clear();

    public static readonly Dictionary<int, int> CraftingStationsItems = new(); // tile -> item
    public static readonly Dictionary<string, int> ConditionItems = new(); // description -> id

    public static Item guideTile = new();
    public static Item[] GuideItems {
        get {
            (s_guideItems[0], s_guideItems[1]) = (Main.guideItem, guideTile);
            return s_guideItems;
        }
        set => (Main.guideItem, guideTile) = (value[0], value[1]);
    }
    private static readonly Item[] s_guideItems = new Item[2];


    public static readonly Asset<Texture2D>[] DefaultTextures = [ TextureAssets.InventoryBack4, TextureAssets.InventoryBack14 ];
    public static readonly Asset<Texture2D>[] FavoriteTextures = [ TextureAssets.InventoryBack10, TextureAssets.InventoryBack17 ];
    public static readonly Asset<Texture2D>[] BlacklistedTextures = [ TextureAssets.InventoryBack5, TextureAssets.InventoryBack11 ];
    public static readonly Asset<Texture2D>[] TileTextures = [ TextureAssets.InventoryBack3, TextureAssets.InventoryBack6 ];
    public static readonly Asset<Texture2D>[] ConditionTextures = [ TextureAssets.InventoryBack12, TextureAssets.InventoryBack8 ];

    public static Asset<Texture2D> InventoryTickBorder => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Border");
    public static Asset<Texture2D> UnknownTexture => ModContent.Request<Texture2D>($"BetterInventory/Assets/Unknown_Item");

    private static Asset<Texture2D> s_inventoryBack4 = null!;

    private static bool s_collectingGuide;
    private static readonly RangeSet s_unknownRecipes = [];
    private static readonly RangeSet s_availableRecipes = [];

    private static Item s_dispGuide = new(), s_dispTile = new();


    private static bool s_visibilityHover;
    private static Rectangle s_hitBox;

    private static int s_textRecipe = -1;
    internal static readonly List<Item> textTiles = [];
    internal static readonly List<(Item item, Condition condition)> textConditions = [];

    private static bool s_hideNextItem;
    public static LocalizedText? forcedTooltip;

    public const int TilesPerLine = 7;
    public const float TileScale = 0.46f;
    public const float TileSpacingRatio = 0.08f;

    private static IEnumerator<int>? s_ilRecipes;
}

public enum PlaceholderType { None, ByHand, Tile, Condition}
public enum FavoriteState : byte { Default, Blacklisted, Favorited }
