using System;
using System.Collections.Generic;
using System.Linq;
using BetterInventory.Crafting;
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
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.ItemSearch;


public sealed class Guide : ModSystem {
    public static bool Enabled => Configs.ItemSearch.Instance.betterGuide;
    public static Configs.BetterGuide Config => Configs.ItemSearch.Instance.betterGuide.Value;

    public static VisibilityFilters LocalFilters => InventoryManagement.BetterPlayer.LocalPlayer.VisibilityFilters;

    public override void Load() {
        On_Main.DrawGuideCraftText += HookGuideCraftText;
        IL_Recipe.CollectGuideRecipes += ILOverrideGuideRecipes;

        IL_Main.DrawInventory += IlDrawInventory;

        IL_Recipe.FindRecipes += ILFindRecipes;
        IL_Main.HoverOverCraftingItemButton += ILOverrideCraftHover;

        On_ItemSlot.PickItemMovementAction += HookAllowGuideItem;
        On_ItemSlot.OverrideHover_ItemArray_int_int += HookOverrideHover;
        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;

        On_ItemSlot.Draw_SpriteBatch_refItem_int_Vector2_Color += HookHideItem;
        MonoModHooks.Add(typeof(ItemLoader).GetMethod(nameof(ItemLoader.ModifyTooltips)), HookHideTooltip);

        s_inventoryBack4 = TextureAssets.InventoryBack4;
    }

    public override void Unload() => s_inventoryBack4 = null!;

    public override void PostAddRecipes() {
        for (int r = 0; r < Recipe.numRecipes; r++) {
            foreach (int tile in Main.recipe[r].requiredTile) CraftingStations[tile] = 0;
        }
        for (int type = 0; type < ItemLoader.ItemCount; type++) {
            Item item = new(type);
            if (CraftingStations.ContainsKey(item.createTile) && CraftingStations[item.createTile] == ItemID.None) CraftingStations[item.createTile] = item.type;
        }
    }


    private void HookGuideCraftText(On_Main.orig_DrawGuideCraftText orig, int adjY, Color craftingTipColor, out int inventoryX, out int inventoryY) {
        if (!Enabled) {
            orig(adjY, craftingTipColor, out inventoryX, out inventoryY);
            return;
        }
        inventoryX = 73;
        inventoryY = 331 + adjY;

        Recipe recipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];

        if (recipe.RecipeIndex != s_focusRecipe) {
            s_craftingTiles.Clear();
            s_craftingConditions.Clear();
            if (Main.numAvailableRecipes != 0 && !IsUnknown(Main.availableRecipe[Main.focusRecipe])) {
                s_focusRecipe = recipe.RecipeIndex;
                if (recipe.requiredTile.Count == 0) {
                    s_craftingTiles.Add(new(ItemID.HandOfCreation));
                } else {
                    for (int i = 0; i < recipe.requiredTile.Count && recipe.requiredTile[i] != -1; i++) {
                        if (CraftingStations.TryGetValue(recipe.requiredTile[i], out int type) && type != ItemID.None) s_craftingTiles.Add(new(type));
                        else {
                            string mapObjectName = Lang.GetMapObjectName(MapHelper.TileToLookup(recipe.requiredTile[i], Recipe.GetRequiredTileStyle(recipe.requiredTile[i])));
                            s_craftingConditions.Add(mapObjectName);
                        }
                    }
                }
                if (Reflection.Recipe.needWater.GetValue(recipe)) s_craftingConditions.Add(Lang.inter[53].Value);
                if (Reflection.Recipe.needHoney.GetValue(recipe)) s_craftingConditions.Add(Lang.inter[58].Value);
                if (Reflection.Recipe.needLava.GetValue(recipe)) s_craftingConditions.Add(Lang.inter[56].Value);
                if (Reflection.Recipe.needSnowBiome.GetValue(recipe)) s_craftingConditions.Add(Lang.inter[123].Value);
                if (Reflection.Recipe.needGraveyardBiome.GetValue(recipe)) s_craftingConditions.Add(Lang.inter[124].Value);
                s_craftingConditions.AddRange(from x in recipe.Conditions select x.Description.Value);
            }
        }

