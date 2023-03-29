// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum LfgRoles
{
    None = 0x00,
    Leader = 0x01,
    Tank = 0x02,
    Healer = 0x04,
    Damage = 0x08,
    Any = Leader | Tank | Healer | Damage
}