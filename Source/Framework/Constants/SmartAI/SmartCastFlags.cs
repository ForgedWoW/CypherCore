// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SmartCastFlags
{
    InterruptPrevious = 0x01, //Interrupt any spell casting
    Triggered = 0x02,         //Triggered (this makes spell cost zero mana and have no cast time)

    //CAST_FORCE_CAST             = 0x04,                     //Forces cast even if creature is out of mana or out of range
    //CAST_NO_MELEE_IF_OOM        = 0x08,                     //Prevents creature from entering melee if out of mana or out of range
    //CAST_FORCE_TARGET_SELF      = 0x10,                     //Forces the target to cast this spell on itself
    AuraNotPresent = 0x20, //Only casts the spell if the target does not have an aura from the spell
    CombatMove = 0x40      //Prevents combat movement if cast successful. Allows movement on range, OOM, LOS
}