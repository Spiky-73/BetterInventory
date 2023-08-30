using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory;

public static class BetterCrafting {
    public static void Load(){
        FindItemRecipes = KeybindLoader.RegisterKeybind(BetterInventory.Instance, "FindRecipes", Microsoft.Xna.Framework.Input.Keys.N);
        IL_Main.DrawInventory += IlDrawInventory;
        On_Player.dropItemCheck += OndropItems;
        On_ItemSlot.OverrideHover_ItemArray_int_int += HookOverrideHover;
        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClick;
        Main.OnPostDraw += UpdateHoveredItem;
    }

    private static void OndropItems(On_Player.orig_dropItemCheck orig, Player self) {
        if(!Configs.ClientConfig.Instance.betterCrafting){
            orig(self);
            return;
        }
        bool old = Main.InGuideCraftMenu;
        Main.InGuideCraftMenu = true;
        orig(self);
        Main.InGuideCraftMenu = old;
    }

    private static void HookOverrideHover(On_ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Configs.ClientConfig.Instance.betterCrafting) {
            orig(inv, context, slot);
            return;
        }
        if (FindItemRecipes.Current) return;
        if (context == ItemSlot.Context.GuideItem && ((Main.guideItem.type > ItemID.None && Main.mouseItem.type == ItemID.None) || ItemSlot.ShiftInUse)) Main.cursorOverride = CursorOverrideID.TrashCan;
        else if (Main.InGuideCraftMenu && ItemSlot.ShiftInUse) Main.cursorOverride = CursorOverrideID.Magnifiers;
        else orig(inv, context, slot);
    }

    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if (!Configs.ClientConfig.Instance.betterCrafting) return orig(inv, context, slot);
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
        if (Configs.ClientConfig.Instance.betterCrafting && context == ItemSlot.Context.GuideItem && Main.mouseRight && Main.mouseRightRelease) {
            FindRecipes(ItemID.None);
            return;
        }
        orig(inv, context, slot);
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
    }

    private static void UpdateHoveredItem(GameTime time) {
        if (HoverItemInfo.Type == Main.HoverItem.type) return;
        List<int> crafts = new();
        for (int i = 0; i < Recipe.maxRecipes; i++) {
            if (!Main.recipe[i].Disabled && Main.recipe[i].createItem.type == Main.HoverItem.type) crafts.Add(i);
        }
        HoverItemInfo = new(Main.HoverItem.type, Main.HoverItem.material, crafts.AsReadOnly());
    }


    private static void CheckFindRecipes() {
        if(!Configs.ClientConfig.Instance.betterCrafting) return;
        if (!FindItemRecipes.Current) {
            if(FindItemRecipes.JustReleased && _findRecipesFrames <= 20) Main.recBigList = !Main.recBigList;
            return;
        }
        if(FindItemRecipes.JustPressed) _findRecipesFrames = 0;
        _findRecipesFrames++;

        if(HoverItemInfo.Type <= 0) return;
        if(!HoverItemInfo.HasAnyRecipe) return;
        Main.cursorOverride = CursorOverrideID.Magnifiers;

        if (!Main.mouseLeft || !Main.mouseLeftRelease) return;
        _findRecipesFrames = 20;
        Player player = Main.LocalPlayer;
        if (Main.CreativeMenu.Enabled) Main.CreativeMenu.CloseMenu();
        if (player.tileEntityAnchor.InUse) player.tileEntityAnchor.Clear(); // TODO Allow some anchors
        if(Main.InReforgeMenu) player.SetTalkNPC(-1);
        FindRecipes(HoverItemInfo.Type);
    }

    public static void FindRecipes(int type) {
        if (type == Main.guideItem.type) return;
        Main.guideItem.SetDefaults(type);
        SoundEngine.PlaySound(SoundID.Grab);
        Recipe.FindRecipes(false);
        Main.recBigList = type > ItemID.None;
    }

    public static bool AlternateGuideItem => !Main.InGuideCraftMenu && Configs.ClientConfig.Instance.betterCrafting;

    public static HoverItemCache HoverItemInfo { get; private set; }

    public static ModKeybind FindItemRecipes { get; private set; } = null!;
    private static int _findRecipesFrames = 0;

    public static readonly PropertyInfo AlternateGuideItemField = typeof(BetterCrafting).GetProperty(nameof(AlternateGuideItem), BindingFlags.Static | BindingFlags.Public)!;
    public static readonly FieldInfo InGuideCraftMenuField = typeof(Main).GetField(nameof(Main.InGuideCraftMenu), BindingFlags.Static | BindingFlags.Public)!;
}

public readonly record struct HoverItemCache(int Type, bool Material, ReadOnlyCollection<int> Crafts) {
    public bool HasAnyRecipe => Material || Crafts.Count != 0;
}