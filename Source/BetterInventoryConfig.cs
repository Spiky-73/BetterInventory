using System;
using System.ComponentModel;
using System.Reflection;
using BetterInventory.BetterBestiary;
using BetterInventory.BetterInventoryManagement;
using BetterInventory.BetterItemInformationDisplay;
using BetterInventory.BetterItemPickup;
using BetterInventory.BetterMenuNavigation;
using BetterInventory.BetterRecipeList;
using BetterInventory.BetterTooltips;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Newtonsoft.Json;
using SpikysLib;
using SpikysLib.Collections;
using SpikysLib.Configs;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;

namespace BetterInventory;

public sealed class BetterInventoryConfig : ModConfig {
    public override bool Autoload(ref string name) => Instance is null; // Could have been loaded earlier due to other configs
    public static bool EnsureLoaded(ModConfig module) {
        if (Instance is not null) return true;
        Type type = typeof(BetterInventoryConfig);
        var mod = module.Mod;
        // From `Mod.AutoloadConfig()`
        ModConfig mc = (ModConfig)Activator.CreateInstance(type, nonPublic: true)!;
        if (mc.Mode == ConfigScope.ServerSide && (mod.Side == ModSide.Client || mod.Side == ModSide.NoSync)) {
            throw new Exception("The ModConfig " + mc.Name + " can't be loaded because the config is ServerSide but this Mods ModSide isn't Both or Server");
        }
        if (mc.Mode == ConfigScope.ClientSide && mod.Side == ModSide.Server) {
            throw new Exception("The ModConfig " + mc.Name + " can't be loaded because the config is ClientSide but this Mods ModSide is Server");
        }
        // mc.Mod = mod;
        mod.AddConfig(type.Name, mc);
        return true;
    }
    public override void OnLoaded() => MonoModHooks.Modify(TypeHelper.GetMethod((ConfigElement i) => i.DrawSelf), FallibleAttribute.ILFailedEdit);

    [Fallible, ReloadRequired, DefaultValue(true)] public bool betterTooltips = true;
    [Fallible, ReloadRequired, DefaultValue(true)] public bool betterBestiary = true;
    [Fallible, ReloadRequired, DefaultValue(true)] public bool betterRecipeList = true;
    [Fallible, ReloadRequired, DefaultValue(true)] public bool betterItemInformationDisplay = true;
    [Fallible, ReloadRequired, DefaultValue(true)] public bool betterItemPickup = true;
    [Fallible, ReloadRequired, DefaultValue(true)] public bool betterInventoryManagement = true;
    [Fallible, ReloadRequired, DefaultValue(true)] public bool betterMenuNavigation = true;

    [Expand(false), JsonIgnore, ShowDespiteJsonIgnore] public FailedBetterInventoryConfig failedBetterInventory = new();

    public static BetterInventoryConfig Instance = null!;
    public static bool BetterTooltips => Instance.betterTooltips;
    public static bool BetterBestiary => Instance.betterBestiary;
    public static bool BetterRecipeList => Instance.betterRecipeList;
    public static bool BetterItemInformationDisplay => Instance.betterItemInformationDisplay;
    public static bool BetterItemPickup => Instance.betterItemPickup;
    public static bool BetterInventoryManagement => Instance.betterInventoryManagement;
    public static bool BetterMenuNavigation => Instance.betterMenuNavigation;

    public sealed override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class FailedBetterInventoryConfig {
    public FailedBetterTooltipsConfig betterTooltips = new();
    public FailedBetterBestiaryConfig betterBestiary = new();
    public FailedBetterRecipeListConfig betterRecipeList = new();
    public FailedBetterItemInformationDisplayConfig betterItemInformationDisplay = new();
    public FailedBetterItemPickupConfig betterItemPickup = new();
    public FailedBetterInventoryManagementConfig betterInventoryManagement = new();
    public FailedBetterMenuNavigationConfig betterMenuNavigation = new();

    public static FailedBetterInventoryConfig Instance => BetterInventoryConfig.Instance.failedBetterInventory;
}

public enum FailedState {
    NotFailed,
    PartiallyFailed,
    Failed,
}

[AttributeUsage(AttributeTargets.Field)]
public class FallibleAttribute : Attribute {
    public FallibleAttribute() { }
    public FallibleAttribute(Type type, string field) {
        Type = type; Field = field;
        State = GetFailedState(Type, Field);
    }

    public Type? Type { get; }
    public string? Field { get; }
    public FailedState? State { get; }

    public static FailedState GetFailedState(PropertyFieldWrapper memberInfo) => GetFailedState(Type.GetType($"{memberInfo.MemberInfo.DeclaringType!.Namespace}.Failed{memberInfo.MemberInfo.DeclaringType.Name}")!, memberInfo.Name);
    public static FailedState GetFailedState(Type type, string field) => GetFailedState(type.Retrieve("Instance")!.Retrieve(field)!);
    public static FailedState GetFailedState(object value) => value switch {
        bool b => b ? FailedState.Failed : FailedState.NotFailed,
        IKeyValuePair kvp => (bool)kvp.Key! ? FailedState.Failed : GetFailedState(kvp.Value!),
        _ => value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Exist(f => GetFailedState(f.GetValue(value)!) != FailedState.NotFailed) ? FailedState.PartiallyFailed : FailedState.NotFailed
    };

    internal static void ILFailedEdit(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(i => i.MatchGetppt((ConfigElement i) => i.TextDisplayFunction));
        cursor.GotoNextLoc(MoveType.Before, out int label, i => true, 7);
        cursor.EmitLdarg0();
        cursor.EmitDelegate((string label, ConfigElement self) => {
            var fallible = ConfigManager.GetCustomAttributeFromMemberThenMemberType<FallibleAttribute>(self.MemberInfo, self.Item, self.List);
            if (fallible is null) return label;
            var state = fallible.State ?? GetFailedState(self.MemberInfo);
            if (state == FailedState.NotFailed) return label;
            if (state == FailedState.PartiallyFailed) return label + " - [c/" + Color.Orange.Hex3() + ":" + "Partially Loaded" + "]";
            return label + " - [c/FF0000:" + "Failed to Load" + "]";
        });

        cursor.GotoNext(i => i.MatchGetppt((ConfigElement i) => i.TooltipFunction));
        cursor.GotoNextLoc(MoveType.Before, out int tooltip, i => true, 8);
        cursor.EmitLdarg0();
        cursor.EmitDelegate((string tooltip, ConfigElement self) => {
            var fallible = ConfigManager.GetCustomAttributeFromMemberThenMemberType<FallibleAttribute>(self.MemberInfo, self.Item, self.List);
            if (fallible is null) return tooltip;
            var state = fallible.State ?? GetFailedState(self.MemberInfo);
            if (state == FailedState.NotFailed) return tooltip;
            if (!string.IsNullOrEmpty(tooltip)) tooltip += "\n";
            if (state == FailedState.PartiallyFailed) return tooltip + "[c/" + Color.Orange.Hex3() + ":" + "Parts of this element failed to load and will be disabled in game" + "]";
            return tooltip + "[c/FF0000:" + "This element failed to load and will be disabled in game" + "]";
        });
    }
}
