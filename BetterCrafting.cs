using System.Collections.Generic;
using System.IO;
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

namespace BetterInventory;

public static class BetterCrafting {
    public static void Load(){
        IL_Main.DrawInventory += IlDrawInventory;

        FindItemRecipes = KeybindLoader.RegisterKeybind(BetterInventory.Instance, "FindRecipes", Microsoft.Xna.Framework.Input.Keys.N);
        Main.OnPostDraw += UpdateHoveredItem;

        On_ItemSlot.OverrideHover_ItemArray_int_int += HookOverrideHover;
        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClick;

        On_Player.dropItemCheck += OndropItems;
        On_Player.SaveTemporaryItemSlotContents += HookSaveTemporaryItemSlotContents;

        On_Main.HoverOverCraftingItemButton += HookHoverOverCraftingItemButton;
        On_Recipe.FindRecipes += HookFindRecipes;
        IL_Recipe.CollectGuideRecipes += ILGuideRecipes;

        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
        IL_Main.CraftItem += ILCraftItem;

        _ownedMaterials = (Dictionary<int, int>)OwnedItemsField.GetValue(null)!;
    }


    internal static void OnStateChanged() {
        if (!Enabled) Main.guideItem.TurnToAir();
        else if (!Main.guideItem.IsAir) {
            Main.LocalPlayer.GetDropItem(ref Main.guideItem);
            Main.guideItem = new(Main.guideItem.type);
        }
    }


