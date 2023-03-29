// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ItemSubClassWeapon
{
    Axe = 0,  // One-Handed Axes
    Axe2 = 1, // Two-Handed Axes
    Bow = 2,
    Gun = 3,
    Mace = 4,  // One-Handed Maces
    Mace2 = 5, // Two-Handed Maces
    Polearm = 6,
    Sword = 7,  // One-Handed Swords
    Sword2 = 8, // Two-Handed Swords
    Warglaives = 9,
    Staff = 10,
    Exotic = 11,  // One-Handed Exotics
    Exotic2 = 12, // Two-Handed Exotics
    Fist = 13,
    Miscellaneous = 14,
    Dagger = 15,
    Thrown = 16,
    Spear = 17,
    Crossbow = 18,
    Wand = 19,
    FishingPole = 20,

    MaskRanged = (1 << Bow) | (1 << Gun) | (1 << Crossbow),

    Max = 21
}