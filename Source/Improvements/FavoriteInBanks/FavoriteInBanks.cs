using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.Improvements.FavoriteInBanks;

public sealed class FavoriteInBanksPlayer : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || ImprovementsConfig.FavoriteInBanks;
    public override void Load() {
        IL_ItemSlot.LeftClick_ItemArray_int_int += static il => {
            il.TryEdit(ILKeepFavoriteInBanks, ref UnloadedImprovementsConfig.Instance.favoriteInBanks);
        };
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += static il => {
            il.TryEdit(ILFavoritedBankBackground, ref UnloadedImprovementsConfig.Instance.favoriteInBanks);
        };

        On_ChestUI.LootAll += HookLootAll;
        On_ChestUI.Restock += HookRestock;
    }

    public static void OnConfigChanged() {
        ItemSlot.canFavoriteAt[ItemSlot.Context.BankItem] = ImprovementsConfig.FavoriteInBanks;
    }

    private static void ILKeepFavoriteInBanks(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.Before, i => i.MatchStfld((Item i) => i.favorited));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitDelegate((bool fav, Item[] inv, int context, int slot) => {
            if (ImprovementsConfig.FavoriteInBanks && context == ItemSlot.Context.BankItem) fav = inv[slot].favorited;
            return fav;
        });
    }
    private static void ILFavoritedBankBackground(ILContext il) {
        ILCursor cursor = new(il);

        // if (item.type > 0 && item.stack > 0 && item.favorited && context != 13 && context != 21 && context != 22 && context != 14) {
        //     value = TextureAssets.InventoryBack10.Value;
        //     ++ <favorited>
        cursor.GotoNext(MoveType.After, i => i.MatchGetppt((Asset<Texture2D> a) => a.Value) && i.Previous.MatchLdsfld(() => TextureAssets.InventoryBack10));
        cursor.EmitLdarg1().EmitLdarg2().EmitLdarg3();
        cursor.EmitDelegate((Texture2D texture, Item[] inv, int context, int slot) => {
            if (!ImprovementsConfig.FavoriteInBanks || context != ItemSlot.Context.BankItem || !inv[slot].favorited) return texture;
            return TextureAssets.InventoryBack19.Value;
        });
        // }
    }

    private static void HookRestock(On_ChestUI.orig_Restock orig) => HookChestAction(() => orig());
    private static void HookLootAll(On_ChestUI.orig_LootAll orig) => HookChestAction(() => orig());
    private static void HookChestAction(Action orig) {
        ChestUI.GetContainerUsageInfo(out bool sync, out Item[] items);
        if (!sync && ImprovementsConfig.FavoriteInBanks) ItemHelper.RunWithHiddenItems(items, () => orig(), i => i.favorited);
        else orig();
    }

    public override void SaveData(TagCompound tag) {
        if (TrySaveBank(Player.bank, out var bankSlots)) tag[PiggyTag] = bankSlots;
        if (TrySaveBank(Player.bank2, out var bank2Slots)) tag[SafeTag] = bank2Slots;
        if (TrySaveBank(Player.bank3, out var bank3Slots)) tag[ForgeTag] = bank3Slots;
    }
    public override void LoadData(TagCompound tag) {
        if (tag.TryGet(PiggyTag, out List<int> bankSlots)) LoadBank(Player.bank, bankSlots);
        if (tag.TryGet(SafeTag, out List<int> bank2Slots)) LoadBank(Player.bank2, bank2Slots);
        if (tag.TryGet(ForgeTag, out List<int> bank3Slots)) LoadBank(Player.bank3, bank3Slots);
    }

    public static bool TrySaveBank(Chest bank, out List<int> favoritedSlots) {
        favoritedSlots = bank.item.Where(item => item.favorited).Select((_, index) => index).ToList();
        return favoritedSlots.Count > 1;
    }
    public static void LoadBank(Chest bank, List<int> favoritedSlots) {
        foreach (int i in favoritedSlots) bank.item[i].favorited = true;
    }

    public const string PiggyTag = "piggy";
    public const string SafeTag = "safe";
    public const string ForgeTag = "forge";
}