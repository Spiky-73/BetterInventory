using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using TTextureAssets = Terraria.GameContent.TextureAssets;
using TAsset_T2D = ReLogic.Content.Asset<Microsoft.Xna.Framework.Graphics.Texture2D>;
using SpikysLib.Reflection;

using TBestiary = Terraria.GameContent.UI.States.UIBestiaryTest;
using TSearchBar = Terraria.GameContent.UI.Elements.UISearchBar;
using TIcon = Terraria.GameContent.UI.Elements.UIBestiaryEntryIcon;
using TButton = Terraria.GameContent.UI.Elements.UIBestiaryEntryButton;
using TPage = Terraria.GameContent.UI.Elements.UIBestiaryEntryInfoPage;
using TFilterGrid = Terraria.GameContent.UI.Elements.UIBestiaryFilteringOptionsGrid;
using TItemDropE = Terraria.GameContent.Bestiary.ItemDropBestiaryInfoElement;
using Terraria.GameContent.ItemDropRules;
using Terraria.UI;

namespace BetterInventory.Reflection;

public static class UIBestiaryTest {
    public static readonly Field<TBestiary, List<BestiaryEntry>> _workingSetEntries = new(nameof(_workingSetEntries));
    public static readonly Field<TBestiary, TSearchBar> _searchBar = new(nameof(_searchBar));
    public static readonly Field<TBestiary, TButton> _selectedEntryButton = new(nameof(_selectedEntryButton));
    public static readonly Field<TBestiary, UIBestiaryEntryGrid> _entryGrid = new(nameof(_entryGrid));
    public static readonly Method<TBestiary, object?> SelectEntryButton = new(nameof(SelectEntryButton), typeof(TButton));
}

public static class UIBestiaryEntryIcon {
    public static readonly Field<TIcon, BestiaryEntry> _entry = new(nameof(_entry));
    public static readonly Field<TIcon, BestiaryUICollectionInfo> _collectionInfo = new(nameof(_collectionInfo));
}

public static class UIBestiaryEntryButton {
    public static readonly Field<TButton, UIImage> _borders = new(nameof(_borders));
    public static readonly Field<TButton, UIImage> _bordersGlow = new(nameof(_bordersGlow));
}

public static class UIBestiaryEntryInfoPage {
    public static readonly Field<TPage, UIList> _list = new(nameof(_list));
}

public static class UIBestiaryFilteringOptionsGrid {
    public static readonly Field<TFilterGrid, UIElement> _container = new(nameof(_container));
    public static readonly Field<TFilterGrid, EntryFilterer<BestiaryEntry, IBestiaryEntryFilter>> _filterer = new(nameof(_filterer));
    public static readonly Field<TFilterGrid, List<Terraria.GameContent.UI.Elements.GroupOptionButton<int>>> _filterButtons = new(nameof(_filterButtons));
    public static readonly Field<TFilterGrid, List<List<BestiaryEntry>>> _filterAvailabilityTests = new(nameof(_filterAvailabilityTests));
    public static readonly Method<TFilterGrid, bool> GetIsFilterAvailableForEntries = new(nameof(GetIsFilterAvailableForEntries), typeof(IBestiaryEntryFilter), typeof(List<BestiaryEntry>));
}

public static class GroupOptionButton<T> {
    public static readonly Field<Terraria.GameContent.UI.Elements.GroupOptionButton<T>, Color> _color = new(nameof(_color));
}

public static class ItemDropBestiaryInfoElement {
    public static readonly Field<TItemDropE, DropRateInfo> _droprateInfo = new(nameof(_droprateInfo));
}
public static class UISearchBar {
    public static readonly Field<TSearchBar, string> actualContents = new(nameof(actualContents));
}

public static class TextureAssets {
    public static readonly StaticField<TAsset_T2D> CraftUpButton = new(typeof(TTextureAssets), nameof(TTextureAssets.CraftUpButton));
    public static readonly StaticField<TAsset_T2D> CraftDownButton = new(typeof(TTextureAssets), nameof(TTextureAssets.CraftDownButton));
    public static readonly StaticField<TAsset_T2D[]> CraftToggle = new(typeof(TTextureAssets), nameof(TTextureAssets.CraftToggle));
    public static readonly StaticField<TAsset_T2D> InventoryBack10 = new(typeof(TTextureAssets), nameof(TTextureAssets.InventoryBack10));
}

public static class Asset<T> where T : class {
    public static readonly Property<ReLogic.Content.Asset<T>, T> Value = new(nameof(ReLogic.Content.Asset<T>.Value));
}