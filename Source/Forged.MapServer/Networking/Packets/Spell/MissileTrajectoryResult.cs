// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Spell;

public struct MissileTrajectoryResult
{
    public float Pitch;
    public uint TravelTime;
    public void Write(WorldPacket data)
    {
        data.WriteUInt32(TravelTime);
        data.WriteFloat(Pitch);
    }
}