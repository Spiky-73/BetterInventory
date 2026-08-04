using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.BetterItemPickup;

public sealed class FillMouseSlot : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterItemPickupConfig.FillMouseSlot;
    public void Load(Mod mod) {
        IL_Player.GetItem += il => il.TryEdit(ILGetItem, ref UnloadedBetterItemPickupConfig.Instance.fillMouseSlot);
    }
    public void Unload() { }

    private static void ILGetItem(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int coin, i => i.Previous.MatchCallvirt(Reflection.Item.IsACoin.GetMethod!), 0);
        cursor.GotoNextLoc(out int returnItem, i => i.Previous.MatchLdarg2(), 1);

        // ...
        // if (newItem.uniqueStack && this.HasItem(newItem.type)) return item;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Player.HasItem));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdloc(coin));

        cursor.EmitLdarg0().EmitLdarg1().EmitLdloc(returnItem).EmitLdarg3();
        cursor.EmitDelegate((Player player, int plr, Item item, GetItemSettings settings) => {
            if (!BetterItemPickupConfig.FillMouseSlot) return item;
            if (Main.mouseItem.IsAir || Main.mouseItem.stack >= Main.mouseItem.maxStack || !Main.mouseItem.IsTheSameAs(item)) return item;
            ItemLoader.TryStackItems(Main.mouseItem, item, out var numTransferred);
            SoundEngine.PlaySound(SoundID.Grab);
            Main.mouseItem.position = player.position;
            if (!settings.NoText) PopupText.NewText(PopupTextContext.ItemPickupToVoidContainer, Main.mouseItem, numTransferred, false, settings.LongText);
            return item;
        });
        cursor.EmitDup();
        cursor.EmitStloc(returnItem);

        // ++if (newItem.IsAir) return new()
        cursor.EmitDelegate((Item item) => item.IsAir);
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrfalse(skip);
        cursor.EmitDelegate(() => new Item());
        cursor.EmitRet();
        cursor.MarkLabel(skip);


        // ++ item = <previousSlot>
    }

}