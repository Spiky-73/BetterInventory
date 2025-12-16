using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib.Constants;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;
using Terraria.GameInput;

namespace BetterInventory.Features.QuickMove;

public sealed class QuickMovePlayer : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => !Configs.Compatibility.CompatibilityMode || FeaturesConfig.QuickMove;

    public override void Load() {
        On_Main.DrawInventory += HookDrawInventory;
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (!FeaturesConfig.QuickMove) return;
        MoveChainInputs.ProcessTriggers(triggersSet);
    }

    private static void HookDrawInventory(On_Main.orig_DrawInventory orig, Main self) {
        orig(self);
        if (!FeaturesConfig.QuickMove) return;
        MoveChainInputs.PostDrawInventory();
        if (QuickMoveConfig.DisplayHotkeys) MoveChainUI.PostDrawInventory();
    }

    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        if (!FeaturesConfig.QuickMove) return false;
        MoveChainInputs.HoverSlot(inventory, context, slot);
        if (QuickMoveConfig.DisplayHotkeys || QuickMoveConfig.ItemTooltip) MoveChainUI.HoverSlot(inventory, context, slot);
        return false;
    }
}


public sealed class QuickMoveItem : GlobalItem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || FeaturesConfig.QuickMove;

    public override void Load() {
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookItemSlotDraw;
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += il => il.TryEdit(ILHideHotbarText, ref UnloadedQuickMoveConfig.Instance.displayedHotkeys);
    }

    private static void HookItemSlotDraw(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        orig(spriteBatch, inv, context, slot, position, lightColor);
        if (!FeaturesConfig.QuickMove) return;
        MoveChainInputs.PostDrawItemSlot(position);
        if (QuickMoveConfig.DisplayHotkeys) MoveChainUI.PostDrawItemSlot(spriteBatch, inv, context, slot, position);
    }

    private static void ILHideHotbarText(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // if(...) {
        // } else if (context == 6) {
        //     ...
        //     spriteBatch.Draw(value10, position4, null, new Color(100, 100, 100, 100), 0f, default(Vector2), inventoryScale, 0, 0f);
        // }
        // if (context == 0 && ++[!<hideKeys> &&] slot < 10) {
        //     ...
        // }
        // if (gamepadPointForSlot != -1) {
        //     UILinkPointNavigator.SetPosition(gamepadPointForSlot, position + vector * 0.75f);
        // }
        cursor.GotoNext(i => i.MatchCall(() => UILinkPointNavigator.SetPosition));
        cursor.GotoPrev(i => i.SaferMatchCall(typeof(ChatManager), nameof(ChatManager.DrawColorCodedStringWithShadow)));
        cursor.GotoPrev(i => i.MatchLdarg2());
        cursor.GotoNext(MoveType.After, i => i.MatchLdarg3());

        cursor.EmitDelegate((int slot) => {
            if (!FeaturesConfig.QuickMove) return slot;
            return QuickMoveConfig.DisplayHotkeys && MoveChainUI.HideHotbarText() ? InventorySlots.Hotbar.End : slot;
        });
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!FeaturesConfig.QuickMove) return;
        if (QuickMoveConfig.ItemTooltip) MoveChainUI.ModifyTooltips(tooltips);
    }
}
