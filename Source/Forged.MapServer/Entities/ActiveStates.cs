// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities;

public enum ActiveStates
{
    Passive = 0x01,  // 0x01 - passive
    Disabled = 0x81, // 0x80 - castable
    Enabled = 0xC1,  // 0x40 | 0x80 - auto cast + castable
    Command = 0x07,  // 0x01 | 0x02 | 0x04
    Reaction = 0x06, // 0x02 | 0x04
    Decide = 0x00    // custom
}