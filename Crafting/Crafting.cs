using System.Collections.Generic;
using BetterInventory.Default.Inventories;
using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class Crafting : ModPlayer {

    public override void Load() {
        On_ItemSlot.RecordLoadoutChange += HookSwapLoadout;
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILCraftOnList, Configs.CraftOnList.Enabled)) Configs.UnloadedCrafting.Value.craftOnList = true;
        };
    }

    private static void ILCraftOnList(ILContext il) {

        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int recipeListIndex, i => i.Previous.MatchLdsfld(Reflection.Main.recStart), 153);
        
        // if(<recBigListVisible>) {
        //     ...
        //     while (<showingRecipes>) {
        //         ...
        //         if (<mouseHover>) {
        //             Main.player[Main.myPlayer].mouseInterface = true;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Main.LockCraftingForThisCraftClickDuration));
        cursor.GotoPrev(MoveType.After, i => i.MatchStfld(Reflection.Player.mouseInterface));

        ILLabel skipVanillaHover = null!;
        cursor.FindPrev(out _, i => i.MatchBrtrue(out skipVanillaHover!));

        //             if(++[!craftInList] &&<click>) {
        cursor.EmitLdloc(recipeListIndex);
        cursor.EmitDelegate((int i) => {
            if (!Configs.CraftOnList.Enabled) return false;
            int f = Main.focusRecipe;
            if (Configs.CraftOnList.Value.focusHovered) Main.focusRecipe = i;
            Reflection.Main.HoverOverCraftingItemButton.Invoke(i);
            if (f != Main.focusRecipe) Main.recFastScroll = true;
            Main.craftingHide = false;
            return true;
        });
        cursor.EmitBrtrue(skipVanillaHover);
        //                 <scrollList>
        //                 ...
        //             }
        //             ...
        //         }

        cursor.GotoLabel(skipVanillaHover, MoveType.AfterLabel);
        cursor.EmitLdloc(recipeListIndex);
        cursor.EmitDelegate((int i) => {
            if (!Configs.CraftOnList.Enabled) return;
            if (Main.numAvailableRecipes > 0 && Main.focusRecipe == i && !Configs.CraftOnList.Value.focusHovered) ItemSlot.DrawGoldBGForCraftingMaterial = true;
        });
        //     }
        // }
    }

    private void HookSwapLoadout(On_ItemSlot.orig_RecordLoadoutChange orig) {
        orig();
        if (Configs.MoreMaterials.Equipment) Recipe.FindRecipes();
    }
    public override IEnumerable<Item> AddMaterialsForCrafting(out ItemConsumedCallback? itemConsumedCallback) {
        itemConsumedCallback = (item, index) => {
            if (item == Main.mouseItem) item.stack -= Reflection.RecipeLoader.ConsumedItems.GetValue()[^1].stack; // FIXME seems hacky
            return;
        };
        List<Item> materials = [];
        if (Configs.MoreMaterials.Mouse && Main.myPlayer == Player.whoAmI) materials.Add(Main.mouseItem);
        if (Configs.MoreMaterials.Equipment) {
            void AddSubInventory(ModSubInventory template) {
                var inventories = Configs.MoreMaterials.Value.equipment.Value.allLoadouts ? template.GetInventories(Player) : template.GetActiveInventories(Player);
                foreach (var subInventory in inventories) materials.AddRange(subInventory.Items);
            }

            AddSubInventory(ModContent.GetInstance<HeadArmor>());
            AddSubInventory(ModContent.GetInstance<BodyArmor>());
            AddSubInventory(ModContent.GetInstance<LegArmor>());
            AddSubInventory(ModContent.GetInstance<HeadVanity>());
            AddSubInventory(ModContent.GetInstance<BodyVanity>());
            AddSubInventory(ModContent.GetInstance<LegVanity>());
            AddSubInventory(ModContent.GetInstance<Accessories>());
            AddSubInventory(ModContent.GetInstance<VanityAccessories>());
            AddSubInventory(ModContent.GetInstance<SharedAccessories>());
            AddSubInventory(ModContent.GetInstance<SharedVanityAccessories>());
            AddSubInventory(ModContent.GetInstance<ArmorDyes>());
            AddSubInventory(ModContent.GetInstance<AccessoryDyes>());
            AddSubInventory(ModContent.GetInstance<SharedAccessoryDyes>());
            AddSubInventory(ModContent.GetInstance<EquipmentDyes>());
        }
        return materials;
    }
}
