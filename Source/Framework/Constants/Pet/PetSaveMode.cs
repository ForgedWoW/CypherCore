// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PetSaveMode
{
    AsDeleted = -2, // not saved in fact
    AsCurrent = -3, // in current slot (with player)
    FirstActiveSlot = 0,
    LastActiveSlot = FirstActiveSlot + SharedConst.MaxActivePets,
    FirstStableSlot = 5,
    LastStableSlot = FirstStableSlot + SharedConst.MaxPetStables, // last in DB stable slot index
    NotInSlot = -1,                                               // for avoid conflict with stable size grow will use negative value
}