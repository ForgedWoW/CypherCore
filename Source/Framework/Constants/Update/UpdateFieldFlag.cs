// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum UpdateFieldFlag
{
    None = 0,
    Owner = 0x01,
    PartyMember = 0x02,
    UnitAll = 0x04,
    Empath = 0x08
}