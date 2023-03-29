// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ItemClass : sbyte
{
    None = -1,
    Consumable = 0,
    Container = 1,
    Weapon = 2,
    Gem = 3,
    Armor = 4,
    Reagent = 5,
    Projectile = 6,
    TradeGoods = 7,
    ItemEnhancement = 8,
    Recipe = 9,
    Money = 10, // Obsolete
    Quiver = 11,
    Quest = 12,
    Key = 13,
    Permanent = 14, // Obsolete
    Miscellaneous = 15,
    Glyph = 16,
    BattlePets = 17,
    WowToken = 18,
    Profession = 19,
    Max
}