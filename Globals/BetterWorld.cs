using System.Reflection;
using BetterInventory.Configs;
using MonoMod.Cil;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Globals;

public sealed class BetterWorld : ModSystem {
    public override void Load() {
        if (ClientConfig.Instance.betterGuide) {
            IL_Main.DrawInventory += IlDrawInventory;
            On_Player.dropItemCheck += OndropItems;
            On_ItemSlot.OverrideHover_ItemArray_int_int += HookOverrideHover;
            On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;
            On_ItemSlot.RightClick_ItemArray_int_int += HookRightClick;
        }
    }

    private static void HookOverrideHover(On_ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        // TODO keybind

        if(ItemSlot.ShiftInUse && (Main.InGuideCraftMenu || context == ItemSlot.Context.GuideItem)){
            Main.cursorOverride = CursorOverrideID.Magnifiers;
            return;
        }
        orig(inv, context, slot);

        if(ItemSlot.ShiftInUse && Main.cursorOverride == -1){
            Main.cursorOverride = CursorOverrideID.Magnifiers;
        }
    }

    private static void OndropItems(On_Player.orig_dropItemCheck orig, Player self) {
        bool old = Main.InGuideCraftMenu;
        Main.InGuideCraftMenu = true;
        orig(self);
        Main.InGuideCraftMenu = old;
    }

    private void HookRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (Main.mouseRight && Main.mouseRightRelease && context == ItemSlot.Context.GuideItem) {
            Main.guideItem.SetDefaults();
            SoundEngine.PlaySound(SoundID.Grab);
            return;
        }
        orig(inv, context, slot);
    }

    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if(ItemSlot.ShiftInUse && Main.cursorOverride == CursorOverrideID.Magnifiers){
            Main.guideItem.SetDefaults(inv[slot].type);
            SoundEngine.PlaySound(SoundID.Grab);
            return true;
        }
        if(context == ItemSlot.Context.GuideItem){
            if(Main.cursorOverride == CursorOverrideID.TrashCan){
                Main.guideItem.SetDefaults();
            } else {
                Main.guideItem.SetDefaults(Main.mouseItem.type);
                if (Main.mouseItem.type > ItemID.None) {
                    Main.mouseItem.position = Main.LocalPlayer.Center;
                    Item item = Main.LocalPlayer.GetItem(Main.LocalPlayer.whoAmI, Main.mouseItem, GetItemSettings.GetItemInDropItemCheck);
                    if (item.stack > 0) {
                        int i = Item.NewItem(new EntitySource_OverfullInventory(Main.LocalPlayer, null), (int)Main.LocalPlayer.position.X, (int)Main.LocalPlayer.position.Y, Main.LocalPlayer.width, Main.LocalPlayer.height, item.type, item.stack, false, Main.mouseItem.prefix, true, false);
                        Main.item[i] = item.Clone();
                        Main.item[i].newAndShiny = false;
                        if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.SyncItem, -1, -1, null, i, 1f, 0f, 0f, 0, 0, 0);
                    }
                    Main.mouseItem = new Item();
                    Main.LocalPlayer.inventory[58] = new Item();
                    Recipe.FindRecipes(false);
                }
            }
            SoundEngine.PlaySound(SoundID.Grab);
            return true;
        }
        return orig(inv, context, slot);
        
    }

    private static void IlDrawInventory(ILContext il) {
        // ...
        // if(Main.InReforgeMenu){
        //     ...
        // } else if(Main.InGuideCraftMenu) {
        //     if(<closeGuideUI>) {
        //         <drop>
        //     } else {
        //         ++ guide:
        //         <guide>
        //         ++ if(!Main.InGuideCraftMenu) goto recipe;
        //     }
        // }
        // ...
        // if(<showRecipes>){
        //     ++ if(!Main.InGuideCraftMenu) goto guide;
        //     ++ recipe:
        //     <recipe>
        //     ...
        // }
        // ...

        ILCursor cursor = new(il);

        FieldInfo inGuideCraftMenu = typeof(Main).GetField(nameof(Main.InGuideCraftMenu), BindingFlags.Static | BindingFlags.Public)!;

        // Mark guide
        ILLabel? endGuide = null;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "DrawGuideCraftText"));
        cursor.GotoPrev(MoveType.After, i => i.MatchBr(out endGuide));
        ILLabel guide = cursor.DefineLabel();
        cursor.MarkLabel(guide);

        // Apply recipe
        cursor.GotoLabel(endGuide, MoveType.Before);
        ILLabel recipe = cursor.DefineLabel();
        cursor.EmitLdsfld(inGuideCraftMenu);
        cursor.EmitBrfalse(recipe);

        // Apply guide + Mark recipe
        cursor.GotoNext(i => i.MatchStsfld(typeof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts), nameof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig)));
        cursor.GotoPrev(MoveType.AfterLabel);
        cursor.EmitLdsfld(inGuideCraftMenu);
        cursor.EmitBrfalse(guide);
        cursor.MarkLabel(recipe);
    }
}