        Vector2 position = new(inventoryX + 50, inventoryY + 12 - 14 + 24);
        Main.inventoryScale *= 0.5f;
        for (int i = 0; i < s_craftingTiles.Count; i++) { // TODO wrapping
            Item tile = s_craftingTiles[i];
            Color inventoryBack = Main.inventoryBack;
            OverrideRecipeTexture(FavoriteState.Default, false, tile.type == ItemID.HandOfCreation || Main.LocalPlayer.adjTile[tile.createTile]);
            ItemSlot.Draw(Main.spriteBatch, ref tile, ContextID.CraftingMaterial, position);
            TextureAssets.InventoryBack4 = s_inventoryBack4;
            float size = TextureAssets.InventoryBack.Width() * Main.inventoryScale;
            if (new Rectangle((int)position.X, (int)position.Y, (int)size, (int)size).Contains(Main.mouseX, Main.mouseY)) {
                Main.LocalPlayer.mouseInterface = true;
                ItemSlot.MouseHover(ref tile, ContextID.CraftingMaterial);
            }
            Main.inventoryBack = inventoryBack;
            position.X += size * 1.1f;
        }
        Main.inventoryScale *= 2;
        position = new(inventoryX + 50, inventoryY + 12 - 14);

        if (s_craftingConditions.Count > 0) // TODO use Slots
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, string.Join(", ", s_craftingConditions), position, new(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, Vector2.Zero, 1f, 0, 0f);
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
            if (Main.recipe[i].HasResult(Main.guideItem.type)) {
                Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
                return true;
            }
            if (Main.guideItem.type == ItemID.None) {
                Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
                return true;
            }
            return false;
        });
        cursor.EmitBrtrue(endLoop!);
        // }
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
        //         ++ if(<visibilityHover>) HandleVisibility();
        //         ++ else {
        cursor.EmitLdloc(122);
        cursor.EmitLdloc(123);
        cursor.EmitDelegate(HandleVisibility);
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
            if (!BetterCrafting.Config.focusRecipe && Main.focusRecipe == i) ItemSlot.DrawGoldBGForCraftingMaterial = true;
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
    public static bool HandleVisibility(int x, int y) {
        if (!Enabled || !Config.craftInMenu) return false;
        x += (int)((TextureAssets.InventoryBack.Width() - 2) * Main.inventoryScale);
        y += (int)(4 * Main.inventoryScale);
        Vector2 size = InventoryTickBorder.Size() * Main.inventoryScale;
        s_hitBox = new(x - (int)(size.X / 2f), y - (int)(size.Y / 2f), (int)size.X, (int)size.Y);

        if (!(s_hover = s_hitBox.Contains(Main.mouseX, Main.mouseY)) || PlayerInput.IgnoreMouseInterface) return false;

        Main.player[Main.myPlayer].mouseInterface = true;
        if (Main.mouseLeft && Main.mouseLeftRelease) {
            LocalFilters.ShowAllRecipes = !LocalFilters.ShowAllRecipes;
            SoundEngine.PlaySound(SoundID.MenuTick);
            Recipe.FindRecipes();
        }
        return true;
    }
    public static void DrawVisibility() {
        if (!Enabled || !Config.craftInMenu) return;
        VisibilityFilters filters = LocalFilters;
        Asset<Texture2D> tick = filters.ShowAllRecipes ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff;
        Color color = Color.White * 0.7f;
        Main.spriteBatch.Draw(tick.Value, s_hitBox.Center(), null, color, 0f, tick.Value.Size() / 2, Main.inventoryScale, 0, 0f);
        if (s_hover) {
            string key = filters.ShowAllRecipes ? "Mods.BetterInventory.UI.ShowAll" : "Mods.BetterInventory.UI.ShowAvailable";
            Main.instance.MouseText(Language.GetTextValue(key));
            Main.spriteBatch.Draw(InventoryTickBorder.Value, s_hitBox.Center(), null, color, 0f, InventoryTickBorder.Value.Size() / 2, Main.inventoryScale, 0, 0f);
        }
    }


    public static IEnumerable<Item> AddMaterials(out ModPlayer.ItemConsumedCallback itemConsumedCallback) {
        itemConsumedCallback = (item, amount) => Main.guideItem.stack -= amount;
        if (!Enabled || SearchItem.Config.searchRecipes) return null!;
        return new Item[] { Main.guideItem };
    }
    private void ILFindRecipes(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // Recipe.ClearAvailableRecipes()
        cursor.GotoNext(MoveType.After, i => i.MatchCall(Reflection.Recipe.ClearAvailableRecipes));

        // ++ <skipGuide>
        ILLabel skipGuide = cursor.DefineLabel();
        cursor.EmitDelegate(() => Enabled);
        cursor.EmitBrtrue(skipGuide);


        // if(<guideItem>) {
        //     <guideRecipes>
        // }
        cursor.GotoNext(i => i.MatchCall(Reflection.Recipe.CollectItemsToCraftWithFrom));

        // ++ skipGuide:
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());
        cursor.MarkLabel(skipGuide);

        // Player localPlayer = Main.LocalPlayer;
        // Recipe.CollectItemsToCraftWithFrom(localPlayer);
        // <availableRecipes>
        cursor.GotoNext(i => i.MatchCall(Reflection.Recipe.TryRefocusingRecipe));

        cursor.EmitDelegate(() => {
            if (Enabled) {
                s_availableRecipes = new(Main.availableRecipe[..Main.numAvailableRecipes]);
                Recipe.ClearAvailableRecipes();
                Reflection.Recipe.CollectGuideRecipes.Invoke();

                s_unknownRecipes.Clear();
                List<int> knownRecipes = new();
                if (Configs.ItemSearch.Instance.unknownDisplay != Configs.ItemSearch.UnknownDisplay.Known) {
                    VisibilityFilters filters = LocalFilters;
                    for (int i = 0; i < Main.numAvailableRecipes; i++) {
                        if (filters.IsKnownRecipe(Main.recipe[Main.availableRecipe[i]])) knownRecipes.Add(Main.availableRecipe[i]);
                        else if (Configs.ItemSearch.Instance.unknownDisplay == Configs.ItemSearch.UnknownDisplay.Unknown) s_unknownRecipes.Add(Main.availableRecipe[i]);
                    }
                } else knownRecipes.AddRange(Main.availableRecipe[..Main.numAvailableRecipes]);

                List<int> fav = new(), black = new(), others = new();
                if (Config.favoriteRecipes) {
                    foreach (int i in knownRecipes) {
                        (LocalFilters.GetFavoriteState(i) switch {
                            FavoriteState.Favorited => fav,
                            FavoriteState.Blacklisted => black,
                            _ => others,
                        }).Add(i);
                    }
                } else others = knownRecipes;

                Recipe.ClearAvailableRecipes();
                bool showAll = Config.craftInMenu ? LocalFilters.ShowAllRecipes : !Main.guideItem.IsAir;
                foreach (int i in fav) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
                foreach (int i in others) if (showAll || s_availableRecipes.Contains(i)) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
                if (showAll) {
                    foreach (int i in black) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
                    foreach (int i in s_unknownRecipes) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
                }
            }
        });
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
            if (IsUnknown(Main.availableRecipe[recipeIndex])) s_hideNextTooltip = true;
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
            BetterCrafting.OverrideHover(recipe);
            return false;
        });
        //     ...
        // }

        cursor.GotoNext(i => i.MatchStsfld(Reflection.Main.craftingHide));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdcI4(1));
        cursor.MarkLabel(skip);

    }


    private int HookAllowGuideItem(On_ItemSlot.orig_PickItemMovementAction orig, Item[] inv, int context, int slot, Item checkItem) {
        if (Enabled && context == ContextID.GuideItem) return 0;
        return orig(inv, context, slot, checkItem);
    }
    private static void HookOverrideHover(On_ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (SearchItem.OverrideHover(inv, context, slot)) return;
        if (context == ContextID.InventoryItem && ItemSlot.ShiftInUse && !ItemSlot.ShiftForcedOn && Main.InGuideCraftMenu && !inv[slot].IsAir && ItemSlot.PickItemMovementAction(inv, ContextID.GuideItem, 0, inv[slot]) == 0) Main.cursorOverride = CursorOverrideID.InventoryToChest;
        else orig(inv, context, slot);
    }
    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if (Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.InventoryToChest) {
            (Item mouse, Main.mouseItem, inv[slot]) = (Main.mouseItem, inv[slot], new());
            (int cursor, Main.cursorOverride) = (Main.cursorOverride, 0);
            ItemSlot.LeftClick(ref Main.guideItem, ContextID.GuideItem);
            if (Enabled) Main.LocalPlayer.GetDropItem(ref Main.mouseItem);
            else inv[slot] = Main.mouseItem;
            Main.mouseItem = mouse;
            Main.cursorOverride = cursor;
            return true;
        }
        if (SearchItem.OverrideLeftClick(inv, context, slot)) return true;
        return orig(inv, context, slot);
    }

    public delegate List<TooltipLine> ModifyTooltipsFn(Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex);
    private void HookHideItem(On_ItemSlot.orig_Draw_SpriteBatch_refItem_int_Vector2_Color orig, SpriteBatch spriteBatch, ref Item inv, int context, Vector2 position, Color lightColor) {
        if (s_hideNextItem) {
            Item item = UnknownItem.Instance;
            orig(spriteBatch, ref item, context, position, lightColor);
            s_hideNextItem = false;
        } else {
            orig(spriteBatch, ref inv, context, position, lightColor);
        }
    }
    private static List<TooltipLine> HookHideTooltip(ModifyTooltipsFn orig, Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex) {
        if (!s_hideNextTooltip) return orig.Invoke(item, ref numTooltips, names, ref text, ref modifier, ref badModifier, ref oneDropLogo, out overrideColor, prefixlineIndex);
        s_hideNextTooltip = false;
        List<TooltipLine> tooltips = new() { new(BetterInventory.Instance, names[0], "???") };
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
        s_hideNextTooltip = true;
    }


    public static FavoriteState GetFavoriteState(int recipe) => Config.favoriteRecipes ? LocalFilters.GetFavoriteState(recipe) : FavoriteState.Default;
    public static bool IsUnknown(int recipe) => /* Config.progression && */ s_unknownRecipes.Contains(recipe);

    public static void OverrideRecipeTexture(FavoriteState state, bool selected, bool available) {
        TextureAssets.InventoryBack4 = (selected ? SelectedRecipeTextures : DefaultRecipeTextures)[(int)state];
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

    public static readonly Dictionary<int, int> CraftingStations = new();

    public static readonly Asset<Texture2D>[] DefaultRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack4, TextureAssets.InventoryBack5, TextureAssets.InventoryBack10 };
    public static readonly Asset<Texture2D>[] SelectedRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack14, TextureAssets.InventoryBack11, TextureAssets.InventoryBack17 };

    public static Asset<Texture2D> InventoryTickBorder => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Border");
    public static Asset<Texture2D> InventoryTickForced => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Forced");
    public static Asset<Texture2D> InventoryTickGreen => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Green");


    private static Asset<Texture2D> s_inventoryBack4 = null!;

    private static readonly RangeSet s_unknownRecipes = new();
    private static RangeSet s_availableRecipes = new();

    private static bool s_hover;
    private static Rectangle s_hitBox;

    private static int s_focusRecipe;
    private static readonly List<Item> s_craftingTiles = new();
    private static readonly List<string> s_craftingConditions = new();

    private static bool s_hideNextItem, s_hideNextTooltip;

    public static readonly int[] InventoryContexts = new int[] { ContextID.InventoryItem, ContextID.InventoryAmmo, ContextID.InventoryCoin };
}