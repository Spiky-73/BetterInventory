using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using SpikysLib.Configs;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.ItemSearch;

public sealed class QuickSearch : ILoadable {

    public void Load(Mod mod) {
        QuickSearchKb = KeybindLoader.RegisterKeybind(mod, "QuickSearch", Microsoft.Xna.Framework.Input.Keys.N);

        On_Main.DrawCursor += HookRedirectCursor;
        On_Main.DrawThickCursor += HookRedirectThickCursor;
        On_Main.DrawInterface_36_Cursor += HookDrawInterfaceCursor;
        On_Main.DrawInterface += HookClickOverrideInterface;
    }

    public void Unload() {
        foreach(ModEntityCatalogue catalogue in s_catalogues) ConfigHelper.SetInstance(catalogue, true);
        s_catalogues.Clear();
    }

    private static void HookRedirectCursor(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart) {
        if (!Configs.QuickSearch.IndividualKeybinds || !Configs.IndividualKeybinds.Value.composite || !s_redirect) orig(bonus, smart);
        else Reflection.Main.DrawInterface_36_Cursor.Invoke();
    }
    private static Vector2 HookRedirectThickCursor(On_Main.orig_DrawThickCursor orig, bool smart) => !Configs.QuickSearch.IndividualKeybinds || !Configs.IndividualKeybinds.Value.composite || !s_redirect ? orig(smart) : Vector2.Zero;
    private static void HookDrawInterfaceCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        s_redirect = false;
        if(!s_customCursor) {
            orig();
            return;
        }
        Main.cursorOverride = CursorOverrideID.Magnifiers;
        orig();
        s_redirect = true;
        Main.cursorOverride = -1;
    }
    private static void HookClickOverrideInterface(On_Main.orig_DrawInterface orig, Main self, GameTime time) {
        if (!s_customCursor) {
            orig(self, time);
        } else {
            (bool left, Main.mouseLeft, bool right, Main.mouseRight) = (Main.mouseLeft, false, Main.mouseRight, false);
            orig(self, time);
            (Main.mouseLeft, Main.mouseRight) = (left, right);
        }
    }

    public static void ProcessTriggers() {
        if (!Configs.QuickSearch.Enabled) return;
        if (Configs.QuickSearch.IndividualKeybinds) ProcessIndividualKeybinds();
        if (Configs.QuickSearch.SharedKeybind) ProcessSharedKeybind();
    }

    private static void ProcessIndividualKeybinds() {
        s_customCursor = false;
        if(Configs.IndividualKeybinds.Value.composite && !QuickSearchKb.Current) return;
        if(!CanQuickSearch(Configs.QuickSearch.Value.individualKeybinds, Main.HoverItem, out var canSearch, out var _)) return;

        if (Configs.IndividualKeybinds.Value.composite) {
            s_customCursor = true;
            Main.LocalPlayer.mouseInterface = true;
        }

        // if (canSearch && !CanSearch(Main.HoverItem)) return;

        foreach(ModEntityCatalogue catalogue in EntityCatalogues) {
            if (!catalogue.Enabled || !catalogue.Keybind.JustPressed) continue;
            if (canSearch) QuickItemSearch(catalogue, Main.HoverItem);
            else QuickToggle(catalogue);
        }
    }
    private static void ProcessSharedKeybind() {
        if (QuickSearchKb.JustPressed) {
            if (s_timer >= Configs.SharedKeybind.Value.delay) s_provider = -1;
            s_timer = 0;
        }
        else if (QuickSearchKb.JustReleased) {
            if (s_timer >= Configs.SharedKeybind.Value.tap) s_provider = -1;
            else {
                bool first = s_provider == -1;
                if (first) {
                    if(!CanQuickSearch(Configs.QuickSearch.Value.sharedKeybind, Main.HoverItem, out s_sharedSearch, out _)) return;
                    // if (s_sharedSearch && !CanSearch(Main.HoverItem)) return;
                    s_sharedItem = Main.HoverItem.Clone();
                    s_enabledProviders = EntityCatalogues.Where(p => p.Enabled).ToList();
                    s_provider = Math.Max(s_enabledProviders.FindIndex(p => p.Visible), 0);
                }
                if (s_provider == -1 || s_enabledProviders.Count == 0) return;

                if (!first) {
                    s_enabledProviders[s_provider].Toggle(false);
                    s_provider = (s_provider + 1) % s_enabledProviders.Count;
                }
                if (s_sharedSearch) QuickItemSearch(s_enabledProviders[s_provider], s_sharedItem);
                else QuickToggle(s_enabledProviders[s_provider]);
            }
            s_timer = 0;
        }
        s_timer++;
    }

    public static bool CanQuickSearch(Configs.SearchAction actions, Item item, out bool canSearch, out bool canToggle) {
        canSearch = actions.HasFlag(Configs.SearchAction.Search) && !item.IsAir && !GuideUnknownDisplayPlayer.IsUnknown(item);
        canToggle = actions.HasFlag(Configs.SearchAction.Toggle);
        return canSearch || canToggle;
    }
    // public static bool CanSearch(Item item) => !GuideUnknownDisplay.IsUnknown(item);

    public static void QuickItemSearch(ModEntityCatalogue catalogue, Item item) {
        catalogue.Toggle(true);
        catalogue.Search(item);
        SoundEngine.PlaySound(SoundID.Grab);
    }
    public static void QuickToggle(ModEntityCatalogue catalogue) {
        catalogue.Toggle();
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    internal static void Register(ModEntityCatalogue catalogue) {
        ConfigHelper.SetInstance(catalogue);

        int before = s_catalogues.FindIndex(p => catalogue.ComparePositionTo(p) < 0 || p.ComparePositionTo(catalogue) > 0);
        if (before != -1) s_catalogues.Insert(before, catalogue);
        else s_catalogues.Add(catalogue);
    }

    public static ModEntityCatalogue? GetEntityCatalogue(string mod, string name) => s_catalogues.Find(p => p.Mod.Name == mod && p.Name == name);

    public static ModKeybind QuickSearchKb { get; private set; } = null!;
    public static ReadOnlyCollection<ModEntityCatalogue> EntityCatalogues => s_catalogues.AsReadOnly();

    private static readonly List<ModEntityCatalogue> s_catalogues = [];
    private static bool s_redirect = false;

    private static bool s_customCursor = false;

    private static bool s_sharedSearch;
    private static int s_timer = 0, s_provider = 0;
    private static Item s_sharedItem = new();
    private static List<ModEntityCatalogue> s_enabledProviders = [];
}