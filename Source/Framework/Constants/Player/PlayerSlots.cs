// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PlayerSlots
{
    // first slot for item stored (in any way in player items data)
    Start = 0,

    // last+1 slot for item stored (in any way in player items data)
    End = 218,
    Count = (End - Start)
}