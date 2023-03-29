// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PetTameResult
{
    Ok = 0,
    InvalidCreature = 1,
    TooMany = 2,
    CreatureAlreadyOwned = 3,
    NotTameable = 4,
    AnotherSummonActive = 5,
    UnitsCantTame = 6,
    NoPetAvailable = 7,
    InternalError = 8,
    TooHighLevel = 9,
    Dead = 10,
    NotDead = 11,
    CantControlExotic = 12,
    InvalidSlot = 13,
    EliteTooHighLevel = 14
}