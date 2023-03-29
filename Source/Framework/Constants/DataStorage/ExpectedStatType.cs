// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ExpectedStatType : byte
{
    CreatureHealth = 0,
    PlayerHealth = 1,
    CreatureAutoAttackDps = 2,
    CreatureArmor = 3,
    PlayerMana = 4,
    PlayerPrimaryStat = 5,
    PlayerSecondaryStat = 6,
    ArmorConstant = 7,
    None = 8,
    CreatureSpellDamage = 9
}