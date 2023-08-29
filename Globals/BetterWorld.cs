using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.Globals;

public sealed class BetterWorld : ModSystem {
    public override void Load() {
        IL_Main.DrawInventory += IlDrawInventory;
        On_Player.dropItemCheck += OndropItems;
        On_Main.DrawInterface_26_InterfaceLogic3 += OnLogic;
    }

    private static void OnLogic(On_Main.orig_DrawInterface_26_InterfaceLogic3 orig) {
        if(!Main.playerInventory) InRecipes = false;
        orig();
    }

    private static void OndropItems(On_Player.orig_dropItemCheck orig, Player self) {
        bool old = Main.InGuideCraftMenu;
        Main.InGuideCraftMenu = InRecipes;
        orig(self);
        Main.InGuideCraftMenu = old;
    }

    private static void IlDrawInventory(ILContext il) {
        // if(Main.InReforgeMenu){
        //     ...
        // } else {
        //     ++ goto skip
        //     if(Main.InGuideCraftMenu) {
        //         if(...) {
        //             <drop>
        //             ++ goto end
        //         } else {
        //             <guide>
        //             ++ goto recipe
        //         }
        //     }
        // }
        // skip:
        // ...
        // if(<show recipes>){
        //     ++ Main.InGuideCraftMenu = true;
        //     ++ goto guide
        //     <recipe>
        //     ...
        //     ++ goto skipHammer
        //     if(InGuideCraftMenu){
        //         <moveHammer>
        //     }
        //     <postHammer>
        //     ...
        // }
        // ++ else if(InGuideCraftMenu) goto drop
        // <end>
        
        ILCursor cursor = new(il);

        FieldInfo inGuideCraftMenu = typeof(Main).GetField(nameof(Main.InGuideCraftMenu), BindingFlags.Static | BindingFlags.Public)!;

        // Find + Apply skip
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "DrawGuideCraftText"));
        cursor.GotoPrev(i => i.MatchLdsfld(inGuideCraftMenu));
        IEnumerable<ILLabel> originalLabels = cursor.IncomingLabels;
        ILLabel? skip = null;
        cursor.GotoPrev(i => i.MatchBr(out skip));
        foreach(ILLabel label in originalLabels) label.Target = skip.Target; // ? change with emit br

        // Find guide + Mark drop
        ILLabel? guide = null;
        cursor.GotoNext(i => i.MatchCallvirt(typeof(Player), nameof(Player.dropItemCheck)));
        cursor.GotoPrev(i => i.MatchBrfalse(out guide));
        cursor.GotoNext();
        ILLabel drop = cursor.DefineLabel();
        cursor.MarkLabel(drop);
        cursor.EmitDelegate<Action>(() => InRecipes = false);

        // Apply end
        ILLabel end = cursor.DefineLabel();
        ILLabel? endIf = null;
        cursor.GotoNext(i => i.MatchBr(out endIf));
        cursor.Remove();
        cursor.EmitBr(end);

        // Apply recipe
        ILLabel? recipe = cursor.DefineLabel();
        cursor.GotoLabel(endIf, MoveType.Before);
        cursor.EmitBr(recipe);

        // Apply guide + Mark recipe
        cursor.GotoNext(i => i.MatchStsfld(typeof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts), nameof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig)));
        cursor.GotoPrev(MoveType.AfterLabel);
        cursor.EmitDelegate<Action>(() => {
            Main.InGuideCraftMenu = Main.player[Main.myPlayer].chest == -1 && Main.npcShop == 0 && !Main.InReforgeMenu;
            InRecipes = true;
        });
        cursor.EmitBr(guide);
        cursor.GotoNext(MoveType.AfterLabel);
        cursor.MarkLabel(recipe);

        // Find new drop entry
        ILLabel? noRecipe = null;
        cursor.GotoPrev(i => i.MatchBrtrue(out noRecipe));

        // Mark + Apply noHammer
        ILLabel? postHammer = null;
        cursor.GotoNext(i => i.MatchStloc(138));
        cursor.GotoNext(i => i.MatchLdsfld(inGuideCraftMenu) && i.Next.MatchBrfalse(out postHammer));
        cursor.EmitBr(postHammer);

        // Find + Apply end
        cursor.GotoLabel(noRecipe, MoveType.AfterLabel);
        cursor.EmitLdloc(16);
        cursor.EmitDelegate((bool flag10) => (Main.InReforgeMenu || Main.LocalPlayer.tileEntityAnchor.InUse || flag10) && InRecipes);
        cursor.EmitBrtrue(drop);
        cursor.GotoNext();
        cursor.MarkLabel(end);
    }

    public static bool InRecipes { get; private set; }
}