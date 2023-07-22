// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PowerType : sbyte
{
    Mana = 0,
    Rage = 1,
    Focus = 2,
    Energy = 3,
    ComboPoints = 4,
    Runes = 5,
    RunicPower = 6,
    SoulShards = 7,
    LunarPower = 8,
    HolyPower = 9,
    AlternatePower = 10, // Used in some quests
    Maelstrom = 11,
    Chi = 12,
    Insanity = 13,
    BurningEmbers = 14,
    DemonicFury = 15,
    ArcaneCharges = 16,
    Fury = 17,
    Pain = 18,
    Essence = 19,
    RuneBlood = 20,
    RuneFrost = 21,
    RuneUnholy = 22,
    AlternateQuest = 23,
    AlternateEncounter = 24,
    AlternateMount = 25,
    Max = 26,

    All = 127,   // default for class?
    Health = -2, // (-2 as signed value)
    MaxPerClass = 10
}