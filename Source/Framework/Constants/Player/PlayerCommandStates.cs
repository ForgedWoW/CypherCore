// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PlayerCommandStates
{
    None = 0x00,
    God = 0x01,
    Casttime = 0x02,
    Cooldown = 0x04,
    Power = 0x08,
    Waterwalk = 0x10
}