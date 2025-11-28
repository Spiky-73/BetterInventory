using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.VanillaFixes;

public sealed class ConsistantScrollDirection : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => !Configs.Compatibility.CompatibilityMode || Configs.VanillaFixes.ConsistantScrollDirection;
    public void Load(Mod mod) {
        IL_Player.Update += static il => {
            if (!il.ApplyTo(ILFixRecipeScrollUpdate, Configs.ConsistantScrollDirection.RecipesUnpaused)) Configs.UnloadedVanillaFixes.Instance.consistantScrollDirection_recipesUnpaused = true;
        };
        IL_Main.DoUpdate_WhilePaused += static il => {
            if (!il.ApplyTo(ILFixRecipeScrollWhilePaused, Configs.ConsistantScrollDirection.RecipesPaused)) Configs.UnloadedVanillaFixes.Instance.consistantScrollDirection_recipesPaused = true;
        };
        MonoModHooks.Modify(Reflection.AccessorySlotLoader.DrawScrollbar, static il => {
            if (!il.ApplyTo(ILFixAccessoryScroll, Configs.ConsistantScrollDirection.Accessories)) Configs.UnloadedVanillaFixes.Instance.consistantScrollDirection_accessories = true;
        });

    }
    public void Unload() { }


    private static void ILFixRecipeScrollUpdate(ILContext il) {
        ILCursor cursor = new(il);

        // int num8 = Player.GetMouseScrollDelta();
        cursor.GotoNextLoc(out var offset, i => i.Previous.MatchCall(Reflection.Player.GetMouseScrollDelta), 41);
        // if (Main.recBigList) ...
        // else {
        //     Main.focusRecipe += ++[-1 *] num8;
        cursor.GotoNext(i => i.MatchStsfld(Reflection.Main.focusRecipe));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdloc(offset));
        cursor.EmitDelegate((int offset) => Configs.ConsistantScrollDirection.RecipesUnpaused ? -offset : offset);
        // }
    }

    private static void ILFixRecipeScrollWhilePaused(ILContext il) {
        ILCursor cursor = new(il);

        // int num = ++[-1 *] PlayerInput.ScrollWheelDelta / 120;
        cursor.GotoNext(MoveType.After, i => i.MatchLdsfld(Reflection.PlayerInput.ScrollWheelDelta));
        cursor.EmitDelegate((int ScrollWheelDelta) => Configs.ConsistantScrollDirection.RecipesUnpaused ? -ScrollWheelDelta : ScrollWheelDelta);
    }

    private static void ILFixAccessoryScroll(ILContext il) {
        ILCursor cursor = new(il);
        // int scrollDelta = AccessorySlotLoader.ModSlotPlayer(AccessorySlotLoader.Player).scrollbarSlotPosition + ++[-1 *] PlayerInput.ScrollWheelDelta / 120;
        cursor.GotoNext(MoveType.After, i => i.MatchLdsfld(Reflection.PlayerInput.ScrollWheelDelta));
        cursor.EmitDelegate((int ScrollWheelDelta) => Configs.ConsistantScrollDirection.Accessories ? -ScrollWheelDelta : ScrollWheelDelta);
    }

}