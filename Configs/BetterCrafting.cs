using System;
using System.ComponentModel;

namespace BetterInventory.Configs;


public class BetterCrafting {

    [DefaultValue(RecListScroll.FastFocus)] public RecListScroll customScroll = RecListScroll.Fast;
    [DefaultValue(true)] public bool craftingOverrides = true;
    [DefaultValue(true)] public bool craftingOnRecList = true;


    [Flags] // TODO redo
    public enum RecListScroll { Vanilla = 0b00, Fast = 0b01, Focus = 0b10, FastFocus = 0b11 }
}