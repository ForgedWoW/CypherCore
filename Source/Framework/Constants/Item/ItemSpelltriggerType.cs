// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ItemSpelltriggerType : sbyte
{
    OnUse = 0, // use after equip cooldown
    OnEquip = 1,
    OnProc = 2,
    SummonedBySpell = 3,
    OnDeath = 4,
    OnPickup = 5,
    OnLearn = 6, // used in itemtemplate.spell2 with spellid with SPELLGENERICLEARN in spell1
    OnLooted = 7,
    Max
}