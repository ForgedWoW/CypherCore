// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ItemEnchantmentType : byte
{
    None = 0,
    CombatSpell = 1,
    Damage = 2,
    EquipSpell = 3,
    Resistance = 4,
    Stat = 5,
    Totem = 6,
    UseSpell = 7,
    PrismaticSocket = 8,
    ArtifactPowerBonusRankByType = 9,
    ArtifactPowerBonusRankByID = 10,
    BonusListID = 11,
    BonusListCurve = 12,
    ArtifactPowerBonusRankPicker = 13
}