    private static void IlDrawInventory(ILContext il) {
        // if(Main.InReforgeMenu){
        //     ...
        // } else if(Main.InGuideCraftMenu) {
        //     if(<closeGuideUI>) {
        //         ...
        //     } else {
        //         ++ guide:
        //         ...
        //         ++ if(!Main.InGuideCraftMenu) goto recipe;
        //     }
        // }
        // ...
        // if(<showRecipes>){
        //     ++ if(!Main.InGuideCraftMenu) goto guide;
        //     ++ recipe:
        //     ...
        // }

        ILCursor cursor = new(il);

        // Keybind
        cursor.EmitDelegate(CheckFindRecipes);

        // Mark guide
        ILLabel? endGuide = null;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "DrawGuideCraftText"));
        cursor.GotoPrev(MoveType.After, i => i.MatchBr(out endGuide));
        ILLabel guide = cursor.DefineLabel();
        cursor.MarkLabel(guide);

        // Apply recipe
        cursor.GotoLabel(endGuide!, MoveType.Before);
        ILLabel recipe = cursor.DefineLabel();
        cursor.EmitCall(AlternateGuideItemField.GetMethod!);
        cursor.EmitBrtrue(recipe);

        // Apply guide + Mark recipe
        cursor.GotoNext(i => i.MatchStsfld(typeof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts), nameof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig)));
        cursor.GotoPrev(MoveType.AfterLabel);
        cursor.EmitCall(AlternateGuideItemField.GetMethod!);
        cursor.EmitBrtrue(guide);
        cursor.MarkLabel(recipe);

        // Background of createdItem
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "HoverOverCraftingItemButton"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(124);
        cursor.EmitDelegate((int i) => {
            if (!Enabled || Main.guideItem.type == ItemID.None || AvailableRecipes[i]) return;
            if (ItemSlot.DrawGoldBGForCraftingMaterial) {
                ItemSlot.DrawGoldBGForCraftingMaterial = false;
                Main.inventoryBack.B = (byte)(Main.inventoryBack.B / 8f);
            } else {
                byte alpha = Main.inventoryBack.A;
                Main.inventoryBack *= 0.5f;
                Main.inventoryBack.A = alpha;
            }
        });

        // Background of materials
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "SetRecipeMaterialDisplayName"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(130);
        cursor.EmitDelegate((int matI) => {
            if (!Enabled || Main.guideItem.type == ItemID.None ||  AvailableRecipes[Main.focusRecipe]) return;
            Item material = Main.recipe[Main.availableRecipe[Main.focusRecipe]].requiredItem[matI];
            if (_ownedMaterials.GetValueOrDefault(material.type, 0) >= material.stack) return;
            byte alpha = Main.inventoryBack.A;
            Main.inventoryBack *= 0.5f;
            Main.inventoryBack.A = alpha;
        });

        // Show all recipes Toggle
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(typeof(Main), nameof(Main.hidePlayerCraftingMenu)));
        cursor.EmitLdloc(13);
        cursor.EmitDelegate((int yOffset) => {
            if (!Enabled || Main.guideItem.IsAir) return;
            int x = 94;
            int y = 450 + yOffset + TextureAssets.CraftToggle[0].Height();
            Vector2 hibBox = TextureAssets.CraftToggle[0].Size() * 0.45f;
            Asset<Texture2D> eye = forceAllRecipes ? EyeForced : hideUnavailableRecipes ? TextureAssets.InventoryTickOff : TextureAssets.InventoryTickOn;
            Main.spriteBatch.Draw(eye.Value, new Vector2(x, y), null, Color.White, 0f, eye.Value.Size() / 2, 1f, 0, 0f);
            if (Main.mouseX > x - hibBox.X && Main.mouseX < x + hibBox.X && Main.mouseY > y - hibBox.Y && Main.mouseY < y + hibBox.Y && !PlayerInput.IgnoreMouseInterface) {
                Main.instance.MouseText(Language.GetTextValue("Mods.BetterInventory.UI.ShowRecipes"));
                Main.spriteBatch.Draw(EyeBorder.Value, new Vector2(x, y), null, Color.White, 0f, EyeBorder.Value.Size() / 2, 1f, 0, 0f);
                Main.player[Main.myPlayer].mouseInterface = true;
                if (Main.mouseLeft && Main.mouseLeftRelease) {
                    hideUnavailableRecipes = !hideUnavailableRecipes;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    Recipe.FindRecipes();
                }
            }
        });

        // Background of recipes
        cursor.GotoNext(i => i.MatchCall(typeof(Main), nameof(Main.LockCraftingForThisCraftClickDuration)));
        cursor.GotoNext(i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.MouseHover)));
        cursor.EmitDelegate<System.Action>(() => Main.recBigList |= Enabled);
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(153);
        cursor.EmitDelegate((int i) => {
            if (!Enabled || Main.guideItem.type == ItemID.None || AvailableRecipes[i]) return;
            byte alpha = Main.inventoryBack.A;
            Main.inventoryBack *= 0.5f;
            Main.inventoryBack.A = alpha;
        });
    }


    private static void HookOverrideHover(On_ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Enabled) {
            orig(inv, context, slot);
            return;
        }
        if (FindItemRecipes.Current) return;
        if (context == ItemSlot.Context.GuideItem && ((Main.guideItem.type > ItemID.None && Main.mouseItem.type == ItemID.None) || ItemSlot.ShiftInUse)) Main.cursorOverride = CursorOverrideID.TrashCan;
        else if (Main.InGuideCraftMenu && ItemSlot.ShiftInUse) Main.cursorOverride = CursorOverrideID.Magnifiers;
        else orig(inv, context, slot);
    }

    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if (!Enabled) return orig(inv, context, slot);
        if (FindItemRecipes.Current) return true;
        if (context == ItemSlot.Context.GuideItem) {
            if (Main.cursorOverride == CursorOverrideID.TrashCan) FindRecipes(ItemID.None);
            else {
                FindRecipes(Main.mouseItem.type);
                Main.LocalPlayer.GetDropItem(ref Main.mouseItem);
            }
            return true;
        }
        if (Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.Magnifiers) {
            FindRecipes(inv[slot].type);
            return true;
        }
        return orig(inv, context, slot);
    }

    private static void HookRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (Enabled && context == ItemSlot.Context.GuideItem && Main.mouseRight && Main.mouseRightRelease) {
            FindRecipes(ItemID.None);
            return;
        }
        orig(inv, context, slot);
    }


    private static void OndropItems(On_Player.orig_dropItemCheck orig, Player self) {
        if (!Enabled) {
            orig(self);
            return;
        }
        bool old = Main.InGuideCraftMenu;
        Main.InGuideCraftMenu = true;
        orig(self);
        Main.InGuideCraftMenu = old;
    }

    private static void HookSaveTemporaryItemSlotContents(On_Player.orig_SaveTemporaryItemSlotContents orig, Player self, BinaryWriter writer) {
        if (!Enabled || Main.guideItem.IsAir) {
            orig(self, writer);
            return;
        }
        Main.guideItem.stack = 0;
        orig(self, writer);
        Main.guideItem.stack = 1;
    }


    private static void HookHoverOverCraftingItemButton(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (!Enabled) {
            orig(recipeIndex);
            return;
        }
        _inHoverCrafting = true;
        if(Main.guideItem.IsAir || !AvailableRecipes[recipeIndex]) {
            orig(recipeIndex);
            return;
        }
        Main.guideItem.stack = 0;
        orig(recipeIndex);
        Main.guideItem.stack = 1;
        _inHoverCrafting = false;
    }
    
    private static void HookFindRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        if(!Enabled || Main.guideItem.type == ItemID.None || canDelayCheck){
            orig(canDelayCheck);
            return;
        }
        forceAllRecipes = false;
        int oldRecipe = Main.availableRecipe[Main.focusRecipe];
        float focusY = Main.availableRecipeY[Main.focusRecipe];
        int stack = Main.guideItem.stack;
        Main.guideItem.stack = 0;
        orig(canDelayCheck);
        int[] available = Main.availableRecipe[..Main.numAvailableRecipes];
    retry:
        Main.guideItem.stack = 1;
        orig(canDelayCheck);
        Main.guideItem.stack = stack;

        int a = 0, g = 0;
        AvailableRecipes = new bool[Main.numAvailableRecipes];

        if (hideUnavailableRecipes && !forceAllRecipes) {
            int[] guide = Main.availableRecipe[..Main.numAvailableRecipes];
            Recipe.ClearAvailableRecipes();
            while (a < available.Length && g < guide.Length) {
                int sign = available[a].CompareTo(guide[g]);
                if (sign == 0) {
                    AvailableRecipes[Main.numAvailableRecipes] = true;
                    Main.availableRecipe[Main.numAvailableRecipes++] = available[a];
                    g++;
                    a++;
                } else if (sign < 0) a++;
                else g++;
            }
            if (Main.numAvailableRecipes != 0) {
                TryRefocusingRecipeMethod.Invoke(null, new object[] { oldRecipe });
                VisuallyRepositionRecipesMethod.Invoke(null, new object[] { focusY });
            } else {
                forceAllRecipes = true;
                goto retry;
            }
            return;
        }
        while (g < Main.numAvailableRecipes && a < available.Length) {
            int sign = available[a].CompareTo(Main.availableRecipe[g]);
            if (sign == 0) {
                AvailableRecipes[g] = true;
                a++;
                g++;
            } else if (sign < 0) a++;
            else g++;
        }
    }

    private static void ILGuideRecipes(ILContext il) {
        ILCursor cursor = new(il);
        ILLabel? endLoop = null;
        cursor.GotoNext(i => i.MatchCallvirt(typeof(Recipe).GetProperty(nameof(Recipe.Disabled))!.GetMethod!));
        cursor.GotoNext(i => i.MatchBrtrue(out endLoop));
        cursor.GotoNext(MoveType.AfterLabel);
        cursor.EmitLdloc1();
        cursor.EmitDelegate<System.Func<int, bool>>(i => {
            if(Enabled && Main.recipe[i].createItem.type == Main.guideItem.type){
                Main.availableRecipe[Main.numAvailableRecipes] = i;
                Main.numAvailableRecipes++;
                return true;
            }
            return false;
        });
        cursor.EmitBrtrue(endLoop!);
    }


    private static void UpdateHoveredItem(GameTime time) {
        if (HoverItemInfo.Type == Main.HoverItem.type) return;
        bool canBeCrafted = false;
        for (int i = 0; i < Recipe.maxRecipes; i++) {
            if (Main.recipe[i].Disabled || Main.recipe[i].createItem.type != Main.HoverItem.type) continue;
            canBeCrafted = true;
            break;
        }
        HoverItemInfo = new(Main.HoverItem.type, Main.HoverItem.material, canBeCrafted);
    }

    private static void CheckFindRecipes() {
        if (!Enabled) return;
        if (!FindItemRecipes.Current) {
            if (FindItemRecipes.JustReleased && _findRecipesFrames <= 15) Main.recBigList = !Main.recBigList;
            return;
        }
        if (FindItemRecipes.JustPressed) _findRecipesFrames = 0;
        _findRecipesFrames++;

        if (HoverItemInfo.Type <= 0) return;
        if (!HoverItemInfo.HasAnyRecipe) return;
        Main.cursorOverride = CursorOverrideID.Magnifiers;

        if (!Main.mouseLeft || !Main.mouseLeftRelease) return;
        _findRecipesFrames = 20;
        Player player = Main.LocalPlayer;
        if (Main.CreativeMenu.Enabled) Main.CreativeMenu.CloseMenu();
        if (player.tileEntityAnchor.InUse) player.tileEntityAnchor.Clear();
        if (Main.InReforgeMenu) player.SetTalkNPC(-1);
        FindRecipes(HoverItemInfo.Type);
    }

    public static void FindRecipes(int type) {
        if (type == Main.guideItem.type) return;
        Main.guideItem.SetDefaults(type);
        SoundEngine.PlaySound(SoundID.Grab);
        Recipe.FindRecipes(false);
        Main.recBigList = type > ItemID.None;
    }


    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting) {
        movedAnItemToAllowCrafting = false;
        if(Main.mouseLeft && !Main.mouseLeftRelease) return false;
        if(ItemSlot.ShiftInUse && Main.mouseLeft) return Main.LocalPlayer.ItemSpace(currentRecipe.createItem).CanTakeItem;
        return orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting || Enabled, out movedAnItemToAllowCrafting);
    }

    private static void ILCraftItem(ILContext il) {
        ILCursor cursor = new(il);

        ILLabel vanillaCheck = cursor.DefineLabel();
        ILLabel skipCheck = cursor.DefineLabel();

        // Shift Click check
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => ItemSlot.ShiftInUse && Main.mouseLeft);
        cursor.EmitBrfalse(vanillaCheck);
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => Main.LocalPlayer.ItemSpace(r.createItem).CanTakeItem);
        cursor.EmitBrtrue(skipCheck);
        cursor.EmitRet();
        cursor.MarkLabel(vanillaCheck);

        // Vanilla check
        cursor.GotoNext(i => i.MatchCallOrCallvirt(typeof(Recipe), nameof(Recipe.Create)));
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());
        cursor.MarkLabel(skipCheck);

        // Shift Click grab
        ILLabel dontRet = cursor.DefineLabel();
        cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt(typeof(RecipeLoader), nameof(RecipeLoader.OnCraft)));
        cursor.EmitLdloc0();
        cursor.EmitDelegate((Item crafted) => {
            if (!(ItemSlot.ShiftInUse && Main.mouseLeft)) return false;
            Main.LocalPlayer.GetItem(Main.myPlayer, crafted, GetItemSettings.InventoryUIToInventorySettingsShowAsNew);
            return true;
        });
        cursor.EmitBrfalse(dontRet);
        cursor.EmitRet();
        cursor.MarkLabel(dontRet);
    }


    public static Asset<Texture2D> EyeBorder => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Border");
    public static Asset<Texture2D> EyeForced => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Forced");

    public static bool Enabled => Configs.ClientConfig.Instance.betterCrafting;

    public static bool[] AvailableRecipes { get; private set; } = System.Array.Empty<bool>();

    public static bool hideUnavailableRecipes = false;
    public static bool forceAllRecipes = false;
    public static bool AlternateGuideItem => Enabled && !Main.InGuideCraftMenu;

    public static HoverItemCache HoverItemInfo { get; private set; }

    public static ModKeybind FindItemRecipes { get; private set; } = null!;
    private static int _findRecipesFrames = 0;

    private static Dictionary<int, int> _ownedMaterials = null!;
    private static bool _inHoverCrafting;

    public static readonly PropertyInfo AlternateGuideItemField = typeof(BetterCrafting).GetProperty(nameof(AlternateGuideItem), BindingFlags.Static | BindingFlags.Public)!;
    public static readonly FieldInfo RecBigListField = typeof(Main).GetField(nameof(Main.recBigList), BindingFlags.Static | BindingFlags.Public)!;
    public static readonly FieldInfo InGuideCraftMenuField = typeof(Main).GetField(nameof(Main.InGuideCraftMenu), BindingFlags.Static | BindingFlags.Public)!;
    
    
    public static readonly FieldInfo OwnedItemsField = typeof(Recipe).GetField("_ownedItems", BindingFlags.Static | BindingFlags.NonPublic)!;
    public static readonly MethodInfo TryRefocusingRecipeMethod = typeof(Recipe).GetMethod("TryRefocusingRecipe", BindingFlags.Static | BindingFlags.NonPublic)!;
    public static readonly MethodInfo VisuallyRepositionRecipesMethod = typeof(Recipe).GetMethod("VisuallyRepositionRecipes", BindingFlags.Static | BindingFlags.NonPublic)!;
}

public readonly record struct HoverItemCache(int Type, bool Material, bool CanBeCrafted) {
    public bool HasAnyRecipe => Material || CanBeCrafted;
}

/*
Shift + Left => Craft max + go in inventory
    -> ? cursor override
Left => Craft Max + default
    -> ? cursor override
Right => default
*/