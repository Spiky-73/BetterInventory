using MonoMod.Cil;
using SpikysLib;
using SpikysLib.IL;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace BetterInventory.VanillaPatches;

public sealed class ConsistantScrollDirection : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || VanillaPatchesConfig.ConsistantScrollDirection;
    public void Load(Mod mod) {
        IL_Player.Update += il => il.TryEdit(ILFixRecipeScrollUpdate, ref UnloadedConsistantScrollDirectionConfig.Instance.recipesUnpaused);
        IL_Main.DoUpdate_WhilePaused += il => il.TryEdit(ILFixRecipeScrollWhilePaused, ref UnloadedConsistantScrollDirectionConfig.Instance.recipesPaused);
        MonoModHooks.Modify(TypeHelper.GetMethod((AccessorySlotLoader i) => i.DrawScrollbar), il => il.TryEdit(ILFixAccessoryScroll, ref UnloadedConsistantScrollDirectionConfig.Instance.accessories));
    }
    public void Unload() { }

    private static void ILFixRecipeScrollUpdate(ILContext il) {
        ILCursor cursor = new(il);

        // int num8 = Player.GetMouseScrollDelta();
        cursor.GotoNextLoc(out var offset, i => i.Previous.MatchCall(() => Player.GetMouseScrollDelta), 41);
        // if (Main.recBigList) ...
        // else {
        //     Main.focusRecipe += ++[-1 *] num8;
        cursor.GotoNext(i => i.MatchStsfld(() => Main.focusRecipe));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdloc(offset));
        cursor.EmitDelegate((int offset) => VanillaPatchesConfig.ConsistantScrollDirection && ConsistantScrollDirectionConfig.RecipesUnpaused ? -offset : offset);
        // }
    }

    private static void ILFixRecipeScrollWhilePaused(ILContext il) {
        ILCursor cursor = new(il);

        // int num = ++[-1 *] PlayerInput.ScrollWheelDelta / 120;
        cursor.GotoNext(MoveType.After, i => i.MatchLdsfld(() => PlayerInput.ScrollWheelDelta));
        cursor.EmitDelegate((int ScrollWheelDelta) => VanillaPatchesConfig.ConsistantScrollDirection && ConsistantScrollDirectionConfig.RecipesUnpaused ? -ScrollWheelDelta : ScrollWheelDelta);
    }

    private static void ILFixAccessoryScroll(ILContext il) {
        ILCursor cursor = new(il);
        // int scrollDelta = AccessorySlotLoader.ModSlotPlayer(AccessorySlotLoader.Player).scrollbarSlotPosition + ++[-1 *] PlayerInput.ScrollWheelDelta / 120;
        cursor.GotoNext(MoveType.After, i => i.MatchLdsfld(() => PlayerInput.ScrollWheelDelta));
        cursor.EmitDelegate((int ScrollWheelDelta) => VanillaPatchesConfig.ConsistantScrollDirection && ConsistantScrollDirectionConfig.Accessories ? -ScrollWheelDelta : ScrollWheelDelta);
    }

}