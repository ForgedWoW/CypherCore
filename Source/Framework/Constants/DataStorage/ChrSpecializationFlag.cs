// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ChrSpecializationFlag
{
    Caster = 0x01,
    Ranged = 0x02,
    Melee = 0x04,
    Unknown = 0x08,
    DualWieldTwoHanded = 0x10, // Used For Cunitdisplay::Setsheatheinvertedfordualwield
    PetOverrideSpec = 0x20,
    Recommended = 0x40,
}