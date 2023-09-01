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

public static class RecipeFiltering {

    public static bool Enabled => Configs.ClientConfig.Instance.betterCrafting;

    public static bool ShowAllRecipes => Main.guideItem.IsAir ? showAllRecipes : showAllRecipesWithGuide; // TODO save
    public static bool showAllRecipesWithGuide = true;
    public static bool showAllRecipes = false;

    public static void Load(){

        On_Main.HoverOverCraftingItemButton += HookHoverOverCraftingItemButton;

        IL_Main.DrawInventory += IlDrawInventory;

        On_Recipe.FindRecipes += HookFindRecipes;

        _inventoryBack4 = TextureAssets.InventoryBack4;
    }

    public static void PostAddRecipes() => CraftableRecipes = new bool[Recipe.maxRecipes];

    private static void HookHoverOverCraftingItemButton(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (!Enabled) {
            orig(recipeIndex);
            return;
        }

        bool cancel = false;

        bool click = Main.mouseLeft && Main.mouseLeftRelease;
        if (Main.keyState.IsKeyDown(Main.FavoriteKey)) {
            Main.cursorOverride = CursorOverrideID.FavoriteStar;
            if (click) {
                RecipeState state = RecipeStatus.GetValueOrDefault(Main.availableRecipe[recipeIndex]);
                RecipeStatus[Main.availableRecipe[recipeIndex]] = state == RecipeState.Favorited ? RecipeState.Default : RecipeState.Favorited;
                cancel = true;
                Recipe.FindRecipes(true);
            }
        } else if (ItemSlot.ControlInUse) {
            Main.cursorOverride = CursorOverrideID.TrashCan;
            if (click) {
                RecipeState state = RecipeStatus.GetValueOrDefault(Main.availableRecipe[recipeIndex]);
                RecipeStatus[Main.availableRecipe[recipeIndex]] = state == RecipeState.Blacklisted ? RecipeState.Default : RecipeState.Blacklisted;
                cancel = true;
                Recipe.FindRecipes(true);
            }
        }
        
        cancel |= recipeIndex == Main.focusRecipe && !CraftableRecipes[recipeIndex];
        (bool lstate, bool rstate) = (Main.mouseLeft, Main.mouseRight);
        if (cancel) (Main.mouseLeft, Main.mouseRight) = (false, false);
        orig(recipeIndex);
        if (cancel) (Main.mouseLeft, Main.mouseRight) = (lstate, rstate);
    }


