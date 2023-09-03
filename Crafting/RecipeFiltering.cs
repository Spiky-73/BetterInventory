using System.Collections.Generic;
using System.Reflection;
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
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class RecipeFiltering : ILoadable {

    public static bool Enabled => Configs.ClientConfig.Instance.recipeFiltering;
    public static Filters LocalFilters => Main.LocalPlayer.GetModPlayer<Globals.BetterPlayer>().RecipeFilters;

    public void Load(Mod mod){
        On_Main.HoverOverCraftingItemButton += OverrideCraftHover;

        IL_Main.DrawInventory += IlDrawInventory;
        On_Recipe.FindRecipes += HookFindRecipes;

        s_inventoryBack4 = TextureAssets.InventoryBack4;
    }
    public static void PostAddRecipes() => craftableRecipes = new bool[Recipe.maxRecipes];
    public void Unload() => s_inventoryBack4 = null!;

    private static void OverrideCraftHover(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (!Enabled) {
            orig(recipeIndex);
            return;
        }
        if (OverrideRecipeHover(recipeIndex) || (recipeIndex == Main.focusRecipe && !craftableRecipes[recipeIndex])) {
            (bool lstate, bool rstate) = (Main.mouseLeft, Main.mouseRight);
            (Main.mouseLeft, Main.mouseRight) = (false, false);
            orig(recipeIndex);
            (Main.mouseLeft, Main.mouseRight) = (lstate, rstate);
        } else orig(recipeIndex);
    }
    
    private static void IlDrawInventory(ILContext il) {
        ILCursor cursor = new(il);

        // ----- Background for recipes -----
        // if (<showRecipes>) {
        //     ...
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
            OverrideRecipeTexture(LocalFilters.FavoriteRecipes.GetValueOrDefault(Main.availableRecipe[i]), ItemSlot.DrawGoldBGForCraftingMaterial, craftableRecipes[i]);
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
            bool canCraft = craftableRecipes[Main.focusRecipe];
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

        // ++ <drawFilter>
        cursor.EmitLdloc(13);
        cursor.EmitCall(typeof(RecipeFiltering).GetMethod(nameof(DrawFilters), BindingFlags.Static | BindingFlags.NonPublic)!);

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
            if (Enabled) OverrideRecipeTexture(LocalFilters.FavoriteRecipes.GetValueOrDefault(Main.availableRecipe[i]), false, craftableRecipes[i]);
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

    private static void HookFindRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        if (canDelayCheck || !Enabled) {
            orig(canDelayCheck);
            return;
        }
        int oldRecipe = Main.availableRecipe[Main.focusRecipe];
        float focusY = Main.availableRecipeY[Main.focusRecipe];

        int type = Main.guideItem.type;
        Main.guideItem.type = ItemID.None;
        orig(canDelayCheck);
        int[] availableRecipes = Main.availableRecipe[..Main.numAvailableRecipes];
        Main.guideItem.type = type;

        IEnumerable<int> recipes;
        if(!Main.guideItem.IsAir) {
            Recipe.ClearAvailableRecipes();
            CollectGuideRecipesMethod.Invoke(null, null);
            int[] guideRecipes = Main.availableRecipe[..Main.numAvailableRecipes];
            recipes = guideRecipes;
        } else recipes = AllRecipesIndex();

        Recipe.ClearAvailableRecipes();
        for (int i = 0; i < craftableRecipes.Length; i++) craftableRecipes[i] = false;

        AddFilteredRecipes(recipes, availableRecipes, FavoriteState.Favorited);
        AddFilteredRecipes(recipes, availableRecipes, FavoriteState.Default);
        if(LocalFilters.ShowAllRecipes) AddFilteredRecipes(recipes, availableRecipes, FavoriteState.Blacklisted);

        TryRefocusingRecipeMethod.Invoke(null, new object[] { oldRecipe });
        VisuallyRepositionRecipesMethod.Invoke(null, new object[] { focusY });
    }


    private static void DrawFilters(int yOffset) {
        if (!Enabled) return;
        Filters filters = LocalFilters;
        int x = 94;
        int y = 450 + yOffset + TextureAssets.CraftToggle[0].Height();
        Vector2 hibBox = TextureAssets.CraftToggle[0].Size() * 0.45f;
        Asset<Texture2D> eye = filters.ShowAllRecipes ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff;
        Main.spriteBatch.Draw(eye.Value, new Vector2(x, y), null, Color.White, 0f, eye.Value.Size() / 2, 1f, 0, 0f);
        if (Main.mouseX > x - hibBox.X && Main.mouseX < x + hibBox.X && Main.mouseY > y - hibBox.Y && Main.mouseY < y + hibBox.Y && !PlayerInput.IgnoreMouseInterface) {
            Main.instance.MouseText(Language.GetTextValue("Mods.BetterInventory.UI.ShowRecipes"));
            Main.spriteBatch.Draw(EyeBorder.Value, new Vector2(x, y), null, Color.White, 0f, EyeBorder.Value.Size() / 2, 1f, 0, 0f);
            Main.player[Main.myPlayer].mouseInterface = true;
            if (Main.mouseLeft && Main.mouseLeftRelease) {
                filters.raw ^= Main.guideItem.IsAir ? Filters.ShowNoGuideFlag : Filters.ShowGuideFlag;
                SoundEngine.PlaySound(SoundID.MenuTick);
                Recipe.FindRecipes();
            }
        }
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

    private static void AddFilteredRecipes(IEnumerable<int> recipes, int[] craftableRecipes, FavoriteState applyTo){
        int a = 0;
        foreach(int index in recipes){
            if(LocalFilters.FavoriteRecipes.GetValueOrDefault(index) != applyTo) continue;
            while(a < craftableRecipes.Length && craftableRecipes[a] < index) a++;
            Recipe recipe = Main.recipe[index];
            bool craftable = a < craftableRecipes.Length && index == craftableRecipes[a];
            if (!LocalFilters.MatchShowAll(recipe, craftable)) continue;
            RecipeFiltering.craftableRecipes[Main.numAvailableRecipes] = craftable;
            Utility.AddToAvailableRecipesMethod.Invoke(null, new object[] { index });
        }
    }

    public static IEnumerable<int> AllRecipesIndex() {
        for (int i = 0; i < Recipe.numRecipes; i++) yield return i;
    }
    

    public static bool[] craftableRecipes = System.Array.Empty<bool>();
    private static int _recDelay = 0;


    public static readonly Asset<Texture2D>[] DefaultRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack4, TextureAssets.InventoryBack5, TextureAssets.InventoryBack10 };
    public static readonly Asset<Texture2D>[] SelectedRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack14, TextureAssets.InventoryBack11, TextureAssets.InventoryBack17 };

    public static Asset<Texture2D> EyeBorder => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Border");
    public static Asset<Texture2D> EyeForced => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Forced");
    
    private static Asset<Texture2D> s_inventoryBack4 = null!;
    
    
    public static readonly MethodInfo CollectGuideRecipesMethod = typeof(Recipe).GetMethod("CollectGuideRecipes", BindingFlags.Static | BindingFlags.NonPublic)!;
    public static readonly MethodInfo TryRefocusingRecipeMethod = typeof(Recipe).GetMethod("TryRefocusingRecipe", BindingFlags.Static | BindingFlags.NonPublic)!;
    public static readonly MethodInfo VisuallyRepositionRecipesMethod = typeof(Recipe).GetMethod("VisuallyRepositionRecipes", BindingFlags.Static | BindingFlags.NonPublic)!;
}