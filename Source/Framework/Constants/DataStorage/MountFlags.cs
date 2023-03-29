// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum MountFlags : ushort
{
    SelfMount = 0x02, // Player becomes the mount himself
    FactionSpecific = 0x04,
    PreferredSwimming = 0x10,
    PreferredWaterWalking = 0x20,
    HideIfUnknown = 0x40
}