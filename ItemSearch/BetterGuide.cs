using System.Collections.Generic;
using System.Linq;
using BetterInventory.Crafting;
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

    public static Filters LocalFilters => Globals.BetterPlayer.LocalPlayer.RecipeFilters;

    public void Load(Mod mod){
        IL_Main.DrawInventory += IlDrawInventory;
        On_Main.DrawGuideCraftText += HookGuideText;

        On_Main.HoverOverCraftingItemButton += HookOverrideCraftHover;

        IL_Recipe.FindRecipes += ILFindRecipes;
        On_Recipe.AddToAvailableRecipes += HookAddAvailableRecipe;
        IL_Recipe.CollectGuideRecipes += ILOverrideGuideRecipes;

        s_inventoryBack4 = TextureAssets.InventoryBack4;
    }

    public void Unload() => s_inventoryBack4 = null!;

    private void HookGuideText(On_Main.orig_DrawGuideCraftText orig, int adjY, Color craftingTipColor, out int inventoryX, out int inventoryY) {
        if(!Enabled) {
            orig(adjY, craftingTipColor, out inventoryX, out inventoryY);
            return;
        }
        inventoryX = 73;
        inventoryY = 331 + adjY;
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

        Main.inventoryScale *= 0.5f;
        Vector2 position = new(inventoryX + 50, inventoryY + 12 - 14 + 24);
        for (int i = 0; i < recipe.requiredTile.Count; i++) {
            if (recipe.requiredTile[i] == -1) break;
            if(_craftingTiles[i] is null) {
                string mapObjectName = Lang.GetMapObjectName(MapHelper.TileToLookup(recipe.requiredTile[i], Recipe.GetRequiredTileStyle(recipe.requiredTile[i])));
                conditions.Add(mapObjectName);
                continue;
            }
            Color inventoryBack = Main.inventoryBack;
            OverrideRecipeTexture(FavoriteState.Default, false, Main.LocalPlayer.adjTile[recipe.requiredTile[i]]);
            ItemSlot.Draw(Main.spriteBatch, ref _craftingTiles[i], ItemSlot.Context.CraftingMaterial, position);
            TextureAssets.InventoryBack4 = s_inventoryBack4;
            float size = TextureAssets.InventoryBack.Width() * Main.inventoryScale;
            if(new Rectangle((int)position.X, (int)position.Y, (int)size, (int)size).Contains(Main.mouseX, Main.mouseY)){
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
        if (conditions.Count > 0)
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, string.Join(", ", conditions), position, new(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, Vector2.Zero, 1f, 0, 0f);
    }

    public static void PostAddRecipes() {
        for (int r = 0; r < Recipe.numRecipes; r++){
            foreach(int tile in Main.recipe[r].requiredTile) CraftingStations[tile] = 0;
        }
        SetStations();
    }

    public static void SetStations() {
        for (int type = 0; type < ItemLoader.ItemCount; type++){
            Item item = new(type);
            if(CraftingStations.ContainsKey(item.createTile) && CraftingStations[item.createTile] == ItemID.None) CraftingStations[item.createTile] = item.type;
        }
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


        // ----- Background for recipes -----
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
        cursor.EmitLdloc(126);
        cursor.EmitDelegate((int i) => {
            if (!Enabled) return;
            OverrideRecipeTexture(LocalFilters.FavoriteRecipes.GetValueOrDefault(Main.availableRecipe[i]), ItemSlot.DrawGoldBGForCraftingMaterial, AvailableRecipes.Contains(Main.availableRecipe[i]));
            ItemSlot.DrawGoldBGForCraftingMaterial = false;
        });

        //                 ItemSlot.Draw(...);
        cursor.GotoNext(MoveType.After, i => true);

        //                 ++ <restoreBackground>
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = s_inventoryBack4; });
        //             }
        //         }
        //     }

        // ----- Background for materials -----
        //     if (Main.numAvailableRecipes > 0) {
        //         for (<focusRecipeMaterialIndex>) {
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

        // Main.hidePlayerCraftingMenu = false;
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(typeof(Main), nameof(Main.hidePlayerCraftingMenu)));

        // ----- recBigList Scroll ----- 
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
        cursor.GotoPrev(i => i.MatchLdsfld(typeof(Main), nameof(Main.mouseLeft)));
        ILLabel? noClick = null;
        cursor.GotoNext(i => i.MatchBrfalse(out noClick));
        cursor.GotoPrev(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));

        //             ++ if(<overrideHover>) goto noClick;
        cursor.EmitLdloc(155);
        cursor.EmitDelegate((int i) => Enabled && OverrideRecipeHover(i));
        cursor.EmitBrtrue(noClick!);
        //             if(<click>) <scrollList>
        //             ++ noClick:

        // ----- Force recBigList On -----
        //             ...
        //             ItemSlot.MouseHover(22);
        cursor.GotoNext(i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.MouseHover)));

        //             ++ <forceListOn>
        cursor.EmitDelegate<System.Action>(() => Main.recBigList |= Enabled);
        //             ...
        //         }

        // ----- Background of recipes -----
        //         if (Main.numAvailableRecipes > 0) {
        //             ...
        //             Main.inventoryBack = ...;
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //             ++ <overrideBackground>
        cursor.EmitLdloc(155);
        cursor.EmitDelegate((int i) => {
            if (Enabled) OverrideRecipeTexture(LocalFilters.FavoriteRecipes.GetValueOrDefault(Main.availableRecipe[i]), false, AvailableRecipes.Contains(Main.availableRecipe[i]));
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
        x += (int)((TextureAssets.InventoryBack.Width() - 2) * Main.inventoryScale);
        y += (int)(4 * Main.inventoryScale);
        Vector2 size = InventoryTickBorder.Size() * Main.inventoryScale;
        _hitBox = new(x - (int)(size.X/2f), y - (int)(size.Y / 2f), (int)size.X, (int)size.Y);

        if (!(_hover = _hitBox.Contains(Main.mouseX, Main.mouseY)) || PlayerInput.IgnoreMouseInterface) return false;

        Main.player[Main.myPlayer].mouseInterface = true;
        if (Main.mouseLeft && Main.mouseLeftRelease) {
            Filters filters = LocalFilters;
            if (filters.TileMode) filters.TileMode = false;
            else {
                filters.ShowAllRecipes = !filters.ShowAllRecipes;
                if (Filters.CurrentVisibility == FilterFlags.ShowAllTile && filters.ShowAllRecipes) filters.TileMode = true;
            }
            SoundEngine.PlaySound(SoundID.MenuTick);
            Recipe.FindRecipes();
        }
        return true;
    }
    public static void DrawVisibility(){
        Filters filters = LocalFilters;
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
        cursor.EmitDelegate<System.Func<int, bool>>(i => {
            if (!Enabled) return false;
            if (Main.guideItem.IsAir) {
                Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
                return true;
            }
            if (LocalFilters.TileMode) {
                if (Main.recipe[i].requiredTile.Contains(Main.guideItem.createTile)) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
                return true;
            }
            if (Main.recipe[i].createItem.type == Main.guideItem.type) {
                Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
                return true;
            }
            return false;
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
            if(!Enabled) return false;
            AvailableRecipes.Clear();
            _adding = false;
            return true;
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
        cursor.EmitDelegate(() => { if(Enabled) AddRecipes(); });
    }
    public static void AddRecipes(){
        List<int> fav = new(), black = new(), others = new();

        _adding = true;
        Reflection.Recipe.CollectGuideRecipes.Invoke();
        for (int i = 0; i < Main.numAvailableRecipes; i++){
            (LocalFilters.FavoriteRecipes.GetValueOrDefault(Main.availableRecipe[i]) switch {
                FavoriteState.Favorited => fav,
                FavoriteState.Blacklisted => black,
                _ => others,
            }).Add(Main.availableRecipe[i]);
        }

        Recipe.ClearAvailableRecipes();
        bool showAll = LocalFilters.ShowAllRecipes;
        foreach(int i in fav) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
        foreach(int i in others) if(showAll || AvailableRecipes.Contains(i)) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);
        if(showAll) foreach (int i in black) Reflection.Recipe.AddToAvailableRecipes.Invoke(i);

    }

    private static void HookAddAvailableRecipe(On_Recipe.orig_AddToAvailableRecipes orig, int recipeIndex) {
        if (!_adding) AvailableRecipes.Add(recipeIndex);
        else orig(recipeIndex);
    }


    private static void HookOverrideCraftHover(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (!Enabled) {
            orig(recipeIndex);
            return;
        }
        if (OverrideRecipeHover(recipeIndex) || (recipeIndex == Main.focusRecipe && !AvailableRecipes.Contains(Main.availableRecipe[recipeIndex]))) {
            (bool lstate, bool rstate) = (Main.mouseLeft, Main.mouseRight);
            (Main.mouseLeft, Main.mouseRight) = (false, false);
            orig(recipeIndex);
            (Main.mouseLeft, Main.mouseRight) = (lstate, rstate);
        } else orig(recipeIndex);
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

    public static readonly HashSet<int> AvailableRecipes = new();
    public static readonly Dictionary<int, int> CraftingStations = new();

    public static readonly Asset<Texture2D>[] DefaultRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack4, TextureAssets.InventoryBack5, TextureAssets.InventoryBack10 };
    public static readonly Asset<Texture2D>[] SelectedRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack14, TextureAssets.InventoryBack11, TextureAssets.InventoryBack17 };


    private static int _recDelay = 0;
    private static Asset<Texture2D> s_inventoryBack4 = null!;

    private static int _focusRecipe;
    private static Item?[] _craftingTiles = System.Array.Empty<Item>();

    private static bool _adding = true;
    private static List<int> _favRecipes = new(), _blackRecipes = new(), _otherRecipes = new();
}