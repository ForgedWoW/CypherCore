// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum DamageEffectType
{
    Direct = 0,      // used for normal weapon damage (not for class abilities or spells)
    SpellDirect = 1, // spell/class abilities damage
    DOT = 2,
    Heal = 3,
    NoDamage = 4, // used also in case when damage applied to health but not applied to spell channelInterruptFlags/etc
    Self = 5
}