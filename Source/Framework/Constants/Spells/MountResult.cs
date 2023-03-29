// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum MountResult
{
    InvalidMountee = 0,
    TooFarAway = 1,
    AlreadyMounted = 2,
    NotMountable = 3,
    NotYourPet = 4,
    Other = 5,
    Looting = 6,
    RaceCantMount = 7,
    Shapeshifted = 8,
    ForcedDismount = 9,
    Ok = 10 // never sent
}