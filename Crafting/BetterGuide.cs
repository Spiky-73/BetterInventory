
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Crafting;


public static class BetterGuide {

    public static bool Enabled => Configs.ClientConfig.Instance.betterCrafting;

    public static ModKeybind FindRecipes { get; private set; } = null!;
    private static int _findRecipesFrames = 0;

    public static HoverItemCache HoverItemInfo { get; private set; }

    public static void Load(){
        IL_Main.DrawInventory += IlDrawInventory;

        IL_Recipe.CollectGuideRecipes += ILGuideRecipes;

        FindRecipes = KeybindLoader.RegisterKeybind(BetterInventory.Instance, "FindRecipes", Microsoft.Xna.Framework.Input.Keys.N);

        Main.OnPostDraw += UpdateHoveredItem;

        On_ItemSlot.OverrideHover_ItemArray_int_int += HookOverrideHover;
        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClick;

        On_Player.dropItemCheck += OndropItems;
        On_Player.SaveTemporaryItemSlotContents += HookSaveTemporaryItemSlotContents;
    }

    private static void ILGuideRecipes(ILContext il) {
        ILCursor cursor = new(il);
        ILLabel? endLoop = null;
        // for (...) {
        //     ...
        //     if (recipe.Disabled) continue;
        cursor.GotoNext(i => i.MatchCallvirt(typeof(Recipe).GetProperty(nameof(Recipe.Disabled))!.GetMethod!));
        cursor.GotoNext(i => i.MatchBrtrue(out endLoop));

        //     ++ if(<extraRecipe>) {
        //        <addRecipe>
        //        continue;
        //    }
        cursor.GotoNext(MoveType.AfterLabel);
        cursor.EmitLdloc1();
        cursor.EmitDelegate<System.Func<int, bool>>(i => {
            if (Enabled && Main.recipe[i].createItem.type == Main.guideItem.type) {
                Utility.AddToAvailableRecipesMethod.Invoke(null, new object[] { i });
                return true;
            }
            return false;
        });
        cursor.EmitBrtrue(endLoop!);
        // }
    }

    private static void IlDrawInventory(ILContext il) {
        ILCursor cursor = new(il);

        cursor.EmitCall(typeof(BetterGuide).GetMethod(nameof(CheckFindRecipes), BindingFlags.Static | BindingFlags.NonPublic)!);

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

        //         <draw guide item slot>
        cursor.GotoLabel(endGuide!, MoveType.Before);

        //         ++ if(!Main.InGuideCraftMenu) goto recipe;
        ILLabel recipe = cursor.DefineLabel();
        cursor.EmitCall(AlternateGuideItemProp.GetMethod!);
        cursor.EmitBrtrue(recipe);
        //     }
        // }

        // ...
        // if(<showRecipes>){
        cursor.GotoNext(i => i.MatchStsfld(typeof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts), nameof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig)));
        cursor.GotoPrev(MoveType.AfterLabel);

        //     ++ if(!Main.InGuideCraftMenu) goto guide;
        cursor.EmitCall(AlternateGuideItemProp.GetMethod!);
        cursor.EmitBrtrue(guide);

        //     ++ recipe:
        cursor.MarkLabel(recipe);
        //     ...
        // }
    }

    public static void UpdateMouseItem() {
        if (!Enabled) Main.guideItem.TurnToAir();
        else if (!Main.guideItem.IsAir) {
            Main.LocalPlayer.GetDropItem(ref Main.guideItem);
            Main.guideItem = new(Main.guideItem.type);
        }
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
        if (!FindRecipes.Current) {
            if (FindRecipes.JustReleased && _findRecipesFrames <= 15) {
                if (FocusRecipes() || !Main.recBigList) {
                    if (TryFocusRecipeList()) SoundEngine.PlaySound(SoundID.MenuTick);
                } else if (Main.recBigList) {
                    Main.recBigList = false;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
            return;
        }
        if (FindRecipes.JustPressed) _findRecipesFrames = 0;
        _findRecipesFrames++;

        if (HoverItemInfo.Type <= 0) return;
        if (!HoverItemInfo.HasAnyRecipe) return;
        Main.cursorOverride = CursorOverrideID.Magnifiers;

        if (!Main.mouseLeft || !Main.mouseLeftRelease) return;
        _findRecipesFrames = 20;
        FocusRecipes();
        SetGuideItem(HoverItemInfo.Type);
    }

    private static void HookOverrideHover(On_ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Enabled) {
            orig(inv, context, slot);
            return;
        }
        if (FindRecipes.Current) return;
        if (context == ItemSlot.Context.GuideItem && ((!Main.guideItem.IsAir && Main.mouseItem.IsAir) || ItemSlot.ShiftInUse)) Main.cursorOverride = CursorOverrideID.TrashCan;
        else if (Main.InGuideCraftMenu && ItemSlot.ShiftInUse) Main.cursorOverride = CursorOverrideID.Magnifiers;
        else orig(inv, context, slot);
    }

    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if (!Enabled) return orig(inv, context, slot);
        if (FindRecipes.Current) return true;

        if (context == ItemSlot.Context.GuideItem) {
            if (Main.cursorOverride == CursorOverrideID.TrashCan) SetGuideItem(ItemID.None);
            else {
                SetGuideItem(Main.mouseItem.type);
                Main.LocalPlayer.GetDropItem(ref Main.mouseItem);
            }
            return true;
        }
        if (Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.Magnifiers) {
            SetGuideItem(inv[slot].type);
            return true;
        }
        return orig(inv, context, slot);
    }

    private static void HookRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (Enabled && context == ItemSlot.Context.GuideItem && Main.mouseRight && Main.mouseRightRelease) {
            SetGuideItem(ItemID.None);
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

    public static void SetGuideItem(int type) {
        if (type != Main.guideItem.type) {
            Main.guideItem.SetDefaults(type);
            Recipe.FindRecipes(false);
        }
        if (type > ItemID.None) TryFocusRecipeList();
        SoundEngine.PlaySound(SoundID.Grab);
    }

    public static bool FocusRecipes() {
        Player player = Main.LocalPlayer;
        if (Main.CreativeMenu.Enabled) Main.CreativeMenu.CloseMenu();
        else if (player.tileEntityAnchor.InUse) player.tileEntityAnchor.Clear();
        else if (Main.InReforgeMenu) player.SetTalkNPC(-1);
        else return false;
        return true;
    }

    public static bool TryFocusRecipeList() => Main.recBigList = Main.numAvailableRecipes > 0;


    public static bool AlternateGuideItem => Enabled && !Main.InGuideCraftMenu;

    public static readonly PropertyInfo AlternateGuideItemProp = typeof(BetterGuide).GetProperty(nameof(AlternateGuideItem), BindingFlags.Static | BindingFlags.Public)!;

}

public readonly record struct HoverItemCache(int Type, bool Material, bool CanBeCrafted) {
    public bool HasAnyRecipe => Material || CanBeCrafted;
}
