using System;
using System.Collections.Generic;
using System.Linq;
using BetterInventory.DataStructures;
using BetterInventory.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.ItemSearch;


public sealed class BetterGuide : ILoadable {

    public static bool Enabled => Configs.ClientConfig.Instance.betterGuide;

    public static VisibilityFilters LocalFilters => Globals.BetterPlayer.LocalPlayer.VisibilityFilters;

    public void Load(Mod mod) {
        IL_Main.DrawInventory += IlDrawInventory;
        On_Main.DrawGuideCraftText += HookGuideCraftText;

        On_Main.HoverOverCraftingItemButton += HookOverrideCraftHover;

        IL_Recipe.FindRecipes += ILFindRecipes;
        On_Recipe.AddToAvailableRecipes += HookAddAvailableRecipe;
        IL_Recipe.CollectGuideRecipes += ILOverrideGuideRecipes;

        On_ItemSlot.Draw_SpriteBatch_refItem_int_Vector2_Color += HookHideItem;
        On_Main.DrawPendingMouseText += HookFakePendingTooltip;
        MonoModHooks.Add(typeof(ItemLoader).GetMethod(nameof(ItemLoader.ModifyTooltips)), HookHideTooltip);

        s_inventoryBack4 = TextureAssets.InventoryBack4;
    }

    public void Unload() => s_inventoryBack4 = null!;

    public static void PostAddRecipes() {
        for (int r = 0; r < Recipe.numRecipes; r++) {
            foreach (int tile in Main.recipe[r].requiredTile) CraftingStations[tile] = 0;
        }
        SetStations();
    }

    public static void SetStations() {
        for (int type = 0; type < ItemLoader.ItemCount; type++) {
            Item item = new(type);
            if (CraftingStations.ContainsKey(item.createTile) && CraftingStations[item.createTile] == ItemID.None) CraftingStations[item.createTile] = item.type;
        }
    }


    private void HookHideItem(On_ItemSlot.orig_Draw_SpriteBatch_refItem_int_Vector2_Color orig, SpriteBatch spriteBatch, ref Item inv, int context, Vector2 position, Color lightColor){
        Item? old = null;
        if (_hideItem) {
            _hideItem = false;
            old = inv;
            inv = UnknownItem.Instance;
        }
        orig(spriteBatch, ref inv, context, position, lightColor);
        if (old is not null) inv = old;
    }

