using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using SpikysLib.Extensions;
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
        foreach(ModEntityCatalogue catalogue in s_catalogues) ModConfigExtensions.SetInstance(catalogue, true);
        s_catalogues.Clear();
    }

    private static void HookRedirectCursor(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart) {
        if (!Configs.QuickSearch.IndividualKeybinds || !Configs.IndividualKeybinds.Value.composite || !s_redirect) orig(bonus, smart);
        else Reflection.Main.DrawInterface_36_Cursor.Invoke();
    }
    private static Vector2 HookRedirectThickCursor(On_Main.orig_DrawThickCursor orig, bool smart) => !Configs.QuickSearch.IndividualKeybinds || !Configs.IndividualKeybinds.Value.composite || !s_redirect ? orig(smart) : Vector2.Zero;
    private static void HookDrawInterfaceCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        s_redirect = false;
        if(!s_individualCanClick) {
            orig();
            return;
        }
        Main.cursorOverride = CursorOverrideID.Magnifiers;
        orig();
        s_redirect = true;
        Main.cursorOverride = -1;
    }
    private static void HookClickOverrideInterface(On_Main.orig_DrawInterface orig, Main self, GameTime time) {
        if (!s_individualCanClick) {
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
        s_individualCanClick = (QuickSearchKb.Current || !Configs.IndividualKeybinds.Value.composite) && (Configs.QuickSearch.Value.individualKeybinds.Key.HasFlag(Configs.SearchAction.Toggle) || !Main.HoverItem.IsAir);
        if (!s_individualCanClick) return;

        Main.LocalPlayer.mouseInterface = true;
        foreach(ModEntityCatalogue catalogue in EntityCatalogue) {
            if(catalogue.Enabled && catalogue.Keybind.JustPressed) {
                if (Configs.QuickSearch.Value.individualKeybinds.Key.HasFlag(Configs.SearchAction.Search) && !Main.HoverItem.IsAir) {
                    catalogue.Toggle(true);
                    if (Guide.forcedTooltip?.Key != $"{Localization.Keys.UI}.Unknown") {
                        catalogue.Search(Main.HoverItem);
                        SoundEngine.PlaySound(SoundID.Grab); 
                    } else {
                        SoundEngine.PlaySound(SoundID.MenuTick); 
                    }
                } else if (Configs.QuickSearch.Value.individualKeybinds.Key.HasFlag(Configs.SearchAction.Toggle)) {
                    catalogue.Toggle();
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
        }
    }
    private static void ProcessSharedKeybind() {
        if (QuickSearchKb.JustPressed) {
            if (s_timer >= Configs.SharedKeybind.Value.delay) s_taps = -1;
            s_timer = 0;
        }
        else if (QuickSearchKb.JustReleased) {
            if (s_timer < Configs.SharedKeybind.Value.tap) {
                bool first = s_taps == -1;
                if (first) {
                    s_sharedItem = Main.HoverItem.Clone();
                    s_enabledProviders = EntityCatalogue.Where(p => p.Enabled).ToList();
                    s_taps = Math.Max(s_enabledProviders.FindIndex(p => p.Visible), 0);
                }
                if (s_enabledProviders.Count == 0) return;

                int last = s_taps;
                if (!first) s_taps = (s_taps + 1) % s_enabledProviders.Count;

                if (Configs.QuickSearch.Value.sharedKeybind.Key.HasFlag(Configs.SearchAction.Search) && !s_sharedItem.IsAir) {
                    if (!first) s_enabledProviders[last].Toggle(false);
                    s_enabledProviders[s_taps].Toggle(true);
                    if (Guide.forcedTooltip?.Key != $"{Localization.Keys.UI}.Unknown") s_enabledProviders[s_taps].Search(s_sharedItem);
                    SoundEngine.PlaySound(SoundID.Grab);
                } else if (Configs.QuickSearch.Value.sharedKeybind.Key.HasFlag(Configs.SearchAction.Toggle)) {
                    if (first) s_enabledProviders[s_taps].Toggle();
                    else {
                        s_enabledProviders[last].Toggle(false);
                        s_enabledProviders[s_taps].Toggle(true);
                    }
                    SoundEngine.PlaySound(SoundID.MenuTick);
                } else {
                    s_taps = -1;
                }
            } else s_taps = -1;
            s_timer = 0;
        }
        s_timer++;
    }

    internal static void Register(ModEntityCatalogue catalogue) {
        ModConfigExtensions.SetInstance(catalogue);

        int before = s_catalogues.FindIndex(p => catalogue.ComparePositionTo(p) < 0 || p.ComparePositionTo(catalogue) > 0);
        if (before != -1) s_catalogues.Insert(before, catalogue);
        else s_catalogues.Add(catalogue);
    }

    public static ModEntityCatalogue? GetEntityCatalogue(string mod, string name) => s_catalogues.Find(p => p.Mod.Name == mod && p.Name == name);

    public static ModKeybind QuickSearchKb { get; private set; } = null!;
    public static ReadOnlyCollection<ModEntityCatalogue> EntityCatalogue => s_catalogues.AsReadOnly();

    private static readonly List<ModEntityCatalogue> s_catalogues = [];
    private static bool s_redirect = false;

    private static bool s_individualCanClick = false;

    private static int s_timer = 0, s_taps = 0;
    private static Item s_sharedItem = new();
    private static List<ModEntityCatalogue> s_enabledProviders = new();
}