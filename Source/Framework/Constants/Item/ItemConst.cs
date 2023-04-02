// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public struct ItemConst
{
    public const int MaxBagSize = 36;
    public const int MaxDamages = 2; // changed in 3.1.0
    public const int MaxEquipmentSetIndex = 20;
    public const int MaxGemSockets = 3;
    public const int MaxItemEnchantmentEffects = 3;
    public const int MaxItemExtCostCurrencies = 5;
    public const int MaxItemExtCostItems = 5;
    public const int MaxItemSetItems = 17;
    public const int MaxItemSetSpells = 8;
    public const int MaxItemSubclassTotal = 21;
    public const int MaxOutfitItems = 24;
    public const int MaxProtoSpells = 5;
    public const int MaxSpells = 5;
    public const int MaxStats = 10;
    public const byte NullBag = 0;
    public const byte NullSlot = 255;

    public static ItemModifier[] AppearanceModifierSlotBySpec =
    {
        ItemModifier.TransmogAppearanceSpec1, ItemModifier.TransmogAppearanceSpec2, ItemModifier.TransmogAppearanceSpec3, ItemModifier.TransmogAppearanceSpec4, ItemModifier.TransmogAppearanceSpec5
    };

    public static ItemModifier[] IllusionModifierSlotBySpec =
    {
        ItemModifier.EnchantIllusionSpec1, ItemModifier.EnchantIllusionSpec2, ItemModifier.EnchantIllusionSpec3, ItemModifier.EnchantIllusionSpec4, ItemModifier.EnchantIllusionSpec5
    };

    public static uint[] ItemQualityColors =
    {
        0xff9d9d9d, // GREY
        0xffffffff, // WHITE
        0xff1eff00, // GREEN
        0xff0070dd, // BLUE
        0xffa335ee, // PURPLE
        0xffff8000, // ORANGE
        0xffe6cc80, // LIGHT YELLOW
        0xffe6cc80  // LIGHT YELLOW
    };

    public static ItemModifier[] SecondaryAppearanceModifierSlotBySpec =
    {
        ItemModifier.TransmogSecondaryAppearanceSpec1, ItemModifier.TransmogSecondaryAppearanceSpec2, ItemModifier.TransmogSecondaryAppearanceSpec3, ItemModifier.TransmogSecondaryAppearanceSpec4, ItemModifier.TransmogSecondaryAppearanceSpec5
    };

    public static SocketColor[] SocketColorToGemTypeMask =
    {
        0, SocketColor.Meta, SocketColor.Red, SocketColor.Yellow, SocketColor.Blue, SocketColor.Hydraulic, SocketColor.Cogwheel, SocketColor.Prismatic, SocketColor.RelicIron, SocketColor.RelicBlood, SocketColor.RelicShadow, SocketColor.RelicFel, SocketColor.RelicArcane, SocketColor.RelicFrost, SocketColor.RelicFire, SocketColor.RelicWater, SocketColor.RelicLife, SocketColor.RelicWind, SocketColor.RelicHoly
    };
}