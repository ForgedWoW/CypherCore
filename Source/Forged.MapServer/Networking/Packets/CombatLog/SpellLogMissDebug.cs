﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal struct SpellLogMissDebug
{
    public float HitRoll;

    public float HitRollNeeded;

    public void Write(WorldPacket data)
    {
        data.WriteFloat(HitRoll);
        data.WriteFloat(HitRollNeeded);
    }
}