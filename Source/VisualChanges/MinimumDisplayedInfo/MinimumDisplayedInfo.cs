using System;
using MonoMod.Cil;
using SpikysLib.IL;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace BetterInventory.VisualChanges.MinimalDisplayedInfo;

public sealed class MinimumDisplayedInfo : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || VisualChangesConfig.MinimalDisplayedInfo;

    public void Load(Mod mod) {
        IL_Filters.BySearch.FitsFilter += il => il.TryEdit(ILSearchAddEntries, ref UnloadedVisualChangesConfig.Instance.minimalDisplayedInfo);

        IL_UIBestiaryEntryIcon.Update += il => il.TryEdit(ILIconUpdateFakeUnlock, ref UnloadedVisualChangesConfig.Instance.minimalDisplayedInfo);
        IL_UIBestiaryEntryInfoPage.AddInfoToList += il => il.TryEdit(IlEntryPageFakeUnlock, ref UnloadedVisualChangesConfig.Instance.minimalDisplayedInfo);
    }

    public void Unload() { }

    public static BestiaryEntryUnlockState GetDisplayedUnlockLevel(BestiaryEntryUnlockState state) => state < (BestiaryEntryUnlockState)MinimalDisplayedInfoConfig.Instance.unlockLevel ? (BestiaryEntryUnlockState)MinimalDisplayedInfoConfig.Instance.unlockLevel : state;

    private static void ILSearchAddEntries(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // BestiaryUICollectionInfo info = entry.UIInfoProvider.GetEntryUICollectionInfo();
        cursor.GotoNext(i => i.MatchCallvirt((IBestiaryUICollectionInfoProvider i) => i.GetEntryUICollectionInfo));
        cursor.GotoNextLoc(MoveType.Before, out _, i => true, 0);

        // ++ <fakeUnlock> 
        cursor.EmitDelegate((BestiaryUICollectionInfo info) => {
            if (VisualChangesConfig.MinimalDisplayedInfo && info.UnlockState > BestiaryEntryUnlockState.NotKnownAtAll_0) info.UnlockState = GetDisplayedUnlockLevel(info.UnlockState);
            return info;
        });
        // ...
    }

    private static void ILIconUpdateFakeUnlock(ILContext il) {
        ILCursor cursor = new(il);

        // this._collectionInfo = this._entry.UIInfoProvider.GetEntryUICollectionInfo();
        cursor.GotoNext(i => i.MatchCallvirt((IBestiaryUICollectionInfoProvider i) => i.GetEntryUICollectionInfo));
        cursor.GotoNext(MoveType.Before, i => i.MatchStfld((UIBestiaryEntryIcon i) => i._collectionInfo));

        // ++ <fakeUnlock> 
        cursor.EmitDelegate((BestiaryUICollectionInfo info) => {
            if (VisualChangesConfig.MinimalDisplayedInfo && info.UnlockState > BestiaryEntryUnlockState.NotKnownAtAll_0) info.UnlockState = GetDisplayedUnlockLevel(info.UnlockState);
            return info;
        });
        // ...
    }

    private static void IlEntryPageFakeUnlock(ILContext il) {
        ILCursor cursor = new(il);

        // BestiaryUICollectionInfo uICollectionInfo = this.GetUICollectionInfo(entry, extraInfo);
        cursor.GotoNext(i => i.SaferMatchCall((UIBestiaryEntryInfoPage i) => i.GetUICollectionInfo));
        cursor.GotoNextLoc(MoveType.Before, out _, i => true, 0);

        // ++ <fakeUnlock>
        cursor.EmitDelegate((BestiaryUICollectionInfo info) => {
            if (VisualChangesConfig.MinimalDisplayedInfo && info.UnlockState > BestiaryEntryUnlockState.NotKnownAtAll_0) info.UnlockState = GetDisplayedUnlockLevel(info.UnlockState);
            return info;
        });
        // ...
    }
}