    private static void IlDrawInventory(ILContext il) {
        ILCursor cursor = new(il);

        // ----- Force GuideItem display -----
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
        ILLabel? noHover = null;
        cursor.FindNext(out var cursors, i => i.MatchBlt(out noHover));

        // ----- Visibility Filters -----
        //         ++ if(<visibilityHover>) HandleVisibility();
        //         ++ else {
        cursor.EmitLdloc(124);
        cursor.EmitLdloc(125);
        cursor.EmitDelegate(HandleVisibility);
        cursor.EmitBrtrue(noHover!);

        //             <handle guide item slot>
        //         ++ }
        //         ItemSlot.Draw(Main.spriteBatch, ref Main.guideItem, ...);
        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //         ++ <drawVisibility>
        cursor.EmitDelegate(DrawVisibility);

        cursor.GotoLabel(endGuide!, MoveType.Before);

        //         ++ if(<alternateGuideDraw>) goto recipe;
        ILLabel recipe = cursor.DefineLabel();
        cursor.EmitDelegate(() => Enabled && !Main.InGuideCraftMenu);
        cursor.EmitBrtrue(recipe);
        //     }
        // }

        // ...
        // if(<showRecipes>){
        cursor.GotoNext(i => i.MatchStsfld(typeof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts), nameof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig)));
        cursor.GotoPrev(MoveType.AfterLabel);

        //     ++ if(<alternateGuideDraw>) goto guide;
        cursor.EmitDelegate(() => Enabled && !Main.InGuideCraftMenu);
        cursor.EmitBrtrue(guide);

        //     ++ recipe:
        cursor.MarkLabel(recipe);


        // ----- Recipe fast scroll -----
        //     for (<recipeIndex>) {
        //         ...
        for (int j = 0; j < 2; j++) { // Up and Down

            //     if(<scrool>) {
            //         if(...) SoundEngine.PlaySound(...);
            //         Main.availableRecipeY[num63] += 6.5f;
            cursor.GotoNext(i => i.MatchCall(typeof(SoundEngine), nameof(SoundEngine.PlaySound)));
            cursor.GotoNext(i => i.MatchLdsfld(typeof(Main), nameof(Main.recFastScroll)));

            // ++ <custom scroll>
            cursor.EmitLdloc(126);
            int s = j == 0 ? -1 : 1;
            cursor.EmitDelegate((int r) => {
                Main.availableRecipeY[r] += s * 6.5f;
                float d = Main.availableRecipeY[r] - (r - Main.focusRecipe)*65;
                if (Main.recFastScroll) {
                    Main.availableRecipeY[r] += 130000f * s;
                    d *= 3;
                }
                // Main.recFastScroll = false;
                Main.availableRecipeY[r] -= s == 1 ? MathF.Max(s*6.5f, d/10) : MathF.Min(s*6.5f, d/10);
            });

            //         if (Main.recFastScroll) ...
            //         ...
            //     }
        }

        // ----- Recipes createItem -----
        //         ...
        //         if(<visible>) {
        //             ...
        //             if (Main.numAvailableRecipes > 0) {
        //                 ...
        //                 Main.inventoryBack = ...;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "HoverOverCraftingItemButton"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //                 ++ <overrideBackground>
        cursor.EmitLdloc(126);
        cursor.EmitDelegate((int i) => {
            if (!Enabled) return;
            OverrideRecipeTexture(LocalFilters.FavoriteRecipes.GetValueOrDefault(Main.availableRecipe[i]), ItemSlot.DrawGoldBGForCraftingMaterial, AvailableRecipes.Contains(Main.availableRecipe[i]));
            ItemSlot.DrawGoldBGForCraftingMaterial = false;
            if (!KnownRecipes.Contains(Main.availableRecipe[i])) _hideItem = true;
        });

        //                 ItemSlot.Draw(...);
        cursor.GotoNext(MoveType.After, i => true);

        //                 ++ <restoreBackground>
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = s_inventoryBack4; });
        //             }
        //         }
        //     }

        // ----- Unknown recipes -----
        //     if (++<known> && Main.numAvailableRecipes > 0) {
        cursor.GotoNext(MoveType.After, i => i.MatchLdsfld(Reflection.Main.numAvailableRecipes));
        cursor.EmitDelegate((int num) => num == 0 || KnownRecipes.Contains(Main.availableRecipe[Main.focusRecipe]) ? num : 0);

        // ----- Material wrapping -----
        //         for (<focusRecipeMaterialIndex>) {
        //             ...
        //             int num69 = 80 + num68 * 40;
        //             int num70 = 380 + num51;
        cursor.GotoNext(MoveType.Before, i => i.MatchStloc(133));
        
        //             ++ <wrapping>
        cursor.EmitLdloc(132);
        cursor.EmitDelegate((int x, int i) => {
            if (!Enabled) return x;
            if (!Main.recBigList) return x - 2*i;
            x -= i*40;
            if(i >= MaterialsPerLine[0]) i = MaterialsPerLine[0]-MaterialsPerLine[1]  + (i - MaterialsPerLine[0]) % MaterialsPerLine[1];
            return x+38*i;
        });
        
        cursor.GotoNext(MoveType.Before, i => i.MatchStloc(134));
        cursor.EmitLdloc(132);
        cursor.EmitDelegate((int y, int i) => {
            if (!Enabled || !Main.recBigList) return y;
            i = i < MaterialsPerLine[0] ? 0 : ((i - MaterialsPerLine[0]) / MaterialsPerLine[1] + 1);
            return y + 38 * i;
        });

        // ----- Recipe requiredItems -----
        //             ...
        //             Item tempItem = ...;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "SetRecipeMaterialDisplayName"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //             ++ <overrideBackground>
        cursor.EmitLdloc(132);
        cursor.EmitDelegate((int matI) => {
            if (!Enabled) return;
            bool canCraft = AvailableRecipes.Contains(Main.availableRecipe[Main.focusRecipe]);
            FavoriteState state = LocalFilters.FavoriteRecipes.GetValueOrDefault(Main.availableRecipe[Main.focusRecipe]);
            if (!canCraft) {
                Item material = Main.recipe[Main.availableRecipe[Main.focusRecipe]].requiredItem[matI];
                canCraft = Utility.OwnedItems.GetValueOrDefault(material.type, 0) >= material.stack;
            }
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

        // ----- recBigList Scroll ----- 
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
                if (!Enabled || !Main.mouseLeft) return;
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
        cursor.GotoPrev(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));
        ILLabel? noClick = null;
        cursor.GotoPrev(i => i.MatchBrtrue(out noClick));
        cursor.GotoNext(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));

        //             ++ if(<enabled>) goto noClick;
        cursor.EmitLdloc(155);
        cursor.EmitDelegate((int i) => {
            if (!Enabled) return false;
            Reflection.Main.HoverOverCraftingItemButton.Invoke(i);
            Main.recFastScroll = true;
            Main.craftingHide = false;
            return true;
        });
        cursor.EmitBrtrue(noClick!);
        //             if(<click>) <scrollList>
        //             ...
        //         }
        //         ++ noClick:

        // ----- Recipe big list -----
        //         if (Main.numAvailableRecipes > 0) {
        //             ...
        //             Main.inventoryBack = ...;
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //             ++ <overrideBackground>
        cursor.EmitLdloc(155);
        cursor.EmitDelegate((int i) => {
            if (!Enabled) return;
            OverrideRecipeTexture(LocalFilters.FavoriteRecipes.GetValueOrDefault(Main.availableRecipe[i]), Main.focusRecipe == i, AvailableRecipes.Contains(Main.availableRecipe[i]));
            if (!KnownRecipes.Contains(Main.availableRecipe[i])) _hideItem = true;
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


    private void HookGuideCraftText(On_Main.orig_DrawGuideCraftText orig, int adjY, Color craftingTipColor, out int inventoryX, out int inventoryY) {
        if (!Enabled) {
            orig(adjY, craftingTipColor, out inventoryX, out inventoryY);
            return;
        }
        inventoryX = 73;
        inventoryY = 331 + adjY;
        bool hideText = !KnownRecipes.Contains(Main.availableRecipe[Main.focusRecipe]);

        List<string> conditions = new();
        Recipe recipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];

        if (recipe.RecipeIndex != _focusRecipe) {
            _focusRecipe = recipe.RecipeIndex;
            _craftingTiles = new Item[recipe.requiredTile.Count];
            for (int i = 0; i < recipe.requiredTile.Count; i++) {
                if (recipe.requiredTile[i] == -1) break;
                else if (CraftingStations.TryGetValue(recipe.requiredTile[i], out int type) && type != ItemID.None) _craftingTiles[i] = new(type);
            }
        }

        Vector2 position;
        if (!hideText) {
            Main.inventoryScale *= 0.5f;
            position = new(inventoryX + 50, inventoryY + 12 - 14 + 24);
            for (int i = 0; i < recipe.requiredTile.Count; i++) {
                if (recipe.requiredTile[i] == -1) break;
                if (_craftingTiles[i] is null) {
                    string mapObjectName = Lang.GetMapObjectName(MapHelper.TileToLookup(recipe.requiredTile[i], Recipe.GetRequiredTileStyle(recipe.requiredTile[i])));
                    conditions.Add(mapObjectName);
                    continue;
                }
                Color inventoryBack = Main.inventoryBack;
                OverrideRecipeTexture(FavoriteState.Default, false, Main.LocalPlayer.adjTile[recipe.requiredTile[i]]);
                ItemSlot.Draw(Main.spriteBatch, ref _craftingTiles[i], ItemSlot.Context.CraftingMaterial, position);
                TextureAssets.InventoryBack4 = s_inventoryBack4;
                float size = TextureAssets.InventoryBack.Width() * Main.inventoryScale;
                if (new Rectangle((int)position.X, (int)position.Y, (int)size, (int)size).Contains(Main.mouseX, Main.mouseY)) {
                    Main.LocalPlayer.mouseInterface = true;
                    ItemSlot.MouseHover(ref _craftingTiles[i], ItemSlot.Context.CraftingMaterial);
                }
                Main.inventoryBack = inventoryBack;
                position.X += size * 1.1f;
            }
            Main.inventoryScale *= 2;

            if (Reflection.Recipe.needWater.GetValue(recipe)) conditions.Add(Lang.inter[53].Value);
            if (Reflection.Recipe.needHoney.GetValue(recipe)) conditions.Add(Lang.inter[58].Value);
            if (Reflection.Recipe.needLava.GetValue(recipe)) conditions.Add(Lang.inter[56].Value);
            if (Reflection.Recipe.needSnowBiome.GetValue(recipe)) conditions.Add(Lang.inter[123].Value);
            if (Reflection.Recipe.needGraveyardBiome.GetValue(recipe)) conditions.Add(Lang.inter[124].Value);
            conditions.AddRange(from x in recipe.Conditions select x.Description.Value);
            position = new(inventoryX + 50, inventoryY + 12 - 14);
        } else {
            conditions.Add("???");
            position = new(inventoryX + 50, inventoryY + 12);
        }
        if (conditions.Count > 0)
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, string.Join(", ", conditions), position, new(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, Vector2.Zero, 1f, 0, 0f);



    }


    public static bool HandleVisibility(int x, int y) {
        x += (int)((TextureAssets.InventoryBack.Width() - 2) * Main.inventoryScale);
        y += (int)(4 * Main.inventoryScale);
        Vector2 size = InventoryTickBorder.Size() * Main.inventoryScale;
        _hitBox = new(x - (int)(size.X / 2f), y - (int)(size.Y / 2f), (int)size.X, (int)size.Y);

        if (!(_hover = _hitBox.Contains(Main.mouseX, Main.mouseY)) || PlayerInput.IgnoreMouseInterface) return false;

        Main.player[Main.myPlayer].mouseInterface = true;
        if (Main.mouseLeft && Main.mouseLeftRelease) {
            VisibilityFilters filters = LocalFilters;
            if (filters.TileMode) filters.TileMode = false;
            else {
                filters.ShowAllRecipes = !filters.ShowAllRecipes;
                if (VisibilityFilters.CurrentVisibility != VisibilityFilters.Flags.ShowAllGuide && filters.ShowAllRecipes) filters.TileMode = true;
            }
            SoundEngine.PlaySound(SoundID.MenuTick);
            Recipe.FindRecipes();
        }
        return true;
    }
    public static void DrawVisibility() {
        VisibilityFilters filters = LocalFilters;
        Asset<Texture2D> tick = filters.TileMode ? InventoryTickForced : filters.ShowAllRecipes ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff;
        Color color = Color.White * 0.7f;
        Main.spriteBatch.Draw(tick.Value, _hitBox.Center(), null, color, 0f, tick.Value.Size() / 2, Main.inventoryScale, 0, 0f);
        if (_hover) {
            string key = filters.TileMode ? "Mods.BetterInventory.UI.ShowTile" : filters.ShowAllRecipes ? "Mods.BetterInventory.UI.ShowAll" : "Mods.BetterInventory.UI.ShowAvailable";
            Main.instance.MouseText(Language.GetTextValue(key));
            Main.spriteBatch.Draw(InventoryTickBorder.Value, _hitBox.Center(), null, color, 0f, InventoryTickBorder.Value.Size() / 2, Main.inventoryScale, 0, 0f);
        }
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
        cursor.EmitDelegate<Func<int, bool>>(i => {
            if (!Enabled) return false;
            Recipe recipe = Main.recipe[i];

            if (LocalFilters.TileMode) {
                if (Main.guideItem.IsAir ? recipe.requiredTile.Count == 0 : recipe.HasTile(Main.guideItem.createTile)) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
            }
            else if (Main.guideItem.IsAir) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
            else if (Main.recipe[i].HasResult(Main.guideItem.type)) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
            else return false;
            return true;
        });
        cursor.EmitBrtrue(endLoop!);
        // }
    }

    private void ILFindRecipes(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // Recipe.ClearAvailableRecipes()
        cursor.GotoNext(MoveType.After, i => i.MatchCall(Reflection.Recipe.ClearAvailableRecipes));

        // ++ if (Enabled) goto skipGuide;
        ILLabel skipGuide = cursor.DefineLabel();
        cursor.EmitDelegate(() => {
            if (!Enabled) return false;
            AvailableRecipes.Clear();
            return _collectingAvaiblable = true;
        });
        cursor.EmitBrtrue(skipGuide);

        // <guideRecipes>
        cursor.GotoNext(i => i.MatchCall(Reflection.Recipe.CollectItemsToCraftWithFrom));

        // ++ skipGuide:
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());
        cursor.MarkLabel(skipGuide);
        // Player localPlayer = Main.LocalPlayer;
        // Recipe.CollectItemsToCraftWithFrom(localPlayer);
        // ...

        cursor.GotoNext(i => i.MatchCall(Reflection.Recipe.TryRefocusingRecipe));
        cursor.EmitDelegate(() => {
            GuideRecipes.Clear();
            KnownRecipes.Clear();
            List<int> unknownRecipes = new();
            if (!Enabled) {
                KnownRecipes.AddRange(Main.availableRecipe[..Main.numAvailableRecipes]);
                GuideRecipes.AddRange(Main.availableRecipe[..Main.numAvailableRecipes]);
                return;
            }
            _collectingAvaiblable = false;
            Recipe.ClearAvailableRecipes();
            Reflection.Recipe.CollectGuideRecipes.Invoke();
            VisibilityFilters filters = LocalFilters;
            for (int i = 0; i < Main.numAvailableRecipes; i++) {
                Recipe recipe = Main.recipe[Main.availableRecipe[i]];
                bool known = Configs.ClientConfig.Instance.unknownBehaviour == Configs.UnknownSearchBehaviour.Known || filters.HasOwnedItems(recipe.createItem) || recipe.requiredItem.Exists(i => filters.HasOwnedItems(i));
                if (!known)
                {
                    foreach (int g in recipe.acceptedGroups) {
                        foreach (int type in RecipeGroup.recipeGroups[g].ValidItems) {
                            if (!filters.HasOwnedItems(type)) continue;
                            known = true;
                            goto known;
                        }
                    }
                }
            known:
                if (known) {
                    GuideRecipes.Add(Main.availableRecipe[i]);
                    KnownRecipes.Add(Main.availableRecipe[i]);
                } else if (Configs.ClientConfig.Instance.unknownBehaviour != Configs.UnknownSearchBehaviour.Hidden) unknownRecipes.Add(Main.availableRecipe[i]);
            }
            GuideRecipes.AddRange(unknownRecipes);
        });
    }

    private static void HookAddAvailableRecipe(On_Recipe.orig_AddToAvailableRecipes orig, int recipeIndex) {
        if (_collectingAvaiblable) AvailableRecipes.Add(recipeIndex);
        orig(recipeIndex);
    }


    public static IEnumerable<int> GetRecipesInOrder(){
        if(!Enabled) {
            foreach (int i in GuideRecipes) yield return i;
        } else {
            List<int> fav = new(), black = new(), others = new();

            for (int i = 0; i < KnownRecipes.Count; i++) {
                (LocalFilters.FavoriteRecipes.GetValueOrDefault(GuideRecipes[i]) switch {
                    FavoriteState.Favorited => fav,
                    FavoriteState.Blacklisted => black,
                    _ => others,
                }).Add(GuideRecipes[i]);
            }

            bool showAll = LocalFilters.ShowAllRecipes;
            foreach (int i in fav) yield return i;
            foreach (int i in others) if (showAll || AvailableRecipes.Contains(i)) yield return i;
            if (showAll) {
                foreach (int i in black) yield return i;
                for (int i = KnownRecipes.Count; i < GuideRecipes.Count; i++) yield return GuideRecipes[i];
            }
        }
    }


    private static void HookOverrideCraftHover(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (!Enabled) {
            orig(recipeIndex);
            return;
        }
        if (!KnownRecipes.Contains(Main.availableRecipe[recipeIndex])) _unknownTooltip = true;
        if(_unknownTooltip && recipeIndex != Main.focusRecipe) orig(recipeIndex);
        else if (_unknownTooltip || OverrideRecipeHover(recipeIndex) || (recipeIndex == Main.focusRecipe && !AvailableRecipes.Contains(Main.availableRecipe[recipeIndex]))) {
            (bool lstate, bool rstate) = (Main.mouseLeft, Main.mouseRight);
            (Main.mouseLeft, Main.mouseRight) = (false, false);
            orig(recipeIndex);
            (Main.mouseLeft, Main.mouseRight) = (lstate, rstate);
        } else orig(recipeIndex);
        
        if(_unknownTooltip) {
            Main.HoverItem.TurnToAir();
            Main.hoverItemName = "???";
        }
        
    }
    private void HookFakePendingTooltip(On_Main.orig_DrawPendingMouseText orig) {
        if(Main.gameMenu) {
            orig();
            return;
        }
        Item old = Main.HoverItem;
        if ((bool)Reflection.Main._mouseTextCache_isValid.GetValue(Reflection.Main._mouseTextCache.GetValue(Main.instance))! && _unknownTooltip) Main.HoverItem = UnknownItem.Instance;
        orig();
        Main.HoverItem = old;
        _unknownTooltip = false;
    }

    public delegate List<TooltipLine> ModifyTooltipsFn(Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex);
    private static List<TooltipLine> HookHideTooltip(ModifyTooltipsFn orig, Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex) {
        if (!_unknownTooltip) return orig.Invoke(item, ref numTooltips, names, ref text, ref modifier, ref badModifier, ref oneDropLogo, out overrideColor, prefixlineIndex);
        List<TooltipLine> tooltips = new() { new(BetterInventory.Instance, names[0], "???") };
        numTooltips = 1;
        text = new string[] { tooltips[0].Text
        };
        modifier = new bool[] { tooltips[0].IsModifier };
        badModifier = new bool[] { tooltips[0].IsModifierBad };
        oneDropLogo = -1;
        overrideColor = new Color?[] { null };
        return tooltips;
    }
    public static bool OverrideRecipeHover(int recipeIndex) {
        bool click = Main.mouseLeft && Main.mouseLeftRelease;
        Dictionary<int, FavoriteState> favorites = LocalFilters.FavoriteRecipes;
        if (Main.keyState.IsKeyDown(Main.FavoriteKey)) {
            Main.cursorOverride = CursorOverrideID.FavoriteStar;
            if (click) {
                FavoriteState state = favorites.GetValueOrDefault(Main.availableRecipe[recipeIndex]);
                if (state == FavoriteState.Favorited) favorites.Remove(Main.availableRecipe[recipeIndex]);
                else favorites[Main.availableRecipe[recipeIndex]] = FavoriteState.Favorited;
                Recipe.FindRecipes(true);
                return true;
            }
        } else if (ItemSlot.ControlInUse) {
            Main.cursorOverride = CursorOverrideID.TrashCan;
            if (click) {
                FavoriteState state = favorites.GetValueOrDefault(Main.availableRecipe[recipeIndex]);
                if (state == FavoriteState.Blacklisted) favorites.Remove(Main.availableRecipe[recipeIndex]);
                else favorites[Main.availableRecipe[recipeIndex]] = FavoriteState.Blacklisted;
                Recipe.FindRecipes(true);
                return true;
            }
        }
        return false;
    }

    public static void OverrideRecipeTexture(FavoriteState state, bool selected, bool canCraft) {
        TextureAssets.InventoryBack4 = (selected ? SelectedRecipeTextures : DefaultRecipeTextures)[(int)state];
        if (!canCraft) Main.inventoryBack.ApplyRGB(0.5f);
    }

    public static Asset<Texture2D> InventoryTickBorder => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Border");
    public static Asset<Texture2D> InventoryTickForced => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Forced");
    public static Asset<Texture2D> InventoryTickGreen => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Green");

    private static bool _hover;
    private static Rectangle _hitBox;

    public static readonly List<int> GuideRecipes = new();
    public static readonly RangeSet KnownRecipes = new();
    public static readonly HashSet<int> AvailableRecipes = new();
    public static readonly Dictionary<int, int> CraftingStations = new();

    public static readonly Asset<Texture2D>[] DefaultRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack4, TextureAssets.InventoryBack5, TextureAssets.InventoryBack10 };
    public static readonly Asset<Texture2D>[] SelectedRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack14, TextureAssets.InventoryBack11, TextureAssets.InventoryBack17 };

    private static int _recDelay = 0;
    private static Asset<Texture2D> s_inventoryBack4 = null!;

    private static int _focusRecipe;
    private static Item?[] _craftingTiles = System.Array.Empty<Item>();

    private static bool _collectingAvaiblable;
    private static bool _unknownTooltip;
    private static bool _hideItem;

    public static readonly int[] MaterialsPerLine = new int[] { 6, 4 };

}