    private static void IlDrawInventory(ILContext il) {
        ILCursor cursor = new(il);

        // Background of createdItem
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "HoverOverCraftingItemButton"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(124);
        cursor.EmitDelegate((int i) => {
            if (!Enabled) return;
            OverrideRecipeTexture(RecipeStatus.GetValueOrDefault(Main.availableRecipe[i]), ItemSlot.DrawGoldBGForCraftingMaterial, CraftableRecipes[i]);
            ItemSlot.DrawGoldBGForCraftingMaterial = false;
        });
        cursor.GotoNext(MoveType.After, i => true);
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = _inventoryBack4; });

        // Background of materials
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "SetRecipeMaterialDisplayName"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(130);
        cursor.EmitDelegate((int matI) => {
            if(!Enabled) return;
            bool canCraft = CraftableRecipes[Main.focusRecipe];
            if(!canCraft) {
                Item material = Main.recipe[Main.availableRecipe[Main.focusRecipe]].requiredItem[matI];
                canCraft = Utility.OwnedItems.GetValueOrDefault(material.type, 0) >= material.stack;
            }

            RecipeState state = RecipeStatus.GetValueOrDefault(Main.availableRecipe[Main.focusRecipe]);
            if(state == RecipeState.Favorited) state = RecipeState.Default;
            OverrideRecipeTexture(state, false, canCraft);
        });
        cursor.GotoNext(MoveType.After, i => true);
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = _inventoryBack4; });

        // Show all recipes Toggle
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(typeof(Main), nameof(Main.hidePlayerCraftingMenu)));
        cursor.EmitLdloc(13);
        cursor.EmitCall(typeof(RecipeFiltering).GetMethod(nameof(DrawFilters), BindingFlags.Static | BindingFlags.NonPublic)!);

        // Cursor override
        cursor.GotoNext(i => i.MatchCall(typeof(Main), nameof(Main.LockCraftingForThisCraftClickDuration)));
        cursor.GotoPrev(i => i.MatchLdsfld(typeof(Main), nameof(Main.mouseLeft)));
        ILLabel? noClick = null;
        cursor.GotoNext(i => i.MatchBrfalse(out noClick));
        cursor.GotoPrev(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));
        cursor.EmitLdloc(153);
        cursor.EmitDelegate((int i) => {
            bool click = Main.mouseLeft && Main.mouseLeftRelease;
            if(Main.keyState.IsKeyDown(Main.FavoriteKey)) {
                Main.cursorOverride = CursorOverrideID.FavoriteStar;
                if (click) {
                    RecipeState state = RecipeStatus.GetValueOrDefault(Main.availableRecipe[i]);
                    RecipeStatus[Main.availableRecipe[i]] = state == RecipeState.Favorited ? RecipeState.Default : RecipeState.Favorited;
                    Recipe.FindRecipes(true);
                    return true;
                }
            }
            if (ItemSlot.ControlInUse) {
                Main.cursorOverride = CursorOverrideID.TrashCan;
                if (click) {
                    RecipeState state = RecipeStatus.GetValueOrDefault(Main.availableRecipe[i]);
                    RecipeStatus[Main.availableRecipe[i]] = state == RecipeState.Blacklisted ? RecipeState.Default : RecipeState.Blacklisted;
                    Recipe.FindRecipes(true);
                    return true;
                }
            }
            return false;
        });
        cursor.EmitBrtrue(noClick!);


        // Force recBigList on
        cursor.GotoNext(i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.MouseHover)));
        cursor.EmitDelegate<System.Action>(() => Main.recBigList |= Enabled);
        
        // Background of recipes
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(153);
        cursor.EmitDelegate((int i) => {
            if (Enabled) OverrideRecipeTexture(RecipeStatus.GetValueOrDefault(Main.availableRecipe[i]), false, CraftableRecipes[i]);
        });
        cursor.GotoNext(MoveType.After, i => true);
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = _inventoryBack4; });
    }

    private static void DrawFilters(int yOffset){
        if (!Enabled) return;
        int x = 94;
        int y = 450 + yOffset + TextureAssets.CraftToggle[0].Height();
        Vector2 hibBox = TextureAssets.CraftToggle[0].Size() * 0.45f;
        Asset<Texture2D> eye = ShowAllRecipes ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff;
        Main.spriteBatch.Draw(eye.Value, new Vector2(x, y), null, Color.White, 0f, eye.Value.Size() / 2, 1f, 0, 0f);
        if (Main.mouseX > x - hibBox.X && Main.mouseX < x + hibBox.X && Main.mouseY > y - hibBox.Y && Main.mouseY < y + hibBox.Y && !PlayerInput.IgnoreMouseInterface) {
            Main.instance.MouseText(Language.GetTextValue("Mods.BetterInventory.UI.ShowRecipes"));
            Main.spriteBatch.Draw(EyeBorder.Value, new Vector2(x, y), null, Color.White, 0f, EyeBorder.Value.Size() / 2, 1f, 0, 0f);
            Main.player[Main.myPlayer].mouseInterface = true;
            if (Main.mouseLeft && Main.mouseLeftRelease) {
                if (Main.guideItem.IsAir) showAllRecipes = !ShowAllRecipes;
                else showAllRecipesWithGuide = !ShowAllRecipes;
                SoundEngine.PlaySound(SoundID.MenuTick);
                Recipe.FindRecipes();
            }
        }
    }


    public static void OverrideRecipeTexture(RecipeState state, bool selected, bool canCraft){
        TextureAssets.InventoryBack4 = (selected ? SelectedRecipeTextures : DefaultRecipeTextures)[(int)state];
        if (!canCraft) {
            byte alpha = Main.inventoryBack.A;
            Main.inventoryBack *= 0.5f;
            Main.inventoryBack.A = alpha;
        }
    }

    public static readonly Asset<Texture2D>[] DefaultRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack4, TextureAssets.InventoryBack5, TextureAssets.InventoryBack10 };
    public static readonly Asset<Texture2D>[] SelectedRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack14, TextureAssets.InventoryBack11, TextureAssets.InventoryBack17 };

    public static IEnumerable<int> AllRecipesIndex() {
        for (int i = 0; i < Recipe.numRecipes; i++) yield return i;
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
        AddFilteredRecipes(recipes, availableRecipes, RecipeState.Favorited);
        AddFilteredRecipes(recipes, availableRecipes, RecipeState.Default);
        if(ShowAllRecipes) AddFilteredRecipes(recipes, availableRecipes, RecipeState.Blacklisted);

        TryRefocusingRecipeMethod.Invoke(null, new object[] { oldRecipe });
        VisuallyRepositionRecipesMethod.Invoke(null, new object[] { focusY });
    }

    private static void AddFilteredRecipes(IEnumerable<int> recipes, int[] craftableRecipes, RecipeState applyTo){
        int a = 0;
        foreach(int index in recipes){
            if(RecipeStatus.GetValueOrDefault(index) != applyTo) continue;
            while(a < craftableRecipes.Length && craftableRecipes[a] < index) a++;
            Recipe recipe = Main.recipe[index];
            bool craftable = a < craftableRecipes.Length && index == craftableRecipes[a];
            if (!MatchVisibilityFilter(recipe, craftable)) continue;
            CraftableRecipes[Main.numAvailableRecipes] = craftable;
            Utility.AddToAvailableRecipesMethod.Invoke(null, new object[] { index });
        }
    }

    private static void HookClearRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        for (int i = 0; i < Main.numAvailableRecipes; i++) CraftableRecipes[i] = false;
        orig();
    }

    public static bool MatchVisibilityFilter(Recipe recipe, bool craftable) => RecipeStatus.GetValueOrDefault(recipe.RecipeIndex) switch {
        RecipeState.Favorited => true,
        RecipeState.Blacklisted => ShowAllRecipes,
        _ => ShowAllRecipes || craftable,
    };


    public static bool[] CraftableRecipes = System.Array.Empty<bool>();

    private static Asset<Texture2D> _inventoryBack4 = null!;

    public static readonly PropertyInfo EnabledField = typeof(RecipeFiltering).GetProperty(nameof(Enabled), BindingFlags.Static | BindingFlags.Public)!;
    public static readonly PropertyInfo DisabledProp = typeof(Recipe).GetProperty(nameof(Recipe.Disabled), BindingFlags.Instance | BindingFlags.Public)!;

    public static readonly MethodInfo useWoodMethod = typeof(Recipe).GetMethod("useWood", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo useIronBarMethod = typeof(Recipe).GetMethod("useIronBar", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo useSandMethod = typeof(Recipe).GetMethod("useSand", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo useFragmentMethod = typeof(Recipe).GetMethod("useFragment", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo usePressurePlateMethod = typeof(Recipe).GetMethod("usePressurePlate", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo CollectGuideRecipesMethod = typeof(Recipe).GetMethod("CollectGuideRecipes", BindingFlags.Static | BindingFlags.NonPublic)!;

    public static Asset<Texture2D> EyeBorder => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Border");
    public static Asset<Texture2D> EyeForced => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Forced");

    public static readonly MethodInfo TryRefocusingRecipeMethod = typeof(Recipe).GetMethod("TryRefocusingRecipe", BindingFlags.Static | BindingFlags.NonPublic)!;
    public static readonly MethodInfo VisuallyRepositionRecipesMethod = typeof(Recipe).GetMethod("VisuallyRepositionRecipes", BindingFlags.Static | BindingFlags.NonPublic)!;

    public static readonly Dictionary<int, RecipeState> RecipeStatus = new(); // TODO save
}


public class RecipeInfo {
    public RecipeInfo(bool canCraft, RecipeState state) {
        State = state;
        CanCraft = canCraft;
    }
    public RecipeState State { get; }
    public bool CanCraft { get; internal set; }
    public bool Favorited => State == RecipeState.Favorited;
    public bool Blacklisted => State == RecipeState.Blacklisted;
}

public enum RecipeState { Default, Blacklisted, Favorited }