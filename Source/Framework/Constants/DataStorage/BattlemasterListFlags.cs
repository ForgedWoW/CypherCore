// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum BattlemasterListFlags : int
{
    Disabled = 0x01,
    SkipRoleCheck = 0x02,
    Unk4 = 0x04,
    CanInitWarGame = 0x08,
    CanSpecificQueue = 0x10,
    Brawl = 0x20,
    Factional = 0x40
}