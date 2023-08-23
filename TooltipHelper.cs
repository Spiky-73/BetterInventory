using System.Collections.Generic;

using Terraria.ModLoader;

namespace BetterInventory;

public enum TooltipLineID {
    ItemName,
    Favorite,
    FavoriteDesc,
    NoTransfer,
    Social,
    SocialDesc,
    Damage,
    CritChance,
    Speed,
    Knockback,
    FishingPower,
    NeedsBait,
    BaitPower,
    Equipable,
    WandConsumes,
    Quest,
    Vanity,
    VanityLegal,
    Defense,
    PickPower,
    AxePower,
    HammerPower,
    TileBoost,
    HealLife,
    HealMana,
    UseMana,
    Placeable,
    Ammo,
    Consumable,
    Material,
    Tooltip,
    EtherianManaWarning,
    WellFedExpert,
    BuffTime,
    OneDropLogo,
    PrefixDamage,
    PrefixSpeed,
    PrefixCritChance,
    PrefixUseMana,
    PrefixSize,
    PrefixShootSpeed,
    PrefixKnockback,
    PrefixAccDefense,
    PrefixAccMaxMana,
    PrefixAccCritChance,
    PrefixAccDamage,
    PrefixAccMoveSpeed,
    PrefixAccMeleeSpeed,
    SetBonus,
    Expert,
    Master,
    JourneyResearch,
    BestiaryNotes,
    SpecialPrice,
    Price,
    Modded
}

public static class TooltipHelper {

    public static TooltipLine? FindLine(this List<TooltipLine> tooltips, string name) => tooltips.Find(l => l.Name == name);
    public static TooltipLine AddLine(this List<TooltipLine> tooltips, TooltipLine line, TooltipLineID after) {
        for (int i = 0; i < tooltips.Count; i++) {
            if (tooltips[i].Name == line.Name) return tooltips[i];
            TooltipLineID lookingAt;
            if (tooltips[i].Name.StartsWith("Tooltip")) lookingAt = TooltipLineID.Tooltip;
            else if (!System.Enum.TryParse(tooltips[i].Name, out lookingAt)) lookingAt = TooltipLineID.Modded;

            if (lookingAt <= after) continue;
            tooltips.Insert(i, line);
            return line;
        }
        tooltips.Add(line);
        return line;
    }

    public static TooltipLine FindorAddLine(this List<TooltipLine> tooltips, TooltipLine line, TooltipLineID after, out bool addedLine) {
        TooltipLine? target = tooltips.FindLine(line.Name);
        if (addedLine = target is null) target = tooltips.AddLine(line, after);
        return target!;
    }
    public static TooltipLine FindorAddLine(this List<TooltipLine> tooltips, TooltipLine line, TooltipLineID after = TooltipLineID.Modded) => FindorAddLine(tooltips, line, after, out _);
}
